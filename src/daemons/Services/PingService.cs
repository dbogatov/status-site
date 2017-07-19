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
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.AspNetCore.WebUtilities;

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

	/// <summary>
	/// Makes requests to a separate ping server
	/// Expects RemotePingServerResponse response
	/// </summary>
	public class RemotePingService : IPingService
	{
		private readonly IMetricService _metrics;
		private readonly ILogger<PingService> _logger;
		private readonly IConfiguration _conf;
		private readonly IHttpClientFactory _factory;

		public RemotePingService(
			IMetricService metrics,
			ILogger<PingService> logger,
			IConfiguration conf,
			IHttpClientFactory factory
		)
		{
			_conf = conf;
			_logger = logger;
			_metrics = metrics;
			_factory = factory;
		}

		public async Task<PingDataPoint> PingServerAsync(PingSetting setting)
		{
			using (var client = _factory.BuildClient())
			{
				var parameters = new Dictionary<string, string> {
					{ "url", setting.ServerUrl },
					{ "method", setting.GetMethodRequired ? "GET" : "HEAD" },
					{ "timeout", setting.MaxResponseTime.TotalMilliseconds.ToString() }
				};

				var result = await client.GetAsync(QueryHelpers.AddQueryString(_conf["Data:PingServerUrl"], parameters));

				var data = JsonConvert.DeserializeObject<RemotePingServerResponse>(
					await result.Content.ReadAsStringAsync()
				);

				_logger.LogDebug(LoggingEvents.Ping.AsInt(), $"Ping completed for {setting.ServerUrl}");

				var metric = await _metrics.GetOrCreateMetricAsync(Metrics.Ping, new Uri(setting.ServerUrl).Host);

				return
					data.IsError ?
					new PingDataPoint
					{
						Metric = metric,
						ResponseTime = new TimeSpan(0),
						Success = data.StatusCode / 100 == 2, // 2xx
						Message = data.Error
					}:
					new PingDataPoint
					{
						Metric = metric,
						ResponseTime = new TimeSpan(0, 0, 0, 0, data.Latency),
						Success = data.StatusCode / 100 == 2, // 2xx
						Message = "OK"
					};
			}
		}
	}

	/// <summary>
	/// Structure of a separate ping server response expected by RemotePingService
	/// </summary>
	internal class RemotePingServerResponse
	{
		public string Url { get; set; }
		public string Method { get; set; }
		/// <summary>
		/// In milliseconds
		/// </summary>
		public int Timeout { get; set; }

		/// <summary>
		/// In milliseconds
		/// </summary>
		public int Latency { get; set; }
		public string[] Headers { get; set; }
		public int ContentLength { get; set; }
		public int StatusCode { get; set; }

		public string Error { get; set; }
		public bool IsError { get; set; }
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
				HttpStatusCode statusCode = HttpStatusCode.ServiceUnavailable;

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

				if (
					statusCode == HttpStatusCode.ServiceUnavailable ||
					responseTime > setting.MaxResponseTime
				)
				{
					// Timeout/cancellation logic
					// If problem occurred then set service unavailable.
					responseTime = new TimeSpan(0);
					statusCode = HttpStatusCode.ServiceUnavailable;

					// _logger.LogWarning(LoggingEvents.Ping.AsInt(), $"Resource {setting.ServerUrl} is unavailable. See stack trace.");
				}

				_logger.LogDebug(LoggingEvents.Ping.AsInt(), $"Ping completed for {setting.ServerUrl}");

				var metric = await _metrics.GetOrCreateMetricAsync(Metrics.Ping, new Uri(setting.ServerUrl).Host);

				return new PingDataPoint
				{
					Metric = metric,
					ResponseTime = responseTime,
					Success = statusCode.AsInt() / 100 == 2, // 2xx
					Message = statusCode.AsInt() / 100 == 2 ? "OK" : "Failure"
				};
			}
		}
	}
}
