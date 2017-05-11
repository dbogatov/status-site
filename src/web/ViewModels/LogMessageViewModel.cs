using System;
using System.ComponentModel.DataAnnotations;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Shared.Models.Entities;

namespace StatusMonitor.Web.ViewModels
{
	/// <summary>
	/// View model identifying parameters for LogMessage API endpoint.
	/// </summary>
	public class LogMessageViewModel
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

		[Required]
		[StringLength(32)]
		[RegularExpression("[a-z0-9\\.\\-]+")]
		/// <summary>
		/// Required. Source identifier. Should be server id in the case of CPU Load.
		/// </summary>
		public string Source { get; set; }

		public string AuxiliaryData { get; set; } = "";

		[Required]
		public string Message { get; set; }

		public int Category { get; set; } = 0;
	}
}
