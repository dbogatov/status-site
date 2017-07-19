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
		public void SingleGapInData()
		{
			// Arrange
			var mockConfig = new Mock<IConfiguration>();
			mockConfig
				.SetupGet(conf => conf["ServiceManager:DiscrepancyService:Gaps:MaxDifference"])
				.Returns("60");

			var discrepancyService = new DiscrepancyService(
				new Mock<ILogger<DiscrepancyService>>().Object,
				new Mock<IDataContext>().Object,
				new Mock<INotificationService>().Object,
				mockConfig.Object
			);

			var input = new List<DateTime>() {
				DateTime.UtcNow - new TimeSpan(0, 0, 0),
				DateTime.UtcNow - new TimeSpan(0, 1, 0),
				DateTime.UtcNow - new TimeSpan(0, 2, 0),
				DateTime.UtcNow - new TimeSpan(0, 4, 0),
				DateTime.UtcNow - new TimeSpan(0, 5, 0)
			};

			var expected = new List<Discrepancy> {
				new Discrepancy
				{
					DateFirstOffense = input[3],
					Type = DiscrepancyType.GapInData,
					MetricType = Metrics.CpuLoad,
					MetricSource = "the-source"
				}
			};

			// Act
			var actual = discrepancyService
				.FindGapsInDataPoints(
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
		public void GapNoData()
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
				.FindGapsInDataPoints(
					new List<DateTime>(),
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
		public void NoGapsInData()
		{
			// Arrange
			var mockConfig = new Mock<IConfiguration>();
			mockConfig
				.SetupGet(conf => conf["ServiceManager:DiscrepancyService:Gaps:MaxDifference"])
				.Returns("60");

			var discrepancyService = new DiscrepancyService(
				new Mock<ILogger<DiscrepancyService>>().Object,
				new Mock<IDataContext>().Object,
				new Mock<INotificationService>().Object,
				mockConfig.Object
			);

			var input = new List<DateTime>() {
				DateTime.UtcNow - new TimeSpan(0, 0, 0),
				DateTime.UtcNow - new TimeSpan(0, 1, 0),
				DateTime.UtcNow - new TimeSpan(0, 2, 0),
				DateTime.UtcNow - new TimeSpan(0, 3, 0),
				DateTime.UtcNow - new TimeSpan(0, 4, 0),
				DateTime.UtcNow - new TimeSpan(0, 5, 0)
			};

			// Act
			var actual = discrepancyService
				.FindGapsInDataPoints(
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
		public void MultipleGapsInData()
		{
			// Arrange
			var mockConfig = new Mock<IConfiguration>();
			mockConfig
				.SetupGet(conf => conf["ServiceManager:DiscrepancyService:Gaps:MaxDifference"])
				.Returns("60");

			var discrepancyService = new DiscrepancyService(
				new Mock<ILogger<DiscrepancyService>>().Object,
				new Mock<IDataContext>().Object,
				new Mock<INotificationService>().Object,
				mockConfig.Object
			);

			var input = new List<DateTime>() {
				DateTime.UtcNow - new TimeSpan(0, 0, 0),
				DateTime.UtcNow - new TimeSpan(0, 1, 0),
				DateTime.UtcNow - new TimeSpan(0, 4, 0),
				DateTime.UtcNow - new TimeSpan(0, 5, 0),
				DateTime.UtcNow - new TimeSpan(0, 7, 0)
			};

			var expected = new List<Discrepancy> {
				new Discrepancy
				{
					DateFirstOffense = input[2],
					Type = DiscrepancyType.GapInData,
					MetricType = Metrics.CpuLoad,
					MetricSource = "the-source"
				},
				new Discrepancy
				{
					DateFirstOffense = input[4],
					Type = DiscrepancyType.GapInData,
					MetricType = Metrics.CpuLoad,
					MetricSource = "the-source"
				}
			}
			.OrderBy(d => d.DateFirstOffense);

			// Act
			var actual = discrepancyService
				.FindGapsInDataPoints(
					input,
					new Metric
					{
						Type = Metrics.CpuLoad.AsInt(),
						Source = "the-source"
					}
				)
				.OrderBy(d => d.DateFirstOffense);

			// Assert
			Assert.Equal(expected, actual);
		}

		[Fact]
		public async Task FindsGap()
		{
			// Arrange
			var mockConfig = new Mock<IConfiguration>();
			mockConfig
				.SetupGet(conf => conf["ServiceManager:DiscrepancyService:Gaps:MaxDifference"])
				.Returns(60.ToString());
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
			var dataPoints = new List<NumericDataPoint> {
				new NumericDataPoint {
					Timestamp = DateTime.UtcNow.AddMinutes(0),
					Metric = metric.Entity
				},
				new NumericDataPoint {
					Timestamp = DateTime.UtcNow.AddMinutes(-1),
					Metric = metric.Entity
				},
				new NumericDataPoint {
					Timestamp = DateTime.UtcNow.AddMinutes(-2),
					Metric = metric.Entity
				},
				new NumericDataPoint {
					Timestamp = DateTime.UtcNow.AddMinutes(-4),
					Metric = metric.Entity
				},
				new NumericDataPoint {
					Timestamp = DateTime.UtcNow.AddMinutes(-5),
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
					Type = DiscrepancyType.GapInData,
					MetricType = Metrics.CpuLoad,
					MetricSource = "the-source"
				}
			};

			// Act
			var actual = await discrepancyService
				.FindGapsAsync(
					metric.Entity,
					new TimeSpan(0, 30, 0)
				);

			// Assert
			Assert.Equal(expected, actual);
		}

		[Fact]
		public async Task ReportsGapIfServerIsDown()
		{
			// Arrange
			var mockConfig = new Mock<IConfiguration>();
			mockConfig
				.SetupGet(conf => conf["ServiceManager:DiscrepancyService:Gaps:MaxDifference"])
				.Returns(60.ToString());
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
			var dataPoints = new List<NumericDataPoint> {
				new NumericDataPoint {
					Timestamp = DateTime.UtcNow.AddMinutes(-2),
					Metric = metric.Entity
				},
				new NumericDataPoint {
					Timestamp = DateTime.UtcNow.AddMinutes(-3),
					Metric = metric.Entity
				},
				new NumericDataPoint {
					Timestamp = DateTime.UtcNow.AddMinutes(-4),
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
					DateFirstOffense = dataPoints[0].Timestamp,
					Type = DiscrepancyType.GapInData,
					MetricType = Metrics.CpuLoad,
					MetricSource = "the-source"
				}
			};

			// Act
			var actual = await discrepancyService
				.FindGapsAsync(
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
		public async Task VerifyMetricForGaps(Metrics type, bool shouldSucceed)
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

			// Act & Assert
			if (shouldSucceed)
			{
				// It will pass metric check, will try to interact with data layer and fail
				await Assert.ThrowsAsync<NullReferenceException>(
					async () => await discrepancyService.FindGapsAsync(metric, new TimeSpan())
				);
			}
			else
			{
				await Assert.ThrowsAsync<ArgumentException>(
					async () => await discrepancyService.FindGapsAsync(metric, new TimeSpan())
				);
			}
		}

	}
}
