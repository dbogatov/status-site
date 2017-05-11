using System.Collections.Generic;
using System.Net.Http;
using StatusMonitor.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StatusMonitor.Shared.Extensions;

namespace StatusMonitor.Web.ActionFilters
{
	/// <summary>
	/// Action filter that extracts ReCaptcha challenge response and verifies it with Google
	/// </summary>
	public class ReCaptcha : ActionFilterAttribute
	{
		private readonly string CAPTCHA_URL = "https://www.google.com/recaptcha/api/siteverify";
		private readonly string SECRET = "set-by-config";
		private readonly IConfiguration _config;
		private readonly ILogger _logger;

		public ReCaptcha(IConfiguration config, ILogger<ReCaptcha> logger)
		{
			_config = config;
			_logger = logger;

			SECRET = config["Secrets:ReCaptcha:SecretKey"];
		}

		public override async Task OnActionExecutionAsync(ActionExecutingContext filterContext, ActionExecutionDelegate next)
		{
			// No need for localhost
			if (Convert.ToBoolean(_config["Secrets:ReCaptcha:Enabled"]))
			{
				try
				{
					// Get recaptcha value
					var captchaResponse = filterContext.HttpContext.Request.Form["g-recaptcha-response"];

					using (var client = new HttpClient())
					{
						var values = new Dictionary<string, string>
						{
							{ "secret", SECRET },
							{ "response", captchaResponse },
							{ "remoteip", filterContext.HttpContext.Request.HttpContext.Connection.RemoteIpAddress.ToString() }
						};

						var content = new FormUrlEncodedContent(values);

						var result = await client.PostAsync(CAPTCHA_URL, content);

						if (result.IsSuccessStatusCode)
						{
							string responseString = await result.Content.ReadAsStringAsync();

							var captchaResult = JsonConvert.DeserializeObject<CaptchaResponseViewModel>(responseString);

							if (!captchaResult.Success)
							{
								_logger.LogWarning(LoggingEvents.ActionFilters.AsInt(), "Captcha not solved");

								((Controller)filterContext.Controller).ModelState.AddModelError("ReCaptcha", "Captcha not solved");
							}
						}
						else
						{
							_logger.LogWarning(LoggingEvents.ActionFilters.AsInt(), "Unknown captcha error");

							((Controller)filterContext.Controller).ModelState.AddModelError("ReCaptcha", "Captcha error");
						}
					}

				}
				catch (System.Exception e)
				{
					_logger.LogError(LoggingEvents.ActionFilters.AsInt(), e, "Unknown error");

					((Controller)filterContext.Controller).ModelState.AddModelError("ReCaptcha", "Unknown error");
				}
			}
			
			await next();
		}
	}
}
