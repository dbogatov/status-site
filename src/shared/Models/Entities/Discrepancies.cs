using System;
using StatusMonitor.Shared.Extensions;

namespace StatusMonitor.Shared.Models.Entities
{
	/// <summary>
	/// Model describing a generic in data set problem that needs to be reported
	/// </summary>
	public class Discrepancy : ExtendedObject
	{
		/// <summary>
		/// The well-defined start of the problem
		/// </summary>
		public DateTime DateFirstOffense { get; set; }
		public DiscrepancyType Type { get; set; }
		/// <summary>
		/// Source of associated metric
		/// </summary>
		public string MetricSource { get; set; }
		/// <summary>
		/// Type of associated metric
		/// </summary>
		public Metrics MetricType { get; set; }

		public DateTime DateResolved { get; set; }
		public bool Resolved { get; set; } = false;

		public override string ToString()
		{
			switch (Type)
			{
				case DiscrepancyType.GapInData:
					return $"Gap in data from {MetricSource} has been detected. The gap starts on {DateFirstOffense}.";
				case DiscrepancyType.HighLoad:
					return $"{MetricSource} reported high load starting from {DateFirstOffense}.";
				case DiscrepancyType.PingFailedNTimes:
					return $"Requests to {MetricSource} failed too many consecutive times. First failure occurred on {DateFirstOffense}.";
				default:
					return $"Discrepancy of an unknown type from {MetricSource} has been detected. It started on {DateFirstOffense}.";
			}
		}
	}

	public enum DiscrepancyType
	{
		GapInData, PingFailedNTimes, HighLoad
	}
}
