using System;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Shared.Models.Entities;
using StatusMonitor.Shared.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Collections.Generic;

namespace StatusMonitor.Shared.Services
{
	/// <summary>
	/// Service used to access and create Metric objects.
	/// </summary>
	public interface IMetricService
	{
		/// <summary>
		/// Returns the list of metrics that match given parameters
		/// </summary>
		/// <param name="type">Metric type requested</param>
		/// <param name="source">Metric source requested (eq. server id or URL)</param>
		/// <returns>List of metrics that match given parameters</returns>
		Task<IEnumerable<Metric>> GetMetricsAsync(Metrics? type = null, string source = null);

		/// <summary>
		/// Returns the metric if it exists, or creates it for the specified type and source otherwise
		/// </summary>
		/// <param name="type">Metric type requested</param>
		/// <param name="source">Metric source requested (eq. server id or URL)</param>
		/// <returns>Returns the metric (already existing or newly created)</returns>
		/// <exception cref="ArgumentException">Thrown if there is no AbstractMetric of this type.</exception> 
		Task<Metric> GetOrCreateMetricAsync(Metrics type, string source);

		/// <summary>
		/// Returns a current (numeric) value for the given metric bypassing caches (directly from the data provider).
		/// </summary>
		/// <param name="metric">Metric for which to return the current value.</param>
		/// <returns>Current numeric value (always the most current).</returns>
		Task<int> GetCurrentValueForMetricAsync(Metric metric);
	}

	/// <summary>
	/// Specific implementation of IMetricService.
	/// </summary>
	public class MetricService : IMetricService
	{
		private readonly IDataContext _context;
		private readonly ILogger<MetricService> _logger;

		public MetricService(
			IDataContext context,
			ILogger<MetricService> logger
		)
		{
			_context = context;
			_logger = logger;
		}

		public async Task<int> GetCurrentValueForMetricAsync(Metric metric)
		{
			switch ((Metrics)metric.Type)
			{
				case Metrics.CpuLoad:
					return await GetCurrentValueForMetricFromDataPointsAsync(metric, _context.NumericDataPoints);
				case Metrics.Health:
					return await GetCurrentValueForMetricFromDataPointsAsync(metric, _context.HealthReports);
				case Metrics.Compilation:
					return await GetCurrentValueForMetricFromDataPointsAsync(metric, _context.CompilationDataPoints);
				case Metrics.Ping:
					return await GetCurrentValueForMetricFromDataPointsAsync(metric, _context.PingDataPoints);
				case Metrics.Log:
					return await GetCurrentValueForMetricFromDataPointsAsync(metric, _context.LogDataPoints);
				case Metrics.UserAction:
					return await GetCurrentValueForMetricFromDataPointsAsync(metric, _context.UserActionDataPoints);
				default:
					var ex = new ArgumentOutOfRangeException($"Unknown metric type: {metric.Type}");
					_logger.LogCritical(
						LoggingEvents.Metrics.AsInt(),
						ex,
						"Unknown metric in GetCurrentValueForMetricAsync"
					);
					throw ex;
			}
		}

		/// <summary>
		/// Helper method that returns the most current numeric (normalized) value
		/// for the metric and the set of data points.
		/// </summary>
		/// <returns>Current numeric value (always the most current).</returns>
		private async Task<int> GetCurrentValueForMetricFromDataPointsAsync<T>(
			Metric metric, DbSet<T> dataPoints
		) where T : DataPoint
		{
			_context.Metrics.Attach(metric);

			return (await dataPoints
				.Where(dp => dp.Metric == metric)
				.OrderByDescending(dp => dp.Timestamp)
				.FirstAsync())
				.NormalizedValue() ?? 0;
		}

		public async Task<Metric> GetOrCreateMetricAsync(Metrics type, string source)
		{
			if (await _context.Metrics.AnyAsync(mt => mt.Type == type.AsInt() && mt.Source == source))
			{
				return await _context
					.Metrics
					.FirstOrDefaultAsync(
						mt => mt.Type == type.AsInt() && mt.Source == source
					);
			}

			var abstractMetric = await _context.AbstractMetrics.FirstOrDefaultAsync(mt => mt.Type == type.AsInt());
			if (abstractMetric == null)
			{
				throw new ArgumentException($"No generic metric exists with the type {type}");
			}

			var autoLabel = await _context.AutoLabels.FirstOrDefaultAsync(lbl => lbl.Id == AutoLabels.Normal.AsInt());
			if (autoLabel == null)
			{
				throw new ArgumentException($"No AutoLabel exists with the Id {AutoLabels.Normal}");
			}

			var manualLabel = await _context.
				ManualLabels.
				FirstOrDefaultAsync(lbl => lbl.Id == ManualLabels.None.AsInt());
			if (manualLabel == null)
			{
				throw new ArgumentException($"No ManualLabel exists with the Id {ManualLabels.Investigating}");
			}

			var metric = new Metric
			{
				Type = abstractMetric.Type,
				Public = abstractMetric.Public,
				AutoLabel = autoLabel,
				ManualLabel = manualLabel,
				Title = abstractMetric.Title,
				Source = source
			};

			await _context.Metrics.AddAsync(metric);
			await _context.SaveChangesAsync();

			_logger.LogInformation(LoggingEvents.Metrics.AsInt(), $"New metric created: {(Metrics)metric.Type}, {metric.Source}");

			return metric;
		}

		public async Task<IEnumerable<Metric>> GetMetricsAsync(Metrics? type, string source)
		{
			var metrics = await _context
				.Metrics
				.Include(mt => mt.AutoLabel)
				.Include(mt => mt.ManualLabel)
				.ToListAsync();

			if (metrics.Count == 0)
			{
				return metrics;
			}

			if (type.HasValue)
			{
				metrics = metrics.Where(mt => mt.Type == type.AsInt()).ToList();
				if (metrics.Count == 0)
				{
					return metrics;
				}
			}

			if (source != null)
			{
				metrics = metrics.Where(mt => mt.Source == source).ToList();
				if (metrics.Count == 0)
				{
					return metrics;
				}
			}

			try
			{
				foreach (var metric in metrics)
				{
					metric.CurrentValue = await GetCurrentValueForMetricAsync(metric);
				}
			}
			catch (System.Exception ex)
			{
				// Its alright for testing
				_logger.LogWarning(
					LoggingEvents.Metrics.AsInt(),
					ex,
					"GetMetric called for metric without data"
				);
			}

			return metrics;
		}
	}
}
