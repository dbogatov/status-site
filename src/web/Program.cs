using System;
using System.IO;
using System.Threading;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using StatusMonitor.Shared.Extensions;

namespace StatusMonitor.Web
{
	/// <summary>
	/// This class contains the entry point for the application.
	/// </summary>
	public class Program
	{           
		/// <summary>
		/// The entry point.
		/// This function is responsible for starting up the server object.
		/// The server will try to start a number of times until it succeeds.
		/// For example, it may not connect to the database from the first attempt, because of network issues, or
		/// the database may not be ready at the time of application start.
		/// </summary>
		/// <param name="args">Arguments passed to the application through the command line. At the moment not used. </param>
		public static int Main(string[] args)
		{
			int port = 5555;
			if (args.Length != 0 && (!Int32.TryParse(args[0], out port) || port < 1024 || port > 65534))
			{
				ColoredConsole.WriteLine("Usage: dotnet web.dll [port | number 1024-65534]", ConsoleColor.Red);
				return 1;
			}

			/// <summary>
			/// A number of times app tries to connect to the database before quiting
			/// </summary>
			var _connectionRetryNumber = 6;

			/// <summary>
			/// A number of seconds before trying to connect to db again
			/// </summary>
			var _connectionRetryInterval = 10;

			for (int i = 0; i < _connectionRetryNumber; i++)
			{
				try
				{
					var host = WebHost
						.CreateDefaultBuilder(args)
						.UseStartup<Startup>()
						.ConfigureLogging(
							logging => {
								logging.ClearProviders();
								logging.AddFilter("Microsoft", LogLevel.None);
							}
						)
						.UseUrls($"http://*:{port}")
						.Build();
						
					host.Run();

					return 0;
				}
				catch (System.Net.Sockets.SocketException)
				{
					Console.WriteLine("Failed to connect to DB, retrying...");
					Thread.Sleep(_connectionRetryInterval * 1000);
				}
			}

			ColoredConsole.WriteLine($"Could not connect to DB after {_connectionRetryNumber} times.", ConsoleColor.Red);
			return 1;
		}
	}
}
