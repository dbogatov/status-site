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
using StatusMonitor.Web.Controllers.Api;

namespace StatusMonitor.Tests.ControllerTests
{
	public partial class AdminControllerTest
	{
		private readonly Mock<ILoggingService> _mockLoggingService = new Mock<ILoggingService>();
		private readonly Mock<IMetricService> _mockMetricService = new Mock<IMetricService>();
		private readonly Mock<IApiController> _mockApiController = new Mock<IApiController>();
		private readonly Mock<ICleanService> _mockCleanService = new Mock<ICleanService>();
		

		private readonly AdminController _controller;

		public AdminControllerTest()
		{
			var services = new ServiceCollection();

			var mockEnv = new Mock<IHostingEnvironment>();
			mockEnv
				.SetupGet(environment => environment.EnvironmentName)
				.Returns("Testing");
			var env = mockEnv.Object;

			services.RegisterSharedServices(env, new Mock<IConfiguration>().Object);

			var context = services
				.BuildServiceProvider()
				.GetRequiredService<IDataContext>();

			var mockServiceProvider = new Mock<IServiceProvider>();
			mockServiceProvider
				.Setup(provider => provider.GetService(typeof(IApiController)))
				.Returns(_mockApiController.Object);

			_controller = new AdminController(
				_mockLoggingService.Object,
				new Mock<ILogger<AdminController>>().Object,
				_mockMetricService.Object,
				mockServiceProvider.Object,
				_mockCleanService.Object,
				context
			);
			_controller.ControllerContext.HttpContext = new DefaultHttpContext();
			_controller.TempData = new Mock<ITempDataDictionary>().Object;
		}
	}
}
