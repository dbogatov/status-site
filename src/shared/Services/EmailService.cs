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

namespace StatusMonitor.Shared.Services
{
	/// <summary>
	/// Service used to send email messages
	/// </summary>
	public interface IEmailService
	{
		/// <summary>
		/// Connects to SMTP server and send the email.
		/// </summary>
		/// <param name="to">Array of emails of recipients</param>
		/// <param name="subject">Subject of the message</param>
		/// <param name="message">Body of the massage</param>
		Task SendEmailAsync(string[] to, string subject, string message);
	}

	public class EmailService : IEmailService
	{
		private readonly ILogger<EmailService> _logger;
		private readonly IConfiguration _config;

		public EmailService(
			ILogger<EmailService> logger,
			IConfiguration config
		)
		{
			_logger = logger;
			_config = config;
		}

		public async Task SendEmailAsync(string[] to, string subject, string message)
		{
			if (to.Count() == 0)
			{
				throw new ArgumentException("Empty list of recipients.");
			}

			if (Convert.ToBoolean(_config["Secrets:Email:Enabled"]))
			{
				var emailMessage = new MimeMessage();

				emailMessage.From.Add(
					new MailboxAddress(
						_config["Secrets:Email:FromTitle"],
						_config["Secrets:Email:FromEmail"]
					)
				);

				foreach (var recipient in to)
				{
					emailMessage.To.Add(new MailboxAddress(recipient, recipient));
				}

				emailMessage.Subject = subject;
				emailMessage.Body = new TextPart("plain") { Text = message };

				using (var client = new SmtpClient())
				{
					// TODO: security
					client.ServerCertificateValidationCallback = (
						object s,
						X509Certificate certificate,
						X509Chain chain,
						SslPolicyErrors sslPolicyErrors
					) => true;

					await client.ConnectAsync(
						_config["Secrets:Email:Host"],
						Convert.ToInt32(_config["Secrets:Email:SMTP:Port"]),
						(SecureSocketOptions)Enum.Parse(
							typeof(SecureSocketOptions),
							_config["Secrets:Email:SMTP:Security"]
						)
					).ConfigureAwait(false);

					// Note: since we don't have an OAuth2 token, disable 	
					// the XOAUTH2 authentication mechanism.     
					client.AuthenticationMechanisms.Remove("XOAUTH2");
					
					await client.AuthenticateAsync(
						_config["Secrets:Email:FromEmail"],
						_config["Secrets:Email:Password"]
					);

					await client.SendAsync(emailMessage).ConfigureAwait(false);
					await client.DisconnectAsync(true).ConfigureAwait(false);

					_logger.LogDebug($"Message '{subject}' has been sent to {to.Aggregate((self, next) => $"{next}, {self}")}");
				}
			}
			else
			{
				_logger.LogInformation($"Message '{subject}' was supposed to be  sent to {to.Aggregate((self, next) => $"{next}, {self}")}");
			}
		}
	}
}
