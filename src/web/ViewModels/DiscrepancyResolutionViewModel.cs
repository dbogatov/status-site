using System;
using System.ComponentModel.DataAnnotations;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Shared.Models.Entities;

namespace StatusMonitor.Web.ViewModels
{
	public class DiscrepancyResolutionViewModel
	{
		public DateTime DateFirstOffense { get; set; }

		/// <summary>
		/// Alias for DateFirstOffense.
		/// </summary>
		[Required]
		public string Date
		{
			get
			{
				return DateFirstOffense.ToString();
			}
			set
			{
				try
				{
					DateFirstOffense = new DateTime(Convert.ToInt64(value));
				}
				catch (System.Exception)
				{
					throw new ArgumentException("Invalid Date parameter.");
				}
			}
		}

		public DiscrepancyType EnumDiscrepancyType { get; set; }

		/// <summary>
		/// Alias for EnumDiscrepancyType.
		/// </summary>
		[Required]
		public string DiscrepancyType
		{
			get
			{
				return EnumDiscrepancyType.ToString();
			}
			set
			{
				try
				{
					EnumDiscrepancyType = value.ToEnum<DiscrepancyType>();
				}
				catch (System.Exception)
				{
					throw new ArgumentException("Invalid DiscrepancyType parameter.");
				}
			}
		}

		public Metrics EnumMetricType { get; set; }

		/// <summary>
		/// Alias for EnumMetricType.
		/// </summary>
		[Required]
		public string MetricType
		{
			get
			{
				return EnumMetricType.ToString();
			}
			set
			{
				try
				{
					EnumMetricType = value.ToEnum<Metrics>();
				}
				catch (System.Exception)
				{
					throw new ArgumentException("Invalid MetricType parameter.");
				}
			}
		}

		/// <summary>
		/// Source identifier. May be server id or website URL.
		/// </summary>
		[Required]
		[StringLength(32)]
		[RegularExpression("[a-z0-9\\.\\-]+")]
		public string Source { get; set; }

		public override string ToString()
		{
			return $"Discrepancy removal model: type {DiscrepancyType} from {MetricType} of {Source} at {DateFirstOffense}.";
		}
	}
}
