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

namespace StatusMonitor.Tests.ControllerTests
{
	public partial class AccountControllerTest
	{
		private readonly Mock<IAuthService> _mockAuth = new Mock<IAuthService>();

		private readonly AccountController _controller;

		public AccountControllerTest()
		{
			var mockQuery = new Mock<IQueryCollection>();
			mockQuery.Setup(query => query["returnurl"]).Returns("/return");

			var mockContext = new Mock<HttpContext>();
			mockContext.Setup(context => context.Request.Query).Returns(mockQuery.Object);

			var config = new Mock<IConfiguration>();
			config
				.SetupGet(conf => conf["Secrets:AdminPassword"])
				.Returns("good-password");

			_controller = new AccountController(
				config.Object,
				_mockAuth.Object
			);

			_controller.ControllerContext.HttpContext = mockContext.Object;
			_controller.TempData = new Mock<ITempDataDictionary>().Object;
		}
	}
}
