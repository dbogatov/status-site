using System;
using System.ComponentModel.DataAnnotations;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Shared.Models.Entities;

namespace StatusMonitor.Web.ViewModels
{
	public class MetricUpdateViewModel
	{
		[Required]
		public int ManualLabelId { get; set; }

		[Required]
		public bool Public { get; set; }

		[Required]
		[StringLength(32)]
		[RegularExpression("[a-z0-9\\.\\-]+")]
		/// <summary>
		/// Required. Source identifier. May be server id or website URL.
		/// </summary>
		public string Source { get; set; }

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
	}
}
