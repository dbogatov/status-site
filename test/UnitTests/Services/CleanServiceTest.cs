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
	public class CleanServiceTest
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly IConfiguration _config;

		public CleanServiceTest()
		{
			var services = new ServiceCollection();

			var mockEnv = new Mock<IHostingEnvironment>();
			mockEnv
				.SetupGet(environment => environment.EnvironmentName)
				.Returns("Testing");
			var env = mockEnv.Object;

			var mockConfig = new Mock<IConfiguration>();
			mockConfig
				.SetupGet(config => config["ServiceManager:CleanService:MaxAge"])
				.Returns($"{24 * 60 * 60}");
			_config = mockConfig.Object;

			services.RegisterSharedServices(env, _config);

			_serviceProvider = services.BuildServiceProvider();
		}

		[Fact]
		public async Task ProperlyCleanData()
		{
			// Arrange
			var context = _serviceProvider.GetRequiredService<IDataContext>();

			var twoDaysAgo = DateTime.UtcNow.AddDays(-2);
			var hourAgo = DateTime.UtcNow.AddHours(-1);
			var now = DateTime.UtcNow;


			await context.NumericDataPoints.AddRangeAsync(new List<NumericDataPoint> {
				new NumericDataPoint { Timestamp = twoDaysAgo },
				new NumericDataPoint { Timestamp = hourAgo },
				new NumericDataPoint { Timestamp = now }
			});

			await context.LogDataPoints.AddRangeAsync(new List<LogDataPoint> {
				new LogDataPoint { Timestamp = twoDaysAgo },
				new LogDataPoint { Timestamp = hourAgo },
				new LogDataPoint { Timestamp = now }
			});

			await context.UserActionDataPoints.AddRangeAsync(new List<UserActionDataPoint> {
				new UserActionDataPoint { Timestamp = twoDaysAgo },
				new UserActionDataPoint { Timestamp = hourAgo },
				new UserActionDataPoint { Timestamp = now }
			});

			await context.CompilationDataPoints.AddRangeAsync(new List<CompilationDataPoint> {
				new CompilationDataPoint { Timestamp = twoDaysAgo },
				new CompilationDataPoint { Timestamp = hourAgo },
				new CompilationDataPoint { Timestamp = now }
			});

			await context.PingDataPoints.AddRangeAsync(new List<PingDataPoint> {
				new PingDataPoint { Timestamp = twoDaysAgo },
				new PingDataPoint { Timestamp = hourAgo },
				new PingDataPoint { Timestamp = now }
			});

			await context.LogEntries.AddRangeAsync(new List<LogEntry> {
				new LogEntry { Timestamp = twoDaysAgo },
				new LogEntry { Timestamp = hourAgo },
				new LogEntry { Timestamp = now }
			});

			await context.Notifications.AddRangeAsync(new List<Notification> {
				new Notification { DateCreated = twoDaysAgo.AddDays(-1)},
				new Notification { DateCreated = twoDaysAgo, IsSent = true },
				new Notification { DateCreated = hourAgo },
				new Notification { DateCreated = now }
			});

			await context.Discrepancies.AddRangeAsync(new List<Discrepancy> {
				new Discrepancy {
					DateFirstOffense = twoDaysAgo,
					Type = DiscrepancyType.GapInData,
					MetricSource = "the-source",
					MetricType = Metrics.CpuLoad
				},
				new Discrepancy {
					DateFirstOffense = hourAgo,
					Type = DiscrepancyType.GapInData,
					MetricSource = "the-source",
					MetricType = Metrics.CpuLoad
				},
				new Discrepancy {
					DateFirstOffense = now,
					Type = DiscrepancyType.GapInData,
					MetricSource = "the-source",
					MetricType = Metrics.CpuLoad
				}
			});

			await context.HealthReports.AddRangeAsync(
				new HealthReport { Timestamp = twoDaysAgo },
				new HealthReport { Timestamp = hourAgo },
				new HealthReport { Timestamp = now }
			);

			await context.SaveChangesAsync();

			// Act
			await new CleanService(
				new Mock<ILogger<CleanService>>().Object,
				context,
				_config
			)
			.CleanDataPointsAsync();

			// Assert
			Assert.Equal(2, await context.NumericDataPoints.CountAsync());
			Assert.Equal(2, await context.LogDataPoints.CountAsync());
			Assert.Equal(2, await context.UserActionDataPoints.CountAsync());
			Assert.Equal(2, await context.CompilationDataPoints.CountAsync());
			Assert.Equal(2, await context.PingDataPoints.CountAsync());
			Assert.Equal(2, await context.LogEntries.CountAsync());
			Assert.Equal(3, await context.Notifications.CountAsync());
			Assert.Equal(2, await context.Discrepancies.CountAsync());
			Assert.Equal(2, await context.HealthReports.CountAsync());
		}
	}
}
