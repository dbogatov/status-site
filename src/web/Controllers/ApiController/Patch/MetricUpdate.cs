using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StatusMonitor.Web.ActionFilters;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Shared.Models.Entities;
using StatusMonitor.Web.ViewModels;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace StatusMonitor.Web.Controllers.Api
{
	public partial class ApiController
	{
		[HttpPatch]
		[ServiceFilter(typeof(ApiKeyCheck))]
		[ServiceFilter(typeof(ModelValidation))]
		public async Task<IActionResult> MetricUpdate(MetricUpdateViewModel model)
		{
			if (await _context.Metrics.AnyAsync(mt => mt.Source == model.Source && mt.Type == model.MetricType.AsInt()))
			{
				if (await _context.ManualLabels.AnyAsync(lbl => lbl.Id == model.ManualLabelId))
				{
					var manualLabel = await _context.ManualLabels.FirstAsync(lbl => lbl.Id == model.ManualLabelId);
					var metric = await _context.Metrics.FirstAsync(mt => mt.Source == model.Source && mt.Type == model.MetricType.AsInt());

					metric.ManualLabel = manualLabel;
					metric.Public = model.Public;

					await _context.SaveChangesAsync();

					return Ok("Metric manual label has been updated.");
				}
				else
				{
					return NotFound($"Manual label with id {model.ManualLabelId} is not found");
				}
			}
			else
			{
				return NotFound($"Metric of type {model.MetricType} and source {model.Source} is not found");
			}
		}
	}
}
