using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Newtonsoft.Json;
using StatusMonitor.Shared.Extensions;

namespace StatusMonitor.Shared.Models.Entities
{
	/// <summary>
	/// An elementary piece of health data.
	/// Stores the label of a metric.
	/// </summary>
	public class HealthReportDataPoint
	{
		[JsonIgnore]
		[NotMapped]
		/// <summary>
		/// Type of the metric
		/// </summary>
		public Metrics MetricType
		{
			get
			{
				try
				{
					return Type.ToEnum<Metrics>();
				}
				catch (System.Exception)
				{
					throw new ArgumentException("Invalid MetricType parameter.");
				}
			}
			set
			{
				Type = value.ToString().ToLower();
			}
		}

		/// <summary>
		/// String alias of the metric's type
		/// Should not be used directly
		/// Serves as a column in DB
		/// </summary>
		public string Type { get; set; }


		[JsonIgnore]
		[NotMapped]
		/// <summary>
		/// Label of the metric
		/// </summary>
		public AutoLabels MetricLabel
		{
			get
			{
				try
				{
					return Label.ToEnum<AutoLabels>();
				}
				catch (System.Exception)
				{
					throw new ArgumentException("Invalid AutoLabels parameter.");
				}
			}
			set
			{
				Label = value.ToString().ToLower();
			}
		}

		[JsonProperty("Label")]
		/// <summary>
		/// String alias of the metric's label
		/// Should not be used directly
		/// Serves as a column in DB
		/// </summary>
		public string Label { get; set; }

		public string Source { get; set; }
	}

	/// <summary>
	/// Entity that encapsulates the overall health of the system at the moment
	/// Also serves as a datapoint for health metric
	/// </summary>
	public class HealthReport : DataPoint
	{
		/// <summary>
		/// Percentage that encapsulates numeric value of the health
		/// Computed as a weighted average of individual healths, which are derived from the auto labels
		/// </summary>
		public virtual int Health
		{
			get
			{
				return 
					Data.Count() > 0 ?
					(int)Math.Round(
						(
							(double)(
								Data
								.GroupBy(d => d.MetricLabel)
								.Aggregate(
									0,
									(sum, element) =>
										sum +
										new AutoLabel { Id = element.Key.AsInt() }.HealthValue() * element.Count()
								)
							)
							/
							(
								Data.Count() * AutoLabel.MaxHealthValue()
							)
						) * 100
					) :
					0
				;
			}
			set {

			}
		}

		[NotMapped]
		/// <summary>
		/// Collection of individual health pieces.
		/// Wrapper around HealthData which is a JSON of this property
		/// </summary>
		public IEnumerable<HealthReportDataPoint> Data
		{
			get
			{
				return JsonConvert.DeserializeObject<HealthReportDataPoint[]>(HealthData);
			}
			set
			{
				HealthData = JsonConvert.SerializeObject(value);
			}
		}

		[JsonIgnore]
		/// <summary>
		/// JSONified value of Data
		/// Should not be used directly
		/// Serves as a column in DB
		/// </summary>
		public string HealthData { get; set; } = JsonConvert.SerializeObject(new List<HealthReportDataPoint>());

		public override int? NormalizedValue()
		{
			return Health;
		}

		public override object PublicFields()
		{
			return new {
				Timestamp,
				Health
			};
		}
	}
}
