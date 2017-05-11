using Microsoft.Extensions.DependencyInjection;
using StatusMonitor.Shared.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using StatusMonitor.Shared.Models;
using StatusMonitor.Shared.Services.Factories;

namespace StatusMonitor.Shared.Extensions
{
	/// <summary>
	/// Utility class for testing methods
	/// </summary>
	public static class IServiceCollectionExtensions
	{
		/// <summary>
		/// Registers all available services for testing environment.
		/// </summary>
		/// <returns>Service provider with all services registered</returns>
		public static IServiceCollection RegisterSharedServices(
			this IServiceCollection services, 
			IHostingEnvironment env, 
			IConfiguration config)
		{

			// Create service of DataContext with inMemory data provider
			// Use Entity Framework
			if (env.IsProduction())
			{
				services
					.AddEntityFrameworkNpgsql()
					.AddDbContext<DataContext>(
						b => b.UseNpgsql(config["Secrets:ConnectionString"])
					);
			}
			else if (env.IsDevelopment())
			{
				services
					.AddEntityFrameworkSqlite()
					.AddDbContext<DataContext>(
						b => b.UseSqlite("Data Source=../../../../../development.db")
					);
			}
			else // Testing and Staging
			{
				services
					.AddEntityFrameworkInMemoryDatabase()
					.AddDbContext<DataContext>(
						b => b
							.UseInMemoryDatabase()
							.UseInternalServiceProvider(
								new ServiceCollection()
									.AddEntityFrameworkInMemoryDatabase()
									.BuildServiceProvider()
							)
					);
			}

			services.AddTransient<IDataContext, DataContext>();

			services.AddTransient<IDataSeedService, DataSeedService>();
			services.AddTransient<IMetricService, MetricService>();
			services.AddTransient<ILoggingService, LoggingService>();
			services.AddTransient<ICleanService, CleanService>();
			services.AddTransient<IEmailService, EmailService>();
			services.AddTransient<ISlackService, SlackService>();
			services.AddTransient<INotificationService, NotificationService>();
			
			services.AddSingleton<IHttpClientFactory, HttpClientFactory>();
			
			services.AddSingleton<IConfiguration>(config);
			services.AddSingleton<IHostingEnvironment>(env);

			return services;
		}
	}
}
