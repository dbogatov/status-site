using System;
using System.Linq;
using System.Reflection;

namespace StatusMonitor.Shared.Extensions
{
	/// <summary>
	/// Redefines some of the Object methods
	/// </summary>
	public class ExtendedObject
	{
		/// <summary>
		/// Iterates over non-static public properties of the object invoking 
		/// ToString on each of them.
		/// </summary>
		/// <returns>A string representation of the object</returns>
		public override string ToString()
		{
			return
				this.GetType().ToShortString() + ":" +
				Environment.NewLine +
				this.GetType()
					.GetProperties(BindingFlags.Public | BindingFlags.Instance)
					.Select(pi => $"\t{pi.Name}: {pi.GetValue(this)}")
					.Aggregate(
						(self, next) => $"{self}{Environment.NewLine}{next}"
					);
		}

		/// <summary>
		/// Verifies that all non-static public properties of the object are pairwise equal.
		/// Deep equality check.
		/// </summary>
		/// <param name="obj">Another object to which to compare</param>
		/// <returns>True if objects are equal and false otherwise</returns>
		public override bool Equals(object obj)
		{
			if (obj == null || GetType() != obj.GetType())
			{
				return false;
			}

			return this.GetType()
				.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Aggregate(true, (accumulator, self) => self.GetValue(this).Equals(self.GetValue(obj)) && accumulator);
		}

		/// <summary>
		/// Generates hash of the objects as a derivative of hashes of its non-static public properties.
		/// </summary>
		/// <returns>Hash of the object</returns>
		public override int GetHashCode()
		{
			return this.GetType()
				.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Aggregate(
					new
					{
						Hash = 1,
						Count = 1
					},
					(accumulator, self) => new
					{
						Hash = self.GetValue(this).GetHashCode() * accumulator.Count + accumulator.Hash,
						Count = accumulator.Count + 1
					}
				).Hash;
		}
	}
}
