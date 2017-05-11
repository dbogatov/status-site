using StatusMonitor.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StatusMonitor.Shared.Extensions;

namespace StatusMonitor.Web.Controllers.View
{
	/// <summary>
	/// Controller responsible for error endpoints - /error
	/// </summary>
	public class ErrorController : Controller
	{
		private readonly ILogger<ErrorController> _logger;

		public ErrorController(ILogger<ErrorController> logger)
		{
			_logger = logger;
		}

		/// <summary>
		/// Show error page for specific code
		/// </summary>
		/// <param name="code">HTTP error code</param>
		[Route("/error/{code?}")]
		public IActionResult Error(int? code)
		{
			_logger.LogWarning(
				LoggingEvents.GenericError.AsInt(), 
				$"Error page is invoked with code {(code.HasValue ? code.Value : 404)}"
			);

			return View(new ErrorViewModel(code.HasValue ? code.Value : 404));
		}
	}
}
