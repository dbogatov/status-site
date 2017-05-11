using System;
using System.ComponentModel.DataAnnotations;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Shared.Models.Entities;

namespace StatusMonitor.Web.ViewModels
{
	/// <summary>
	/// View model identifying parameters for Get Data API endpoint.
	/// </summary>
	public class DataRequestViewModel
	{
		/// <summary>
		/// Required. A type of the metric for which data is requested.
		/// </summary>
		public Metrics MetricType { get; set; }

		[Required]
		/// <summary>
		/// Alias for MetricType.
		/// </summary>
		public string Type
		{
			get
			{
				return MetricType.ToString();
			}
			set
			{
				try
				{
					MetricType = value.ToEnum<Metrics>();
				}
				catch (System.Exception)
				{
					throw new ArgumentException("Invalid Type parameter.");
				}
			}
		}

		[Required]
		[StringLength(32)]
		[RegularExpression("[a-z0-9\\.\\-]+")]
		/// <summary>
		/// Required. Source identifier. May be server id or website URL.
		/// </summary>
		public string Source { get; set; }

		/// <summary>
		/// Optional. Number of seconds ago from which data is requested.
		/// Default value is roughly a month.
		/// </summary>
		public int TimePeriod { get; set; } = 60 * 60 * 24 * 30;
	}
}
