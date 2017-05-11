using System;
using System.ComponentModel.DataAnnotations;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Shared.Models.Entities;

namespace StatusMonitor.Web.ViewModels
{
	/// <summary>
	/// View model identifying parameters for Compilation API endpoint.
	/// </summary>
	public class CompilationViewModel
	{
		public CompilationStages CompilationStage { get; set; }

		[Required]
		public string Stage
		{
			get
			{
				return CompilationStage.ToString();
			}
			set
			{
				try
				{
					CompilationStage = value.ToEnum<CompilationStages>();
				}
				catch (System.Exception)
				{
					throw new ArgumentException("Invalid Action parameter.");
				}
			}
		}

		[Required]
		[StringLength(32)]
		[RegularExpression("[a-z0-9\\.\\-]+")]
		/// <summary>
		/// Required. Source identifier. May be server id or website URL.
		/// </summary>
		public string Source { get; set; }

		[Required]
		/// <summary>
		/// Required. The size of the the source in bytes.
		/// <returns></returns>
		public int SourceSize { get; set; }

		[Required]
		/// <summary>
		/// Required. The compilation time in milliseconds.
		/// </summary>
		public int CompileTime { get; set; }
	}
}
