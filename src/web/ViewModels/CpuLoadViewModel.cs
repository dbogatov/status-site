using System.ComponentModel.DataAnnotations;

namespace StatusMonitor.Web.ViewModels
{
	/// <summary>
	/// View model identifying parameters for CPU Load API endpoint.
	/// </summary>
	public class CpuLoadViewModel
	{
		[Required]
		/// <summary>
		/// Required. A numeric value for CPU Load (like percentage).
		/// </summary>
		public int Value { get; set; }

		[Required]
		[StringLength(32)]
		[RegularExpression("[a-z0-9\\.\\-]+")]
		/// <summary>
		/// Required. Source identifier. Should be server id in the case of CPU Load.
		/// </summary>
		public string Source { get; set; }
	}
}
