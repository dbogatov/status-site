using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using StatusMonitor.Shared.Extensions;

namespace StatusMonitor.Web.ActionFilters
{
	/// <summary>
	/// Action filter that prints out view model
	/// </summary>
	public class LogModel : ActionFilterAttribute
	{
		private readonly ILoggerFactory _factory;

		public LogModel(ILoggerFactory factory)
		{
			_factory = factory;
		}

		public override void OnResultExecuted(ResultExecutedContext context)
		{
			var request = $"{context.HttpContext.Request.Method}: {context.HttpContext.Request.Path}";

			var logger = _factory.CreateLogger(
				$"{context.Controller.GetType().ToShortString()} | {this.GetType().ToShortString()} | {request}"
			);
			try
			{
				logger.LogDebug(LoggingEvents.ModelState.AsInt(), ((Controller)context.Controller).ViewData.Model.ToString());
			}
			catch (System.Exception e)
			{
				logger.LogError(LoggingEvents.ModelState.AsInt(), e, "Model error");
			}
		}
	}
}
