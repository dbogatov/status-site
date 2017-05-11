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
		public async Task Index()
		{
			// Arrange
			_mockMetricService
				.Setup(mock => mock.GetMetricsAsync(It.IsAny<Metrics?>(), It.IsAny<string>()))
				.ReturnsAsync(new List<Metric> { new Metric() });

			// Act
			var result = await _controller.Index();

			// Assert
			var viewResult = Assert.IsType<ViewResult>(result);

			var metrics = Assert.IsAssignableFrom<IEnumerable<Metric>>(
				viewResult.ViewData["Metrics"]
			);

			Assert.NotEmpty(metrics);
		}
	}
}
