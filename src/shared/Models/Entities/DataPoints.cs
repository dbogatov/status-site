using System;
using System.ComponentModel.DataAnnotations;
using StatusMonitor.Shared.Extensions;

namespace StatusMonitor.Shared.Models.Entities
{
	/// <summary>
	/// Abstract entity for a piece of information displayed in Metric.
	/// </summary>
	public abstract class DataPoint
	{
		[Key]
		public int Id { get; set; }
		public virtual DateTime Timestamp { get; set; } = DateTime.UtcNow;
		public virtual Metric Metric { get; set; }

		/// <summary>
		/// Generates an object which contains the fields of DataPoint visible to public
		/// </summary>
		/// <returns>An object which contains the fields of DataPoint visible to public</returns>
		public abstract object PublicFields();

		/// <summary>
		/// Returns an integer value that represents the CurrentValue of the metric associated with the data point.
		/// </summary>
		/// <returns>
		/// Integer value that represents the CurrentValue of the metric associated with the data point.
		/// </returns>
		public abstract int? NormalizedValue();
	}

	/// <summary>
	/// The most basic data point. Contains a value - real number. Suitable for simple metrics, like CPU load.
	/// </summary>
	public class NumericDataPoint : DataPoint
	{
		public int Value { get; set; }

		public override int? NormalizedValue()
		{
			return Value;
		}

		public override object PublicFields()
		{
			return new
			{
				Timestamp,
				Value
			};
		}
	}

	/// <summary>
	/// Simple data point that stores a generic message
	/// </summary>
	public class LogDataPoint : DataPoint
	{
		public LogEntrySeverity Severity { get; set; }
		/// <summary>
		/// A number of log messages of given severity
		/// </summary>
		public int Count { get; set; }

		public override int? NormalizedValue()
		{
			return Count;
		}

		public override object PublicFields()
		{
			return new
			{
				Timestamp,
				Severity = Severity.Description,
				Count
			};
		}
	}

	/// <summary>
	/// Data point for compilation metrics.
	/// </summary>
	public class CompilationDataPoint : DataPoint
	{
		/// <summary>
		/// Number of bytes
		/// </summary>
		public int SourceSize { get; set; }
		public TimeSpan CompileTime { get; set; }
		public CompilationStage Stage { get; set; }

		public override int? NormalizedValue()
		{
			return Convert.ToInt32(CompileTime.TotalMilliseconds);
		}

		public override object PublicFields()
		{
			return new
			{
				Timestamp,
				SourceSize,
				CompileTime = Convert.ToInt32(CompileTime.TotalMilliseconds),
				Stage = Stage.Name
			};
		}
	}

	/// <summary>
	/// Data point for website status metrics. Records a response time for a given website (stored in Source field).
	/// </summary>
	public class PingDataPoint : DataPoint
	{
		/// <summary>
		/// Time it took for a request to complete.
		/// 0 for ServiceUnavailable status code.
		/// </summary>
		public TimeSpan ResponseTime { get; set; }
		public bool Success { get; set; }
		public string Message { get; set; }

		public override int? NormalizedValue()
		{
			return
				Success ?
				Convert.ToInt32(ResponseTime.TotalMilliseconds) :
				(int?)null
			;
		}

		public override object PublicFields()
		{
			return new
			{
				Timestamp,
				ResponseTime = Convert.ToInt32(ResponseTime.TotalMilliseconds),
				Success,
				Message
			};
		}
	}

	/// <summary>
	/// Data point for user actions, like login / logout.
	/// </summary>
	public class UserActionDataPoint : DataPoint
	{
		public int Count { get; set; }
		public string Action { get; set; }

		public override int? NormalizedValue()
		{
			return Count;
		}

		public override object PublicFields()
		{
			return new
			{
				Timestamp,
				Count,
				Action
			};
		}
	}
}
