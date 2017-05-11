using System;
using System.ComponentModel.DataAnnotations;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Shared.Models.Entities;

namespace StatusMonitor.Web.ViewModels
{
	/// <summary>
	/// View model identifying parameters for LogData API endpoint.
	/// </summary>
	public class LogDataViewModel
	{
		public LogEntrySeverities MessageSeverity { get; set; }

		[Required]
		/// <summary>
		/// Alias for MessageSeverity.
		/// </summary>
		public string Severity
		{
			get
			{
				return MessageSeverity.ToString();
			}
			set
			{
				try
				{
					MessageSeverity = value.ToEnum<LogEntrySeverities>();
				}
				catch (System.Exception)
				{
					throw new ArgumentException("Invalid Severity parameter.");
				}
			}
		}

		/// <summary>
		/// Number of messages of given severity.
		/// By default: 1.
		/// </summary>
		public int Count { get; set; } = 1;

		[Required]
		[StringLength(32)]
		[RegularExpression("[a-z0-9\\.\\-]+")]
		/// <summary>
		/// Required. Source identifier. Should be server id in the case of CPU Load.
		/// </summary>
		public string Source { get; set; }
	}
}
