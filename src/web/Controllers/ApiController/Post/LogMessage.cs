using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Shared.Models.Entities;
using StatusMonitor.Web.ActionFilters;
using StatusMonitor.Web.ViewModels;

namespace StatusMonitor.Web.Controllers.Api
{
	public partial class ApiController
	{
		private readonly object _logLocker = new object();
		static private readonly Dictionary<SourceCategory, Counter> _guard =
			new Dictionary<SourceCategory, Counter>(new SourceCategoryEqualityComparer());

		[HttpPost]
		[ServiceFilter(typeof(ApiKeyCheck))]
		[ServiceFilter(typeof(ModelValidation))]
		public async Task<IActionResult> LogMessage(LogMessageViewModel model)
		{
			if (UpdateCounter(model.Source, model.Category))
			{
				await _loggingService.RecordLogMessageAsync(
					model.Message,
					model.AuxiliaryData,
					model.Source,
					model.Category,
					model.MessageSeverity
				);

				if (model.MessageSeverity >= _config["Logging:LogSeverityReported"].ToEnum<LogEntrySeverities>())
				{
					await _notify.ScheduleNotificationAsync(
						$"Log message of severity {model.MessageSeverity} has been received from {model.Source}",
						NotificationSeverity.High
					);
				}

				return Ok("Log message has been recorded.");
			}
			else
			{
				return StatusCode(
					429,
					$"Too may requests. Limit to {_config["Guard:Logging:Requests"]} per {_config["Guard:Logging:PerSeconds"]} seconds."
				);
			}
		}

		[NonAction]
		/// <summary>
		/// Increments (or sets) counter for given source and category in thread-safe manner
		/// </summary>
		/// <param name="source">Source part of key</param>
		/// <param name="category">Category part of key</param>
		/// <returns>True if request may pass, or false if request needs to be rejected</returns>
		private bool UpdateCounter(string source, int category)
		{
			lock (_logLocker)
			{
				var key = new SourceCategory
				{
					Source = source,
					Category = category
				};

				var intervalAgo = DateTime.UtcNow - new TimeSpan(0, 0, Convert.ToInt32(_config["Guard:Logging:PerSeconds"]));

				if (_guard.ContainsKey(key))
				{
					var value = _guard[key];

					if (value.Timestamp < intervalAgo)
					{
						_guard[key] = new Counter();
						return true;
					}

					_guard[key].Count++;

					if (value.Count <= Convert.ToInt32(_config["Guard:Logging:Requests"]))
					{
						return true;
					}
					else
					{
						if (!_guard[key].Notified)
						{
							_guard[key].Notified = true;

							var errorMessage = $"{source} is SPAMing logs with category {category}";

							_notify.ScheduleNotificationAsync(errorMessage, NotificationSeverity.High).Wait();
							_logger.LogWarning(LoggingEvents.GenericError.AsInt(), errorMessage);
						}

						return false;
					}
				}
				else
				{
					_guard.Add(key, new Counter());
					return true;
				}
			}
		}
	}

	/// <summary>
	/// Helper model for value of guard dictionary
	/// </summary>
	internal class Counter
	{
		public int Count { get; set; } = 1;
		public DateTime Timestamp { get; set; } = DateTime.UtcNow;
		public bool Notified { get; set; } = false;
	}

	/// <summary>
	/// Helper model for key of guard dictionary
	/// </summary>
	internal class SourceCategory
	{
		public string Source { get; set; }
		public int Category { get; set; }
	}

	/// <summary>
	/// Implementation of IEqualityComparer for SourceCategory so that it may be used as key in guard dictionary
	/// </summary>
	class SourceCategoryEqualityComparer : IEqualityComparer<SourceCategory>
	{
		public bool Equals(SourceCategory sc1, SourceCategory sc2)
		{
			if (sc1 == null && sc1 == null)
			{
				return true;
			}

			if (sc2 == null | sc2 == null)
			{
				return false;
			}

			return sc1.Category.Equals(sc2.Category) && sc1.Source.Equals(sc2.Source);
		}

		public int GetHashCode(SourceCategory sc)
		{
			return
				sc.Source.GetHashCode() * 2 +
				sc.Category.GetHashCode() * 3;
		}
	}
}
