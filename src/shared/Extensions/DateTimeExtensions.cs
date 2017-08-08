using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using StatusMonitor.Shared.Models.Entities;

namespace StatusMonitor.Shared.Extensions
{
	public static class DateTimeExtensions
	{
		/// <summary>
		/// Calls .ToString method on a date converted from UTC to given time zone.
		/// </summary>
		/// <param name="value">The date to convert to string</param>
		/// <param name="timeZoneId">ID of a time zone (eq. "America/New_York"). 
		/// Does not change date object if this parameter is omitted.</param>
		/// <returns>String representation of a correct date.</returns>
		public static string ToStringUsingTimeZone(this DateTime value, string timeZoneId = null)
		{	
			return 
				timeZoneId == null 
				? value.ToString() 
				: TimeZoneInfo.ConvertTime(value, TimeZoneInfo.FindSystemTimeZoneById(timeZoneId)).ToString();
		}

		/// <summary>
		/// Return the number of milliseconds between epoch and given date
		/// </summary>
		/// <param name="value">The end date of selected range</param>
		/// <returns>The number of milliseconds between epoch and given date</returns>
		public static long TotalMilliseconds(this DateTime value) =>
			Convert.ToInt64((value - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds);
		
	}
}
