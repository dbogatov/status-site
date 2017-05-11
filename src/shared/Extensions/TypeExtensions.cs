using System;

namespace StatusMonitor.Shared.Extensions
{
	public static class TypeExtensions
	{
		/// <summary>
		/// Returns the short description of Type leaving only the class/interface name (no namespaces).
		/// For example, StatusMonitor.Extensions.TypeExtensions will be TypeExtensions.
		/// </summary>
		/// <param name="type">Type to be stringified.</param>
		/// <returns>Short description of the type.</returns>
		public static string ToShortString(this Type type)
		{
			var typeString = type.ToString();
			var index = typeString.LastIndexOf(".");
			return typeString.Substring(index + 1);
		}
	}

}
