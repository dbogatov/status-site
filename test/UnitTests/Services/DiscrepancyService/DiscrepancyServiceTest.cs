using System;
using Xunit;
using StatusMonitor.Shared.Models.Entities;
using StatusMonitor.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using StatusMonitor.Shared.Extensions;
using System.Net;
using StatusMonitor.Daemons.Services;
using Moq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StatusMonitor.Shared.Services.Factories;
using System.Net.Http;
using StatusMonitor.Tests.Mock;
using Microsoft.Extensions.Logging;
using StatusMonitor.Shared.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging.Internal;
using Microsoft.EntityFrameworkCore;

namespace StatusMonitor.Tests.UnitTests.Services
{
	public partial class DiscrepancyServiceTest
	{
		private readonly IServiceProvider _serviceProvider;

		public DiscrepancyServiceTest()
		{
			var services = new ServiceCollection();

			var mockEnv = new Mock<IHostingEnvironment>();
			mockEnv
				.SetupGet(environment => environment.EnvironmentName)
				.Returns("Testing");
			var env = mockEnv.Object;

			services.RegisterSharedServices(env, new Mock<IConfiguration>().Object);

			_serviceProvider = services.BuildServiceProvider();
		}
	}
}
