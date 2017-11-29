using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace StatusMonitor.Web.Services
{
	/// <summary>
	/// Service responsible for performing and verifying user authentication
	/// through MS Identity mechanism using Cookie based auth.
	/// </summary>
	public interface IAuthService
	{
		/// <summary>
		/// Verifies if the user is authenticated
		/// </summary>
		/// <returns>Tru if the user is authenticated, false otherwise</returns>
		bool IsAuthenticated();

		/// <summary>
		/// Signs in the user
		/// </summary>
		Task SignInAsync();

		/// <summary>
		/// Signs out the user
		/// </summary>
		Task SignOutAsync();
	}

	public class AuthService : IAuthService
	{
		private readonly IHttpContextAccessor _http;
		private readonly IConfiguration _config;

		public AuthService(
			IHttpContextAccessor http,
			IConfiguration config
		)
		{
			_http = http;
			_config = config;
		}

		public bool IsAuthenticated()
		{
			return 
				_http.HttpContext.User.Identity.IsAuthenticated ||
				_http.HttpContext.Request.Headers["apikey"] ==  _config["Secrets:ApiKey"];
		}

		public async Task SignInAsync()
		{
			var principal = new ClaimsPrincipal(
					new ClaimsIdentity(
						new List<Claim> {
							new Claim("UserId", "Admin"),
						},
						CookieAuthenticationDefaults.AuthenticationScheme
					)
				);
				
			await _http.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
		}

		public async Task SignOutAsync()
		{
			await _http.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
		}
	}
}
