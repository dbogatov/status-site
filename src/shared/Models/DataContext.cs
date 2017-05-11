using StatusMonitor.Shared.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace StatusMonitor.Shared.Models
{
	public interface IDataContext : IDisposable
	{
		DbSet<Metric> Metrics { get; set; }
		DbSet<AbstractMetric> AbstractMetrics { get; set; }
		DbSet<AutoLabel> AutoLabels { get; set; }
		DbSet<ManualLabel> ManualLabels { get; set; }
		DbSet<NumericDataPoint> NumericDataPoints { get; set; }
		DbSet<LogDataPoint> LogDataPoints { get; set; }
		DbSet<CompilationDataPoint> CompilationDataPoints { get; set; }
		DbSet<PingDataPoint> PingDataPoints { get; set; }
		DbSet<UserActionDataPoint> UserActionDataPoints { get; set; }
		DbSet<LogEntry> LogEntries { get; set; }
		DbSet<LogEntrySeverity> LogEntrySeverities { get; set; }
		DbSet<CompilationStage> CompilationStages { get; set; }
		DbSet<UserAction> UserActions { get; set; }
		DbSet<PingSetting> PingSettings { get; set; }
		DbSet<Notification> Notifications { get; set; }
		DbSet<Discrepancy> Discrepancies { get; set; }

		DatabaseFacade Database { get; }

		Task<int> SaveChangesAsync(CancellationToken token = default(CancellationToken));
		int SaveChanges();
		void RemoveRange(params object[] entities);
		EntityEntry Remove(object entity);
		EntityEntry Entry(object entity);
	}

	/// <summary>
	/// %Models the database. 
	/// DbSet's represent tables. 
	/// See https://msdn.microsoft.com/en-us/library/jj729737(v=vs.113).aspx
	/// </summary>
	public class DataContext : DbContext, IDataContext
	{
		public DataContext(DbContextOptions options) : base(options) { }

		public DbSet<Metric> Metrics { get; set; }
		public DbSet<AbstractMetric> AbstractMetrics { get; set; }
		public DbSet<AutoLabel> AutoLabels { get; set; }
		public DbSet<ManualLabel> ManualLabels { get; set; }
		public DbSet<NumericDataPoint> NumericDataPoints { get; set; }
		public DbSet<LogDataPoint> LogDataPoints { get; set; }
		public DbSet<CompilationDataPoint> CompilationDataPoints { get; set; }
		public DbSet<PingDataPoint> PingDataPoints { get; set; }
		public DbSet<UserActionDataPoint> UserActionDataPoints { get; set; }
		public DbSet<LogEntry> LogEntries { get; set; }
		public DbSet<LogEntrySeverity> LogEntrySeverities { get; set; }
		public DbSet<CompilationStage> CompilationStages { get; set; }
		public DbSet<UserAction> UserActions { get; set; }
		public DbSet<PingSetting> PingSettings { get; set; }
		public DbSet<Notification> Notifications { get; set; }
		public DbSet<Discrepancy> Discrepancies { get; set; }

		/// <summary>
		/// This method gets called by the framework.
		/// Use FluentAPI to set up relations.
		/// See https://msdn.microsoft.com/en-us/library/jj591617(v=vs.113).aspx
		/// </summary>
		protected override void OnModelCreating(ModelBuilder builder)
		{
			// Create compound primary key
			builder
				.Entity<Metric>()
				.HasKey(metric => new { metric.Type, metric.Source });

			builder
				.Entity<Discrepancy>()
				.HasKey(disc => new { 
					disc.Type, 
					disc.DateFirstOffense, 
					disc.MetricSource, 
					disc.MetricType
				});

			// Add indexes for timestamps since data points are frequently
			// searched and filtered by timestamps.
			AddIndexToDataPoint(builder.Entity<NumericDataPoint>());
			AddIndexToDataPoint(builder.Entity<LogDataPoint>());
			AddIndexToDataPoint(builder.Entity<CompilationDataPoint>());
			AddIndexToDataPoint(builder.Entity<PingDataPoint>());
			AddIndexToDataPoint(builder.Entity<UserActionDataPoint>());
			builder
				.Entity<LogEntry>()
					.HasIndex(msg => msg.Timestamp)
					.IsUnique(false);

			builder
				.Entity<LogEntry>()
					.HasIndex(msg => msg.Source)
					.IsUnique(false);

			builder
				.Entity<LogEntry>()
					.HasIndex(msg => msg.Category)
					.IsUnique(false);

			builder
				.Entity<Notification>()
					.HasIndex(ntf => ntf.IsSent)
					.IsUnique(false);

			builder
				.Entity<Notification>()
					.HasIndex(ntf => ntf.DateCreated)
					.IsUnique(false);

			foreach (var relationship in builder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
			{
				relationship.DeleteBehavior = DeleteBehavior.Cascade;
			}

			base.OnModelCreating(builder);
		}

		/// <summary>
		/// Helper that adds non-unique index to the timestamp of DataPoint DbSet.
		/// </summary>
		/// <param name="builder">Model builder to which to add index.</param>
		/// <returns></returns>
		private IndexBuilder AddIndexToDataPoint<T>(EntityTypeBuilder<T> builder) where T : DataPoint
		{
			return builder
				.HasIndex(dp => dp.Timestamp)
				.IsUnique(false);
		}
	}
}
