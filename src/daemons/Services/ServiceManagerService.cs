using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Shared.Models;
using StatusMonitor.Shared.Models.Entities;
using StatusMonitor.Shared.Services;

namespace StatusMonitor.Daemons.Services
{
	/// <summary>
	/// Enum of available demonable services
	/// </summary>
	public enum ServiceManagerServices
	{
		Ping, Cache, Clean, Demo, Notification, Discrepancy
	}

	/// <summary>
	/// Service used to run some other services as daemons.
	/// </summary>
	public interface IServiceManagerService
	{
		/// <summary>
		/// Runs all available demonable services.
		/// Does NOT return until services are stopped.
		/// This method is NOT recommended to be awaited.
		/// </summary>
		Task StartServices();

		/// <summary>
		/// Stops all services gracefully.
		/// It may take a few ticks until those services actually stop.
		/// Returns immediately, does NOT wait until services are stopped.
		/// </summary>
		void StopServices();
	}

	public class ServiceManagerService : IServiceManagerService
	{
		private readonly ILogger<ServiceManagerService> _logger;
		private readonly IServiceProvider _serviceProvider;
		private readonly IConfiguration _config;

		/// <summary>
		/// Holds flags whether services should run or stop.
		/// Used by the service itself.
		/// </summary>
		private Dictionary<ServiceManagerServices, bool> _status = new Dictionary<ServiceManagerServices, bool>() { };

		/// <summary>
		/// Holds intervals to wait for each service.
		/// Interval says how much time to wait between runing the task again.
		/// </summary>
		private Dictionary<ServiceManagerServices, TimeSpan> _intervals =
			new Dictionary<ServiceManagerServices, TimeSpan>();

		/// <summary>
		/// A collection of metrics for which to generate demo data points
		/// </summary>
		private IEnumerable<Tuple<Metrics, string>> _demoMetrics =
			new List<Tuple<Metrics, string>>() {
				new Tuple<Metrics, string>(Metrics.CpuLoad, "source-1"),
				new Tuple<Metrics, string>(Metrics.CpuLoad, "source-2"),
				new Tuple<Metrics, string>(Metrics.CpuLoad, "source-3"),
				new Tuple<Metrics, string>(Metrics.Ping, "some.web.site")
			};

		public ServiceManagerService(
			ILogger<ServiceManagerService> logger,
			IServiceProvider serviceProvider,
			IConfiguration config
		)
		{
			_logger = logger;
			_serviceProvider = serviceProvider;
			_config = config;

			// Sets all services to run.
			// Does NOT run the services.
			var services = Enum.GetValues(typeof(ServiceManagerServices)).Cast<ServiceManagerServices>();
			foreach (var service in services)
			{
				_intervals[service] = new TimeSpan(
					0,
					0,
					Convert.ToInt32(config[$"ServiceManager:{service.ToString()}Service:Interval"])
				);
				_status[service] = Convert.ToBoolean(config[$"ServiceManager:{service.ToString()}Service:Enabled"]);
			}
		}

		/// <summary>
		/// Starts INotificationService service.
		/// Does NOT return until _status is set to false.
		/// </summary>
		private async Task RunNotificationServiceAsync()
		{
			_logger.LogInformation(LoggingEvents.ServiceManager.AsInt(), "Notification service started.");

			while (true)
			{
				// Check exit condition
				if (!_status[ServiceManagerServices.Notification])
				{
					_logger.LogInformation(LoggingEvents.ServiceManager.AsInt(), "Notification service stopped.");
					break;
				}

				try
				{
					using (var scope = _serviceProvider.CreateScope())
					{
						// Run the task, wait to completion
						await scope
							.ServiceProvider
							.GetRequiredService<INotificationService>()
							.ProcessNotificationQueueAsync();
					}

					// Wait
					Thread.Sleep(_intervals[ServiceManagerServices.Notification]);

					_logger.LogInformation(LoggingEvents.ServiceManager.AsInt(), "Notification service run complete");
				}
				catch (System.Exception e)
				{
					_logger.LogError(
						LoggingEvents.ServiceManager.AsInt(),
						e,
						"Something terribly wrong happend to Notification Service run in Service Manager"
					);
					Thread.Sleep(_intervals[ServiceManagerServices.Notification]);
				}
			}
		}

