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
using StatusMonitor.Web.ViewModels;

namespace StatusMonitor.Tests.ControllerTests
{
	public partial class ErrorControllerTest
	{

		private readonly ErrorController _controller;

		public ErrorControllerTest()
		{
			_controller = new ErrorController(
				new Mock<ILogger<ErrorController>>().Object
			);
			_controller.ControllerContext.HttpContext = new DefaultHttpContext();
			_controller.TempData = new Mock<ITempDataDictionary>().Object;
		}

		[Theory]
		[InlineData(400)]
		[InlineData(401)]
		[InlineData(404)]
		[InlineData(500)]
		[InlineData(null)]
		public void Error(int? code)
		{
			// Act
			var result = _controller.Error(code);

			// Assert
			var viewResult = Assert.IsType<ViewResult>(result);

			var model = Assert.IsAssignableFrom<ErrorViewModel>(
				viewResult.ViewData.Model
			);

			Assert.Equal(code ?? 404, model.Code);
		}
	}
}
