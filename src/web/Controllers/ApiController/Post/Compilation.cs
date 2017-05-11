using System;
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
		public async Task<IActionResult> Compilation(CompilationViewModel model)
		{
			// Retrieve requested metric
			var metric = await _metricService.GetOrCreateMetricAsync(Metrics.Compilation, model.Source);

			// Retrieve requested user action
			var compilationStage =
				await _context
				.CompilationStages
				.FirstAsync(act => act.Id == model.CompilationStage.AsInt());

			// Record data
			await _context
				.CompilationDataPoints
				.AddAsync(
					new CompilationDataPoint
					{
						SourceSize = model.SourceSize,
						Stage = compilationStage,
						CompileTime = new TimeSpan(0, 0, model.CompileTime),
						Metric = metric
					}
				);

			// Submit changes to data provider
			await _context.SaveChangesAsync();

			return Ok("Data point has been recorded.");
		}
	}
}
