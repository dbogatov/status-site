using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
		public void SinglePingFailure()
		{
			// Arrange
			var discrepancyService = new DiscrepancyService(
				new Mock<ILogger<DiscrepancyService>>().Object,
				new Mock<IDataContext>().Object,
				new Mock<INotificationService>().Object,
				new Mock<IConfiguration>().Object
			);

			var input = new List<PingDataPoint>() {
				new PingDataPoint {
					Success = true,
					Timestamp = DateTime.UtcNow - new TimeSpan(0, 0, 0)
				},
				new PingDataPoint {
					Success = false,
					Timestamp = DateTime.UtcNow - new TimeSpan(0, 1, 0)
				},
				new PingDataPoint {
					Success = false,
					Timestamp = DateTime.UtcNow - new TimeSpan(0, 2, 0)
				},
				new PingDataPoint {
					Success = true,
					Timestamp = DateTime.UtcNow - new TimeSpan(0, 3, 0)
				},
				new PingDataPoint {
					Success = true,
					Timestamp = DateTime.UtcNow - new TimeSpan(0, 4, 0)
				},
			};

			var expected = new List<Discrepancy> {
				new Discrepancy
				{
					DateFirstOffense = input[2].Timestamp,
					Type = DiscrepancyType.PingFailedNTimes,
					MetricType = Metrics.Ping,
					MetricSource = "the-source"
				}
			};

			// Act
			var actual = discrepancyService
				.FindPingFailuresFromDataPoints(
					input,
					new PingSetting { MaxFailures = 1 },
					new Metric
					{
						Type = Metrics.Ping.AsInt(),
						Source = "the-source"
					}
				);

			// Assert
			Assert.Equal(expected, actual);
		}

		[Fact]
		public void PingFailureNoData()
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
				.FindPingFailuresFromDataPoints(
					new List<PingDataPoint>(),
					new PingSetting { MaxFailures = 1 },
					new Metric
					{
						Type = Metrics.Ping.AsInt(),
						Source = "the-source"
					}
				);

			// Assert
			Assert.Empty(actual);
		}

		[Fact]
		public void PermanentPingFailure()
		{
			// Arrange
			var discrepancyService = new DiscrepancyService(
				new Mock<ILogger<DiscrepancyService>>().Object,
				new Mock<IDataContext>().Object,
				new Mock<INotificationService>().Object,
				new Mock<IConfiguration>().Object
			);

			var input = new List<PingDataPoint>() {
				new PingDataPoint {
					Success = false,
					Timestamp = DateTime.UtcNow - new TimeSpan(0, 0, 0)
				},
				new PingDataPoint {
					Success = false,
					Timestamp = DateTime.UtcNow - new TimeSpan(0, 1, 0)
				},
				new PingDataPoint {
					Success = false,
					Timestamp = DateTime.UtcNow - new TimeSpan(0, 2, 0)
				},
				new PingDataPoint {
					Success = false,
					Timestamp = DateTime.UtcNow - new TimeSpan(0, 3, 0)
				},
				new PingDataPoint {
					Success = false,
					Timestamp = DateTime.UtcNow - new TimeSpan(0, 4, 0)
				},
			};

			// Act
			var actual = discrepancyService
				.FindPingFailuresFromDataPoints(
					input,
					new PingSetting { MaxFailures = 1 },
					new Metric
					{
						Type = Metrics.Ping.AsInt(),
						Source = "the-source"
					}
				);

			// Assert
			Assert.Empty(actual);
		}

		[Fact]
		public void NoPingFailures()
		{
			// Arrange
			var discrepancyService = new DiscrepancyService(
				new Mock<ILogger<DiscrepancyService>>().Object,
				new Mock<IDataContext>().Object,
				new Mock<INotificationService>().Object,
				new Mock<IConfiguration>().Object
			);

			var input = new List<PingDataPoint>() {
				new PingDataPoint {
					Success = true,
					Timestamp = DateTime.UtcNow - new TimeSpan(0, 0, 0)
				},
				new PingDataPoint {
					Success = true,
					Timestamp = DateTime.UtcNow - new TimeSpan(0, 1, 0)
				},
				new PingDataPoint {
					Success = false,
					Timestamp = DateTime.UtcNow - new TimeSpan(0, 2, 0)
				},
				new PingDataPoint {
					Success = true,
					Timestamp = DateTime.UtcNow - new TimeSpan(0, 3, 0)
				},
				new PingDataPoint {
					Success = true,
					Timestamp = DateTime.UtcNow - new TimeSpan(0, 4, 0)
				},
			};

			// Act
			var actual = discrepancyService
				.FindPingFailuresFromDataPoints(
					input,
					new PingSetting { MaxFailures = 1 },
					new Metric
					{
						Type = Metrics.Ping.AsInt(),
						Source = "the-source"
					}
				);

			// Assert
			Assert.Empty(actual);
		}

		[Fact]
		public void MultiplePingFailures()
		{
			// Arrange
			var discrepancyService = new DiscrepancyService(
				new Mock<ILogger<DiscrepancyService>>().Object,
				new Mock<IDataContext>().Object,
				new Mock<INotificationService>().Object,
				new Mock<IConfiguration>().Object
			);

			var input = new List<PingDataPoint>() {
				new PingDataPoint {
					Success = true,
					Timestamp = DateTime.UtcNow.AddMinutes(0)
				},
				new PingDataPoint {
					Success = false,
					Timestamp = DateTime.UtcNow.AddMinutes(-1)
				},
				new PingDataPoint {
					Success = false,
					Timestamp = DateTime.UtcNow.AddMinutes(-2)
				},
				new PingDataPoint {
					Success = true,
					Timestamp = DateTime.UtcNow.AddMinutes(-3)
				},
				new PingDataPoint {
					Success = false,
					Timestamp = DateTime.UtcNow.AddMinutes(-4)
				},
				new PingDataPoint {
					Success = false,
					Timestamp = DateTime.UtcNow.AddMinutes(-5)
				},
				new PingDataPoint {
					Success = true,
					Timestamp = DateTime.UtcNow.AddMinutes(-6)
				}
			};

			var expected = new List<Discrepancy> {
				new Discrepancy
				{
					DateFirstOffense = input[5].Timestamp,
					Type = DiscrepancyType.PingFailedNTimes,
					MetricType = Metrics.Ping,
					MetricSource = "the-source"
				},
				new Discrepancy
				{
					DateFirstOffense = input[2].Timestamp,
					Type = DiscrepancyType.PingFailedNTimes,
					MetricType = Metrics.Ping,
					MetricSource = "the-source"
				}
			}
			.OrderBy(d => d.DateFirstOffense); ;

			// Act
			var actual = discrepancyService
				.FindPingFailuresFromDataPoints(
					input,
					new PingSetting { MaxFailures = 0 },
					new Metric
					{
						Type = Metrics.Ping.AsInt(),
						Source = "the-source"
					}
				)
				.OrderBy(d => d.DateFirstOffense); ;

			// Assert
			Assert.Equal(expected, actual);
		}

		[Fact]
		public void PingFailuresDataStartsWithFailure()
		{
			// Arrange
			var discrepancyService = new DiscrepancyService(
				new Mock<ILogger<DiscrepancyService>>().Object,
				new Mock<IDataContext>().Object,
				new Mock<INotificationService>().Object,
				new Mock<IConfiguration>().Object
			);

			var input = new List<PingDataPoint>() {
				new PingDataPoint {
					Success = true,
					Timestamp = DateTime.UtcNow.AddMinutes(0)
				},
				new PingDataPoint {
					Success = false,
					Timestamp = DateTime.UtcNow.AddMinutes(-1)
				},
				new PingDataPoint {
					Success = false,
					Timestamp = DateTime.UtcNow.AddMinutes(-2)
				},
				new PingDataPoint {
					Success = true,
					Timestamp = DateTime.UtcNow.AddMinutes(-3)
				},
				new PingDataPoint {
					Success = false,
					Timestamp = DateTime.UtcNow.AddMinutes(-4)
				},
				new PingDataPoint {
					Success = false,
					Timestamp = DateTime.UtcNow.AddMinutes(-5)
				}
			};

			var expected = new List<Discrepancy> {
				new Discrepancy
				{
					DateFirstOffense = input[2].Timestamp,
					Type = DiscrepancyType.PingFailedNTimes,
					MetricType = Metrics.Ping,
					MetricSource = "the-source"
				}
			}
			.OrderBy(d => d.DateFirstOffense); ;

			// Act
			var actual = discrepancyService
				.FindPingFailuresFromDataPoints(
					input,
					new PingSetting { MaxFailures = 0 },
					new Metric
					{
						Type = Metrics.Ping.AsInt(),
						Source = "the-source"
					}
				)
				.OrderBy(d => d.DateFirstOffense); ;

			// Assert
			Assert.Equal(expected, actual);
		}

		[Fact]
		public async Task FindsPingFailure()
		{
			// Arrange
			var mockConfig = new Mock<IConfiguration>();
			mockConfig
				.SetupGet(conf => conf["ServiceManager:DiscrepancyService:DataTimeframe"])
				.Returns(1800.ToString());

			var context = _serviceProvider.GetRequiredService<IDataContext>();
			var metric = await context.Metrics.AddAsync(
				new Metric
				{
					Source = "lolchik.com",
					Type = Metrics.Ping.AsInt()
				}
			);
			var dataPoints = new List<PingDataPoint> {
				new PingDataPoint {
					Timestamp = DateTime.UtcNow.AddMinutes(0),
					Metric = metric.Entity,
					Success = true
				},
				new PingDataPoint {
					Timestamp = DateTime.UtcNow.AddMinutes(-1),
					Metric = metric.Entity,
					Success = false
				},
				new PingDataPoint {
					Timestamp = DateTime.UtcNow.AddMinutes(-2),
					Metric = metric.Entity,
					Success = false
				},
				new PingDataPoint {
					Timestamp = DateTime.UtcNow.AddMinutes(-3),
					Metric = metric.Entity,
					Success = true
				},
				new PingDataPoint {
					Timestamp = DateTime.UtcNow.AddMinutes(-4),
					Metric = metric.Entity,
					Success = true
				}
			};
			await context.PingDataPoints.AddRangeAsync(dataPoints);
			await context.PingSettings.AddAsync(
				new PingSetting
				{
					ServerUrl = "https://lolchik.com",
					MaxFailures = 1
				}
			);
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
					Type = DiscrepancyType.PingFailedNTimes,
					MetricType = Metrics.Ping,
					MetricSource = "lolchik.com"
				}
			};

			// Act
			var actual = await discrepancyService
				.FindPingFailuresAsync(
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
		[InlineData(Metrics.Ping, true)]
		[InlineData(Metrics.UserAction, false)]
		[InlineData(Metrics.Health, false)]
		public async Task VerifyMetricForPings(Metrics type, bool shouldSucceed)
		{
			// Arrange
			var metric = new Metric
			{
				Type = type.AsInt(),
				Source = "lolchik.com"
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
					async () => await discrepancyService.FindPingFailuresAsync(metric, new TimeSpan())
				);
			}
			else
			{
				await Assert.ThrowsAsync<ArgumentException>(
					async () => await discrepancyService.FindPingFailuresAsync(metric, new TimeSpan())
				);
			}
		}
	}
}
