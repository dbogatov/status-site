using System.Collections.Generic;

namespace StatusMonitor.Web.ViewModels
{
	public class LoginViewModel
	{
		public string Password { get; set; }
	}
	public class ReturnUrlViewModel
	{
		public string ReturnUrl { get; set; }
		public bool IsError { get; set; }
		public string Error { get; set; }
	}
	public class CaptchaResponseViewModel
	{
		public bool Success { get; set; }
		public IEnumerable<string> ErrorCodes { get; set; }
	}
}
