using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StatusMonitor.Shared.Models.Entities;

namespace StatusMonitor.Web.Views.ViewComponents
{
	/// <summary>
	/// View component responsible for rendering build hash.
	/// </summary>
	public class LogViewViewComponent : ViewComponent
	{
		public async Task<IViewComponentResult> InvokeAsync(LogEntry model, bool modal = false)
		{
			// hack
			await Task.CompletedTask;

			ViewBag.Modal = modal;

			return View(model);
		}
	}
}
