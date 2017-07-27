using System;
using Xunit;
using StatusMonitor.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System.Threading;
using StatusMonitor.Daemons.Services;
using Moq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Shared.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using StatusMonitor.Shared.Models.Entities;
using Microsoft.Extensions.Logging.Internal;

namespace StatusMonitor.Tests.UnitTests.Services
{
	public class ServiceManagerServiceTest
	{
		private readonly Mock<IServiceProvider> _mockServiceProvider = new Mock<IServiceProvider>();

		private readonly Mock<INotificationService> _mockNotificationService = new Mock<INotificationService>();
		private readonly Mock<ICleanService> _mockCleanService = new Mock<ICleanService>();
		private readonly Mock<ICacheService> _mockCacheService = new Mock<ICacheService>();
		private readonly Mock<IPingService> _mockPingService = new Mock<IPingService>();
		private readonly Mock<IDiscrepancyService> _mockDiscrepancyService = new Mock<IDiscrepancyService>();
		private readonly Mock<IDemoService> _mockDemoService = new Mock<IDemoService>();
		private readonly Mock<IHealthService> _mockHealthService = new Mock<IHealthService>();

		private readonly Mock<ILogger<ServiceManagerService>> _mockLog = new Mock<ILogger<ServiceManagerService>>();
		private readonly Mock<IConfiguration> _config = new Mock<IConfiguration>();

		private readonly List<Metric> _testMetrics = new List<Metric> {
			new Metric {
				Type = Metrics.CpuLoad.AsInt(),
				Source = "the-source",
				AutoLabel = new AutoLabel { Id = AutoLabels.Normal.AsInt() }
			},
			new Metric {
				Type = Metrics.Ping.AsInt(),
				Source = "the.url.com",
				AutoLabel = new AutoLabel { Id = AutoLabels.Normal.AsInt() }
			}
		};

		public ServiceManagerServiceTest()
		{
			// Arrange 

			_mockCacheService
				.Setup(cache => cache.CacheMetricAsync(It.IsAny<Metric>()))
				.ReturnsAsync(_testMetrics[0]);

			_mockPingService
				.Setup(ping => ping.PingServerAsync(It.IsAny<PingSetting>()))
				.ReturnsAsync(new PingDataPoint());

			_mockDiscrepancyService
				.Setup(d => d.FindResolvedDiscrepanciesAsync(It.IsAny<IEnumerable<Discrepancy>>()))
				.ReturnsAsync(new List<Discrepancy>());

			_mockHealthService
				.Setup(health => health.ProduceHealthReportAsync())
				.ReturnsAsync(new HealthReport());

			_mockServiceProvider
				.Setup(provider => provider.GetService(typeof(IDataContext)))
				.Returns(GenerateNewDataContext());
			_mockServiceProvider
				.Setup(provider => provider.GetService(typeof(INotificationService)))
				.Returns(_mockNotificationService.Object);
			_mockServiceProvider
				.Setup(provider => provider.GetService(typeof(ICleanService)))
				.Returns(_mockCleanService.Object);
			_mockServiceProvider
				.Setup(provider => provider.GetService(typeof(ICacheService)))
				.Returns(_mockCacheService.Object);
			_mockServiceProvider
				.Setup(provider => provider.GetService(typeof(IPingService)))
				.Returns(_mockPingService.Object);
			_mockServiceProvider
				.Setup(provider => provider.GetService(typeof(IDiscrepancyService)))
				.Returns(_mockDiscrepancyService.Object);
			_mockServiceProvider
				.Setup(provider => provider.GetService(typeof(IDemoService)))
				.Returns(_mockDemoService.Object);
			_mockServiceProvider
				.Setup(provider => provider.GetService(typeof(IHealthService)))
				.Returns(_mockHealthService.Object);

			var mockServiceScope = new Mock<IServiceScope>();
			mockServiceScope
				.SetupGet(scope => scope.ServiceProvider)
				.Returns(_mockServiceProvider.Object);

			var mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
			mockServiceScopeFactory
				.Setup(factory => factory.CreateScope())
				.Returns(mockServiceScope.Object);

			_mockServiceProvider
				.Setup(provider => provider.GetService(typeof(IServiceScopeFactory)))
				.Returns(mockServiceScopeFactory.Object);


			foreach (var service in Enum.GetValues(typeof(ServiceManagerServices)).Cast<ServiceManagerServices>())
			{
				_config
					.SetupGet(conf => conf[$"ServiceManager:{service.ToString()}Service:Interval"])
					.Returns(1.ToString());
			}

			_config
				.SetupGet(conf => conf["ServiceManager:DemoService:Gaps:Enabled"])
				.Returns(false.ToString());
		}

