using System;
using System.ComponentModel.DataAnnotations;

namespace StatusMonitor.Shared.Models.Entities
{
	/// <summary>
	/// Entity representing a set of parameters for one ping target.
	/// </summary>
	public class PingSetting
	{
		[Key]
		public string ServerUrl { get; set; }
		/// <summary>
		/// Time period after which the server needs to be considered unresponsive
		/// </summary>
		public TimeSpan MaxResponseTime { get; set; } = new TimeSpan(0, 0, 0, 0, 2000);
		/// <summary>
		/// Number of times to try to ping the server before calling it dead.
		/// </summary>
		public int MaxFailures { get; set; } = 3;
		/// <summary>
		/// Whether or not the server needs to be ping by GET request instead of HEAD.
		/// </summary>
		public bool GetMethodRequired { get; set; } = false;
	}

}
