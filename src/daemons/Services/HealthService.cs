using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StatusMonitor.Shared.Models;
using StatusMonitor.Shared.Models.Entities;

namespace StatusMonitor.Daemons.Services
{
	public interface IHealthService
	{
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
				Health =
					(int)Math.Round(
						(
							(double)(await _context
								.Metrics
								.Include(mt => mt.AutoLabel)
								.ToListAsync())
								.GroupBy(mt => mt.AutoLabel)
								.Aggregate(0, (sum, element) => sum + element.Key.DamageUnit() * element.Count())
							/
							(
								await _context
									.Metrics
									.CountAsync()
									*
									AutoLabel.MaxHealthValue()
							)
						) * 100
					),

				Data = 
					await _context
						.Metrics
						.Include(mt => mt.AutoLabel)
						.Select(mt => new HealthReportDataPoint {
							Source = mt.Source,
							MetricType = (Metrics)mt.Type,
							MetricLabel = (AutoLabels)mt.AutoLabel.Id
						})
						.ToArrayAsync()
			};

			_logger.LogDebug($"Health report created. Health level: {report.Health}");

			return report;
		}
	}
}
