using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StatusMonitor.Shared.Models;
using StatusMonitor.Shared.Models.Entities;

namespace StatusMonitor.Daemons.Services
{
	/// <summary>
	/// Produces overall health report of the system
	/// </summary>
	public interface IHealthService
	{
		/// <summary>
		/// Returns a health report of the whole system at the moment
		/// Analyzes only public metrics
		/// </summary>
		/// <returns>Overall health report of the system</returns>
		Task<HealthReport> ProduceHealthReportAsync();
	}

	public class HealthService : IHealthService
	{
		private readonly IDataContext _context;
		private readonly ILogger<HealthService> _logger;

		public HealthService(IDataContext context, ILogger<HealthService> logger)
		{
			_context = context;
			_logger = logger;
		}

		public async Task<HealthReport> ProduceHealthReportAsync()
		{
			if (await _context.Metrics.CountAsync() == 0)
			{
				_logger.LogDebug($"No metrics, no report");
				return new HealthReport();
			}

			var report = new HealthReport
			{
				Data = 
					await _context
						.Metrics
						.Include(mt => mt.AutoLabel)
						.Where(mt => mt.Public)
						.Select(mt => new HealthReportDataPoint {
							Source = mt.Source,
							MetricType = (Metrics)mt.Type,
							MetricLabel = (AutoLabels)mt.AutoLabel.Id
						})
						.ToListAsync()
			};

			_logger.LogDebug($"Health report created. Health level: {report.Health}");

			return report;
		}
	}
}
