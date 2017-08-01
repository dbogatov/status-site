using System;
using Xunit;
using StatusMonitor.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using StatusMonitor.Daemons.Services;
using Moq;
using Microsoft.AspNetCore.Hosting;
using StatusMonitor.Shared.Extensions;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using StatusMonitor.Shared.Models.Entities;
using Microsoft.Extensions.Logging;
using StatusMonitor.Shared.Services;

namespace StatusMonitor.Tests.UnitTests.Services
{
	public class UptimeReportServiceTest
	{
		private readonly IServiceProvider _serviceProvider;

		public UptimeReportServiceTest()
		{
			var services = new ServiceCollection();

			var mockEnv = new Mock<IHostingEnvironment>();
			mockEnv
				.SetupGet(environment => environment.EnvironmentName)
				.Returns("Testing");

			services.RegisterSharedServices(mockEnv.Object, new Mock<IConfiguration>().Object);

			_serviceProvider = services.BuildServiceProvider();
		}

		[Fact]
		public async Task ComputesUptime()
		{
			// Arrange
			var context = _serviceProvider.GetRequiredService<IDataContext>();

			var metric = await context.Metrics.AddAsync(new Metric {
				Type = Metrics.Ping.AsInt(),
				Source = "url.com"
			});

			await context.PingDataPoints.AddRangeAsync(
				new PingDataPoint { Metric = metric.Entity, Success = true },
				new PingDataPoint { Metric = metric.Entity, Success = false },
				new PingDataPoint { Metric = metric.Entity, Success = true },
				new PingDataPoint { Metric = metric.Entity, Success = false },
				new PingDataPoint { Metric = metric.Entity, Success = true },
				new PingDataPoint { Metric = metric.Entity, Success = true },
				new PingDataPoint { Metric = metric.Entity, Success = false },
				new PingDataPoint { Metric = metric.Entity, Success = true },
				new PingDataPoint { Metric = metric.Entity, Success = false },
				new PingDataPoint { Metric = metric.Entity, Success = true }
			);

			await context.SaveChangesAsync();

			// Act
			var actual = await new UptimeReportService(context)
				.ComputeUptimeAsync("url.com");

			// Assert
			Assert.Equal(60, actual);
		}

		[Fact]
		public async Task ComputesUptimeNoData()
		{
			// Arrange
			var context = _serviceProvider.GetRequiredService<IDataContext>();

			await context.Metrics.AddAsync(new Metric {
				Type = Metrics.Ping.AsInt(),
				Source = "url.com"
			});

			await context.SaveChangesAsync();

			// Act
			var actual = await new UptimeReportService(context)
				.ComputeUptimeAsync("url.com");

			// Assert
			Assert.Equal(0, actual);
		}
	}
}
