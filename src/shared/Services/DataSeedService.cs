using System;
using System.Collections.Generic;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Shared.Models.Entities;
using StatusMonitor.Shared.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace StatusMonitor.Shared.Services
{
	/// <summary>
	/// Service used to initialize some values in the database. Called each time app starts.
	/// </summary>
	public interface IDataSeedService
	{
		/// <summary>
		/// Populates data provider with initial set of records (like enums)
		/// </summary>
		Task SeedDataAsync();

		/// <summary>
		/// A synchronous version of SeedDataAsync.
		/// Does NOT populate data points.
		/// Should NOT be used in production.
		/// </summary>
		void SeedData();
	}

	public class DataSeedService : IDataSeedService
	{
		private readonly IDataContext _context;
		private readonly ILogger<DataSeedService> _logger;
		private readonly IConfiguration _configuration;

		private List<AutoLabel> _autoLabels = new List<AutoLabel>();
		private List<ManualLabel> _manualLabels = new List<ManualLabel>();
		private List<CompilationStage> _compilationStages = new List<CompilationStage>();
		private List<LogEntrySeverity> _logEntrySeverities = new List<LogEntrySeverity>();
		private List<AbstractMetric> _abstractMetrics = new List<AbstractMetric>();

		private List<PingSetting> _pingSettings = new List<PingSetting>();

		public DataSeedService(
			IDataContext context,
			ILogger<DataSeedService> logger,
			IConfiguration configuration
		)
		{
			_context = context;
			_logger = logger;
			_configuration = configuration;

			ReadConfiguration();
		}

		public void SeedData()
		{
			_logger.LogInformation(LoggingEvents.Startup.AsInt(), "Data Seed started.");

			SeedSpecificEntity(_autoLabels, _context.AutoLabels);
			SeedSpecificEntity(_manualLabels, _context.ManualLabels);
			SeedSpecificEntity(_compilationStages, _context.CompilationStages);
			SeedSpecificEntity(_logEntrySeverities, _context.LogEntrySeverities);
			SeedSpecificEntity(_abstractMetrics, _context.AbstractMetrics);

			SeedSpecificEntity(_pingSettings, _context.PingSettings);

			_logger.LogInformation(LoggingEvents.Startup.AsInt(), "Data Seed completed.");
		}

		public async Task SeedDataAsync()
		{
			// Put the data into the data provider
			_logger.LogInformation(LoggingEvents.Startup.AsInt(), "DataSeed started");

			await SeedSpecificEntityAsync(_autoLabels, _context.AutoLabels);
			await SeedSpecificEntityAsync(_manualLabels, _context.ManualLabels);
			await SeedSpecificEntityAsync(_compilationStages, _context.CompilationStages);
			await SeedSpecificEntityAsync(_logEntrySeverities, _context.LogEntrySeverities);
			await SeedSpecificEntityAsync(_abstractMetrics, _context.AbstractMetrics);

			await SeedSpecificEntityAsync(_pingSettings, _context.PingSettings);

			_logger.LogInformation(LoggingEvents.Startup.AsInt(), "DataSeed finished");
		}

		/// <summary>
		/// Populate properties with values read from config
		/// </summary>
		private void ReadConfiguration()
		{
			_autoLabels = ReadStaticValueFromConfiguration<AutoLabel, AutoLabels>(
				(label) => new AutoLabel
				{
					Id = label.AsInt(),
					Title = _configuration[$"Data:AutoLabels:{label.ToString()}"]
				}
			);

			_manualLabels = ReadStaticValueFromConfiguration<ManualLabel, ManualLabels>(
				(label) => new ManualLabel
				{
					Id = label.AsInt(),
					Title = _configuration[$"Data:ManualLabels:{label.ToString()}"]
				}
			);

			_compilationStages = ReadStaticValueFromConfiguration<CompilationStage, CompilationStages>(
				(label) => new CompilationStage
				{
					Id = label.AsInt(),
					Name = _configuration[$"Data:CompilationStages:{label.ToString()}"]
				}
			);

			_logEntrySeverities = ReadStaticValueFromConfiguration<LogEntrySeverity, LogEntrySeverities>(
				(label) => new LogEntrySeverity
				{
					Id = label.AsInt(),
					Description = _configuration[$"Data:LogEntrySeverities:{label.ToString()}"]
				}
			);

			_abstractMetrics = ReadStaticValueFromConfiguration<AbstractMetric, Metrics>(
				(label) => new AbstractMetric
				{
					Type = label.AsInt(),
					Public = label == Metrics.CpuLoad || label == Metrics.Ping,
					Title = _configuration[$"Data:Metrics:{label.ToString()}"]
				}
			);

			var pingConfig = _configuration.SectionsFromArray("Data:PingSettings");

			if (pingConfig.Count() > 0)
			{
				_pingSettings =
					pingConfig
					.Select(section =>
					{
						var setting = new PingSetting
						{
							ServerUrl = section["ServerUrl"]
						};
						if (section["MaxResponseTime"] != null)
						{
							setting.MaxResponseTime = new TimeSpan(0, 0, 0, 0, Convert.ToInt32(section["MaxResponseTime"]));
						}
						if (section["MaxFailures"] != null)
						{
							setting.MaxFailures = Convert.ToInt32(section["MaxFailures"]);
						}
						if (section["GetMethodRequired"] != null)
						{
							setting.GetMethodRequired = Convert.ToBoolean(section["GetMethodRequired"]);
						}

						return setting;
					})
					.ToList();
			}
		}

		/// <summary>
		/// Helper that reads value from config for each Enum
		/// </summary>
		/// <param name="transformer">Delegate that defines how to extract value from config</param>
		/// <returns></returns>
		private List<TEntity> ReadStaticValueFromConfiguration<TEntity, TEnum>(
			Func<TEnum, TEntity> transformer
		) where TEnum : struct, IConvertible
		{
			return Enum
				.GetValues(typeof(TEnum))
				.Cast<TEnum>()
				.Select(transformer)
				.ToList();
		}

		/// <summary>
		/// A synchronous version of SeedSpecificEntityAsync.
		/// </summary>
		private bool SeedSpecificEntity<T>(List<T> values, DbSet<T> dbSets) where T : class
		{
			var inserted = false;

			foreach (var item in values)
			{
				if (dbSets.Any(i => i == item))
				{
					var element = dbSets.Single(i => i == item);

					PropertyInfo[] properties = typeof(T).GetProperties();
					foreach (PropertyInfo property in properties)
					{
						property.SetValue(element, property.GetValue(item));
					}
				}
				else
				{
					dbSets.Add(item);

					inserted = true;
				}
				_context.SaveChanges();
			}

			return inserted;
		}

		/// <summary>
		/// Replace the existing entities of the T type with the supplied one.
		/// As optimization, it does it only if the number of records in the data provider is different from the number
		/// in the supplied list.
		/// </summary>
		/// <param name="values">List of supplied values to be inserted/replaced</param>
		/// <param name="dbSets">The set of entities of this type in the data provider. 
		/// These are to be replaced by values.</param>
		/// <returns>True if values has been replaced, false if values were in sync and did not require 
		/// replacement.</returns>
		private async Task<bool> SeedSpecificEntityAsync<T>(List<T> values, DbSet<T> dbSets) where T : class
		{
			var inserted = false;

			foreach (var item in values)
			{
				if (await dbSets.AnyAsync(i => i == item))
				{
					var element = await dbSets.SingleAsync(i => i == item);

					PropertyInfo[] properties = typeof(T).GetProperties();
					foreach (PropertyInfo property in properties)
					{
						property.SetValue(element, property.GetValue(item));
					}
				}
				else
				{
					await dbSets.AddAsync(item);

					inserted = true;
				}

				await _context.SaveChangesAsync();
			}

			return inserted;
		}
	}
}
