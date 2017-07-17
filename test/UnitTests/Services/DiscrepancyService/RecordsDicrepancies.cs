using System;
using System.Collections.Generic;
using System.Linq;
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
		public async Task RecordsDiscrepancies()
		{
			// Arrange
			var twoDaysAgo = DateTime.UtcNow.AddDays(-2);
			var hourAgo = DateTime.UtcNow.AddHours(-1);
			var now = DateTime.UtcNow;

			var context = _serviceProvider.GetRequiredService<IDataContext>();
			await context.Discrepancies.AddRangeAsync(
				new List<Discrepancy> {
					new Discrepancy {
						DateFirstOffense = now,
						Type = DiscrepancyType.GapInData,
						MetricSource = "the-source",
						MetricType = Metrics.CpuLoad
					},
					new Discrepancy {
						DateFirstOffense = hourAgo,
						Type = DiscrepancyType.PingFailedNTimes,
						MetricSource = "the-source",
						MetricType = Metrics.Ping
					},
					new Discrepancy {
						DateFirstOffense = twoDaysAgo,
						Type = DiscrepancyType.GapInData,
						MetricSource = "the-source",
						MetricType = Metrics.CpuLoad
					}
				}
			);
			await context.SaveChangesAsync();

			var mockNotifications = new Mock<INotificationService>();

			var discrepancyService = new DiscrepancyService(
				new Mock<ILogger<DiscrepancyService>>().Object,
				context,
				mockNotifications.Object,
				new Mock<IConfiguration>().Object
			);

			var input = new List<Discrepancy> {
				new Discrepancy { // already exists
					DateFirstOffense = now,
					Type = DiscrepancyType.GapInData,
					MetricSource = "the-source",
					MetricType = Metrics.CpuLoad
				},
				new Discrepancy { // new
					DateFirstOffense = hourAgo,
					Type = DiscrepancyType.PingFailedNTimes,
					MetricSource = "the-other-source",
					MetricType = Metrics.Ping
				},
				new Discrepancy { // new
					DateFirstOffense = twoDaysAgo.AddHours(1),
					Type = DiscrepancyType.GapInData,
					MetricSource = "the-source",
					MetricType = Metrics.CpuLoad
				}
			};

			var expected = new List<Discrepancy> {
				input[1],
				input[2]
			};

			// Act
			var actual = await discrepancyService.RecordDiscrepanciesAsync(input);

			// Assert
			Assert.Equal(
				expected.OrderBy(d => d.DateFirstOffense),
				actual.OrderBy(d => d.DateFirstOffense)
			);
			mockNotifications
				.Verify(
					n => n.ScheduleNotificationAsync(
						It.IsAny<string>(),
						NotificationSeverity.High
					),
					Times.Exactly(expected.Count)
				);
			Assert.Equal(5, context.Discrepancies.Count());
		}

		[Fact]
		public async Task WarnsAboutResolvedDiscrepanciesInRecord()
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
			var actual = await discrepancyService.RecordDiscrepanciesAsync(input);

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

		[Fact]
		public async Task RecordsDiscrepanciesEmptyList()
		{
			// Arrange
			var discrepancyService = new DiscrepancyService(
				new Mock<ILogger<DiscrepancyService>>().Object,
				new Mock<IDataContext>().Object,
				new Mock<INotificationService>().Object,
				new Mock<IConfiguration>().Object
			);

			// Act
			var actual = await discrepancyService.RecordDiscrepanciesAsync(new List<Discrepancy>());

			// Assert
			Assert.Empty(actual);
		}

		[Fact]
		public async Task UsesCorrectTimeZone()
		{
			// Arrange
			var config = new Mock<IConfiguration>();
			config
				.SetupGet(conf => conf["ServiceManager:NotificationService:TimeZone"])
				.Returns("Asia/Kabul");

			var date = DateTime.SpecifyKind(new DateTime(2017, 07, 14, 18, 25, 43), DateTimeKind.Utc);

			var context = _serviceProvider.GetRequiredService<IDataContext>();
			var mockNotifications = new Mock<INotificationService>();

			var discrepancyService = new DiscrepancyService(
				new Mock<ILogger<DiscrepancyService>>().Object,
				context,
				mockNotifications.Object,
				config.Object
			);

			var input = new List<Discrepancy> {
				new Discrepancy {
					DateFirstOffense = date,
					Type = DiscrepancyType.GapInData,
					MetricSource = "the-source",
					MetricType = Metrics.CpuLoad
				}
			};

			// Act
			await discrepancyService.RecordDiscrepanciesAsync(input);

			// Assert
			mockNotifications
				.Verify(
					n => n.ScheduleNotificationAsync(
						It.Is<string>(s => s.Contains(date.ToStringUsingTimeZone("Asia/Kabul"))),
						NotificationSeverity.High
					)
				);
		}
	}
}
