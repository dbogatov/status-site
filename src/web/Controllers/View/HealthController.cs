using StatusMonitor.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StatusMonitor.Shared.Extensions;
using System.Threading.Tasks;
using StatusMonitor.Shared.Models;
using StatusMonitor.Web.Services;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using StatusMonitor.Shared.Models.Entities;
using StatusMonitor.Shared.Services;

namespace StatusMonitor.Web.Controllers.View
{
	/// <summary>
	/// Controller responsible for health endpoints - /health
	/// </summary>
	public class HealthController : Controller
	{
		private readonly IMetricService _metricService;
		private readonly IDataContext _context;
		private readonly IAuthService _auth;
		private readonly IBadgeService _badge;

		public HealthController(
			IMetricService metricService,
			IDataContext context,
			IAuthService auth,
			IBadgeService badge
		)
		{
			_metricService = metricService;
			_context = context;
			_auth = auth;
			_badge = badge;
		}

		public async Task<IActionResult> Index()
		{
			return
				await _context.HealthReports.CountAsync() > 0 ?
				new BadgeResult(
					_badge.GetSystemHealthBadge(
						await _context.HealthReports.OrderByDescending(hp => hp.Timestamp).FirstAsync()
					)
				) :
				(IActionResult)NoContent();
		}

		[Route("health/{type}/{source}")]
		public async Task<IActionResult> Metric(string type, string source)
		{
			Metrics metricType;

			try
			{
				metricType = type.ToEnum<Metrics>();
			}
			catch (System.Exception)
			{
				return BadRequest("Bad type. Needs to be one of Metrics type.");
			}

			var metrics = await _metricService.GetMetricsAsync(metricType, source);

			if (metrics.Count() == 0)
			{
				return NotFound();
			}

			var metric = metrics.First();

			if (!_auth.IsAuthenticated() && !metric.Public)
			{
				return Unauthorized();
			}

			return new BadgeResult(
				_badge.GetMetricHealthBadge(
					metric.Source, (Metrics)metric.Type, (AutoLabels)metric.AutoLabel.Id
				)
			);
		}
	}
}
