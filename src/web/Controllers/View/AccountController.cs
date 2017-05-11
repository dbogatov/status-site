using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using StatusMonitor.Web.ActionFilters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using StatusMonitor.Web.ViewModels;
using System.Threading.Tasks;
using StatusMonitor.Web.Services;

namespace StatusMonitor.Web.Controllers.View
{
	/// <summary>
	/// Controller responsible for authentication endpoints - /account
	/// </summary>
	public class AccountController : Controller
	{
		private readonly IConfiguration _config;
		private readonly IAuthService _auth;

		public AccountController(
			IConfiguration config,
			IAuthService auth
		)
		{
			_config = config;
			_auth = auth;
		}

		public IActionResult Login()
		{
			return View(new ReturnUrlViewModel
			{
				ReturnUrl = Request.Query["returnurl"].FirstOrDefault() ?? "",
				IsError = false
			});
		}

		public async Task<IActionResult> Logout()
		{
			await _auth.SignOutAsync();

			TempData["MessageSeverity"] = "info";
			TempData["MessageContent"] = $"You have logged out.";

			if (string.IsNullOrEmpty(Request.Query["returnurl"]))
			{
				return RedirectToAction("Index", "Home");
			}
			else
			{
				return Redirect(Request.Query["returnurl"]);
			}
		}

		[ServiceFilter(typeof(ReCaptcha))]
		public async Task<IActionResult> Authenticate()
		{
			if (!ModelState.IsValid)
			{
				return View(
					"Login",
					new ReturnUrlViewModel
					{
						ReturnUrl = Request.Query["returnurl"],
						IsError = true,
						Error = "Wrong reCAPTCHA"
					}
				);
			}

			if (Request.Form["password"] == _config["Secrets:AdminPassword"])
			{
				await _auth.SignInAsync();

				TempData["MessageSeverity"] = "info";
				TempData["MessageContent"] = $"You have logged in.";

				if (string.IsNullOrEmpty(Request.Query["returnurl"]))
				{
					return RedirectToAction("Index", "Home");
				}
				else
				{
					return Redirect(Request.Query["returnurl"]);
				}
			}
			else
			{
				return View("Login", new ReturnUrlViewModel
				{
					ReturnUrl = Request.Query["returnurl"],
					IsError = true,
					Error = "Wrong password"
				});
			}
		}
	}
}
