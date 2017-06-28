using System;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Shared.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq;
using StatusMonitor.Shared.Models.Entities;
using System.Net;
using StatusMonitor.Shared.Services;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace StatusMonitor.Daemons.Services
{
	/// <summary>
	/// Used to periodically add datapoints and log entries mimicing the servers sending their stat data.
	/// </summary>
	public interface IDemoService
	{
		/// <summary>
		/// Generates, stores in data provider and returns a data point for a given metric with random values.
		/// </summary>
		/// <param name="type">Type of the metric for which to generate data point</param>
		/// <param name="source">Source of the metric for which to generate data point</param>
		/// <param name="timestamp">Optional timestamp that will be set for data point. By default, DateTime.UtcNow</param>
		/// <returns>Random data point for the metric</returns>
		Task<DataPoint> GenerateDemoDataAsync(Metrics type, string source, DateTime? timestamp = null);

		/// <summary>
		/// Generates, stores in data provider and returns a log entry for a given source.
		/// </summary>
		/// <param name="source">Source for which to generate log entry</param>
		/// <returns>generated log entry</returns>
		Task<LogEntry> GenerateDemoLogAsync(string source = null);
	}

	public class DemoService : IDemoService
	{
		private readonly ILogger<DemoService> _logger;
		private readonly IDataContext _context;
		private readonly IMetricService _metricService;

		public DemoService(
			ILogger<DemoService> logger,
			IDataContext context,
			IMetricService metricService
		)
		{
			_logger = logger;
			_context = context;
			_metricService = metricService;
		}

		public async Task<DataPoint> GenerateDemoDataAsync(Metrics type, string source, DateTime? timestamp = null)
		{
			var metric = await _metricService.GetOrCreateMetricAsync(type, source);

			// Using plain new Random() will result in the same number if called multiple times in short time period
			var random = new Random(
				Convert.ToInt32(
					(DateTime.UtcNow.Ticks + type.GetHashCode() + source.GetHashCode()) % Int32.MaxValue
				)
			);

			DataPoint result = null;

			switch (type)
			{
				case Metrics.CpuLoad:
					result = new NumericDataPoint
					{
						Timestamp = timestamp ?? DateTime.UtcNow,
						Metric = metric,
						Value = random.Next(5, 100)
					};
					await _context.NumericDataPoints.AddAsync((NumericDataPoint)result);
					break;
				case Metrics.Ping:
					var success = random.Next(100) % 5 != 0;
					result = new PingDataPoint
					{
						Timestamp = timestamp ?? DateTime.UtcNow,
						Metric = metric,
						HttpStatusCode = success ? HttpStatusCode.OK.AsInt() : HttpStatusCode.ServiceUnavailable.AsInt(),
						ResponseTime = new TimeSpan(0, 0, 0, 0, success ? random.Next(100, 900) : 0)
					};
					await _context.PingDataPoints.AddAsync((PingDataPoint)result);
					break;
				case Metrics.UserAction:
					var userActions = new string[] { "Page load", "Project Edit", "Project Create", "User login", "User Logout", "User Register" };

					var action = userActions
						.Skip(random.Next(0, userActions.Count()))
						.Take(1)
						.First();

					result = new UserActionDataPoint
					{
						Timestamp = timestamp ?? DateTime.UtcNow,
						Metric = metric,
						Action = action,
						Count = random.Next(1, 10)
					};

					await _context.UserActionDataPoints.AddAsync((UserActionDataPoint)result);
					break;
				case Metrics.Log:
					var severity = _context
						.LogEntrySeverities
						.Skip(random.Next(0, _context.LogEntrySeverities.Count()))
						.Take(1)
						.First();

					result = new LogDataPoint
					{
						Timestamp = timestamp ?? DateTime.UtcNow,
						Metric = metric,
						Severity = severity,
						Count = random.Next(1, 10)
					};

					await _context.LogDataPoints.AddAsync((LogDataPoint)result);
					break;
				case Metrics.Compilation:
					var stage = _context
						.CompilationStages
						.Skip(random.Next(0, _context.CompilationStages.Count()))
						.Take(1)
						.First();

					result = new CompilationDataPoint
					{
						Timestamp = timestamp ?? DateTime.UtcNow,
						Metric = metric,
						Stage = stage,
						SourceSize = random.Next(1000, 10000),
						CompileTime = new TimeSpan(0, 0, 0, 0, random.Next(100, 900))
					};

					await _context.CompilationDataPoints.AddAsync((CompilationDataPoint)result);
					break;
				default:
					var ex = new ArgumentOutOfRangeException($"Unknown metric type: {type}");
					_logger.LogCritical(LoggingEvents.Metrics.AsInt(), ex, "Unknown metric in CacheMetric");
					throw ex;
			}

			await _context.SaveChangesAsync();

			_logger.LogDebug(
				LoggingEvents.Demo.AsInt(),
				$"Data point for metric of type {type} and source {source} has been generated."
			);

			return result;
		}

		public async Task<LogEntry> GenerateDemoLogAsync(string source = null)
		{
			// Using plain new Random() will result in the same number if called multiple times in short time period
			var random = new Random(
				Convert.ToInt32(
					(DateTime.UtcNow.Ticks + (source == null ? 0 : + source.GetHashCode())) % Int32.MaxValue
				)
			);

			var logSource = source ?? $"source-{random.Next(1, 10)}";

			var severity = _context
				.LogEntrySeverities
				.Skip(random.Next(0, _context.LogEntrySeverities.Count()))
				.Take(1)
				.First();

			var lipsum = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";

			var logEntry = new LogEntry
			{
				Message = lipsum,
				Severity = severity,
				Category = random.Next(1, 10),
				AuxiliaryData =
					random.Next() % 5 == 0 ?
					JsonConvert.SerializeObject(new
					{
						SomeDataPiece = new {
							StringProperty =  "here is the string",
							NumberProperty = 56,
							ArrayProperty = new List<object> {
								new {
									String = "string",
									Number = 45
								},
								new {
									String = "another string",
									Number = 455
								}
							}
						},
						Exception = $"Exception trace:\n at and so on..."
					}) :
					"",
				Source = source
			};

			logEntry = (await _context.LogEntries.AddAsync(logEntry)).Entity;
			await _context.SaveChangesAsync();

			_logger.LogDebug(
				LoggingEvents.Demo.AsInt(),
				$"Log entry for source {source} has been generated."
			);

			return logEntry;
		}
	}
}
