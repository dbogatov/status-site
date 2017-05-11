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
	public partial class AccountControllerTest
	{
		[Fact]
		public async Task LogoutWithReturn()
		{
			// Act
			var result = await _controller.Logout();

			// Assert
			var redirectResult = Assert.IsType<RedirectResult>(result);

			Assert.Equal("/return", redirectResult.Url);

			_mockAuth
				.Verify(
					auth => auth.SignOutAsync()
				);
		}

		[Fact]
		public async Task LogoutWithoutReturn()
		{
			// Arrange
			var mockQuery = new Mock<IQueryCollection>();
			mockQuery.Setup(query => query["returnurl"]).Returns("");

			var mockContext = new Mock<HttpContext>();
			mockContext.Setup(context => context.Request.Query).Returns(mockQuery.Object);

			_controller.ControllerContext.HttpContext = mockContext.Object;

			// Act
			var result = await _controller.Logout();

			// Assert
			var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);

			Assert.Equal("Index", redirectToActionResult.ActionName);
			Assert.Equal("Home", redirectToActionResult.ControllerName);

			_mockAuth
				.Verify(
					auth => auth.SignOutAsync()
				);
		}
	}
}
