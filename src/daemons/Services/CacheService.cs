using System;
using System.Collections.Generic;
using System.Linq;
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

			if (values.Count() == 0)
			{
				return metric;
			}

			// Update metric
			ComputeNumericValues(values, metric);
			metric.AutoLabel = await _context.AutoLabels.FindAsync(GetLabel(metric).AsInt());

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
				.Select(dp => new TimeValuePair
				{
					Value = dp.NormalizedValue(),
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
		private AutoLabels GetLabel(Metric metric)
		{
			// TODO: read from config

			var label = AutoLabels.Normal;

			switch ((Metrics)metric.Type)
			{
				case Metrics.CpuLoad:
					if (metric.CurrentValue >= 90)
					{
						label = AutoLabels.Critical;
					}
					else if (metric.CurrentValue >= 50)
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
