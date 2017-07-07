using System;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Shared.Models.Entities;
using System.Threading.Tasks;
using System.Net.Http;
using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Logging;
using StatusMonitor.Shared.Services;
using StatusMonitor.Shared.Services.Factories;
using System.Collections.Generic;
using System.Linq;
using StatusMonitor.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("test")]

namespace StatusMonitor.Daemons.Services
{
	/// <summary>
	/// Service used to analyze series of datapoints looking for certain discrepancies
	/// </summary>
	public interface IDiscrepancyService
	{
		/// <summary>
		/// Stores in database and schedules notifications for those discrepancies from input
		/// that do not yet exist in data provider.
		/// </summary>
		/// <param name="discrepancies">List of discrepancies to store and notify</param>
		/// <returns>List of discrepancies from input, which has just been added to data provider</returns>
		Task<List<Discrepancy>> RecordDiscrepanciesAsync(IEnumerable<Discrepancy> discrepancies);

		/// <summary>
		/// Returns a list of discrepancies of type DiscrepancyType.HighLoad 
		/// in the data for given metric for given timeframe.
		/// </summary>
		/// <param name="metric">Metric for which to find discrepancies. Must be of type Metrics.CpuLoad.</param>
		/// <param name="ago">Time ago from now which defines a timeframe of data to analyze</param>
		/// <returns>A list of found discrepancies</returns>
		Task<List<Discrepancy>> FindHighLoadsAsync(Metric metric, TimeSpan ago);

		/// <summary>
		/// Returns a list of discrepancies of type DiscrepancyType.GapInData 
		/// in the data for given metric for given timeframe.
		/// </summary>
		/// <param name="metric">Metric for which to find discrepancies. Must be of type Metrics.CpuLoad.</param>
		/// <param name="ago">Time ago from now which defines a timeframe of data to analyze</param>
		/// <returns>A list of found discrepancies</returns>
		Task<List<Discrepancy>> FindGapsAsync(Metric metric, TimeSpan ago);

		/// <summary>
		/// Returns a list of discrepancies of type DiscrepancyType.PingFailedNTimes 
		/// in the data for given metric for given timeframe.
		/// </summary>
		/// <param name="metric">Metric for which to find discrepancies. Must be of type Metrics.Ping.</param>
		/// <param name="ago">Time ago from now which defines a timeframe of data to analyze</param>
		/// <returns>A list of found discrepancies</returns>
		Task<List<Discrepancy>> FindPingFailuresAsync(Metric metric, TimeSpan ago);
	}

	public class DiscrepancyService : IDiscrepancyService
	{
		private readonly object _locker = new object();

		private readonly ILogger<DiscrepancyService> _logger;
		private readonly IDataContext _context;
		private readonly INotificationService _notification;
		private readonly IConfiguration _config;

		public DiscrepancyService(
			ILogger<DiscrepancyService> logger,
			IDataContext context,
			INotificationService notification,
			IConfiguration config
		)
		{
			_context = context;
			_logger = logger;
			_notification = notification;
			_config = config;
		}

		public async Task<List<Discrepancy>> RecordDiscrepanciesAsync(IEnumerable<Discrepancy> discrepancies)
		{
			var unique = new List<Discrepancy>();

			foreach (var discrepancy in discrepancies)
			{
				lock (_locker)
				{
					if (
						!_context.
							Discrepancies.
							Any(d =>
								d.MetricType == discrepancy.MetricType &&
								d.MetricSource == discrepancy.MetricSource &&
								d.DateFirstOffense == discrepancy.DateFirstOffense &&
								d.Type == discrepancy.Type
							)
					)
					{
						_context.Discrepancies.Add(discrepancy);
						_context.SaveChanges();

						unique.Add(discrepancy);
					}
				}
			}

			foreach (var discrepancy in unique)
			{
				await _notification.ScheduleNotificationAsync(
					discrepancy.ToString(),
					NotificationSeverity.High
				);
			}

			return unique;
		}

