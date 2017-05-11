using System;
using System.ComponentModel.DataAnnotations;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Shared.Models.Entities;

namespace StatusMonitor.Web.ViewModels
{
	/// <summary>
	/// View model identifying parameters for Get Metrics API endpoint.
	/// </summary>
	public class MetricRequestViewModel
	{
		/// <summary>
		/// A type of the metric for which data is requested.
		/// </summary>
		public Metrics MetricType { get; set; }

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

		/// <summary>
		/// Source identifier. May be server id or website URL.
		/// </summary>
		[StringLength(32)]
		[RegularExpression("[a-z0-9\\.\\-]+")]
		public string Source { get; set; }
	}
}