		/// <summary>
		/// Starts ICleanService service.
		/// Does NOT return until _status is set to false.
		/// </summary>
		private async Task RunCleanServiceAsync()
		{
			_logger.LogInformation(LoggingEvents.ServiceManager.AsInt(), "Clean service started.");

			while (true)
			{
				// Check exit condition
				if (!_status[ServiceManagerServices.Clean])
				{
					_logger.LogInformation(LoggingEvents.ServiceManager.AsInt(), "Clean service stopped.");
					break;
				}

				try
				{
					using (var scope = _serviceProvider.CreateScope())
					{
						// Run the task, wait to completion
						await scope
							.ServiceProvider
							.GetRequiredService<ICleanService>()
							.CleanDataPointsAsync();
					}
					// Wait
					Thread.Sleep(_intervals[ServiceManagerServices.Clean]);

					_logger.LogInformation(LoggingEvents.ServiceManager.AsInt(), "Clean service run complete");
				}
				catch (System.Exception e)
				{
					_logger.LogError(
						LoggingEvents.ServiceManager.AsInt(),
						e,
						"Something terribly wrong happend to Clean Service run in Service Manager"
					);
					Thread.Sleep(_intervals[ServiceManagerServices.Clean]);
				}
			}
		}

		/// <summary>
		/// Starts ICacheService service.
		/// Does NOT return until _status is set to false.
		/// </summary>
		private async Task RunCacheServiceAsync()
		{
			_logger.LogInformation(LoggingEvents.ServiceManager.AsInt(), "Cache service started.");

			while (true)
			{
				// Check exit condition
				if (!_status[ServiceManagerServices.Cache])
				{
					_logger.LogInformation(LoggingEvents.ServiceManager.AsInt(), "Cache service stopped.");
					break;
				}

				try
				{
					using (var contextScope = _serviceProvider.CreateScope())
					{
						var context = contextScope.ServiceProvider.GetRequiredService<IDataContext>();

						var metrics = await context
							.Metrics
							.Include(m => m.AutoLabel)
							.Include(m => m.ManualLabel)
							.ToListAsync();

						// Run the tasks
						var tasks = metrics
							.Select(metric =>
								Task.Run(async () =>
								{
									using (var scope = _serviceProvider.CreateScope())
									{
										return await scope
											.ServiceProvider
											.GetRequiredService<ICacheService>()
											.CacheMetricAsync(metric);
									}
								})
							);

						// Wait completion of all tasks
						var updatedMetrics = await Task.WhenAll(tasks.ToArray());

						foreach (var metric in updatedMetrics)
						{
							metric.AutoLabel = await context.AutoLabels.FindAsync(metric.AutoLabel.Id);
						}

						context.Metrics.UpdateRange(updatedMetrics);

						await context.SaveChangesAsync();

						// Wait
						Thread.Sleep(_intervals[ServiceManagerServices.Cache]);

						_logger.LogInformation(LoggingEvents.ServiceManager.AsInt(), "Cache service run complete");
					}
				}
				catch (System.Exception e)
				{
					_logger.LogError(
						LoggingEvents.ServiceManager.AsInt(),
						e,
						"Something terribly wrong happend to Cache Service run in Service Manager"
					);
				}
			}
		}

