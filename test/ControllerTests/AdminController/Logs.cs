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
		public async Task LogsOK()
		{
			// Arrange
			_mockLoggingService
				.Setup(log => log.GetAvailableFilterDataAsync())
				.ReturnsAsync(new LogMessagesFilterModel
				{
					Severities = new List<LogEntrySeverities> { LogEntrySeverities.Debug }
				});

			// Act
			var result = await _controller.Logs(new LogMessagesFilterViewModel
			{
				Sources = "the-source"
			});

			// Assert
			var viewResult = Assert.IsType<ViewResult>(result);

			var filterViewModel = Assert.IsAssignableFrom<LogMessagesFilterViewModel>(
				viewResult.Model
			);

			var filterModel = Assert.IsAssignableFrom<LogMessagesFilterModel>(
				viewResult.ViewData["FilterData"]
			);

			Assert.Equal("the-source", filterViewModel.Sources);
			Assert.Equal(
				new List<LogEntrySeverities> { LogEntrySeverities.Debug },
				filterModel.Severities
			);
		}

		[Fact]
		public async Task LogsBadRequest()
		{
			// Arrange
			_mockLoggingService
				.Setup(log => log.GetLogMessagesAsync(It.IsAny<LogMessagesFilterModel>()))
				.ThrowsAsync(new Exception());

			// Act
			var result = await _controller.Logs(new LogMessagesFilterViewModel());

			// Assert
			Assert.IsType<BadRequestObjectResult>(result);
		}

		[Fact]
		public async Task LogsRedirect()
		{
			// Act
			var result = await _controller.Logs(new LogMessagesFilterViewModel{
				Id = 1
			});

			// Assert
			var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);

			Assert.Equal("Log", redirectToActionResult.ActionName);
			Assert.Equal(1, redirectToActionResult.RouteValues["id"]);
		}
	}
}
