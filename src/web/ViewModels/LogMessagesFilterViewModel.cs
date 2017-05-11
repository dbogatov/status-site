using System;
using System.Collections.Generic;
using System.Linq;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Shared.Models.Entities;
using StatusMonitor.Shared.Services;

namespace StatusMonitor.Web.ViewModels
{
	public class LogMessagesFilterViewModel : ExtendedObject
	{
		public string Sources { get; set; }
		public string Categories { get; set; }
		public string Severities { get; set; }
		public string Keywords { get; set; }

		public string Start { get; set; }
		public string End { get; set; }

		public int? Id { get; set; }

		/// <summary>
		/// Generates LogMessagesFilterModel from LogMessagesFilterViewModel
		/// </summary>
		/// <returns>LogMessagesFilterModel with this model's data</returns>
		public LogMessagesFilterModel ToLogMessagesFilterModel()
		{
			return new LogMessagesFilterModel
			{
				Severities =
					string.IsNullOrEmpty(Severities) ?
					new List<LogEntrySeverities>() :
					Severities.Split(',').Select(s => s.ToEnum<LogEntrySeverities>()).ToList(),
				Categories =
					string.IsNullOrEmpty(Categories) ?
					new List<int>() :
					Categories.Split(',').Select(s => Convert.ToInt32(s)).ToList(),
				Sources =
					string.IsNullOrEmpty(Sources) ?
					new List<string>() :
					Sources.Split(',').ToList(),
				Keywords =
					string.IsNullOrEmpty(Keywords) ?
					new List<string>() :
					Keywords.Split(',').ToList(),
				Start =
					string.IsNullOrEmpty(Start) ?
					(DateTime?)null :
					new DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds(Convert.ToInt64(Start)),
				End =
					string.IsNullOrEmpty(End) ?
					(DateTime?)null :
					new DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds(Convert.ToInt64(End))
			};
		}
	}
}
