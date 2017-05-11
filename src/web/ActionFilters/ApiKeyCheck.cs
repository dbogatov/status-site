using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StatusMonitor.Shared.Extensions;

namespace StatusMonitor.Web.ActionFilters
{
	/// <summary>
	/// Underlying class for ApiKeyCheck attribute.
	/// Adds model error if API key is wrong or missing.
	/// See https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/filters for documentation on Action Filters
	/// </summary>
	public class ApiKeyCheck : ActionFilterAttribute
	{
		/// <summary>
		/// API Key provided by configuration (appsettings.json).
		/// </summary>
		private readonly string _apiKey = "set-by-config";
		private readonly ILoggerFactory _factory;

		public ApiKeyCheck(
			ILoggerFactory factory,
			IConfiguration config
		)
		{
			_factory = factory;
			_apiKey = config["Secrets:ApiKey"];
		}

		/// <summary>
		/// This method is called by the framework.
		/// Perform actions that need to be done before control flow reaches the actual action where attribute is put.
		/// </summary>
		/// <param name="context">Execution context. Most importantly, HttpContext.</param>
		/// <param name="next">Delegate to the next stage in the pipeline. If not called, action body
		/// will not be executed.</param>
		public override async Task OnActionExecutionAsync(
			ActionExecutingContext context,
			ActionExecutionDelegate next)
		{
			var request = $"{context.HttpContext.Request.Method}: {context.HttpContext.Request.Path}";
			var logger = _factory.CreateLogger($"{context.Controller.GetType().ToShortString()} | {this.GetType().ToShortString()} | {request}");

			if (context.HttpContext.User.Claims.Any(c => c.Type == "UserId"))
			{
				logger.LogInformation(LoggingEvents.ApiCheck.AsInt(), "Api key check bypassed for admin");
			}
			else
			{
				// Get API key from the headers
				var key = context.HttpContext.Request.Headers["apikey"];

				// Add error for missing key
				if (string.IsNullOrEmpty(key))
				{
					logger.LogWarning(LoggingEvents.ApiCheck.AsInt(), "Authentication failed: API key missing");
					((Controller)context.Controller).ModelState.AddModelError("Authentication", "API key missing");
				}

				// Add error for invalid key
				if (key != _apiKey)
				{
					logger.LogWarning(LoggingEvents.ApiCheck.AsInt(), "Authentication failed: wrong API key");
					((Controller)context.Controller).ModelState.AddModelError("Authentication", "Wrong API key");
				}
			}

			// Continue pipeline
			await next();
		}
	}
}
