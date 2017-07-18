using System;
using Xunit;
using StatusMonitor.Shared.Models.Entities;
using StatusMonitor.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using StatusMonitor.Shared.Extensions;
using System.Net;
using StatusMonitor.Daemons.Services;
using Moq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StatusMonitor.Shared.Services.Factories;
using System.Net.Http;
using StatusMonitor.Tests.Mock;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace StatusMonitor.Tests.UnitTests.Services
{
	public class RemotePingServiceTest
	{
		[Theory]
		[InlineData(HttpResponseOption.Success)]
		[InlineData(HttpResponseOption.ServiceUnavailable)]
		public async Task GeneratesCorrectDataPoint(HttpResponseOption option)
		{
			// Arrange
			var responseHandler = new ResponseHandler();
			responseHandler.AddAction(
				new Uri("http://ping.server"),
				() =>
					option == HttpResponseOption.Success ?
					JsonConvert.SerializeObject(
						new RemotePingServerResponse
						{
							Latency = 100,
							StatusCode = 200,
							IsError = false
						}
					) :
					JsonConvert.SerializeObject(
						new RemotePingServerResponse
						{
							StatusCode = 503,
							IsError = true,
							Error = "Some error"
						}
					)
			);

			var mockHttpFactory = new Mock<IHttpClientFactory>();
			mockHttpFactory
				.Setup(factory => factory.BuildClient())
				.Returns(new HttpClient(responseHandler));


			var metricServiceMock = new Mock<IMetricService>();
			metricServiceMock
				.Setup(mock => mock.GetOrCreateMetricAsync(Metrics.Ping, "https://my.url.com"))
				.ReturnsAsync(new Metric());

			var config = new Mock<IConfiguration>();
			config
				.SetupGet(conf => conf["Data:PingServerUrl"])
				.Returns("http://ping.server");

			var pingService = new RemotePingService(
				metricServiceMock.Object,
				new Mock<ILogger<PingService>>().Object,
				config.Object,
				mockHttpFactory.Object
			);

			// Act
			var dataPoint = await pingService.PingServerAsync(
				new PingSetting
				{
					ServerUrl = "https://my.url.com",
					MaxResponseTime = new TimeSpan(0, 0, 0, 0, 500)
				}
			);

			// Assert
			switch (option)
			{
				case HttpResponseOption.Success:
					Assert.Equal(HttpStatusCode.OK.AsInt(), dataPoint.HttpStatusCode);
					Assert.Equal(dataPoint.ResponseTime, new TimeSpan(0, 0, 0, 0, 100));
					Assert.Equal("OK", dataPoint.Message);
					break;
				case HttpResponseOption.ServiceUnavailable:
					Assert.Equal(HttpStatusCode.ServiceUnavailable.AsInt(), dataPoint.HttpStatusCode);
					Assert.Equal(dataPoint.ResponseTime, new TimeSpan(0));
					Assert.Equal("Some error", dataPoint.Message);
					break;
			}

			// Clean up
			responseHandler.RemoveAction(
				new Uri("http://ping.server")
			);
		}
	}
}