		/// <summary>
		/// Starts IPingService service.
		/// Retrieves a list of PingSetting from the data provider and for each 
		/// setting runs a PingServerAsync method on the IPingService.
		/// Stores resulting PingDataPoint in the data provider.
		/// Does NOT return until _status is set to false.
		/// </summary>
		private async Task RunPingServiceAsync()
		{
			_logger.LogInformation(LoggingEvents.ServiceManager.AsInt(), "Ping service started.");

			while (true)
			{
				// Check exit condition
				if (!_status[ServiceManagerServices.Ping])
				{
					_logger.LogInformation(LoggingEvents.ServiceManager.AsInt(), "Ping service stopped.");
					break;
				}

				try
				{
					using (var contextScope = _serviceProvider.CreateScope())
					{
						var context = contextScope.ServiceProvider.GetRequiredService<IDataContext>();

						var settings = await context.PingSettings.ToListAsync();

						// Run the tasks
						var tasks = settings
							.Select(setting =>
								Task.Run(async () =>
								{
									using (var scope = _serviceProvider.CreateScope())
									{
										return await scope
											.ServiceProvider
											.GetRequiredService<IPingService>()
											.PingServerAsync(setting);
									}
								})
							);

						// Wait completion of all tasks
						var pingDataPoints = await Task.WhenAll(tasks.ToArray());

						await context.PingDataPoints.AddRangeAsync(pingDataPoints);
						await context.SaveChangesAsync();

						// Wait
						Thread.Sleep(_intervals[ServiceManagerServices.Ping]);

						_logger.LogInformation(LoggingEvents.ServiceManager.AsInt(), "Ping service run complete");

						// Check exit condition
						if (!_status[ServiceManagerServices.Ping])
						{
							break;
						}
					}
				}
				catch (System.Exception e)
				{
					_logger.LogError(
						LoggingEvents.ServiceManager.AsInt(),
						e,
						"Something terribly wrong happend to Ping Service run in Service Manager"
					);
					Thread.Sleep(_intervals[ServiceManagerServices.Ping]);
				}
			}
		}

		/// <summary>
		/// Starts IDiscrepancyService service.
		/// Looks for discrepancies of all type for all applicable metrics.
		/// Does NOT return until _status is set to false.
		/// </summary>
		private async Task RunDiscrepancyServiceAsync()
		{
			_logger.LogInformation(LoggingEvents.ServiceManager.AsInt(), "Discrepancy service started.");

			while (true)
			{
				// Check exit condition
				if (!_status[ServiceManagerServices.Discrepancy])
				{
					_logger.LogInformation(LoggingEvents.ServiceManager.AsInt(), "Discrepancy service stopped.");
					break;
				}

				try
				{
					using (var contextScope = _serviceProvider.CreateScope())
					{
						var context = contextScope.ServiceProvider.GetRequiredService<IDataContext>();

						var cpuMetrics = await context
							.Metrics
							.Where(mt => mt.Type == Metrics.CpuLoad.AsInt())
							.ToListAsync();

						var pingMetrics = await context
						   .Metrics
						   .Where(mt => mt.Type == Metrics.Ping.AsInt())
						   .ToListAsync();

						// Find GapInData discrepancies
						var gapTasks = cpuMetrics
							.Select(metric =>
								Task.Run(async () =>
								{
									using (var scope = _serviceProvider.CreateScope())
									{
										return await scope
											.ServiceProvider
											.GetRequiredService<IDiscrepancyService>()
											.FindGapsAsync(
												metric, 
												new TimeSpan(
													0,
													0,
													Convert.ToInt32(_config["ServiceManager:DiscrepancyService:DataTimeframe"])
												)
											);
									}
								})
							);

						// Find HighLoad discrepancies
						var highLoadTasks = cpuMetrics
							.Select(metric =>
								Task.Run(async () =>
								{
									using (var scope = _serviceProvider.CreateScope())
									{
										return await scope
											.ServiceProvider
											.GetRequiredService<IDiscrepancyService>()
											.FindHighLoadsAsync(
												metric, 
												new TimeSpan(
													0,
													0,
													Convert.ToInt32(_config["ServiceManager:DiscrepancyService:DataTimeframe"])
												)
											);
									}
								})
							);

						// Find PingFailedNTimes discrepancies
						var pingTasks = pingMetrics
							.Select(metric =>
								Task.Run(async () =>
								{
									using (var scope = _serviceProvider.CreateScope())
									{
										return await scope
											.ServiceProvider
											.GetRequiredService<IDiscrepancyService>()
											.FindPingFailuresAsync(
												metric, 
												new TimeSpan(
													0,
													0,
													Convert.ToInt32(_config["ServiceManager:DiscrepancyService:DataTimeframe"])
												)
											);
									}
								})
							);

						// Wait completion of all tasks
						var discrepancies = await Task.WhenAll(
							new Task<List<Discrepancy>>[] {}
								.Concat(gapTasks).ToArray()
								.Concat(pingTasks).ToArray()
								.Concat(highLoadTasks).ToArray())
						;

						using (var scope = _serviceProvider.CreateScope())
						{
							await scope
								.ServiceProvider
								.GetRequiredService<IDiscrepancyService>()
								.RecordDiscrepanciesAsync(discrepancies.SelectMany(d => d));
						}

						// Wait
						Thread.Sleep(_intervals[ServiceManagerServices.Discrepancy]);

						_logger.LogInformation(LoggingEvents.ServiceManager.AsInt(), "Discrepancy service run complete");
					}
				}
				catch (System.Exception e)
				{
					_logger.LogError(
						LoggingEvents.ServiceManager.AsInt(),
						e,
						"Something terribly wrong happend to Discrepancy Service run in Service Manager"
					);
					Thread.Sleep(_intervals[ServiceManagerServices.Discrepancy]);
				}
			}
		}

