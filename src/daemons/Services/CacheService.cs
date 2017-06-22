using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Shared.Models;
using StatusMonitor.Shared.Models.Entities;

namespace StatusMonitor.Daemons.Services
{
	/// <summary>
	/// Service used to populate data into Metric objects.
	/// </summary>
	public interface ICacheService
	{
		/// <summary>
		/// Updates a given Metric with pre-computed values (including averages, min and max of recent DataPoints)
		/// The updated metric is returned after the changes have been made in the data provider.
		/// Uses the `Type` and `Source` fields of the given metric object to re-fetch the metric from the database,
		/// which ensures that the old metric object is properly disposed of.
		/// Throws runtime exception if `Type` and `Source` fields don't reference an existing metric.
		/// </summary>
		/// <param name="metric">Metric to update data for.</param>
		/// <returns>New Metric with the data updated.</returns>
		Task<Metric> CacheMetricAsync(Metric metric);
	}

	public class CacheService : ICacheService
	{
		private readonly IDataContext _context;
		private readonly ILogger<CacheService> _logger;

		public CacheService(IDataContext context, ILogger<CacheService> logger)
		{
			_context = context;
			_logger = logger;
		}

		public async Task<Metric> CacheMetricAsync(Metric metric)
		{
			_context.Metrics.Attach(metric);

			List<TimeValuePair> values = null;

			// Normalize values depending on Metric type.
			switch ((Metrics)metric.Type)
			{
				case Metrics.CpuLoad:
					values = await NormalizedValues(metric, _context.NumericDataPoints);
					break;
				case Metrics.Ping:
					values = await NormalizedValues(metric, _context.PingDataPoints);
					break;
				case Metrics.UserAction:
					values = await NormalizedValues(metric, _context.UserActionDataPoints);
					break;
				case Metrics.Log:
					values = await NormalizedValues(metric, _context.LogDataPoints);
					break;
				case Metrics.Compilation:
					values = await NormalizedValues(metric, _context.CompilationDataPoints);
					break;
				default:
					var ex = new ArgumentOutOfRangeException($"Unknown metric type: {metric.Type}");
					_logger.LogCritical(LoggingEvents.Metrics.AsInt(), ex, "Unknown metric in CacheMetric");
					throw ex;
			}
			
			if (values.Count() < 5)
			{
				metric.AutoLabel = await _context.AutoLabels.FindAsync(AutoLabels.Normal.AsInt());
				return metric;
			}

			// Update metric
			ComputeNumericValues(values, metric);
			metric.AutoLabel = await _context.AutoLabels.FindAsync((await GetLabelAsync(metric)).AsInt());

			_logger.LogDebug(
				LoggingEvents.Cache.AsInt(),
				$@"Metric {metric.Source} of type {(Metrics)metric.Type} updated. 
				New CurrentValue is {metric.CurrentValue}."
				.RemoveWhitespaces()
			);

			return metric;
		}

		/// <summary>
		/// Helper that normalizes the data for the given metric.
		/// The output is the list of pairs - integer value and timestamp.
		/// </summary>
		/// <param name="metric">Metric for which to normalize data.</param>
		/// <param name="dataPoints">DbSet of data points which need to be normalized.</param>
		/// <returns>A list of data pieces.</returns>
		private async Task<List<TimeValuePair>> NormalizedValues<T>(Metric metric, DbSet<T> dataPoints) where T : DataPoint
		{
			return
				(await FilterData(metric, dataPoints))
				.Where(dp => dp.NormalizedValue() != null)
				.Select(dp => new TimeValuePair
				{
					Value = dp.NormalizedValue().Value,
					Timestamp = dp.Timestamp
				})
				.ToList();
		}

