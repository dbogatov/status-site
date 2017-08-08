using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using StatusMonitor.Web.Controllers.View;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Shared.Models.Entities;
using StatusMonitor.Shared.Services;
using Xunit;
using Microsoft.AspNetCore.Http;
using Moq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StatusMonitor.Shared.Models;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace StatusMonitor.Tests.ControllerTests
{
	public partial class HomeControllerTest
	{
		[Fact]
		public async Task MetricExists()
		{
			// Arrange
			_mockMetricService
				.Setup(mock => mock.GetMetricsAsync(It.IsAny<Metrics>(), It.IsAny<string>()))
				.ReturnsAsync(
					new List<Metric> {
						new Metric {
							CurrentValue = 50,
							Public = true
						}
					}
				);

			// Act
			var result = await _controller.Metric(Metrics.CpuLoad.ToString(), "existing-source");

			// Assert
			var viewResult = Assert.IsType<ViewResult>(result);

			var model = Assert.IsAssignableFrom<Metric>(
				viewResult.ViewData.Model
			);

			Assert.Equal(50, model.CurrentValue);
		}

		[Fact]
		public async Task MetricNotExists()
		{
			// Arrange
			_mockMetricService
				.Setup(mock => mock.GetMetricsAsync(It.IsAny<Metrics>(), It.IsAny<string>()))
				.ReturnsAsync(new List<Metric>());

			// Act
			var result = await _controller.Metric(Metrics.CpuLoad.ToString(), "non-existing-source");

			// Assert
			var notFoundResult = Assert.IsType<NotFoundResult>(result);
		}

		[Fact]
		public async Task MetricUnauthorized()
		{
			// Arrange
			_mockMetricService
				.Setup(mock => mock.GetMetricsAsync(It.IsAny<Metrics>(), It.IsAny<string>()))
				.ReturnsAsync(new List<Metric> { new Metric { Public = false } });

			_mockAuth
				.Setup(auth => auth.IsAuthenticated())
				.Returns(false);

			// Act
			var result = await _controller.Metric(Metrics.CpuLoad.ToString(), "existing-source");

			// Assert
			var unauthorizedResult = Assert.IsType<UnauthorizedResult>(result);
		}

		[Fact]
		public async Task MetricBadRequest()
		{
			// Act
			var result = await _controller.Metric("bad-type", "any-source");

			// Assert
			var badRequestObjectResult = Assert.IsType<BadRequestObjectResult>(result);

			Assert.Contains("type", (string)badRequestObjectResult.Value);
		}

		[Fact]
		public async Task MetricStartDateRequest()
		{
			// Act
			var result = await _controller.Metric(Metrics.CpuLoad.ToString(), "any-source", "invalid-date");

			// Assert
			var badRequestObjectResult = Assert.IsType<BadRequestObjectResult>(result);

			Assert.Contains("start", (string)badRequestObjectResult.Value);
		}

		[Fact]
		public async Task MetricEndDateRequest()
		{
			// Act
			var result = await _controller.Metric(
				Metrics.CpuLoad.ToString(),
				"any-source",
				DateTime.UtcNow.TotalMilliseconds().ToString(),
				"invalid-date"
			);

			// Assert
			var badRequestObjectResult = Assert.IsType<BadRequestObjectResult>(result);

			Assert.Contains("end", (string)badRequestObjectResult.Value);
		}

		[Fact]
		public async Task MetricStartAfterEndDateRequest()
		{
			// Act
			var result = await _controller.Metric(
				Metrics.CpuLoad.ToString(),
				"any-source",
				DateTime.UtcNow.TotalMilliseconds().ToString(),
				DateTime.UtcNow.AddHours(-1).TotalMilliseconds().ToString()
			);

			// Assert
			var badRequestObjectResult = Assert.IsType<BadRequestObjectResult>(result);

			Assert.Contains("greater than", (string)badRequestObjectResult.Value);
		}

		[Fact]
		public async Task MetricDatesOK()
		{
			// Arrange
			_mockMetricService
				.Setup(mock => mock.GetMetricsAsync(It.IsAny<Metrics>(), It.IsAny<string>()))
				.ReturnsAsync(
					new List<Metric> {
						new Metric {
							CurrentValue = 50,
							Public = true
						}
					}
				);

			var start = DateTime.UtcNow.AddHours(-1);
			var end = DateTime.UtcNow;

			// Act
			var result = await _controller.Metric(
				Metrics.CpuLoad.ToString(), 
				"existing-source",
				start.TotalMilliseconds().ToString(),
				end.TotalMilliseconds().ToString()
			);

			// Assert
			var viewResult = Assert.IsType<ViewResult>(result);

			var model = Assert.IsAssignableFrom<Metric>(
				viewResult.ViewData.Model
			);

			Assert.Equal(start.TotalMilliseconds().ToString(), viewResult.ViewData["Start"]);
			Assert.Equal(end.TotalMilliseconds().ToString(), viewResult.ViewData["End"]);
		}

		[Fact]
		public async Task MetricDatesEndNullOK()
		{
			// Arrange
			_mockMetricService
				.Setup(mock => mock.GetMetricsAsync(It.IsAny<Metrics>(), It.IsAny<string>()))
				.ReturnsAsync(
					new List<Metric> {
						new Metric {
							CurrentValue = 50,
							Public = true
						}
					}
				);

			var start = DateTime.UtcNow;

			// Act
			var result = await _controller.Metric(
				Metrics.CpuLoad.ToString(), 
				"existing-source",
				start.TotalMilliseconds().ToString()
			);

			// Assert
			var viewResult = Assert.IsType<ViewResult>(result);

			var model = Assert.IsAssignableFrom<Metric>(
				viewResult.ViewData.Model
			);

			Assert.Equal(start.TotalMilliseconds().ToString(), viewResult.ViewData["Start"]);
			Assert.Equal(0.ToString(), viewResult.ViewData["End"]);
		}
	}
}
