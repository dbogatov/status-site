using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using Microsoft.AspNetCore.Routing;

namespace StatusMonitor.Shared.Extensions
{
	public static class ObjectExtensions
	{
		/// <summary>
		/// Hack to convert an object to dynamic type.
		/// Needed for Razor views which complain that "object does not have ... property"
		/// </summary>
		/// <param name="anonymousObject">Object to convert</param>
		/// <returns>Dynamic object</returns>
		public static ExpandoObject ToExpando(this object anonymousObject)
		{
			IDictionary<string, object> anonymousDictionary = new RouteValueDictionary(anonymousObject);
			IDictionary<string, object> expando = new ExpandoObject();
			foreach (var item in anonymousDictionary)
			{
				expando.Add(item);
			}
			return (ExpandoObject)expando;
		}
	}
}