		[Fact]
		public async Task StartsAndStopsServices()
		{
			// Arrange
			EnableServices();

			var serviceManagerService = new ServiceManagerService(
				_mockLog.Object,
				_mockServiceProvider.Object,
				_config.Object
			);

			// Act
			var task = serviceManagerService.StartServices();

			// Let it gracefully start services
			await Task.Delay(1000);

			// Record status
			var waitingForActivation = task.Status;

			// Gracefully stop services
			serviceManagerService.StopServices();

			// Assert

			// Let it finish its job
			// Check that services are stopped
			// If not done in 30 seconds, consider timeout
			Assert.Equal(await Task.WhenAny(task, Task.Delay(new TimeSpan(0, 0, 30))), task);

			Assert.Equal(TaskStatus.WaitingForActivation, waitingForActivation);

			_mockLog
				.Verify(
					log => log.Log(
						LogLevel.Information,
						It.IsAny<EventId>(),
						It.Is<FormattedLogValues>(
							v => v
								.ToString()
								.Contains(
									"All services stopped",
									StringComparison.OrdinalIgnoreCase
								)
						),
						It.IsAny<Exception>(),
						It.IsAny<Func<object, Exception, string>>()
					)
				);
		}

		// [Theory(Skip = "Unstable test, needs investigation")]
		[Theory]
		[InlineData(ServiceManagerServices.Cache)]
		[InlineData(ServiceManagerServices.Clean)]
		[InlineData(ServiceManagerServices.Ping)]
		[InlineData(ServiceManagerServices.Demo)]
		[InlineData(ServiceManagerServices.Discrepancy)]
		[InlineData(ServiceManagerServices.Notification)]
		[InlineData(ServiceManagerServices.Health)]
		public async Task RunsOnlyIfEnabled(ServiceManagerServices service)
		{
			// Arrange
			EnableServices(service);

			var serviceManagerService = new ServiceManagerService(
				_mockLog.Object,
				_mockServiceProvider.Object,
				_config.Object
			);

			// Act
			var task = serviceManagerService.StartServices();
			await Task.Delay(3000);
			serviceManagerService.StopServices();

			// Let it finish its job
			// Check that services are stopped
			// If not done in 30 seconds, consider timeout
			Assert.Equal(await Task.WhenAny(task, Task.Delay(new TimeSpan(0, 0, 30))), task);

			// Assert

			// Cache
			_mockCacheService.Verify(
				cache => cache.CacheMetricAsync(It.IsAny<Metric>()),
				service == ServiceManagerServices.Cache ? Times.AtLeastOnce() : Times.Never()
			);

			// Clean
			_mockCleanService.Verify(
				clean => clean.CleanDataPointsAsync(It.IsAny<TimeSpan?>()),
				service == ServiceManagerServices.Clean ? Times.AtLeastOnce() : Times.Never()
			);

			// Demo
			_mockDemoService.Verify(
				demo => demo.GenerateDemoDataAsync(
					It.IsAny<Metrics>(),
					It.IsAny<string>(),
					It.IsAny<DateTime?>()
				),
				service == ServiceManagerServices.Demo ? Times.AtLeastOnce() : Times.Never()
			);
			_mockDemoService.Verify(
				demo => demo.GenerateDemoLogAsync(It.IsAny<string>()),
				service == ServiceManagerServices.Demo ? Times.AtLeastOnce() : Times.Never()
			);

			// Health
			_mockHealthService.Verify(
				health => health.ProduceHealthReportAsync(),
				service == ServiceManagerServices.Health ? Times.AtLeastOnce() : Times.Never()
			);

			// Discrepancy
			_mockDiscrepancyService.Verify(
				discrepancy => discrepancy.FindGapsAsync(
					It.IsAny<Metric>(),
					It.IsAny<TimeSpan>()
				),
				service == ServiceManagerServices.Discrepancy ? Times.AtLeastOnce() : Times.Never()
			);
			_mockDiscrepancyService.Verify(
				discrepancy => discrepancy.FindPingFailuresAsync(
					It.IsAny<Metric>(),
					It.IsAny<TimeSpan>()
				),
				service == ServiceManagerServices.Discrepancy ? Times.AtLeastOnce() : Times.Never()
			);
			_mockDiscrepancyService.Verify(
				discrepancy => discrepancy.FindHighLoadsAsync(
					It.IsAny<Metric>(),
					It.IsAny<TimeSpan>()
				),
				service == ServiceManagerServices.Discrepancy ? Times.AtLeastOnce() : Times.Never()
			);
			_mockDiscrepancyService.Verify(
				discrepancy => discrepancy.RecordDiscrepanciesAsync(It.IsAny<IEnumerable<Discrepancy>>()),
				service == ServiceManagerServices.Discrepancy ? Times.AtLeastOnce() : Times.Never()
			);
			_mockDiscrepancyService.Verify(
				discrepancy => discrepancy.FindResolvedDiscrepanciesAsync(It.IsAny<IEnumerable<Discrepancy>>()),
				service == ServiceManagerServices.Discrepancy ? Times.AtLeastOnce() : Times.Never()
			);
			_mockDiscrepancyService.Verify(
				discrepancy => discrepancy.ResolveDiscrepanciesAsync(It.IsAny<IEnumerable<Discrepancy>>()),
				service == ServiceManagerServices.Discrepancy ? Times.AtLeastOnce() : Times.Never()
			);


			// Notification
			_mockNotificationService.Verify(
				notif => notif.ProcessNotificationQueueAsync(),
				service == ServiceManagerServices.Notification ? Times.AtLeastOnce() : Times.Never()
			);

			// Ping
			_mockPingService.Verify(
				ping => ping.PingServerAsync(It.IsAny<PingSetting>()),
				service == ServiceManagerServices.Ping ? Times.AtLeastOnce() : Times.Never()
			);

			// Verify run until completion
			_mockLog
				.Verify(
					log => log.Log(
						LogLevel.Information,
						It.IsAny<EventId>(),
						It.Is<FormattedLogValues>(
							v => v
								.ToString()
								.Contains(
									$"{service.ToString()} service run complete",
									StringComparison.OrdinalIgnoreCase
								)
						),
						It.IsAny<Exception>(),
						It.IsAny<Func<object, Exception, string>>()
					)
				);
		}

