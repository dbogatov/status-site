using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Shared.Models;
using StatusMonitor.Shared.Services;
using StatusMonitor.Web.Controllers.Api;
using StatusMonitor.Web.Services;
using Moq;

namespace StatusMonitor.Tests.ControllerTests
{
	public partial class ApiControllerTest
	{
		private readonly IDataContext _context;
		private readonly Mock<IMetricService> _mockMetricService = new Mock<IMetricService>();
		private readonly Mock<ILoggingService> _mockLoggingService = new Mock<ILoggingService>();
		private readonly Mock<IConfiguration> _mockConfig = new Mock<IConfiguration>();
		private readonly Mock<INotificationService> _mockNotify = new Mock<INotificationService>();
		private readonly Mock<IAuthService> _mockAuth = new Mock<IAuthService>();

		private readonly ApiController _controller;

		public ApiControllerTest()
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


			var config = new Mock<IConfiguration>();
			config
				.SetupGet(conf => conf["Secrets:AdminPassword"])
				.Returns("good-password");

			_controller = new ApiController(
				_context,
				_mockMetricService.Object,
				new Mock<ILogger<ApiController>>().Object,
				_mockLoggingService.Object,
				_mockConfig.Object,
				_mockNotify.Object,
				_mockAuth.Object
			);
		}
	}
}
