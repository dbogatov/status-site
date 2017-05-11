using System;
using System.ComponentModel.DataAnnotations;

namespace StatusMonitor.Shared.Models.Entities
{
	/// <summary>
	/// Model describing generic notification
	/// </summary>
	public class Notification
	{
		[Key]
		public int Id { get; set; }
		public string Message { get; set; }
		public NotificationSeverity Severity { get; set; }
		public bool IsSent { get; set; } = false;
		public DateTime DateCreated { get; set; } = DateTime.UtcNow;
		public DateTime DateSent { get; set; }
	}

	public enum NotificationSeverity
	{
		Low, Medium, High
	}

}