		/// <summary>
		/// Starts IDemoService service.
		/// Does NOT return until _status is set to false.
		/// </summary>
		private async Task RunDemoServiceAsync()
		{
			_logger.LogInformation(LoggingEvents.ServiceManager.AsInt(), "Demo service started.");

			while (true)
			{
				// Check exit condition
				if (!_status[ServiceManagerServices.Demo])
				{
					_logger.LogInformation(LoggingEvents.ServiceManager.AsInt(), "Demo service stopped.");
					break;
				}

				try
				{
					// Run the task, wait to completion
					var tasks = _demoMetrics
						.Select(demoMetric =>
							Task.Run(async () =>
							{
								using (var scope = _serviceProvider.CreateScope())
								{
									var generate = true;

									// If gaps generation is enabled, decide if gap needs to be generated this run
									if (Convert.ToBoolean(_config["ServiceManager:DemoService:Gaps:Enabled"]))
									{
										if (
											new Random()
											.Next(
												Convert.ToInt32(_config["ServiceManager:DemoService:Gaps:Frequency"])
											) == 0
										)
										{
											generate = false;
											_logger.LogDebug(LoggingEvents.ServiceManager.AsInt(), "Gap generated");
										}
									}

									if (generate)
									{
										await scope
												.ServiceProvider
												.GetRequiredService<IDemoService>()
												.GenerateDemoDataAsync(demoMetric.Item1, demoMetric.Item2);

										await scope
											.ServiceProvider
											.GetRequiredService<IDemoService>()
											.GenerateDemoLogAsync(demoMetric.Item2);
									}
								}
							})
						);

					// Wait completion of all tasks
					await Task.WhenAll(tasks.ToArray());

					// Wait
					Thread.Sleep(_intervals[ServiceManagerServices.Demo]);

					_logger.LogInformation(LoggingEvents.ServiceManager.AsInt(), "Demo service run complete");
				}
				catch (System.Exception e)
				{
					_logger.LogError(
						LoggingEvents.ServiceManager.AsInt(),
						e,
						"Something terribly wrong happend to Demo Service run in Service Manager"
					);
					Thread.Sleep(_intervals[ServiceManagerServices.Demo]);
				}
			}
		}

		public async Task StartServices()
		{
			// Define tasks and run them
			var tasks = new List<Task>() {
				Task.Run(RunPingServiceAsync),
				Task.Run(RunCacheServiceAsync),
				Task.Run(RunCleanServiceAsync),
				Task.Run(RunDemoServiceAsync),
				Task.Run(RunNotificationServiceAsync),
				Task.Run(RunDiscrepancyServiceAsync)
			}.ToArray();

			await Task.WhenAll(tasks);

			_logger.LogInformation(LoggingEvents.ServiceManager.AsInt(), "All services stopped");
		}

		public void StopServices()
		{
			// Set all statuses to stop
			foreach (var key in _status.Keys.ToList())
			{
				_status[key] = false;
			}
		}
	}
}

