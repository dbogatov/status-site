using StatusMonitor.Shared.Extensions;

namespace StatusMonitor.Web.ViewModels
{
	public class ErrorViewModel : ExtendedObject
	{
		public ErrorViewModel(int code)
		{
			this.Code = code;
			switch (code)
			{
				case 404:
					this.Message = "The page requested is not found";
					break;
				case 400:
					this.Message = "Bad request";
					break;
				case 403:
					this.Message = "Forbidden";
					break;
				case 401:
					this.Message = "You are not authorized to view this page";
					break;
				case 500:
					this.Message = "We've got an internal server error";
					break;
				default:
					this.Message = "Unexpected error occurred";
					break;
			}
		}

		public int Code { get; set; }
		public string Message { get; set; }
	}
}
