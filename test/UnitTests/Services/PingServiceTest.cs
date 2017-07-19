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

namespace StatusMonitor.Tests.UnitTests.Services
{
	public class PingServiceTest
	{

		[Theory]
		[InlineData(HttpResponseOption.Success)]
		[InlineData(HttpResponseOption.Timeout)]
		[InlineData(HttpResponseOption.ServiceUnavailable)]
		/// <summary>
		/// Checks that the service correctly determines status code and response time.
		/// </summary>
		/// <param name="option">Option describing the expected behavior</param>
		public async Task GeneratesCorrectDataPoint(HttpResponseOption option)
		{
			// Arrange
			var responseHandler = new ResponseHandler();
			responseHandler.AddHandler(
				new Uri("https://my.url.com"),
				option
			);

			var mockHttpFactory = new Mock<IHttpClientFactory>();
			mockHttpFactory
				.Setup(factory => factory.BuildClient())
				.Returns(new HttpClient(responseHandler));


			var metricServiceMock = new Mock<IMetricService>();
			metricServiceMock
				.Setup(mock => mock.GetOrCreateMetricAsync(Metrics.Ping, "https://my.url.com"))
				.ReturnsAsync(new Metric());

			var pingService = new PingService(
				metricServiceMock.Object,
				new Mock<ILogger<PingService>>().Object,
				mockHttpFactory.Object
			);

			// Act
			var dataPoint = await pingService.PingServerAsync(
				new PingSetting { 
					ServerUrl = "https://my.url.com",
					MaxResponseTime = new TimeSpan(0,0,0,0,500)
				}
			);

			// Assert
			switch (option)
			{
				case HttpResponseOption.Success:
					Assert.True(dataPoint.Success);
					Assert.True(dataPoint.ResponseTime < new TimeSpan(0, 0, 0, 0, 2000));
					break;
				case HttpResponseOption.Timeout:
					Assert.False(dataPoint.Success);
					Assert.Equal(new TimeSpan(0), dataPoint.ResponseTime);
					break;
				case HttpResponseOption.ServiceUnavailable:
					Assert.False(dataPoint.Success);
					Assert.True(dataPoint.ResponseTime < new TimeSpan(0, 0, 0, 0, 2000));
					break;
			}

			// Clean up
			responseHandler.RemoveHandler(
				new Uri("https://my.url.com")
			);
		}
	}
}