		public async Task<List<Discrepancy>> FindGapsAsync(Metric metric, TimeSpan ago)
		{
			if (metric.Type != Metrics.CpuLoad.AsInt())
			{
				throw new ArgumentException($"Metric for FindGapsAsync has to be of type {Metrics.CpuLoad}");
			}

			_context.Metrics.Attach(metric);

			var timeAgo = DateTime.UtcNow - ago;

			return FindGapsInDataPoints(
				await _context
					.NumericDataPoints
					.Where(dp => dp.Metric == metric && dp.Timestamp >= timeAgo)
					.Select(dp => dp.Timestamp)
					.ToListAsync(),
				metric
			);
		}

		public async Task<List<Discrepancy>> FindPingFailuresAsync(Metric metric, TimeSpan ago)
		{
			if (metric.Type != Metrics.Ping.AsInt())
			{
				throw new ArgumentException($"Metric for FindPingFailuresAsync has to be of type {Metrics.Ping}");
			}

			_context.Metrics.Attach(metric);

			var timeAgo = DateTime.UtcNow - ago;

			return FindPingFailuresFromDataPoints(
				await _context
					.PingDataPoints
					.Where(dp => dp.Metric == metric && dp.Timestamp >= timeAgo)
					.ToListAsync(),
				await _context
					.PingSettings
					.FirstAsync(s => new Uri(s.ServerUrl).Host == metric.Source),
				metric
			);
		}

		public async Task<List<Discrepancy>> FindHighLoadsAsync(Metric metric, TimeSpan ago)
		{
			if (metric.Type != Metrics.CpuLoad.AsInt())
			{
				throw new ArgumentException($"Metric for FindHighLoadsAsync has to be of type {Metrics.CpuLoad}");
			}

			_context.Metrics.Attach(metric);

			var timeAgo = DateTime.UtcNow - ago;

			return FindHighLoadInDataPoints(
				await _context
					.NumericDataPoints
					.Where(dp => dp.Metric == metric && dp.Timestamp >= timeAgo)
					.ToListAsync(),
				metric
			);
		}

		/// <summary>
		/// Traverses the lists of timestamps looking for places where the difference between two consecutive
		/// values is greater than the value defined in config ServiceManager:DiscrepancyService:Gaps:MaxDifference
		/// times 1.5
		/// </summary>
		/// <param name="timestamps">List of timestamps to traverse</param>
		/// <param name="metric">Metric which will appear in a resulting discrepancy object</param>
		/// <returns>A list of discrepancies of type DiscrepancyType.GapInData found</returns>
		internal List<Discrepancy> FindGapsInDataPoints(List<DateTime> timestamps, Metric metric)
		{
			if (timestamps.Count() == 0)
			{
				return new List<Discrepancy>();
			}

			var ordered = timestamps.OrderBy(dp => dp);

			return ordered
				.Zip(
					ordered.Skip(1),
					(x, y) => new
					{
						Difference = y - x,
						DateFirstOffense = x
					}
				)
				.Where(
					x => x.Difference >= new TimeSpan(
						0,
						0,
						(int)Math.Round(Convert.ToInt32(_config["ServiceManager:DiscrepancyService:Gaps:MaxDifference"]) * 1.5)
					)
				)
				.Select(x => new Discrepancy
				{
					DateFirstOffense = x.DateFirstOffense,
					Type = DiscrepancyType.GapInData,
					MetricType = (Metrics)metric.Type,
					MetricSource = metric.Source
				})
				.ToList();
		}

