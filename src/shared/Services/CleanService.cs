using System;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Shared.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using StatusMonitor.Shared.Models.Entities;

namespace StatusMonitor.Shared.Services
{
	/// <summary>
	/// Used to clean up database removing old datapoints and log entries
	/// </summary>
	public interface ICleanService
	{
		/// <summary>
		/// Removes all data points and log entries older than the configured timestamp
		/// </summary>
		/// <param name="maxAge">Time ago earlier than which to consider data too old</param>
		Task CleanDataPointsAsync(TimeSpan? maxAge = null);
	}

	public class CleanService : ICleanService
	{
		private readonly ILogger<CleanService> _logger;
		private readonly IDataContext _context;

		/// <summary>
		/// The maximum age of a data point.
		/// </summary>
		private readonly TimeSpan _maxAge = new TimeSpan();

		public CleanService(
			ILogger<CleanService> logger,
			IDataContext context,
			IConfiguration config
		)
		{
			_logger = logger;
			_context = context;
			_maxAge = new TimeSpan(
				0, 0, Convert.ToInt32(
					config["ServiceManager:CleanService:MaxAge"]
				)
			);
		}

		public async Task CleanDataPointsAsync(TimeSpan? maxAge = null)
		{
			var toTimestamp = DateTime.UtcNow - (maxAge ?? _maxAge);

			// Remove data of all types

			await RemoveDataPoints(_context.NumericDataPoints, toTimestamp);
			await RemoveDataPoints(_context.LogDataPoints, toTimestamp);
			await RemoveDataPoints(_context.CompilationDataPoints, toTimestamp);
			await RemoveDataPoints(_context.UserActionDataPoints, toTimestamp);
			await RemoveDataPoints(_context.PingDataPoints, toTimestamp);

			if (await _context.LogEntries.AnyAsync(dp => dp.Timestamp < toTimestamp))
			{
				_context.LogEntries.RemoveRange(
					_context.LogEntries.Where(dp => dp.Timestamp < toTimestamp)
				);
			}

			if (await _context.Notifications.AnyAsync(dp => dp.IsSent && dp.DateCreated < toTimestamp))
			{
				_context.Notifications.RemoveRange(
					_context.Notifications.Where(dp => dp.IsSent && dp.DateCreated < toTimestamp)
				);
			}

			if (await _context.Discrepancies.AnyAsync(dp => dp.DateFirstOffense < toTimestamp))
			{
				_context.Discrepancies.RemoveRange(
					_context.Discrepancies.Where(dp => dp.DateFirstOffense < toTimestamp)
				);
			}

			await _context.SaveChangesAsync();

			_logger.LogDebug(LoggingEvents.Clean.AsInt(), "Cleaned old data.");
		}

		/// <summary>
		/// Helper to remove datapoints
		/// </summary>
		/// <param name="dataPoints">Data points to remove</param>
		/// <param name="toTimestamp">Timestamp earlier than which to remove data</param>
		private async Task RemoveDataPoints<T>(DbSet<T> dataPoints, DateTime toTimestamp) where T : DataPoint
		{
			if (await dataPoints.AnyAsync(dp => dp.Timestamp < toTimestamp))
			{
				dataPoints.RemoveRange(
					dataPoints.Where(dp => dp.Timestamp < toTimestamp)
				);
			}
		}
	}
}
