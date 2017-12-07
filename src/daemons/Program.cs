using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StatusMonitor.Daemons.Services;
using StatusMonitor.Shared.Models;
using StatusMonitor.Shared.Services;
using Microsoft.Extensions.Logging;
using StatusMonitor.Shared.Extensions;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Moq;
using System.Threading.Tasks;
using System;
using System.Threading;

namespace StatusMonitor.Daemons
{
	/// <summary>
	/// This class contains the entry point for the application.
	/// </summary>
	public class Program
	{
		/// <summary>
		/// The entry point.
		/// The program will try to start a number of times until it succeeds.
		/// For example, it may not connect to the database from the first attempt, because of network issues, or
		/// the database may not be ready at the time of application start.
		/// </summary>
		/// <param name="args">Arguments passed to the application through the command line. At the moment not used. </param>
		public static int Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();

		private static async Task<int> MainAsync(string[] args)
		{
			var mockEnv = new Mock<IHostingEnvironment>();
			mockEnv.
				SetupGet(environment => environment.EnvironmentName).
				Returns(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
			var env = mockEnv.Object;


			var builder = new ConfigurationBuilder()
				.AddYamlFile("appsettings.yml", optional: false) // read defaults first
				.AddYamlFile(
					$"{(env.IsProduction() ? "/run/secrets/" : "")}appsettings.{env.EnvironmentName.ToLower()}.yml",
					optional: env.IsStaging()
				) // override with specific settings file
				.AddJsonFile("version.json", optional: true)
				.AddEnvironmentVariables();
			var configuration = builder.Build();

			var services = new ServiceCollection();

			services.RegisterSharedServices(env, configuration);

			if (env.IsProduction())
			{
				services.AddScoped<IPingService, RemotePingService>();
			}
			else
			{
				services.AddScoped<IPingService, PingService>();
			}

			services.AddScoped<ICacheService, CacheService>();
			services.AddScoped<ICleanService, CleanService>();
			services.AddScoped<IHealthService, HealthService>();
			services.AddScoped<IDemoService, DemoService>();
			services.AddScoped<IDiscrepancyService, DiscrepancyService>();
			services.AddTransient<IServiceManagerService, ServiceManagerService>();

			services.AddLogging();

			var provider = services.BuildServiceProvider();

			provider
				.GetService<ILoggerFactory>()
				.AddStatusLogger(
					provider.GetService<ILoggingService>(),
					configuration["Logging:MinLogLevel"].ToEnum<LogLevel>(),
					configuration.StringsFromArray("Logging:Exclude").ToArray()
				);

			/// <summary>
			/// A number of times app tries to connect to the database before quiting
			/// </summary>
			var connectionRetryNumber = 6;

			/// <summary>
			/// A number of seconds before trying to connect to db again
			/// </summary>
			var connectionRetryInterval = 10;

			for (int i = 0; i < connectionRetryNumber; i++)
			{
				try
				{
					using (var context = provider.GetService<IDataContext>())
					{
						// create scheme if it does not exist
						context.Database.EnsureCreated();
					}

					await provider.GetRequiredService<IDataSeedService>().SeedDataAsync();

					await provider.GetRequiredService<IServiceManagerService>().StartServices();
				}
				catch (System.Net.Sockets.SocketException)
				{
					Console.WriteLine("Failed to connect to DB, retrying...");
					Thread.Sleep(connectionRetryInterval * 1000);
				}
			}

			ColoredConsole.WriteLine($"Could not connect to DB after {connectionRetryNumber} times.", ConsoleColor.Red);
			return 1;
		}
	}
}
