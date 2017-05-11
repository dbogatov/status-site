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
		[Fact]
		public void SingleHighLoadInData()
		{
			// Arrange
			var mockConfig = new Mock<IConfiguration>();
			mockConfig
				.SetupGet(conf => conf["ServiceManager:DiscrepancyService:Load:Threshold"])
				.Returns(90.ToString());
			mockConfig
				.SetupGet(conf => conf["ServiceManager:DiscrepancyService:Load:MaxFailures"])
				.Returns(2.ToString()); // 3 is discrepancy

			var discrepancyService = new DiscrepancyService(
				new Mock<ILogger<DiscrepancyService>>().Object,
				new Mock<IDataContext>().Object,
				new Mock<INotificationService>().Object,
				mockConfig.Object
			);

			var input = new List<NumericDataPoint>() {
				new NumericDataPoint { // Good
					Timestamp = DateTime.UtcNow.AddMinutes(0),
					Value = 68
				},
				new NumericDataPoint { // Bad
					Timestamp = DateTime.UtcNow.AddMinutes(-1),
					Value = 91
				},
				new NumericDataPoint { // Bad
					Timestamp = DateTime.UtcNow.AddMinutes(-2),
					Value = 99
				},
				new NumericDataPoint { // Bad
					Timestamp = DateTime.UtcNow.AddMinutes(-3),
					Value = 95
				},
				new NumericDataPoint { // Good
					Timestamp = DateTime.UtcNow.AddMinutes(-4),
					Value = 68
				},
				new NumericDataPoint { // Bad
					Timestamp = DateTime.UtcNow.AddMinutes(-5),
					Value = 99
				},
				new NumericDataPoint { // Bad
					Timestamp = DateTime.UtcNow.AddMinutes(-6),
					Value = 98
				},
				new NumericDataPoint { // Good
					Timestamp = DateTime.UtcNow.AddMinutes(-7),
					Value = 68
				}
			};

			var expected = new List<Discrepancy> {
				new Discrepancy
				{
					DateFirstOffense = input[3].Timestamp,
					Type = DiscrepancyType.HighLoad,
					MetricType = Metrics.CpuLoad,
					MetricSource = "the-source"
				}
			};

			// Act
			var actual = discrepancyService
				.FindHighLoadInDataPoints(
					input,
					new Metric
					{
						Type = Metrics.CpuLoad.AsInt(),
						Source = "the-source"
					}
				);

			// Assert
			Assert.Equal(expected, actual);
		}

		[Fact]
		public void HighLoadNoData()
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
				.FindHighLoadInDataPoints(
					new List<NumericDataPoint>(),
					new Metric
					{
						Type = Metrics.CpuLoad.AsInt(),
						Source = "the-source"
					}
				);

			// Assert
			Assert.Empty(actual);
		}

		[Fact]
		public void NoHighLoadInData()
		{
			// Arrange
			var mockConfig = new Mock<IConfiguration>();
			mockConfig
				.SetupGet(conf => conf["ServiceManager:DiscrepancyService:Load:Threshold"])
				.Returns(90.ToString());
			mockConfig
				.SetupGet(conf => conf["ServiceManager:DiscrepancyService:Load:MaxFailures"])
				.Returns(3.ToString()); // 4 is discrepancy

			var discrepancyService = new DiscrepancyService(
				new Mock<ILogger<DiscrepancyService>>().Object,
				new Mock<IDataContext>().Object,
				new Mock<INotificationService>().Object,
				mockConfig.Object
			);

			var input = new List<NumericDataPoint>() {
				new NumericDataPoint { // Good
					Timestamp = DateTime.UtcNow.AddMinutes(0),
					Value = 68
				},
				new NumericDataPoint { // Bad
					Timestamp = DateTime.UtcNow.AddMinutes(-1),
					Value = 91
				},
				new NumericDataPoint { // Bad
					Timestamp = DateTime.UtcNow.AddMinutes(-2),
					Value = 99
				},
				new NumericDataPoint { // Bad
					Timestamp = DateTime.UtcNow.AddMinutes(-3),
					Value = 95
				},
				new NumericDataPoint { // Good
					Timestamp = DateTime.UtcNow.AddMinutes(-4),
					Value = 68
				},
				new NumericDataPoint { // Bad
					Timestamp = DateTime.UtcNow.AddMinutes(-5),
					Value = 99
				},
				new NumericDataPoint { // Bad
					Timestamp = DateTime.UtcNow.AddMinutes(-6),
					Value = 98
				},
				new NumericDataPoint { // Good
					Timestamp = DateTime.UtcNow.AddMinutes(-7),
					Value = 68
				}
			};

			// Act
			var actual = discrepancyService
				.FindHighLoadInDataPoints(
					input,
					new Metric
					{
						Type = Metrics.CpuLoad.AsInt(),
						Source = "the-source"
					}
				);

			// Assert
			Assert.Empty(actual);
		}

		[Fact]
		public void PermanentHighLoadInData()
		{
			// Arrange
			var mockConfig = new Mock<IConfiguration>();
			mockConfig
				.SetupGet(conf => conf["ServiceManager:DiscrepancyService:Load:Threshold"])
				.Returns(90.ToString());
			mockConfig
				.SetupGet(conf => conf["ServiceManager:DiscrepancyService:Load:MaxFailures"])
				.Returns(3.ToString()); // 4 is discrepancy

			var discrepancyService = new DiscrepancyService(
				new Mock<ILogger<DiscrepancyService>>().Object,
				new Mock<IDataContext>().Object,
				new Mock<INotificationService>().Object,
				mockConfig.Object
			);

			var input = new List<NumericDataPoint>() {
				new NumericDataPoint { // Bad
					Timestamp = DateTime.UtcNow.AddMinutes(0),
					Value = 93
				},
				new NumericDataPoint { // Bad
					Timestamp = DateTime.UtcNow.AddMinutes(-1),
					Value = 91
				},
				new NumericDataPoint { // Bad
					Timestamp = DateTime.UtcNow.AddMinutes(-2),
					Value = 99
				},
				new NumericDataPoint { // Bad
					Timestamp = DateTime.UtcNow.AddMinutes(-3),
					Value = 95
				},
				new NumericDataPoint { // Bad
					Timestamp = DateTime.UtcNow.AddMinutes(-4),
					Value = 98
				},
				new NumericDataPoint { // Bad
					Timestamp = DateTime.UtcNow.AddMinutes(-5),
					Value = 99
				},
				new NumericDataPoint { // Bad
					Timestamp = DateTime.UtcNow.AddMinutes(-6),
					Value = 98
				},
				new NumericDataPoint { // Bad
					Timestamp = DateTime.UtcNow.AddMinutes(-7),
					Value = 90
				}
			};

			// Act
			var actual = discrepancyService
				.FindHighLoadInDataPoints(
					input,
					new Metric
					{
						Type = Metrics.CpuLoad.AsInt(),
						Source = "the-source"
					}
				);

			// Assert
			Assert.Empty(actual);
		}

		[Fact]
		public void MultipleHighLoadInData()
		{
			// Arrange
			var mockConfig = new Mock<IConfiguration>();
			mockConfig
				.SetupGet(conf => conf["ServiceManager:DiscrepancyService:Load:Threshold"])
				.Returns(90.ToString());
			mockConfig
				.SetupGet(conf => conf["ServiceManager:DiscrepancyService:Load:MaxFailures"])
				.Returns(2.ToString()); // 3 is discrepancy

			var discrepancyService = new DiscrepancyService(
				new Mock<ILogger<DiscrepancyService>>().Object,
				new Mock<IDataContext>().Object,
				new Mock<INotificationService>().Object,
				mockConfig.Object
			);

			var input = new List<NumericDataPoint>() {
				new NumericDataPoint { // Good
					Timestamp = DateTime.UtcNow.AddMinutes(0),
					Value = 68
				},
				new NumericDataPoint { // Bad
					Timestamp = DateTime.UtcNow.AddMinutes(-1),
					Value = 91
				},
				new NumericDataPoint { // Bad
					Timestamp = DateTime.UtcNow.AddMinutes(-2),
					Value = 99
				},
				new NumericDataPoint { // Bad
					Timestamp = DateTime.UtcNow.AddMinutes(-3),
					Value = 95
				},
				new NumericDataPoint { // Good
					Timestamp = DateTime.UtcNow.AddMinutes(-4),
					Value = 68
				},
				new NumericDataPoint { // Bad
					Timestamp = DateTime.UtcNow.AddMinutes(-5),
					Value = 99
				},
				new NumericDataPoint { // Bad
					Timestamp = DateTime.UtcNow.AddMinutes(-6),
					Value = 98
				},
				new NumericDataPoint { // Bad
					Timestamp = DateTime.UtcNow.AddMinutes(-7),
					Value = 92
				},
				new NumericDataPoint { // Good
					Timestamp = DateTime.UtcNow.AddMinutes(-8),
					Value = 68
				}
			};

			var expected = new List<Discrepancy> {
				new Discrepancy
				{
					DateFirstOffense = input[3].Timestamp,
					Type = DiscrepancyType.HighLoad,
					MetricType = Metrics.CpuLoad,
					MetricSource = "the-source"
				},
				new Discrepancy
				{
					DateFirstOffense = input[7].Timestamp,
					Type = DiscrepancyType.HighLoad,
					MetricType = Metrics.CpuLoad,
					MetricSource = "the-source"
				}
			};

			// Act
			var actual = discrepancyService
				.FindHighLoadInDataPoints(
					input,
					new Metric
					{
						Type = Metrics.CpuLoad.AsInt(),
						Source = "the-source"
					}
				);

			// Assert
			Assert.Equal(expected, actual);
		}

		[Fact]
		public async Task FindsHighLoad()
		{
			// Arrange
			var mockConfig = new Mock<IConfiguration>();
			mockConfig
				.SetupGet(conf => conf["ServiceManager:DiscrepancyService:Load:Threshold"])
				.Returns(90.ToString());
			mockConfig
				.SetupGet(conf => conf["ServiceManager:DiscrepancyService:Load:MaxFailures"])
				.Returns(2.ToString()); // 3 is discrepancy
			mockConfig
				.SetupGet(conf => conf["ServiceManager:DiscrepancyService:DataTimeframe"])
				.Returns(1800.ToString());

			var context = _serviceProvider.GetRequiredService<IDataContext>();
			var metric = await context.Metrics.AddAsync(
				new Metric
				{
					Source = "the-source",
					Type = Metrics.CpuLoad.AsInt()
				}
			);
			var dataPoints = new List<NumericDataPoint>() {
				new NumericDataPoint { // Good
					Timestamp = DateTime.UtcNow.AddMinutes(0),
					Value = 68,
					Metric = metric.Entity
				},
				new NumericDataPoint { // Bad
					Timestamp = DateTime.UtcNow.AddMinutes(-1),
					Value = 91,
					Metric = metric.Entity
				},
				new NumericDataPoint { // Bad
					Timestamp = DateTime.UtcNow.AddMinutes(-2),
					Value = 99,
					Metric = metric.Entity
				},
				new NumericDataPoint { // Bad
					Timestamp = DateTime.UtcNow.AddMinutes(-3),
					Value = 95,
					Metric = metric.Entity
				},
				new NumericDataPoint { // Good
					Timestamp = DateTime.UtcNow.AddMinutes(-4),
					Value = 68,
					Metric = metric.Entity
				},
				new NumericDataPoint { // Bad
					Timestamp = DateTime.UtcNow.AddMinutes(-5),
					Value = 99,
					Metric = metric.Entity
				},
				new NumericDataPoint { // Bad
					Timestamp = DateTime.UtcNow.AddMinutes(-6),
					Value = 98,
					Metric = metric.Entity
				},
				new NumericDataPoint { // Good
					Timestamp = DateTime.UtcNow.AddMinutes(-7),
					Value = 68,
					Metric = metric.Entity
				}
			};

			await context.NumericDataPoints.AddRangeAsync(dataPoints);
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
					DateFirstOffense = dataPoints[3].Timestamp,
					Type = DiscrepancyType.HighLoad,
					MetricType = Metrics.CpuLoad,
					MetricSource = "the-source"
				}
			};

			// Act
			var actual = await discrepancyService
				.FindHighLoadsAsync(
					metric.Entity,
					new TimeSpan(0, 30, 0)
				);

			// Assert
			Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData(Metrics.Compilation, false)]
		[InlineData(Metrics.CpuLoad, true)]
		[InlineData(Metrics.Log, false)]
		[InlineData(Metrics.Ping, false)]
		[InlineData(Metrics.UserAction, false)]
		public async Task VerifyMetricForHighLoad(Metrics type, bool shouldSucceed)
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
					async () => await discrepancyService.FindHighLoadsAsync(metric, new TimeSpan())
				);
			}
			else
			{
				await Assert.ThrowsAsync<ArgumentException>(
					async () => await discrepancyService.FindHighLoadsAsync(metric, new TimeSpan())
				);
			}
		}
	}
}
