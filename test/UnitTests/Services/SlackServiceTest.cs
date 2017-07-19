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
using StatusMonitor.Shared.Services.Factories;
using StatusMonitor.Tests.Mock;
using System.Net.Http;

namespace StatusMonitor.Tests.UnitTests.Services
{
	public class SlackServiceTest
	{
		[Fact]
		public async Task LogsMessageIfSlackIsDisabled()
		{
			// Arrange
			var message = "The message";

			var config = new Mock<IConfiguration>();
			config
				.SetupGet(conf => conf["Secrets:Slack:Enabled"])
				.Returns(false.ToString());

			var logger = new Mock<ILogger<SlackService>>();

			var slack = new SlackService(
				logger.Object,
				config.Object,
				new Mock<IHttpClientFactory>().Object
			);

			// Act
			await slack.SendMessageAsync(message);

			// Assert
			logger
				.Verify(
					log => log.Log(
						LogLevel.Information,
						It.IsAny<EventId>(),
						It.Is<FormattedLogValues>(v => v.ToString().Contains(message)),
						It.IsAny<Exception>(),
						It.IsAny<Func<object, Exception, string>>()
					)
				);
		}

		[Fact]
		public async Task TriggersSlackWebhook()
		{
			// Arrange
			var requestMade = false;
			var uri = new Uri("http://slack.api/web/hook");

			var config = new Mock<IConfiguration>();
			config
				.SetupGet(conf => conf["Secrets:Slack:Enabled"])
				.Returns(true.ToString());
			config
				.SetupGet(conf => conf["Secrets:Slack:WebHook"])
				.Returns(uri.AbsoluteUri);
			
			var responseHandler = new ResponseHandler();
			responseHandler.AddAction(uri, () => (requestMade = true).ToString());
			
			var httpClientFactory = new Mock<IHttpClientFactory>();
			httpClientFactory
				.Setup(factory => factory.BuildClient())
				.Returns(new HttpClient(responseHandler));

			var slack = new SlackService(
				new Mock<ILogger<SlackService>>().Object,
				config.Object,
				httpClientFactory.Object
			);

			// Act 
			await slack.SendMessageAsync("The message");
			
			// Assert
			Assert.True(requestMade);

			// Clean up
			responseHandler.RemoveAction(uri);
		}
	}
}
