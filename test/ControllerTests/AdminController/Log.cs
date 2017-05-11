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
		public async Task LogOK()
		{
			// Arrange
			_mockLoggingService
				.Setup(log => log.GetMessageByIdAsync(It.IsAny<int>()))
				.ReturnsAsync(new LogEntry { Id = 1 });

			// Act
			var result = await _controller.Log(1);

			// Assert
			var viewResult = Assert.IsType<ViewResult>(result);

			var logEntry = Assert.IsAssignableFrom<LogEntry>(
				viewResult.Model
			);

			Assert.Equal(1, logEntry.Id);
		}

		[Fact]
		public async Task LogNotFound()
		{
			// Arrange
			_mockLoggingService
				.Setup(log => log.GetMessageByIdAsync(It.IsAny<int>()))
				.ReturnsAsync(null);

			// Act
			var result = await _controller.Log(1);

			// Assert
			Assert.IsType<NotFoundObjectResult>(result);
		}

		[Fact]
		public async Task LogRedirect()
		{
			// Act
			var result = await _controller.Log(null);

			// Assert
			var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);

			Assert.Equal("Logs", redirectToActionResult.ActionName);
		}
	}
}