		/// <summary>
		/// Traverses the lists of ping datapoints looking for places where ping failed a number of times defined
		/// in setting. 
		/// </summary>
		/// <param name="pings">List of ping datapoints to traverse</param>
		/// <param name="setting">Ping setting used to determine how many times it is permissible to fail a ping</param>
		/// <param name="metric">Metric which will appear in a resulting discrepancy object</param>
		/// <returns>A list of discrepancies of type DiscrepancyType.PingFailedNTimes found</returns>
		internal List<Discrepancy> FindPingFailuresFromDataPoints(IEnumerable<PingDataPoint> pings, PingSetting setting, Metric metric)
		{
			if (pings.Count() == 0)
			{
				return new List<Discrepancy>();
			}

			var failures = pings
				.Select(p => new
				{
					StatusOK = p.HttpStatusCode == HttpStatusCode.OK.AsInt(),
					Timestamp = p.Timestamp
				});

			if (!failures.Any(l => l.StatusOK))
			{
				// Means that server is dead for the whole timeframe
				// Discrepancy has been noticed already
				return new List<Discrepancy>();
			}

			return failures
				.OrderBy(p => p.Timestamp)
				.Aggregate(
					new Stack<BoolIntDateTuple>(),
					(rest, self) =>
					{
						if (rest.Count == 0 || rest.Peek().StateGood != self.StatusOK)
						{
							rest.Push(new BoolIntDateTuple
							{
								StateGood = self.StatusOK,
								DateFirstOffense = self.Timestamp
							});
						}
						else
						{
							rest.Peek().Count++;
						}
						return rest;
					}
				)
				.Where(t => !t.StateGood && t.Count > setting.MaxFailures)
				.Select(x => new Discrepancy
				{
					DateFirstOffense = x.DateFirstOffense,
					Type = DiscrepancyType.PingFailedNTimes,
					MetricType = (Metrics)metric.Type,
					MetricSource = metric.Source
				})
				.ToList();
		}

		/// <summary>
		/// Traverses the lists of numeric datapoints looking for places where the value is greater than or equal to
		/// the value defined in config ServiceManager:DiscrepancyService:Load:Threshold for more than 
		/// ServiceManager:DiscrepancyService:Load:MaxFailures consecutive datapoints.
		/// </summary>
		/// <param name="datapoints">List of numeric datapoints to traverse</param>
		/// <param name="metric">Metric which will appear in a resulting discrepancy object</param>
		/// <returns>A list of discrepancies of type DiscrepancyType.HighLoad found</returns>
		internal List<Discrepancy> FindHighLoadInDataPoints(List<NumericDataPoint> datapoints, Metric metric)
		{
			if (datapoints.Count() == 0)
			{
				return new List<Discrepancy>();
			}

			var loads = datapoints
				.Select(d => new
				{
					NormalLoad = d.Value < Convert.ToInt32(_config["ServiceManager:DiscrepancyService:Load:Threshold"]),
					Timestamp = d.Timestamp
				});
			
			if (!loads.Any(l => l.NormalLoad))
			{
				// Means that server is overloaded for the whole timeframe
				// Discrepancy has been noticed already
				return new List<Discrepancy>();
			}

			return loads
				.OrderBy(p => p.Timestamp)
				.Aggregate(
					new Stack<BoolIntDateTuple>(),
					(rest, self) =>
					{
						if (rest.Count == 0 || rest.Peek().StateGood != self.NormalLoad)
						{
							rest.Push(new BoolIntDateTuple
							{
								StateGood = self.NormalLoad,
								DateFirstOffense = self.Timestamp
							});
						}
						else
						{
							rest.Peek().Count++;
						}
						return rest;
					}
				)
				.Where(
					t => 
						!t.StateGood && 
						t.Count > Convert.ToInt32(_config["ServiceManager:DiscrepancyService:Load:MaxFailures"])
				)
				.Select(x => new Discrepancy
				{
					DateFirstOffense = x.DateFirstOffense,
					Type = DiscrepancyType.HighLoad,
					MetricType = (Metrics)metric.Type,
					MetricSource = metric.Source
				})
				.ToList();
		}
	}

	/// <summary>
	/// Simple POCO class for holding a tuple of bool, int and DateTime
	/// </summary>
	internal class BoolIntDateTuple
	{
		public bool StateGood { get; set; }
		public int Count { get; set; } = 1;
		public DateTime DateFirstOffense { get; set; }
	}
}