		/// <summary>
		/// Helper that filters data point to include only those no older than timestamp.
		/// </summary>
		/// <param name="metric">Metric for which to filter data.</param>
		/// <param name="dataPoints">Data point to filter.</param>
		/// <returns>Filtered data points as list (no longer DbSet.</returns>
		private async Task<List<T>> FilterData<T>(Metric metric, DbSet<T> dataPoints) where T : DataPoint
		{
			// Compute the timestamp from which data is requested (1 day ago)
			var fromTimestamp = DateTime.UtcNow - new TimeSpan(1, 0, 0, 0);

			return await dataPoints
				.Where(dp =>
					dp.Metric == metric &&
					dp.Timestamp >= fromTimestamp
				).ToListAsync();
		}

		/// <summary>
		/// Traverse data pieces and compute numeric values (averages, min, max, etc)
		/// </summary>
		/// <param name="values">Input list of data pieces from which to compute values</param>
		/// <param name="metric">Metric for which to compute values</param>
		/// <param name="context">Context for data provider (source of data)</param>
		private void ComputeNumericValues(List<TimeValuePair> values, Metric metric)
		{
			var latestDatapoint = values.OrderByDescending(dp => dp.Timestamp).First();
			metric.CurrentValue = latestDatapoint.Value;
			metric.LastUpdated = latestDatapoint.Timestamp;

			metric.DayMin = values.Min(dp => dp.Value);
			metric.DayMax = values.Max(dp => dp.Value);
			metric.DayAvg = Convert.ToInt32(values.Average(dp => dp.Value));

			var fromTimestamp = DateTime.UtcNow - new TimeSpan(0, 1, 0, 0);

			values = values.Where(dp => dp.Timestamp >= fromTimestamp).ToList();

			if (values.Count() == 0)
			{
				metric.HourMin = 0;
				metric.HourMax = 0;
				metric.HourAvg = 0;
			}
			else
			{
				metric.HourMin = values.Min(dp => dp.Value);
				metric.HourMax = values.Max(dp => dp.Value);
				metric.HourAvg = Convert.ToInt32(values.Average(dp => dp.Value));
			}
		}

		/// <summary>
		/// Set metric labels' value
		/// </summary>
		/// <returns>This object with metric labels set</returns>
		private async Task<AutoLabels> GetLabelAsync(Metric metric)
		{
			var label = AutoLabels.Normal;

			switch ((Metrics)metric.Type)
			{
				case Metrics.CpuLoad:

					if (
						await _context
							.NumericDataPoints
							.Where(dp => dp.Metric == metric)
							.CountAsync() < 5
						)
					{
						break;
					}

					var cpuValues =
						(await _context
							.NumericDataPoints
							.Where(dp => dp.Metric == metric)
							.OrderByDescending(dp => dp.Timestamp)
							.Select(dp => dp.Value)
							.ToListAsync())
							.Take(5);

					if (cpuValues.Average() >= 90)
					{
						label = AutoLabels.Critical;
					}
					else if (cpuValues.Average() >= 50)
					{
						label = AutoLabels.Warning;
					}
					break;
				case Metrics.Ping:
					var pingSetting =
						await _context
							.PingSettings
							.FirstOrDefaultAsync(setting => new Uri(setting.ServerUrl).Host == metric.Source);

					if (
						await _context
							.PingDataPoints
							.Where(dp => dp.Metric == metric)
							.CountAsync() < Math.Max(10, pingSetting.MaxFailures)
						)
					{
						break;
					}

					var pingValues =
						(await _context
							.PingDataPoints
							.Where(dp => dp.Metric == metric)
							.OrderByDescending(dp => dp.Timestamp)
							.Select(dp => dp.HttpStatusCode)
							.ToListAsync());

					if (
						pingValues.Take(10).Count(code => code != System.Net.HttpStatusCode.OK.AsInt())
						>=
						pingSetting.MaxFailures
					)
					{
						label = AutoLabels.Critical;
					}
					else if (pingValues.First() != System.Net.HttpStatusCode.OK.AsInt())
					{
						label = AutoLabels.Warning;
					}

					break;

			}

			return label;
		}
	}

	/// <summary>
	/// Helper class holding simple construct to represent data value with its timestamp.
	/// </summary>
	internal class TimeValuePair
	{
		public int Value { get; set; }
		public DateTime Timestamp { get; set; }
	}
}
