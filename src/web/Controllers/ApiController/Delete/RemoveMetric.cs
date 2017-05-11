using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StatusMonitor.Web.ActionFilters;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Web.ViewModels;

namespace StatusMonitor.Web.Controllers.Api
{
	public partial class ApiController
	{
		[HttpDelete]
		[ServiceFilter(typeof(ApiKeyCheck))]
		[ServiceFilter(typeof(ModelValidation))]
		public async Task<IActionResult> RemoveMetric(MetricRemovalViewModel model)
		{
			var metric = await _context
				.Metrics
				.FirstOrDefaultAsync(mt =>
					mt.Source == model.Source &&
					mt.Type == model.MetricType.AsInt()
				);

			if (metric == null)
			{
				return NotFound();
			}

			_context.Remove(metric);
			await _context.SaveChangesAsync();

			_logger.LogWarning(LoggingEvents.Metrics.AsInt(), $"Metric {model.MetricType} of {model.Source} has been deleted.");

			return Ok("Metric has successfully been removed.");
		}
	}
}
