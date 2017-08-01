using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Shared.Models;
using StatusMonitor.Shared.Models.Entities;

namespace StatusMonitor.Shared.Services
{
	public interface IUptimeReportService
	{
		/// <summary>
		/// Computes uptime as a percentage of "OK" ping dta points
		/// </summary>
		/// <param name="source">Source fo the Ping metric</param>
		/// <returns>Uptime percentage</returns>
		Task<int> ComputeUptimeAsync(string source);
	}

	public class UptimeReportService : IUptimeReportService
	{
		private readonly IDataContext _context;

		public UptimeReportService(IDataContext context)
		{
			_context = context;
		}

		public async Task<int> ComputeUptimeAsync(string source)
		{
			var metric = _context.Metrics.Single(mt => mt.Type == Metrics.Ping.AsInt() && mt.Source == source);

			return 
				await _context
					.PingDataPoints
					.AnyAsync(dp => dp.Metric == metric)
					?
						(int)Math.Round(100*
						(
							(double)await _context
								.PingDataPoints
								.Where(dp => dp.Metric == metric && dp.Success)
								.CountAsync()
							/
							await _context
								.PingDataPoints
								.Where(dp => dp.Metric == metric)
								.CountAsync()
						))
					:
						0
			;
		}
	}
}
