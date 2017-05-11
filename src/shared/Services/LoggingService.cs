using StatusMonitor.Shared.Extensions;
using StatusMonitor.Shared.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StatusMonitor.Shared.Models.Entities;
using System.Collections.Generic;
using System;
using System.Linq;

namespace StatusMonitor.Shared.Services
{
	/// <summary>
	/// Service used to record log messages into data provider.
	/// </summary>
	public interface ILoggingService
	{
		/// <summary>
		/// Records a log message given by the model into data provider.
		/// </summary>
		/// <param name="message">Message part of the model</param>
		/// <param name="auxData">Auxiliary data part of the model</param>
		/// <param name="source">Source part of the model</param>
		/// <param name="category">Category part of the model</param>
		/// <param name="severity">Severity part of the model</param>
		/// <returns>An identifier (Id) of a recorded message in the data provider.</returns> 
		Task<int> RecordLogMessageAsync(
			string message,
			string auxData,
			string source,
			int category,
			LogEntrySeverities severity
		);

		/// <summary>
		/// Returns log messages filtered according to filter model
		/// </summary>
		/// <param name="filterModel">Model used to filter the entries</param>
		/// <returns>Filtered log entries</returns>
		Task<IEnumerable<LogEntry>> GetLogMessagesAsync(LogMessagesFilterModel filterModel);

		/// <summary>
		/// Returns a single log entry given by ID
		/// </summary>
		/// <param name="id">ID of the log entry to find</param>
		/// <returns>Log entry with given ID, or null if there is no such log entry</returns>
		Task<LogEntry> GetMessageByIdAsync(int id);

		/// <summary>
		/// Returns a filter model that describes which properties are available for filtering
		/// </summary>
		/// <returns>Model that describes which properties are available for filtering</returns>
		Task<LogMessagesFilterModel> GetAvailableFilterDataAsync();
	}

	public class LoggingService : ILoggingService
	{
		private readonly IDataContext _context;

		public LoggingService(IDataContext context)
		{
			_context = context;
		}

		public async Task<LogMessagesFilterModel> GetAvailableFilterDataAsync()
		{
			return new LogMessagesFilterModel
			{
				Severities = await _context
					.LogEntries
					.GroupBy(e => e.Severity)
					.Select(s => (LogEntrySeverities)s.Key.Id)
					.ToListAsync(),
				Categories = await _context
					.LogEntries
					.GroupBy(e => e.Category)
					.Select(c => c.Key)
					.ToListAsync(),
				Sources = await _context
					.LogEntries
					.GroupBy(e => e.Source)
					.Select(c => c.Key)
					.ToListAsync(),
				Start = (await _context
					.LogEntries
					.OrderBy(e => e.Timestamp)
					.FirstOrDefaultAsync())?
					.Timestamp,
				End = (await _context
					.LogEntries
					.OrderByDescending(e => e.Timestamp)
					.FirstOrDefaultAsync())?
					.Timestamp
			};
		}

		public async Task<IEnumerable<LogEntry>> GetLogMessagesAsync(LogMessagesFilterModel filterModel)
		{
			var logs = _context.LogEntries.Include(e => e.Severity).AsQueryable();

			if (filterModel.Sources.Count() > 0)
			{
				logs = logs.Where(e => filterModel.Sources.Contains(e.Source));
				if (await logs.CountAsync() == 0)
				{
					return new List<LogEntry>();
				}
			}

			if (filterModel.Categories.Count() > 0)
			{
				logs = logs.Where(e => filterModel.Categories.Contains(e.Category));
				if (await logs.CountAsync() == 0)
				{
					return new List<LogEntry>();
				}
			}

			if (filterModel.Severities.Count() > 0)
			{
				var severities = await _context
					.LogEntrySeverities
					.Where(s => filterModel.Severities.Contains((LogEntrySeverities)s.Id))
					.ToListAsync();

				logs = logs.Where(e => severities.Contains(e.Severity));
				if (await logs.CountAsync() == 0)
				{
					return new List<LogEntry>();
				}
			}

			if (filterModel.Start.HasValue)
			{
				logs = logs.Where(e => e.Timestamp >= filterModel.Start.Value);
				if (await logs.CountAsync() == 0)
				{
					return new List<LogEntry>();
				}
			}

			if (filterModel.End.HasValue)
			{
				logs = logs.Where(e => e.Timestamp <= filterModel.End.Value);
				if (await logs.CountAsync() == 0)
				{
					return new List<LogEntry>();
				}
			}

			var results = await logs.ToListAsync();

			if (filterModel.Keywords.Count() > 0)
			{
				var keywordResults = new HashSet<LogEntry>();
				foreach (var keyword in filterModel.Keywords)
				{
					keywordResults
						.UnionWith(
							results
								.Where(e => e.Message.Contains(keyword) || e.AuxiliaryData.Contains(keyword))
						);
				}

				results = keywordResults.ToList();
			}

			return results;
		}

		public async Task<LogEntry> GetMessageByIdAsync(int id)
		{
			return await _context
				.LogEntries
				.Include(e => e.Severity)
				.FirstOrDefaultAsync(e => e.Id == id);
		}

		public async Task<int> RecordLogMessageAsync(
			string message,
			string auxData,
			string source,
			int category,
			LogEntrySeverities severity
		)
		{
			// Retrieve requested user action
			var logSeverity =
				await _context
					.LogEntrySeverities
					.FirstAsync(act => act.Id == severity.AsInt());

			var entry = new LogEntry
			{
				Severity = logSeverity,
				Message = message,
				AuxiliaryData = auxData,
				Source = source,
				Category = category
			};

			// Record data
			await _context.LogEntries.AddAsync(entry);

			// Submit changes to data provider
			await _context.SaveChangesAsync();

			// Return Id of a created object in the data provider.
			return entry.Id;
		}
	}

	/// <summary>
	/// Helper model that describes filter criteria for log entries
	/// </summary>
	public class LogMessagesFilterModel
	{
		public IEnumerable<LogEntrySeverities> Severities { get; set; } = new List<LogEntrySeverities>();
		public IEnumerable<int> Categories { get; set; } = new List<int>();
		public IEnumerable<string> Sources { get; set; } = new List<string>();
		public DateTime? Start { get; set; }
		public DateTime? End { get; set; }
		public IEnumerable<string> Keywords { get; set; } = new List<string>();
	}
}
