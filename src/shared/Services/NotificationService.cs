using System;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Shared.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using StatusMonitor.Shared.Models.Entities;
using MailKit.Net.Smtp;
using MimeKit;
using MailKit.Security;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("test")]

namespace StatusMonitor.Shared.Services
{
	/// <summary>
	/// Service used to schedule and send notifications
	/// </summary>
	public interface INotificationService
	{
		/// <summary>
		/// Generates a notification with given message of given severity and puts it in a queue
		/// </summary>
		/// <param name="message">Notification body</param>
		/// <param name="severity">Notification severity</param>
		/// <returns>Newly created notification</returns>
		Task<Notification> ScheduleNotificationAsync(string message, NotificationSeverity severity);

		/// <summary>
		/// Traverses queues of notifications and sends them out if necessary
		/// </summary>
		/// <returns>True if notification queue has been flushed and false otherwise</returns>
		Task<bool> ProcessNotificationQueueAsync();
	}

	public class NotificationService : INotificationService
	{
		private readonly ILogger<NotificationService> _logger;
		private readonly IConfiguration _config;
		private readonly IDataContext _context;
		private readonly IEmailService _email;
		private readonly ISlackService _slack;

		public NotificationService(
			ILogger<NotificationService> logger,
			IConfiguration config,
			IDataContext context,
			IEmailService email,
			ISlackService slack
		)
		{
			_logger = logger;
			_config = config;
			_context = context;
			_email = email;
			_slack = slack;
		}

		public async Task<bool> ProcessNotificationQueueAsync()
		{
			var send = Enum
				.GetValues(typeof(NotificationSeverity))
				.Cast<object>()
				.Select(async severity => await CheckIfNeedToSendAsync((NotificationSeverity)severity))
				.Select(task => task.Result)
				.Aggregate((self, next) => self || next);

			if (send && await _context.Notifications.AnyAsync(ntf => !ntf.IsSent))
			{
				var notifications = await _context
					.Notifications
					.Where(ntf => !ntf.IsSent)
					.OrderBy(ntf => ntf.DateCreated)
					.ToListAsync();

				var message = await ComposeMessageAsync(notifications);

				await _email.SendEmailAsync(
					new string[] { _config["Secrets:Email:ToEmail"] },
					"Status site notifications",
					message
				);

				await _slack.SendMessageAsync(message);

				notifications
					.ForEach(ntf =>
					{
						ntf.DateSent = DateTime.UtcNow;
						ntf.IsSent = true;
					});
				await _context.SaveChangesAsync();

				return true;
			}
			else
			{
				return false;
			}
		}

		public async Task<Notification> ScheduleNotificationAsync(string message, NotificationSeverity severity)
		{
			var notification = await _context
				.Notifications
				.AddAsync(new Notification
				{
					Message = message,
					Severity = severity
				});

			await _context.SaveChangesAsync();

			return notification.Entity;
		}

		/// <summary>
		/// Verifies if notifications of given severity need to be sent considering the configuration
		/// </summary>
		/// <param name="severity">Severity for which to check</param>
		/// <returns>True if it is time to send this severity and false otherwise</returns>
		internal async Task<bool> CheckIfNeedToSendAsync(NotificationSeverity severity)
		{
			if (await _context.Notifications.AnyAsync(ntf => ntf.IsSent && ntf.Severity == severity))
			{
				var interval = new TimeSpan(
					0,
					0,
					Convert.ToInt32(_config[$"ServiceManager:NotificationService:Frequencies:{severity.ToString()}"])
				);

				var lastSentDate = (await _context
					.Notifications
					.Where(ntf => ntf.IsSent && ntf.Severity == severity)
					.OrderByDescending(ntf => ntf.DateSent)
					.FirstAsync())
					.DateSent;

				return lastSentDate < DateTime.UtcNow - interval;
			}

			return true;
		}

		internal async Task<string> GenerateUnresolvedDiscrepanciesNoteAsync() {
			var unresolved = 	await _context.Discrepancies.Where(d => !d.Resolved).CountAsync();

			return
				unresolved == 0 ?
				"There are no outstanding issues. Well done." :
				$"There {(unresolved == 1 ? "is" : "are")} still outstanding {unresolved} issue{(unresolved == 1 ? "" : "s")}. See admin panel."
			;

		}

		/// <summary>
		/// Generate a message containing all given notifications grouped by severities
		/// </summary>
		/// <param name="notifications">List of notifications to include in the message</param>
		/// <returns>The composed message</returns>
		internal async Task<string> ComposeMessageAsync(IEnumerable<Notification> notifications) =>
			_config["ServiceManager:NotificationService:Verbosity"] == "normal" ?
				$@"
					Dear recipient,

					Following are the notification messages from Status Site.

					{
						notifications
							.GroupBy(ntf => ntf.Severity)
							.Select(			
								(value, key) => $@"Severity {(NotificationSeverity)value.Key}:{Environment.NewLine}			
									{
										value
											.Select(ntf => $"[{ntf.DateCreated.ToStringUsingTimeZone(_config["ServiceManager:NotificationService:TimeZone"])}] {ntf.Message}")
											.Aggregate((self, next) => $"{self}{Environment.NewLine}{next}")
									}
								"
							)
							.Aggregate((self, next) => $"{self}{Environment.NewLine}{next}")
					}

					{await GenerateUnresolvedDiscrepanciesNoteAsync()}

					Always yours, 
					Notificator
				"
				.Replace("\t", "")
			: 
				$@"
					{
						notifications
							.Select(
								ntf => $@"[{
									ntf.
										DateCreated.
										ToStringUsingTimeZone(
											_config["ServiceManager:NotificationService:TimeZone"]
										)
								}] {ntf.Message}"
							)
							.Aggregate((self, next) => $"{self}{Environment.NewLine}{next}")
					}
					{await GenerateUnresolvedDiscrepanciesNoteAsync()}
					"
					.Replace("\t", "")
			;
	}
}
