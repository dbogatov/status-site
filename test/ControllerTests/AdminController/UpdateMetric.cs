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
using StatusMonitor.Web.ViewModels;

namespace StatusMonitor.Tests.ControllerTests
{
	public partial class AdminControllerTest
	{
		[Fact]
		public async Task UpdateMetricFailure()
		{
			// Arrange
			_mockApiController
				.Setup(api => api.MetricUpdate(It.IsAny<MetricUpdateViewModel>()))
				.ReturnsAsync(new BadRequestResult());

			// Act
			var result = await _controller.UpdateMetric(new MetricUpdateViewModel());

			// Assert
			var badRequestResult = Assert.IsType<BadRequestResult>(result);
		}

		[Fact]
		public async Task UpdateMetricSuccess()
		{
			// Arrange
			_mockApiController
				.Setup(api => api.MetricUpdate(It.IsAny<MetricUpdateViewModel>()))
				.ReturnsAsync(new OkObjectResult("Good"));

			// Act
			var result = await _controller.UpdateMetric(
				new MetricUpdateViewModel
				{
					Source = "the-source",
					MetricType = Metrics.CpuLoad
				}
			);

			// Assert
			var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);

			Assert.Equal("Metric", redirectToActionResult.ActionName);
			Assert.Equal("Home", redirectToActionResult.ControllerName);

			Assert.Equal(Metrics.CpuLoad.ToString(), redirectToActionResult.RouteValues["Type"]);
			Assert.Equal("the-source", redirectToActionResult.RouteValues["Source"]);
		}
	}
}
