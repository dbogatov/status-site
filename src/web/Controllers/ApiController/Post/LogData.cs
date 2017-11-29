using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StatusMonitor.Web.ActionFilters;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Shared.Models.Entities;
using StatusMonitor.Web.ViewModels;

namespace StatusMonitor.Web.Controllers.Api
{
	public partial class ApiController
	{
		[HttpPost]
		[ServiceFilter(typeof(ApiKeyCheck))]
		[ServiceFilter(typeof(ModelValidation))]
		public async Task<IActionResult> LogData(LogDataViewModel model)
		{
			// Retrieve requested metric
			var metric = await _metricService.GetOrCreateMetricAsync(Metrics.Log, model.Source);

			_context.Attach(metric);

			// Retrieve requested user action
			var logSeverity =
				await _context
					.LogEntrySeverities
					.FirstAsync(act => act.Id == model.MessageSeverity.AsInt());

			// Record data
			await _context
				.LogDataPoints
				.AddAsync(
					new LogDataPoint
					{
						Severity = logSeverity,
						Count = model.Count,
						Metric = metric
					}
				);

			// Submit changes to data provider
			await _context.SaveChangesAsync();

			return Ok("Data point has been recorded.");
		}
	}
}
