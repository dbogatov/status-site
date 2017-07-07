using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;
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
		public async Task WarnsAboutResolvedDiscrepanciesInResolve()
		{
			// Arrange
			var logger = new Mock<ILogger<DiscrepancyService>>();

			var discrepancyService = new DiscrepancyService(
				logger.Object,
				new Mock<IDataContext>().Object,
				new Mock<INotificationService>().Object,
				new Mock<IConfiguration>().Object
			);

			var input = new List<Discrepancy> {
				new Discrepancy {
					DateFirstOffense = DateTime.UtcNow,
					Type = DiscrepancyType.GapInData,
					MetricSource = "the-source-1",
					MetricType = Metrics.CpuLoad,
					Resolved = true
				},
				new Discrepancy {
					DateFirstOffense = DateTime.UtcNow,
					Type = DiscrepancyType.GapInData,
					MetricSource = "the-source-2",
					MetricType = Metrics.CpuLoad,
					Resolved = true
				}
			};

			// Act
			var actual = await discrepancyService.FindResolvedDiscrepanciesAsync(input);

			// Assert
			Assert.Empty(actual);
			logger
				.Verify(
					log => log.Log(
						LogLevel.Warning,
						It.IsAny<EventId>(),
						It.IsAny<FormattedLogValues>(),
						It.IsAny<Exception>(),
						It.IsAny<Func<object, Exception, string>>()
					),
					Times.Exactly(2)
				);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task ResolvesHighLoad(bool shouldResolve)
		{
			// Arrange
			var input = new List<Discrepancy> {
				new Discrepancy {
					DateFirstOffense = DateTime.UtcNow,
					Type = DiscrepancyType.HighLoad,
					MetricSource = "the-source-1",
					MetricType = Metrics.CpuLoad,
				}
			};

			var metric = new Metric
			{
				Type = Metrics.CpuLoad.AsInt(),
				Source = "the-source-1"
			};

			var context = _serviceProvider.GetRequiredService<IDataContext>();
			await context.Metrics.AddAsync(metric);
			await context.NumericDataPoints.AddRangeAsync(
				new NumericDataPoint
				{
					Metric = metric,
					Value = 50,
					Timestamp = DateTime.UtcNow.AddMinutes(-1)
				},
				new NumericDataPoint
				{
					Metric = metric,
					Value = 95,
					Timestamp = DateTime.UtcNow.AddMinutes(1)
				},
				new NumericDataPoint
				{
					Metric = metric,
					Value = 91,
					Timestamp = DateTime.UtcNow.AddMinutes(2)
				},
				new NumericDataPoint
				{
					Metric = metric,
					Value = shouldResolve ? 85 : 92,
					Timestamp = DateTime.UtcNow.AddMinutes(3)
				}
			);
			await context.SaveChangesAsync();

			var config = new Mock<IConfiguration>();
			config
				.SetupGet(conf => conf["ServiceManager:DiscrepancyService:Load:Threshold"])
				.Returns(90.ToString());

			var discrepancyService = new DiscrepancyService(
				new Mock<ILogger<DiscrepancyService>>().Object,
				context,
				new Mock<INotificationService>().Object,
				config.Object
			);

			// Act
			var actual = await discrepancyService.FindResolvedDiscrepanciesAsync(input);

			// Assert
			if (shouldResolve)
			{
				Assert.NotEmpty(actual);
				Assert.Equal(input, actual);
			}
			else
			{
				Assert.Empty(actual);
			}
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task ResolvesGapInData(bool shouldResolve)
		{
			// Arrange
			var input = new List<Discrepancy> {
				new Discrepancy {
					DateFirstOffense = DateTime.UtcNow,
					Type = DiscrepancyType.GapInData,
					MetricSource = "the-source-1",
					MetricType = Metrics.CpuLoad,
				}
			};

			var metric = new Metric
			{
				Type = Metrics.CpuLoad.AsInt(),
				Source = "the-source-1"
			};

			var context = _serviceProvider.GetRequiredService<IDataContext>();
			await context.Metrics.AddAsync(metric);
			await context.NumericDataPoints.AddRangeAsync(
				new NumericDataPoint
				{
					Metric = metric,
					Value = 50,
					Timestamp = DateTime.UtcNow.AddMinutes(-2)
				},
				new NumericDataPoint
				{
					Metric = metric,
					Value = 92,
					Timestamp = DateTime.UtcNow.AddMinutes(shouldResolve ? 1 : -1)
				}
			);
			await context.SaveChangesAsync();

			var discrepancyService = new DiscrepancyService(
				new Mock<ILogger<DiscrepancyService>>().Object,
				context,
				new Mock<INotificationService>().Object,
				new Mock<IConfiguration>().Object
			);

			// Act
			var actual = await discrepancyService.FindResolvedDiscrepanciesAsync(input);

			// Assert
			if (shouldResolve)
			{
				Assert.NotEmpty(actual);
				Assert.Equal(input, actual);
			}
			else
			{
				Assert.Empty(actual);
			}
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task ResolvesPingFailure(bool shouldResolve)
		{
			// Arrange
			var input = new List<Discrepancy> {
				new Discrepancy {
					DateFirstOffense = DateTime.UtcNow,
					Type = DiscrepancyType.PingFailedNTimes,
					MetricSource = "lolchik.com",
					MetricType = Metrics.Ping,
				}
			};

			var metric = new Metric
			{
				Type = Metrics.Ping.AsInt(),
				Source = "lolchik.com"
			};

			var context = _serviceProvider.GetRequiredService<IDataContext>();
			await context.Metrics.AddAsync(metric);
			await context.PingDataPoints.AddRangeAsync(
				new PingDataPoint
				{
					Metric = metric,
					HttpStatusCode = HttpStatusCode.OK.AsInt(),
					Timestamp = DateTime.UtcNow.AddMinutes(-1)
				},
				new PingDataPoint
				{
					Metric = metric,
					HttpStatusCode = HttpStatusCode.ServiceUnavailable.AsInt(),
					Timestamp = DateTime.UtcNow.AddMinutes(1)
				},
				new PingDataPoint
				{
					Metric = metric,
					HttpStatusCode = HttpStatusCode.ServiceUnavailable.AsInt(),
					Timestamp = DateTime.UtcNow.AddMinutes(2)
				},
				new PingDataPoint
				{
					Metric = metric,
					HttpStatusCode = (shouldResolve ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable).AsInt(),
					Timestamp = DateTime.UtcNow.AddMinutes(3)
				}
			);
			await context.SaveChangesAsync();

			var discrepancyService = new DiscrepancyService(
				new Mock<ILogger<DiscrepancyService>>().Object,
				context,
				new Mock<INotificationService>().Object,
				new Mock<IConfiguration>().Object
			);

			// Act
			var actual = await discrepancyService.FindResolvedDiscrepanciesAsync(input);

			// Assert
			if (shouldResolve)
			{
				Assert.NotEmpty(actual);
				Assert.Equal(input, actual);
			}
			else
			{
				Assert.Empty(actual);
			}
		}
	}
}
