using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using StatusMonitor.Shared.Extensions;

namespace StatusMonitor.Web.Views.ViewComponents
{
	/// <summary>
	/// View component responsible for rendering build hash.
	/// </summary>
	public class VersionHashViewComponent : ViewComponent
	{
		private readonly IConfiguration _configuration;
		private readonly IHostingEnvironment _env;

		public VersionHashViewComponent(
			IHostingEnvironment env,
			IConfiguration configuration
		)
		{
			_configuration = configuration;
			_env = env;
		}

		/// <summary>
		/// Called by the framework. Returns a view for the version hash.
		/// </summary>
		/// <returns>View for the version hash</returns>
		public async Task<IViewComponentResult> InvokeAsync()
		{
			await Task.CompletedTask;

			var hash = _env.IsProduction() ? _configuration["Version:GitHash"].Substring(0,8) ?? "not-set" : "dev";

			return View((object)hash);
		}
	}
}
