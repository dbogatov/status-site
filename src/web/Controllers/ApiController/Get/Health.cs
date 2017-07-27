using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StatusMonitor.Web.ActionFilters;
using StatusMonitor.Shared.Models.Entities;
using StatusMonitor.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace StatusMonitor.Web.Controllers.Api
{
	public partial class ApiController
	{
		[HttpGet]
		[Produces("application/json")]
		public async Task<IActionResult> Health()
		{
			return
				await _context.HealthReports.CountAsync() > 0 ?
			 	Json(await _context.HealthReports.OrderByDescending(hp => hp.Timestamp).FirstAsync()) :
				(IActionResult)NoContent();
		}
	}
}
