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
	public partial class HomeControllerTest
	{
		[Fact]
		public async Task Index()
		{
			// Arrange
			_mockMetricService
				.Setup(mock => mock.GetMetricsAsync(It.IsAny<Metrics>(), It.IsAny<string>()))
				.ReturnsAsync(new List<Metric> { new Metric { Public = true } });

			// Act
			var result = await _controller.Index();

			// Assert
			var viewResult = Assert.IsType<ViewResult>(result);

			var model = Assert.IsAssignableFrom<IEnumerable<Metric>>(
				viewResult.ViewData.Model
			);

			Assert.NotEmpty(model);
		}

		[Fact]
		public async Task IndexNoData()
		{
			// Arrange
			_mockMetricService
				.Setup(mock => mock.GetMetricsAsync(It.IsAny<Metrics>(), It.IsAny<string>()))
				.ReturnsAsync(new List<Metric>());

			// Act
			var result = await _controller.Index();

			// Assert
			var viewResult = Assert.IsType<ViewResult>(result);

			var model = Assert.IsAssignableFrom<IEnumerable<Metric>>(
				viewResult.ViewData.Model
			);

			Assert.Empty(model);
		}

		[Fact]
		public async Task IndexNotAuthenticated()
		{
			// Arrange
			_mockMetricService
				.Setup(mock => mock.GetMetricsAsync(It.IsAny<Metrics>(), It.IsAny<string>()))
				.ReturnsAsync(new List<Metric> {
					new Metric { Public = true },
					new Metric { Public = false }
				});

			_mockAuth
				.Setup(auth => auth.IsAuthenticated())
				.Returns(false);

			// Act
			var result = await _controller.Index();

			// Assert
			var viewResult = Assert.IsType<ViewResult>(result);

			var model = Assert.IsAssignableFrom<IEnumerable<Metric>>(
				viewResult.ViewData.Model
			);

			Assert.Equal(1, model.Count());
		}
	}
}
