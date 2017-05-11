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
		public async Task AuthenticateReCaptchaError()
		{
			// Arrange
			_controller.ModelState.AddModelError("Auth", "ReCaptcha error");

			// Act
			var result = await _controller.Authenticate();

			// Assert
			var viewResult = Assert.IsType<ViewResult>(result);

			var model = Assert.IsAssignableFrom<ReturnUrlViewModel>(
				viewResult.Model
			);

			Assert.True(model.IsError);
			Assert.Equal("/return", model.ReturnUrl);

			_mockAuth
				.Verify(
					auth => auth.SignInAsync(),
					Times.Never()
				);
		}

		[Fact]
		public async Task AuthenticatePasswordError()
		{
			// Arrange
			var mockForm = new Mock<IFormCollection>();
			mockForm.Setup(form => form["password"]).Returns("bad-password");

			var mockQuery = new Mock<IQueryCollection>();
			mockQuery.Setup(query => query["returnurl"]).Returns("/return");

			var mockContext = new Mock<HttpContext>();
			mockContext.Setup(context => context.Request.Form).Returns(mockForm.Object);
			mockContext.Setup(context => context.Request.Query).Returns(mockQuery.Object);

			_controller.ControllerContext.HttpContext = mockContext.Object;

			// Act
			var result = await _controller.Authenticate();

			// Assert
			var viewResult = Assert.IsType<ViewResult>(result);

			var model = Assert.IsAssignableFrom<ReturnUrlViewModel>(
				viewResult.Model
			);

			Assert.True(model.IsError);
			Assert.Equal("/return", model.ReturnUrl);

			_mockAuth
				.Verify(
					auth => auth.SignInAsync(),
					Times.Never()
				);
		}

		[Fact]
		public async Task AuthenticateOKWithRedirect()
		{
			// Arrange
			var mockForm = new Mock<IFormCollection>();
			mockForm.Setup(form => form["password"]).Returns("good-password");

			var mockQuery = new Mock<IQueryCollection>();
			mockQuery.Setup(query => query["returnurl"]).Returns("/return");

			var mockContext = new Mock<HttpContext>();
			mockContext.Setup(context => context.Request.Form).Returns(mockForm.Object);
			mockContext.Setup(context => context.Request.Query).Returns(mockQuery.Object);

			_controller.ControllerContext.HttpContext = mockContext.Object;

			// Act
			var result = await _controller.Authenticate();

			// Assert
			var redirectResult = Assert.IsType<RedirectResult>(result);

			Assert.Equal("/return", redirectResult.Url);

			_mockAuth
				.Verify(
					auth => auth.SignInAsync()
				);
		}

		[Fact]
		public async Task AuthenticateOKWithoutRedirect()
		{
			// Arrange
			var mockForm = new Mock<IFormCollection>();
			mockForm.Setup(form => form["password"]).Returns("good-password");

			var mockQuery = new Mock<IQueryCollection>();
			mockQuery.Setup(query => query["returnurl"]).Returns("");

			var mockContext = new Mock<HttpContext>();
			mockContext.Setup(context => context.Request.Form).Returns(mockForm.Object);
			mockContext.Setup(context => context.Request.Query).Returns(mockQuery.Object);

			_controller.ControllerContext.HttpContext = mockContext.Object;

			// Act
			var result = await _controller.Authenticate();

			// Assert
			var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);

			Assert.Equal("Index", redirectToActionResult.ActionName);
			Assert.Equal("Home", redirectToActionResult.ControllerName);

			_mockAuth
				.Verify(
					auth => auth.SignInAsync()
				);
		}
	}
}
