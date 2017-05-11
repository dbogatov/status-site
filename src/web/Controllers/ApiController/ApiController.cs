using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StatusMonitor.Shared.Models;
using StatusMonitor.Shared.Models.Entities;
using StatusMonitor.Shared.Services;
using StatusMonitor.Web.Services;
using StatusMonitor.Web.ViewModels;

namespace StatusMonitor.Web.Controllers.Api
{
	/// <summary>
	/// Methods that we want to expose for classes directly using API Controller
	/// withing framework (eq. controller.RemoveMetric(...) )
	/// </summary>
	public interface IApiController
	{
		Task<IActionResult> RemoveMetric(MetricRemovalViewModel model);
		Task<IActionResult> MetricUpdate(MetricUpdateViewModel model);
		Task<IActionResult> CpuLoad(CpuLoadViewModel model);
	}

	[Produces("text/plain")]
	/// <summary>
	/// This controller is responsible for API endpoints.
	/// The routing works as follows /{controller}/{action}, so in this case /api/cpuload will lead to 
	/// ApiController.CpuLoad method.
	/// Documentation for the endpoints is in Swagger YML file.
	/// </summary>
	public partial class ApiController : Controller, IApiController
	{
		// Context for data provider
		private readonly IDataContext _context;
		private readonly IMetricService _metricService;
		private readonly ILogger<ApiController> _logger;
		private readonly ILoggingService _loggingService;
		private readonly IConfiguration _config;
		private readonly INotificationService _notify;
		private readonly IAuthService _auth;

		public ApiController(
			IDataContext context,
			IMetricService metricService,
			ILogger<ApiController> logger,
			ILoggingService loggingService,
			IConfiguration config,
			INotificationService notify,
			IAuthService auth
		)
		{
			_context = context;
			_metricService = metricService;
			_logger = logger;
			_loggingService = loggingService;
			_config = config;
			_notify = notify;
			_auth = auth;
		}
	}
}
