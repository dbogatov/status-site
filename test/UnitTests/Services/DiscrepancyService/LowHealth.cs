using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
	public partial class DiscrepancyServiceTest
	{
		public static HealthReport GenerateHealthReport(bool good, Metric metric = null, DateTime? timestamp = null)
		{
			return
				new HealthReport
				{
					Data = new List<HealthReportDataPoint> {
						new HealthReportDataPoint { Label = AutoLabels.Normal.ToString() },
						new HealthReportDataPoint {
							Label = (good ? AutoLabels.Normal : AutoLabels.Critical).ToString()
						},
					},
					Timestamp = timestamp.HasValue ? timestamp.Value : DateTime.UtcNow,
					Metric = metric ?? new Metric
					{
						Type = Metrics.Health.AsInt(),
						Source = "smth.com"
					}
				};
		}

		private HealthReport ConfigureHealthReport(int health, DateTime timestamp, Metric metric = null)
		{
			var report = new Mock<HealthReport>();

			report
				.SetupGet(r => r.Health)
				.Returns(health);
			report
				.Setup(r => r.Timestamp)
				.Returns(timestamp);
			report
				.Setup(r => r.Metric)
				.Returns(metric ?? new Metric
				{
					Type = Metrics.Health.AsInt(),
					Source = "the-source"
				});

			return report.Object;
		}



		[Fact]
		public void SingleLowHealthInData()
		{
			// Arrange
			var mockConfig = new Mock<IConfiguration>();
			mockConfig
				.SetupGet(conf => conf["ServiceManager:DiscrepancyService:Health:Threshold"])
				.Returns(90.ToString());
			mockConfig
				.SetupGet(conf => conf["ServiceManager:DiscrepancyService:Health:MaxFailures"])
				.Returns(2.ToString()); // 3 is discrepancy

			var discrepancyService = new DiscrepancyService(
				new Mock<ILogger<DiscrepancyService>>().Object,
				new Mock<IDataContext>().Object,
				new Mock<INotificationService>().Object,
				mockConfig.Object
			);

			var input = new List<HealthReport>() {
				ConfigureHealthReport(95, DateTime.UtcNow),					// good
				ConfigureHealthReport(89, DateTime.UtcNow.AddMinutes(-1)),	// bad
				ConfigureHealthReport(70, DateTime.UtcNow.AddMinutes(-2)),	// bad
				ConfigureHealthReport(75, DateTime.UtcNow.AddMinutes(-3)),	// bad
				ConfigureHealthReport(100, DateTime.UtcNow.AddMinutes(-4)),	// good
				ConfigureHealthReport(56, DateTime.UtcNow.AddMinutes(-5)),	// bad
				ConfigureHealthReport(69, DateTime.UtcNow.AddMinutes(-6)),	// bad
				ConfigureHealthReport(99, DateTime.UtcNow.AddMinutes(-7)),	// good
			};

			var expected = new List<Discrepancy> {
				new Discrepancy
				{
					DateFirstOffense = input[3].Timestamp,
					Type = DiscrepancyType.LowHealth,
					MetricType = Metrics.Health,
					MetricSource = "the-source"
				}
			};

			// Act
			var actual = discrepancyService
				.FindLowHealthInDataPoints(
					input,
					new Metric
					{
						Type = Metrics.Health.AsInt(),
						Source = "the-source"
					}
				);

			// Assert
			Assert.Equal(expected, actual);
		}

		[Fact]
		public void LowHealthNoData()
		{
			// Arrange
			var discrepancyService = new DiscrepancyService(
				new Mock<ILogger<DiscrepancyService>>().Object,
				new Mock<IDataContext>().Object,
				new Mock<INotificationService>().Object,
				new Mock<IConfiguration>().Object
			);

			// Act
			var actual = discrepancyService
				.FindLowHealthInDataPoints(
					new List<HealthReport>(),
					new Metric
					{
						Type = Metrics.Health.AsInt(),
						Source = "the-source"
					}
				);

			// Assert
			Assert.Empty(actual);
		}

		[Fact]
		public void NoLowHealthInData()
		{
			// Arrange
			var mockConfig = new Mock<IConfiguration>();
			mockConfig
				.SetupGet(conf => conf["ServiceManager:DiscrepancyService:Health:Threshold"])
				.Returns(90.ToString());
			mockConfig
				.SetupGet(conf => conf["ServiceManager:DiscrepancyService:Health:MaxFailures"])
				.Returns(2.ToString()); // 3 is discrepancy

			var discrepancyService = new DiscrepancyService(
				new Mock<ILogger<DiscrepancyService>>().Object,
				new Mock<IDataContext>().Object,
				new Mock<INotificationService>().Object,
				mockConfig.Object
			);

			var input = new List<HealthReport>() {
				ConfigureHealthReport(95, DateTime.UtcNow),					// good
				ConfigureHealthReport(89, DateTime.UtcNow.AddMinutes(-1)),	// bad
				ConfigureHealthReport(70, DateTime.UtcNow.AddMinutes(-2)),	// bad
				ConfigureHealthReport(98, DateTime.UtcNow.AddMinutes(-3)),	// good
				ConfigureHealthReport(100, DateTime.UtcNow.AddMinutes(-4)),	// good
				ConfigureHealthReport(56, DateTime.UtcNow.AddMinutes(-5)),	// bad
				ConfigureHealthReport(69, DateTime.UtcNow.AddMinutes(-6)),	// bad
				ConfigureHealthReport(99, DateTime.UtcNow.AddMinutes(-7)),	// good
			};

			// Act
			var actual = discrepancyService
				.FindLowHealthInDataPoints(
					input,
					new Metric
					{
						Type = Metrics.Health.AsInt(),
						Source = "the-source"
					}
				);

			// Assert
			Assert.Empty(actual);
		}

		[Fact]
		public void PermanentLowHealthInData()
		{
			// Arrange
			var mockConfig = new Mock<IConfiguration>();
			mockConfig
				.SetupGet(conf => conf["ServiceManager:DiscrepancyService:Health:Threshold"])
				.Returns(90.ToString());
			mockConfig
				.SetupGet(conf => conf["ServiceManager:DiscrepancyService:Health:MaxFailures"])
				.Returns(2.ToString()); // 3 is discrepancy

			var discrepancyService = new DiscrepancyService(
				new Mock<ILogger<DiscrepancyService>>().Object,
				new Mock<IDataContext>().Object,
				new Mock<INotificationService>().Object,
				mockConfig.Object
			);

			var input = new List<HealthReport>() {
				ConfigureHealthReport(80, DateTime.UtcNow),					// bad
				ConfigureHealthReport(89, DateTime.UtcNow.AddMinutes(-1)),	// bad
				ConfigureHealthReport(70, DateTime.UtcNow.AddMinutes(-2)),	// bad
				ConfigureHealthReport(60, DateTime.UtcNow.AddMinutes(-3)),	// bad
				ConfigureHealthReport(85, DateTime.UtcNow.AddMinutes(-4)),	// bad
				ConfigureHealthReport(56, DateTime.UtcNow.AddMinutes(-5)),	// bad
				ConfigureHealthReport(69, DateTime.UtcNow.AddMinutes(-6)),	// bad
				ConfigureHealthReport(9, DateTime.UtcNow.AddMinutes(-7)),	// bad
			};

			// Act
			var actual = discrepancyService
				.FindLowHealthInDataPoints(
					input,
					new Metric
					{
						Type = Metrics.Health.AsInt(),
						Source = "the-source"
					}
				);

			// Assert
			Assert.Empty(actual);
		}

		[Fact]
		public void MultipleLowHealthInData()
		{
			// Arrange
			var mockConfig = new Mock<IConfiguration>();
			mockConfig
				.SetupGet(conf => conf["ServiceManager:DiscrepancyService:Health:Threshold"])
				.Returns(90.ToString());
			mockConfig
				.SetupGet(conf => conf["ServiceManager:DiscrepancyService:Health:MaxFailures"])
				.Returns(2.ToString()); // 3 is discrepancy

			var discrepancyService = new DiscrepancyService(
				new Mock<ILogger<DiscrepancyService>>().Object,
				new Mock<IDataContext>().Object,
				new Mock<INotificationService>().Object,
				mockConfig.Object
			);

			var input = new List<HealthReport>() {
				ConfigureHealthReport(95, DateTime.UtcNow),					// good
				ConfigureHealthReport(89, DateTime.UtcNow.AddMinutes(-1)),	// bad
				ConfigureHealthReport(70, DateTime.UtcNow.AddMinutes(-2)),	// bad
				ConfigureHealthReport(75, DateTime.UtcNow.AddMinutes(-3)),	// bad
				ConfigureHealthReport(100, DateTime.UtcNow.AddMinutes(-4)),	// good
				ConfigureHealthReport(56, DateTime.UtcNow.AddMinutes(-5)),	// bad
				ConfigureHealthReport(69, DateTime.UtcNow.AddMinutes(-6)),	// bad
				ConfigureHealthReport(87, DateTime.UtcNow.AddMinutes(-7)),	// bad
				ConfigureHealthReport(99, DateTime.UtcNow.AddMinutes(-8)),	// good
			};

			var expected = new List<Discrepancy> {
				new Discrepancy
				{
					DateFirstOffense = input[3].Timestamp,
					Type = DiscrepancyType.LowHealth,
					MetricType = Metrics.Health,
					MetricSource = "the-source"
				},
				new Discrepancy
				{
					DateFirstOffense = input[7].Timestamp,
					Type = DiscrepancyType.LowHealth,
					MetricType = Metrics.Health,
					MetricSource = "the-source"
				}
			};

			// Act
			var actual = discrepancyService
				.FindLowHealthInDataPoints(
					input,
					new Metric
					{
						Type = Metrics.Health.AsInt(),
						Source = "the-source"
					}
				);

			// Assert
			Assert.Equal(expected, actual);
		}

		[Fact]
		public void LowHealthDataStartsWithLowHealth()
		{
			// Arrange
			var mockConfig = new Mock<IConfiguration>();
			mockConfig
				.SetupGet(conf => conf["ServiceManager:DiscrepancyService:Health:Threshold"])
				.Returns(90.ToString());
			mockConfig
				.SetupGet(conf => conf["ServiceManager:DiscrepancyService:Health:MaxFailures"])
				.Returns(2.ToString()); // 3 is discrepancy

			var discrepancyService = new DiscrepancyService(
				new Mock<ILogger<DiscrepancyService>>().Object,
				new Mock<IDataContext>().Object,
				new Mock<INotificationService>().Object,
				mockConfig.Object
			);

			var input = new List<HealthReport>() {
				ConfigureHealthReport(95, DateTime.UtcNow),					// good
				ConfigureHealthReport(89, DateTime.UtcNow.AddMinutes(-1)),	// bad
				ConfigureHealthReport(70, DateTime.UtcNow.AddMinutes(-2)),	// bad
				ConfigureHealthReport(75, DateTime.UtcNow.AddMinutes(-3)),	// bad
				ConfigureHealthReport(100, DateTime.UtcNow.AddMinutes(-4)),	// good
				ConfigureHealthReport(56, DateTime.UtcNow.AddMinutes(-5)),	// bad
				ConfigureHealthReport(69, DateTime.UtcNow.AddMinutes(-6)),	// bad
				ConfigureHealthReport(87, DateTime.UtcNow.AddMinutes(-7))	// bad
			};

			var expected = new List<Discrepancy> {
				new Discrepancy
				{
					DateFirstOffense = input[3].Timestamp,
					Type = DiscrepancyType.LowHealth,
					MetricType = Metrics.Health,
					MetricSource = "the-source"
				}
			};

			// Act
			var actual = discrepancyService
				.FindLowHealthInDataPoints(
					input,
					new Metric
					{
						Type = Metrics.Health.AsInt(),
						Source = "the-source"
					}
				);

			// Assert
			Assert.Equal(expected, actual);
		}

		[Fact]
		public async Task FindsLowHealth()
		{
			// Arrange
			var mockConfig = new Mock<IConfiguration>();
			mockConfig
				.SetupGet(conf => conf["ServiceManager:DiscrepancyService:Health:Threshold"])
				.Returns(99.ToString());
			mockConfig
				.SetupGet(conf => conf["ServiceManager:DiscrepancyService:LoHealthad:MaxFailures"])
				.Returns(1.ToString()); // 3 is discrepancy

			var context = _serviceProvider.GetRequiredService<IDataContext>();
			var metric = await context.Metrics.AddAsync(
				new Metric
				{
					Source = "the-source",
					Type = Metrics.Health.AsInt()
				}
			);

			var dataPoints = new List<HealthReport>() {
				new HealthReport { // good
					Data = new List<HealthReportDataPoint> {
						new HealthReportDataPoint { Label = AutoLabels.Normal.ToString() },
						new HealthReportDataPoint { Label = AutoLabels.Normal.ToString() },
					},
					Timestamp = DateTime.UtcNow,
					Metric =  metric.Entity
				},
				new HealthReport {
					Data = new List<HealthReportDataPoint> { // bad
						new HealthReportDataPoint { Label = AutoLabels.Normal.ToString() },
						new HealthReportDataPoint { Label = AutoLabels.Critical.ToString() },
					},
					Timestamp = DateTime.UtcNow.AddMinutes(-1),
					Metric =  metric.Entity
				},
				new HealthReport {
					Data = new List<HealthReportDataPoint> { // bad
						new HealthReportDataPoint { Label = AutoLabels.Normal.ToString() },
						new HealthReportDataPoint { Label = AutoLabels.Warning.ToString() },
					},
					Timestamp = DateTime.UtcNow.AddMinutes(-2),
					Metric =  metric.Entity
				},
				new HealthReport { // good
					Data = new List<HealthReportDataPoint> {
						new HealthReportDataPoint { Label = AutoLabels.Normal.ToString() },
						new HealthReportDataPoint { Label = AutoLabels.Normal.ToString() },
					},
					Timestamp = DateTime.UtcNow.AddMinutes(-3),
					Metric =  metric.Entity
				}
			};

			await context.HealthReports.AddRangeAsync(dataPoints);
			await context.SaveChangesAsync();

			var discrepancyService = new DiscrepancyService(
				new Mock<ILogger<DiscrepancyService>>().Object,
				context,
				new Mock<INotificationService>().Object,
				mockConfig.Object
			);

			var expected = new List<Discrepancy> {
				new Discrepancy
				{
					DateFirstOffense = dataPoints[2].Timestamp,
					Type = DiscrepancyType.LowHealth,
					MetricType = Metrics.Health,
					MetricSource = "the-source"
				}
			};

			// Act
			var actual = await discrepancyService
				.FindLowHealthsAsync(
					metric.Entity,
					new TimeSpan(0, 30, 0)
				);

			// Assert
			Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData(Metrics.Compilation, false)]
		[InlineData(Metrics.CpuLoad, false)]
		[InlineData(Metrics.Log, false)]
		[InlineData(Metrics.Ping, false)]
		[InlineData(Metrics.UserAction, false)]
		[InlineData(Metrics.Health, true)]
		public async Task VerifyMetricForLowHealth(Metrics type, bool shouldSucceed)
		{
			// Arrange
			var metric = new Metric
			{
				Type = type.AsInt(),
				Source = "the-source"
			};
			var discrepancyService = new DiscrepancyService(
				new Mock<ILogger<DiscrepancyService>>().Object,
				new Mock<IDataContext>().Object,
				new Mock<INotificationService>().Object,
				new Mock<IConfiguration>().Object
			);

			// Act
			if (shouldSucceed)
			{
				// It will pass metric check, will try to interact with data layer and fail
				await Assert.ThrowsAsync<NullReferenceException>(
					async () => await discrepancyService.FindLowHealthsAsync(metric, new TimeSpan())
				);
			}
			else
			{
				await Assert.ThrowsAsync<ArgumentException>(
					async () => await discrepancyService.FindLowHealthsAsync(metric, new TimeSpan())
				);
			}
		}
	}
}
