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
	public partial class HomeControllerTest
	{
		private readonly Mock<IMetricService> _mockMetricService = new Mock<IMetricService>();
		private readonly Mock<IAuthService> _mockAuth = new Mock<IAuthService>();
		private readonly Mock<IBadgeService> _mockBadge = new Mock<IBadgeService>();

		private readonly IDataContext _context;

		private readonly HomeController _controller;

		public HomeControllerTest()
		{
			var services = new ServiceCollection();

			var mockEnv = new Mock<IHostingEnvironment>();
			mockEnv
				.SetupGet(environment => environment.EnvironmentName)
				.Returns("Testing");
			var env = mockEnv.Object;

			services.RegisterSharedServices(env, new Mock<IConfiguration>().Object);

			_context = services
				.BuildServiceProvider()
				.GetRequiredService<IDataContext>();

			_context.ManualLabels.Add(new ManualLabel { Id = ManualLabels.None.AsInt() });
			_context.SaveChanges();

			_mockAuth
				.Setup(auth => auth.IsAuthenticated())
				.Returns(true);

			_controller = new HomeController(
				_mockMetricService.Object,
				_context,
				_mockAuth.Object,
				_mockBadge.Object
			);
			_controller.ControllerContext.HttpContext = new DefaultHttpContext();
			_controller.TempData = new Mock<ITempDataDictionary>().Object;
		}
	}
}
