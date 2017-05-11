using System;
using Xunit;
using StatusMonitor.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using StatusMonitor.Daemons.Services;
using Moq;
using Microsoft.AspNetCore.Hosting;
using StatusMonitor.Shared.Extensions;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using StatusMonitor.Shared.Models.Entities;
using Microsoft.Extensions.Logging;
using StatusMonitor.Shared.Services;
using Microsoft.Extensions.Logging.Internal;

namespace StatusMonitor.Tests.UnitTests.Services
{
	public class EmailServiceTest
	{
		[Fact]
		public async Task LogsMessageIfEmailIsDisabled()
		{
			// Arrange
			var subject = "The subject";

			var config = new Mock<IConfiguration>();
			config
				.SetupGet(conf => conf["Secrets:Email:Enabled"])
				.Returns(false.ToString());

			var logger = new Mock<ILogger<EmailService>>();

			var email = new EmailService(logger.Object, config.Object);

			// Act
			await email.SendEmailAsync(new string[] { "to@email.com" }, subject, "The message");

			// Assert
			logger
				.Verify(
					log => log.Log(
						LogLevel.Information,
						It.IsAny<EventId>(),
						It.Is<FormattedLogValues>(v => v.ToString().Contains(subject)),
						It.IsAny<Exception>(),
						It.IsAny<Func<object, Exception, string>>()
					)
				);
		}

		[Fact]
		public async Task EmptyRecipients()
		{
			// Arrange
			var email = new EmailService(
				new Mock<ILogger<EmailService>>().Object,
				new Mock<IConfiguration>().Object
			);

			// Act & Assert
			await Assert.ThrowsAsync<ArgumentException>(
				async () => await email.SendEmailAsync(new string[] { }, "The subject", "The message")
			);
		}
	}
}
