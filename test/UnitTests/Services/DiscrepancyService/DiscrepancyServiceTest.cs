using System;
using Xunit;
using StatusMonitor.Shared.Models.Entities;
using StatusMonitor.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using StatusMonitor.Shared.Extensions;
using System.Net;
using StatusMonitor.Daemons.Services;
using Moq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StatusMonitor.Shared.Services.Factories;
using System.Net.Http;
using StatusMonitor.Tests.Mock;
using Microsoft.Extensions.Logging;
using StatusMonitor.Shared.Models;
using System.Collections.Generic;
using System.Linq;

namespace StatusMonitor.Tests.UnitTests.Services
{
	public partial class DiscrepancyServiceTest
	{
		private readonly IServiceProvider _serviceProvider;

		public DiscrepancyServiceTest()
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
	}
}
