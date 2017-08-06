using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Shared.Models.Entities;

namespace StatusMonitor.Web.Views.ViewComponents
{
	/// <summary>
	/// View component responsible for rendering discrepancy card.
	/// </summary>
	[ViewComponent(Name = "DiscrepancyCard")]
	public class DiscrepancyCardViewComponent : ViewComponent
	{
		public async Task<IViewComponentResult> InvokeAsync(Discrepancy model, int number = 0, bool hidden = false)
		{
			await Task.CompletedTask;

			ViewBag.Number = number;
			ViewBag.Hidden = hidden;

			return View(model);
		}
	}
}
