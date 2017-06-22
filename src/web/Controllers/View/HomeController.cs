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

		public HomeController(
			IMetricService metricService,
			IDataContext context,
			IAuthService auth
		)
		{
			_metricService = metricService;
			_context = context;
			_auth = auth;
		}

		public async Task<IActionResult> Index()
		{
			var model = (await _metricService.GetMetricsAsync())
				.Where(mt =>
					mt.Type == Metrics.CpuLoad.AsInt() ||
					mt.Type == Metrics.Ping.AsInt()
				)
				.ToList();

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

		[Route("Home/Metric/{type}/{source}")]
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
			}

			return View(model.First());
		}

	}
}
