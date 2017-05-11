using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace StatusMonitor.Shared.Models.Entities
{
	/// <summary>
	/// Represents a generic log entry.
	/// </summary>
	public class LogEntry
	{
		[Key]
		public int Id { get; set; }
		public string Message { get; set; }
		public string AuxiliaryData { get; set; } = "";
		public LogEntrySeverity Severity { get; set; }
		public string Source { get; set; }
		public DateTime Timestamp { get; set; } = DateTime.UtcNow;
		public int Category { get; set; } = 0;
	}
}
