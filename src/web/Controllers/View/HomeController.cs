using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StatusMonitor.Web.ActionFilters;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Shared.Models.Entities;
using StatusMonitor.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using StatusMonitor.Web.ViewModels;
using System.Collections.Generic;
using System;
using Microsoft.Extensions.Logging;
using StatusMonitor.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StatusMonitor.Web.Controllers.Api;
using System.Net;
using StatusMonitor.Web.Services;

namespace StatusMonitor.Web.Controllers.View
{
	[ServiceFilter(typeof(ModelValidation))]
	/// <summary>
	/// Controller responsible for home endpoints - /home
	/// </summary>
	public class HomeController : Controller
	{
		private readonly IMetricService _metricService;
		private readonly IDataContext _context;
		private readonly IAuthService _auth;
		private readonly IBadgeService _badge;
		private readonly IUptimeReportService _uptime;

		public HomeController(
			IMetricService metricService,
			IDataContext context,
			IAuthService auth,
			IBadgeService badge,
			IUptimeReportService uptime
		)
		{
			_metricService = metricService;
			_context = context;
			_auth = auth;
			_badge = badge;
			_uptime = uptime;
		}


		public async Task<IActionResult> Index()
		{
			var model = await _metricService.GetMetricsAsync();

			if (!_auth.IsAuthenticated())
			{
				model = model.Where(mt => mt.Public).ToList();
			}

			if (model.Count() == 0)
			{
				TempData["MessageSeverity"] = "warning";
				TempData["MessageContent"] = $"No public metrics found in the system.";
			}

			return View(model);
		}

		[Route("Home/Metric/{type}/{source}/{start?}/{end?}")]
		public async Task<IActionResult> Metric(string type, string source, string start = null, string end = null)
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

			if (start != null)
			{
				try
				{
					DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc).AddMilliseconds(Convert.ToInt64(start));
				}
				catch (System.Exception)
				{
					return BadRequest("Bad start date. Needs to be the number of milliseconds since Epoch.");
				}
			}

			if (end != null)
			{
				try
				{
					DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc).AddMilliseconds(Convert.ToInt64(end));
				}
				catch (System.Exception)
				{
					return BadRequest("Bad end date. Needs to be the number of milliseconds since Epoch.");
				}
			}

			if (start != null && end != null)
			{
				if (
					DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc).AddMilliseconds(Convert.ToInt64(start)) >=
					DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc).AddMilliseconds(Convert.ToInt64(end))
				)
				{
					return BadRequest("Bad dates. End date needs to be greater than the start date.");
				}
			}

			var model = await _metricService.GetMetricsAsync(metricType, source);

			if (model.Count() == 0)
			{
				return NotFound();
			}

			if (!_auth.IsAuthenticated() && !model.First().Public)
			{
				return Unauthorized();
			}

			ViewBag.ManualLabels = await _context.ManualLabels.ToListAsync();

			ViewBag.Max = 100;
			ViewBag.Min = 0;

			if (metricType == Metrics.Ping)
			{
				var pingSetting = await _context
					.PingSettings
					.FirstOrDefaultAsync(setting => new Uri(setting.ServerUrl).Host == source);

				ViewBag.Max = pingSetting.MaxResponseTime.TotalMilliseconds;

				ViewBag.Uptime = await _uptime.ComputeUptimeAsync(source);
			}

			ViewBag.Start = start ?? 0.ToString();
			ViewBag.End = end ?? 0.ToString();

			return View(model.First());
		}

	}
}
