using System.ComponentModel.DataAnnotations;

namespace StatusMonitor.Shared.Models.Entities
{
	/// <summary>
	/// Workaround to enable Identity service which requires User model
	/// </summary>
	public class User
	{
		[Key]
		public int Id { get; set; }
	}
}
