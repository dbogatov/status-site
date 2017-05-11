using Microsoft.EntityFrameworkCore;

namespace StatusMonitor.Shared.Extensions
{
	public static class DbSetExtensions
	{
		/// <summary>
		/// Removes all records from the set. Does not execute changes in the data provider. Use SaveChanges() for that.
		/// </summary>
		public static void Clear<T>(this DbSet<T> dbSet) where T : class
		{
			dbSet.RemoveRange(dbSet);
		}
	}
}