		/// <summary>
		/// Sets up mock config to enable all or a particular service
		/// </summary>
		/// <param name="service">If given, only this service will be enabled</param>
		private void EnableServices(ServiceManagerServices? service = null)
		{
			if (service.HasValue)
			{
				_config
					.SetupGet(conf => conf[$"ServiceManager:{service.Value.ToString()}Service:Enabled"])
					.Returns(true.ToString());

				foreach (var toDisable in Enum.GetValues(typeof(ServiceManagerServices)).Cast<ServiceManagerServices>())
				{
					if (toDisable != service)
					{
						_config
							.SetupGet(conf => conf[$"ServiceManager:{toDisable.ToString()}Service:Enabled"])
							.Returns(false.ToString());
					}
				}
			}
			else
			{
				foreach (var toEnable in Enum.GetValues(typeof(ServiceManagerServices)).Cast<ServiceManagerServices>())
				{
					_config
						.SetupGet(conf => conf[$"ServiceManager:{toEnable.ToString()}Service:Enabled"])
						.Returns(true.ToString());
				}
			}
		}

		/// <summary>
		/// Returns newly created IDataContext
		/// </summary>
		/// <returns>Newly created IDataContext</returns>
		private IDataContext GenerateNewDataContext()
		{
			var services = new ServiceCollection();

			var mockEnv = new Mock<IHostingEnvironment>();
			mockEnv
				.SetupGet(environment => environment.EnvironmentName)
				.Returns("Testing");
			var env = mockEnv.Object;

			services.RegisterSharedServices(env, new Mock<IConfiguration>().Object);

			var testProvider = services.BuildServiceProvider();

			var context = testProvider.GetRequiredService<IDataContext>();
			context.Metrics.AddRange(_testMetrics);
			context.PingSettings.Add(
				new PingSetting
				{
					ServerUrl = "the.url.com"
				}
			);
			context.SaveChanges();

			Thread.Sleep(200);

			return context;
		}
	}
}
