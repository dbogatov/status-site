using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StatusMonitor.Web.ActionFilters;
using StatusMonitor.Shared.Models.Entities;
using StatusMonitor.Web.ViewModels;

namespace StatusMonitor.Web.Controllers.Api
{
	public partial class ApiController
	{
		[HttpGet]
		[Produces("application/json")]
		[ServiceFilter(typeof(ModelValidation))]
		public async Task<IActionResult> GetMetrics(MetricRequestViewModel model)
		{
			var metrics = await _metricService.GetMetricsAsync(
				model.MetricType == 0 ? (Metrics?)null : model.MetricType,
				model.Source
			);

			if (!_auth.IsAuthenticated())
			{
				metrics = metrics.Where(mt => mt.Public).ToList();
			}

			if (metrics.Count() == 0)
			{
				return NoContent();
			}

			return Json(
				metrics.Select(mt => new
				{
					mt.Source,
					mt.Title,
					Type = mt.Type.ToString(),
					AutoLabel = new
					{
						Title = mt.AutoLabel.Title,
						Severity = ((AutoLabels)mt.AutoLabel.Id).ToString()
					},
					ManualLabel = new
					{
						Title = mt.ManualLabel.Title,
						Severity = ((ManualLabels)mt.ManualLabel.Id).ToString()
					},
					mt.LastUpdated,
					mt.DayAvg,
					mt.DayMax,
					mt.DayMin,
					mt.HourAvg,
					mt.HourMax,
					mt.HourMin,
					mt.CurrentValue
				})
				.ToList()
			);
		}
	}
}
