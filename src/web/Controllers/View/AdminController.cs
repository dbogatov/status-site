using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using StatusMonitor.Web.ActionFilters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using StatusMonitor.Web.ViewModels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using StatusMonitor.Shared.Services;
using Microsoft.Extensions.Logging;
using StatusMonitor.Shared.Models;
using System;
using Microsoft.Extensions.DependencyInjection;
using StatusMonitor.Web.Controllers.Api;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Shared.Models.Entities;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using StatusMonitor.Web.Services;

namespace StatusMonitor.Web.Controllers.View
{
	[Authorize]
	/// <summary>
	/// Controller responsible for admin endpoints - /admin
	/// </summary>
	public class AdminController : Controller
	{
		private readonly ILoggingService _loggingService;
		private readonly ILogger<AdminController> _logger;
		private readonly IMetricService _metricService;
		private readonly IServiceProvider _provider;
		private readonly ICleanService _cleanService;
		private readonly IDataContext _context;


		public AdminController(
			ILoggingService loggingService,
			ILogger<AdminController> logger,
			IMetricService metricService,
			IServiceProvider provider,
			ICleanService cleanService,
			IDataContext context
		)
		{
			_metricService = metricService;
			_logger = logger;
			_loggingService = loggingService;
			_provider = provider;
			_cleanService = cleanService;
			_context = context;
		}

		public async Task<IActionResult> Index()
		{
			ViewBag.Metrics = await _metricService
				.GetMetricsAsync();

			ViewBag.Discrepancies = await _context
				.Discrepancies
				.OrderByDescending(d => d.DateFirstOffense)
				.ToListAsync();

			return View();
		}

		[HttpPost]
		[ServiceFilter(typeof(ModelValidation))]
		public async Task<IActionResult> DeleteMetric(MetricRemovalViewModel model)
		{
			var response = await _provider.GetRequiredService<IApiController>().RemoveMetric(model);
			if (response is OkObjectResult)
			{
				TempData["MessageSeverity"] = "success";
				TempData["MessageContent"] = $"Metric {model.Type} from {model.Source} and its data have been deleted.";

				return RedirectToAction("Index", "Home");
			}
			else
			{
				return response;
			}
		}

		public IActionResult Metric([FromQuery] string metric)
		{
			if (metric == null)
			{
				return BadRequest("Metric is not specified");
			}

			return RedirectToAction("Metric", "Home", new { Type = metric.Split('@')[0], Source = metric.Split('@')[1] });
		}

		[HttpPost]
		public async Task<IActionResult> Clean(int? maxAge)
		{
			if (!maxAge.HasValue)
			{
				return BadRequest("Max age is not specified");
			}

			await _cleanService.CleanDataPointsAsync(new TimeSpan(0, maxAge.Value, 0));

			TempData["MessageSeverity"] = "success";
			TempData["MessageContent"] = $"Data older than {maxAge} minutes has been cleaned.";

			return RedirectToAction("Index");
		}

		[HttpPost]
		[ServiceFilter(typeof(ModelValidation))]
		public async Task<IActionResult> UpdateMetric(MetricUpdateViewModel model)
		{
			var response = await _provider.GetRequiredService<IApiController>().MetricUpdate(model);
			if (response is OkObjectResult)
			{
				TempData["MessageSeverity"] = "success";
				TempData["MessageContent"] = $"Metric has been updated.";

				return RedirectToAction("Metric", "Home", new { type = model.Type.ToString(), source = model.Source });
			}
			else
			{
				return response;
			}
		}

		public async Task<IActionResult> Log(int? id)
		{
			if (id.HasValue)
			{
				var selectedMessage = await _loggingService.GetMessageByIdAsync(id.Value);

				if (selectedMessage == null)
				{
					return NotFound($"Log Entry with ID {id.Value} does not exist.");
				}

				return View(selectedMessage);
			}
			else
			{
				return RedirectToAction("Logs");
			}
		}

		public async Task<IActionResult> Logs(LogMessagesFilterViewModel model)
		{
			if (model.Id.HasValue)
			{
				return RedirectToActionPermanent("Log", new { id = model.Id.Value });
			}

			try
			{
				ViewBag.Messages = await _loggingService.GetLogMessagesAsync(model.ToLogMessagesFilterModel());
			}
			catch (System.Exception e)
			{
				_logger.LogWarning(LoggingEvents.HomeController.AsInt(), e, "Bad filter for logs");
				return BadRequest("Bad filter parameters");
			}

			ViewBag.FilterData = await _loggingService.GetAvailableFilterDataAsync();

			if (ViewBag.Messages.Count == 0)
			{
				TempData["MessageSeverity"] = "warning";
				TempData["MessageContent"] = $"No log messages found in the system.";
			}

			return View(model);
		}
	}
}
