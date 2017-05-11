using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using StatusMonitor.Shared.Extensions;

namespace StatusMonitor.Web.ActionFilters
{
	/// <summary>
	/// Underlying class for ModelValidation attribute.
	/// Returns (short-circuits pipeline) if model state is invalid. 
	/// </summary>
	public class ModelValidation : ActionFilterAttribute
	{
		private readonly ILoggerFactory _factory;

		public ModelValidation(ILoggerFactory factory)
		{
			_factory = factory;
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

			var logger = _factory.CreateLogger(
				$"{context.Controller.GetType().ToShortString()} | {this.GetType().ToShortString()} | {request}"
			);

			// logger.LogDebug(LoggingEvents.ModelState.AsInt(), ((Controller)context.Controller).ViewData.Model.ToString());

			if (((Controller)context.Controller).ModelState.IsValid)
			{
				logger.LogInformation(LoggingEvents.ModelState.AsInt(), "Model state is valid.");

				// Continue pipeline
				await next();
			}
			else
			{
				// Set result if models state has error
				// Setting result is equivalent to shorting a circuit (returning)
				if (((Controller)context.Controller).ModelState.ContainsKey("Authentication"))
				{
					logger.LogWarning(LoggingEvents.ModelState.AsInt(), "Model state invalid: authentication failed.");
					context.Result = new UnauthorizedResult();
				}
				else
				{
					logger.LogWarning(LoggingEvents.ModelState.AsInt(), "Model state invalid: required parameter missing or malformed.");
					context.Result = new BadRequestObjectResult("Required parameter missing or malformed.");
				}
			}
		}
	}
}
