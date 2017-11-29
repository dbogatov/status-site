using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StatusMonitor.Web.ActionFilters;
using StatusMonitor.Shared.Models.Entities;
using StatusMonitor.Web.ViewModels;

namespace StatusMonitor.Web.Controllers.Api
{
	public partial class ApiController
	{
		[HttpPost]
		[ServiceFilter(typeof(ApiKeyCheck))]
		[ServiceFilter(typeof(ModelValidation))]
		public async Task<IActionResult> CpuLoad(CpuLoadViewModel model)
		{
			// Retrieve requested metric
			var metric = await _metricService.GetOrCreateMetricAsync(Metrics.CpuLoad, model.Source);
			
			_context.Attach(metric);
			
			// Record data
			await _context
				.NumericDataPoints
				.AddAsync(
					new NumericDataPoint
					{
						Value = model.Value,
						Metric = metric
					}
				);

			// Submit changes to data provider
			await _context.SaveChangesAsync();

			return Ok("Data point has been recorded.");
		}
	}
}
