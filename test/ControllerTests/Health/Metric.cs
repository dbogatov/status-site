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
using StatusMonitor.Web.Services;
using System.Linq;

namespace StatusMonitor.Tests.ControllerTests
{
	public partial class HealthControllerTest
	{
		[Fact]
		public async Task MetricOK()
		{
			// Arrange
			_mockMetricService
				.Setup(m => m.GetMetricsAsync(Metrics.CpuLoad, "the-source"))
				.ReturnsAsync(
					new List<Metric> {
						new Metric
						{
							Type = Metrics.CpuLoad.AsInt(),
							Source = "the-source",
							AutoLabel = new AutoLabel { Id = AutoLabels.Normal.AsInt() },
							Public = true
						}
					}
				);

			_mockBadge
				.Setup(mock => mock.GetMetricHealthBadge("the-source", Metrics.CpuLoad, AutoLabels.Normal))
				.Returns(
					new Badge
					{
						Title = $"{Metrics.CpuLoad.ToString()} of the-source",
						Message = AutoLabels.Normal.ToString(),
						Status = BadgeStatus.Success
					}
				);

			// Act
			var result = await _controller.Metric(Metrics.CpuLoad.ToString(), "the-source");

			// Assert
			var badgeResult = Assert.IsType<BadgeResult>(result);

			var model = Assert.IsAssignableFrom<Badge>(
				badgeResult.Badge
			);

			Assert.Contains("the-source", model.Title.ToLower());
			Assert.Contains(Metrics.CpuLoad.ToString().ToLower(), model.Title.ToLower());
			Assert.Contains(AutoLabels.Normal.ToString().ToLower(), model.Message.ToLower());
			Assert.Equal(BadgeStatus.Success, model.Status);
		}

		[Fact]
		public async Task MetricNotFound()
		{
			// Arrange
			_mockMetricService
				.Setup(m => m.GetMetricsAsync(It.IsAny<Metrics>(), It.IsAny<string>()))
				.ReturnsAsync(new List<Metric>());

			// Act
			var result = await _controller.Metric(Metrics.CpuLoad.ToString(), "the-source");

			// Assert
			Assert.IsType<NotFoundResult>(result);
		}

		[Fact]
		public async Task MetricUnauthorized()
		{
			// Arrange
			_mockMetricService
				.Setup(m => m.GetMetricsAsync(Metrics.CpuLoad, "the-source"))
				.ReturnsAsync(
					new List<Metric> {
						new Metric
						{
							Type = Metrics.CpuLoad.AsInt(),
							Source = "the-source",
							AutoLabel = new AutoLabel { Id = AutoLabels.Normal.AsInt() },
							Public = false
						}
					}
				);

			_mockAuth
				.Setup(auth => auth.IsAuthenticated())
				.Returns(false);


			// Act
			var result = await _controller.Metric(Metrics.CpuLoad.ToString(), "the-source");

			// Assert
			Assert.IsType<UnauthorizedResult>(result);
		}

		[Fact]
		public async Task MetricBadRequest()
		{
			// Act
			var result = await _controller.Metric("invalid-type", "the-source");

			// Assert
			Assert.IsType<BadRequestObjectResult>(result);
		}
	}
}
