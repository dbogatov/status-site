using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using StatusMonitor.Daemons.Services;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Shared.Models;
using StatusMonitor.Shared.Models.Entities;
using StatusMonitor.Shared.Services;
using Xunit;

namespace StatusMonitor.Tests.UnitTests.Services
{
	public class HealthServiceTest
	{
		private readonly IServiceProvider _serviceProvider;

		public HealthServiceTest()
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

		[Fact]
		public async Task ReportsHealth()
		{
			// Arrange
			var context = _serviceProvider.GetRequiredService<IDataContext>();

			var normal = new AutoLabel { Id = AutoLabels.Normal.AsInt() };
			var warning = new AutoLabel { Id = AutoLabels.Warning.AsInt() };
			var critical = new AutoLabel { Id = AutoLabels.Critical.AsInt() };

			await context.AutoLabels.AddRangeAsync(normal, warning, critical);
			await context.Metrics.AddRangeAsync(
				new Metric
				{
					Type = Metrics.CpuLoad.AsInt(),
					Source = "the-source-1",
					AutoLabel = normal,
					Public = true
				},
				new Metric
				{
					Type = Metrics.UserAction.AsInt(),
					Source = "the-source-1",
					AutoLabel = warning,
					Public = true
				},
				new Metric
				{
					Type = Metrics.CpuLoad.AsInt(),
					Source = "the-source-3",
					AutoLabel = critical,
					Public = true
				},
				new Metric
				{
					Type = Metrics.CpuLoad.AsInt(),
					Source = "the-source-4",
					AutoLabel = normal,
					Public = true
				},
				new Metric
				{
					Type = Metrics.CpuLoad.AsInt(),
					Source = "the-source-5",
					AutoLabel = critical,
					Public = false
				}
			);

			await context.SaveChangesAsync();

			var healthService = new HealthService(
				context,
				new Mock<ILogger<HealthService>>().Object
			);

			// Act
			var actual = await healthService.ProduceHealthReportAsync();

			// Assert
			Assert.Equal(62, actual.Health);
			Assert.Equal(4, actual.Data.Count());
		}

		[Fact]
		public async Task ReportsHealthData()
		{
			// Arrange
			var healthService = new HealthService(
				_serviceProvider.GetRequiredService<IDataContext>(),
				new Mock<ILogger<HealthService>>().Object
			);

			// Act
			var actual = await healthService.ProduceHealthReportAsync();

			// Assert
			Assert.Equal(0, actual.Health);
			Assert.Empty(actual.Data);
		}
	}
}
