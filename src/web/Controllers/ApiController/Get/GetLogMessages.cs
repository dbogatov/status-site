using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StatusMonitor.Web.ActionFilters;
using StatusMonitor.Web.ViewModels;

namespace StatusMonitor.Web.Controllers.Api
{
	public partial class ApiController
	{
		[HttpGet]
		[Produces("application/json")]
		[ServiceFilter(typeof(ApiKeyCheck))]
		[ServiceFilter(typeof(ModelValidation))]
		public async Task<IActionResult> GetLogMessages(LogMessagesFilterViewModel model)
		{
			var messages = (await _loggingService
				.GetLogMessagesAsync(
					model.ToLogMessagesFilterModel()
				))
				.Select(msg => new
				{
					msg.AuxiliaryData,
					msg.Id,
					msg.Message,
					Severity = msg.Severity.Description,
					msg.Source,
					msg.Timestamp,
					msg.Category
				});		

			if (messages.Count() == 0)
			{
				return NoContent();
			}
			else
			{
				return Json(messages);
			}
		}
	}
}
