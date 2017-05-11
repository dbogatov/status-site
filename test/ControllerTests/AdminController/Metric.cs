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
	public partial class AdminControllerTest
	{
		[Fact]
		public void MetricSuccess()
		{
			// Act
			var result = _controller.Metric($"{Metrics.CpuLoad.ToString()}@the-source");

			// Assert
			var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);

			Assert.Equal("Metric", redirectToActionResult.ActionName);
			Assert.Equal("Home", redirectToActionResult.ControllerName);

			Assert.Equal(Metrics.CpuLoad.ToString(), redirectToActionResult.RouteValues["Type"]);
			Assert.Equal("the-source", redirectToActionResult.RouteValues["Source"]);
		}

		[Fact]
		public void MetricFailure()
		{
			// Act
			var result = _controller.Metric(null);

			// Assert
			var badRequestObjectResult = Assert.IsType<BadRequestObjectResult>(result);
		}
	}
}
