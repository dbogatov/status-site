using System;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Shared.Models.Entities;
using System.Threading.Tasks;
using System.Net.Http;
using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Logging;
using StatusMonitor.Shared.Services;
using StatusMonitor.Shared.Services.Factories;

namespace StatusMonitor.Daemons.Services
{
	/// <summary>
	/// Service used to "ping" or check status of specific web servers by sending an HTTP request and recording
	/// the response code and time.
	/// </summary>
	public interface IPingService
	{
		/// <summary>
		/// Pings a server specifies in settings.
		/// Sends a web request (GET or HEAD), records response time and status code.
		/// </summary>
		/// <param name="setting">Set of parameters needed to ping a server.</param>
		/// <returns>Ping data point with recorded response time and status code.</returns>
		Task<PingDataPoint> PingServerAsync(PingSetting setting);
	}

	public class PingService : IPingService
	{
		private readonly IMetricService _metrics;
		private readonly ILogger<PingService> _logger;
		private readonly IHttpClientFactory _factory;

		public PingService(
			IMetricService metrics,
			ILogger<PingService> logger,
			IHttpClientFactory httpClientFactory
		)
		{
			_factory = httpClientFactory;
			_logger = logger;
			_metrics = metrics;
		}

		public async Task<PingDataPoint> PingServerAsync(PingSetting setting)
		{
			using (var httpClient = _factory.BuildClient())
			{
				TimeSpan responseTime;
				HttpStatusCode statusCode = HttpStatusCode.OK;

				// Generate a request
				var httpContent = new HttpRequestMessage(
					setting.GetMethodRequired ? HttpMethod.Get : HttpMethod.Head,
					setting.ServerUrl
				);

				var timer = new Stopwatch();
				timer.Start();

				var task = httpClient.SendAsync(httpContent);
				// Make sure task finishes in TotalMilliseconds milliseconds
				if (
					await Task.WhenAny(
						task, 
						Task.Delay(Convert.ToInt32(setting.MaxResponseTime.TotalMilliseconds))
					) == task
				)
				{
					var response = await task;

					timer.Stop();

					// Record time and code
					responseTime = timer.Elapsed;
					statusCode = response.StatusCode;
				}
				else
				{
					// Timeout/cancellation logic
					// If problem occurred then set service unavailable.
					responseTime = new TimeSpan(0);
					statusCode = HttpStatusCode.ServiceUnavailable;

					_logger.LogWarning(LoggingEvents.Ping.AsInt(), $"Resource {setting.ServerUrl} is unavailable. See stack trace.");
				}

				_logger.LogDebug(LoggingEvents.Ping.AsInt(), $"Ping completed for {setting.ServerUrl}");

				var metric = await _metrics.GetOrCreateMetricAsync(Metrics.Ping, new Uri(setting.ServerUrl).Host);

				return new PingDataPoint
				{
					Metric = metric,
					ResponseTime = responseTime,
					HttpStatusCode = statusCode.AsInt()
				};
			}
		}
	}
}
