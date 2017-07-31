using System;
using System.ComponentModel.DataAnnotations;

namespace StatusMonitor.Shared.Models.Entities
{
	/// <summary>
	/// Represents a building block for Metric objects.
	/// This is the data given in configuration, while Metric is a "snapshot" or "current state" of dynamic data.!--
	/// This class is used to create Metric objects by giving them initial static data like Title and Type.
	/// </summary>
	public class AbstractMetric
	{
		/// <summary>
		/// Type of the Metric. Must be one of Metrics enum. 
		/// </summary>
		[Key]
		public int Type { get; set; }

		public string Title { get; set; }
		public bool Public { get; set; } = true;
	}

	/// <summary>
	/// Represents a Metric which is then displayed to the user.
	/// Metric is NOT a DataPoint, Metric is rather a category for DataPoint's.
	/// For example, CPU Load is a Metric, and 25% is a specific DataPoint for that Metric.
	/// 
	/// Numeric properties (CurrentValue, DayMin, DayMax, DayAvg, HourMin, HourMax, HourAvg)
	/// along with LastUpdated and AutoLabel are set by ICacheService.
	/// </summary>
	public class Metric
	{
		/// <summary>
		/// Type of the Metric. Must be one of Metrics enum. 
		/// Type is a part of a compound key. See DataContext.OnModelcreating
		/// </summary>
		public int Type { get; set; }
		/// <summary>
		/// Represents a source from which this metric gets its data. 
		/// May be a server identifier, or a website URL.
		/// Source is a part of a compound key. See DataContext.OnModelcreating
		/// </summary>
		public string Source { get; set; }

		public string Title { get; set; }
		public AutoLabel AutoLabel { get; set; }
		public ManualLabel ManualLabel { get; set; }
		public DateTime LastUpdated { get; set; }
		public bool Public { get; set; }

		public int CurrentValue { get; set; }

		public int HourMin { get; set; }
		public int HourMax { get; set; }
		public int HourAvg { get; set; }

		public int DayMin { get; set; }
		public int DayMax { get; set; }
		public int DayAvg { get; set; }
	}

	public enum Metrics
	{
		CpuLoad = 1, UserAction, Compilation, Log, Ping, Health
	}
}
