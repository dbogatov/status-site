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
using System.Net.Http;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using StatusMonitor.Shared.Services.Factories;

namespace StatusMonitor.Shared.Services
{
	/// <summary>
	/// Service used to send Slack messages to channels.
	/// </summary>
	public interface ISlackService
	{
		/// <summary>
		/// Sends a slack message to the configured channel
		/// </summary>
		/// <param name="message">Message to send</param>
		Task SendMessageAsync(string message);
	}

	public class SlackService : ISlackService
	{
		private readonly ILogger<SlackService> _logger;
		private readonly IConfiguration _config;
		private readonly IHttpClientFactory _factory;

		public SlackService(
			ILogger<SlackService> logger,
			IConfiguration config,
			IHttpClientFactory factory
		)
		{
			_logger = logger;
			_config = config;
			_factory = factory;
		}

		public async Task SendMessageAsync(string message)
		{
			if (Convert.ToBoolean(_config["Secrets:Slack:Enabled"]))
			{
				using (var client = _factory.BuildClient())
				{
					try
					{
						var response = await client.PostAsync(
							_config["Secrets:Slack:WebHook"],
							new StringContent(
								JsonConvert.SerializeObject(new { text = message }),
								Encoding.UTF8,
								"application/json"
							)
						);

						response.EnsureSuccessStatusCode();

						_logger.LogDebug($"Message '{message.Truncate(20)}' has been sent through Slack");
					}
					catch (System.Exception e)
					{
						_logger.LogError(LoggingEvents.Notifications.AsInt(), e, "Could not send slack message");
					}

				}
			}
			else
			{
				_logger.LogInformation(LoggingEvents.Notifications.AsInt(), $"Message '{message}' was supposed to be sent through Slack");
			}
		}
	}
}
