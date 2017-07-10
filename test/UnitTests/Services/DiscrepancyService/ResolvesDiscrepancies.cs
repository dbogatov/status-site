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
		public async Task ResolvesDiscrepanciesEmptyList()
		{
			// Arrange
			var discrepancyService = new DiscrepancyService(
				new Mock<ILogger<DiscrepancyService>>().Object,
				new Mock<IDataContext>().Object,
				new Mock<INotificationService>().Object,
				new Mock<IConfiguration>().Object
			);

			// Act
			var actual = await discrepancyService.ResolveDiscrepanciesAsync(new List<Discrepancy>());

			// Assert
			Assert.Empty(actual);
		}

		[Fact]
		public async Task ResolvesDiscrepanciesAlreadyResolved()
		{
			// Arrange
			var logger = new Mock<ILogger<DiscrepancyService>>();
			var context = _serviceProvider.GetRequiredService<IDataContext>();

			var discrepancyService = new DiscrepancyService(
				logger.Object,
				context,
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
			var actual = await discrepancyService.ResolveDiscrepanciesAsync(input);

			// Assert
			Assert.Equal(input, actual);
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
		public async Task ResolvesDiscrepancies()
		{
			// Arrange
			var input = new List<Discrepancy> {
				new Discrepancy {
					DateFirstOffense = DateTime.UtcNow.AddMinutes(-2),
					Type = DiscrepancyType.GapInData,
					MetricSource = "the-source-1",
					MetricType = Metrics.CpuLoad,
				}
			};

			var notifications = new Mock<INotificationService>();
			var context = _serviceProvider.GetRequiredService<IDataContext>();
			await context.Discrepancies.AddRangeAsync(input);
			await context.SaveChangesAsync();

			var discrepancyService = new DiscrepancyService(
				new Mock<ILogger<DiscrepancyService>>().Object,
				context,
				notifications.Object,
				new Mock<IConfiguration>().Object
			);

			// Act
			var actual = await discrepancyService.ResolveDiscrepanciesAsync(input);

			// Assert
			Assert.True(actual.First().Resolved);
			Assert.InRange(actual.First().DateResolved, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
			Assert.True(await context.Discrepancies.AnyAsync(d => d.Resolved));

			notifications
				.Verify(
					notif => notif
						.ScheduleNotificationAsync(
							It.Is<string>(msg => msg.Contains("resolve", StringComparison.OrdinalIgnoreCase)),
							NotificationSeverity.High
						)
				);
		}
	}
}
