using System;
using System.ComponentModel.DataAnnotations;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Shared.Models.Entities;

namespace StatusMonitor.Web.ViewModels
{
	/// <summary>
	/// View model identifying parameters for User Action API endpoint.
	/// </summary>
	public class UserActionViewModel
	{
		[Required]
		public string Action { get; set; }

		[Required]
		[StringLength(32)]
		[RegularExpression("[a-z0-9\\.\\-]+")]
		/// <summary>
		/// Required. Source identifier. May be server id or website URL.
		/// </summary>
		public string Source { get; set; }

		/// <summary>
		/// Number of times Action has been performed.
		/// By default: 1.
		/// </summary>
		public int Count { get; set; } = 1;
	}
}
