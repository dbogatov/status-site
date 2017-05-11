using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Shared.Models.Entities;
using Xunit;

namespace StatusMonitor.Tests.IntegrationTests
{
	public partial class ApiControllerTest
	{
		private readonly object _logLocker = new object();

		[Fact]
		public async Task LogMessageEndpoint()
		{
			var ok = await MakeNLogMessageRequests(1, "LogMessageEndpoint".ToLower());

			Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
		}

		[Fact]
		public async Task LogMessageGuardAllow()
		{
			var ok = await MakeNLogMessageRequests(5, "LogMessageGuardAllow".ToLower());

			Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
		}

		[Fact]
		public async Task LogMessageGuardDeny()
		{
			var tooMany = await MakeNLogMessageRequests(6, "LogMessageGuardDeny".ToLower());

			Assert.Equal(429, tooMany.StatusCode.AsInt());
		}

		[Fact]
		public async Task LogMessageGuardAllowAfter()
		{
			await MakeNLogMessageRequests(10, "LogMessageGuardAllowAfter".ToLower());
			await Task.Delay(5 * 1000);

			var ok = await MakeNLogMessageRequests(5, "LogMessageGuardAllowAfter".ToLower());

			Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
		}

		[Fact]
		public async Task LogMessageGuardMultithreaded()
		{
			var tasks = new List<Task<HttpResponseMessage>>() {
				Task.Run(async () => await MakeNLogMessageRequests(5, "LogMessageGuardMultithreaded-1".ToLower())),
				Task.Run(async () => await MakeNLogMessageRequests(4, "LogMessageGuardMultithreaded-2".ToLower())),
				Task.Run(async () => await MakeNLogMessageRequests(3, "LogMessageGuardMultithreaded-3".ToLower()))
			}.ToArray();

			foreach (var code in await Task.WhenAll(tasks))
			{
				Assert.Equal(HttpStatusCode.OK, code.StatusCode);
			}
		}

		/// <summary>
		/// Makes N POST requests to log a message.
		/// Part of the massage contains a current timestamp, so every log entry is unique.
		/// </summary>
		/// <param name="n">number of times to make a request</param>
		/// <param name="source">Source of the log message</param>
		/// <param name="category">Category of the log message</param>
		/// <returns>Response of the last request</returns>
		private async Task<HttpResponseMessage> MakeNLogMessageRequests(
			int n, 
			string source = "the-source", 
			int category = 1, 
			LogEntrySeverities severity = LogEntrySeverities.Warn
		)
		{
			var _url = "/api/logmessage";

			var parameters = new Dictionary<string, string> {
				{ "severity", severity.ToString() },
				{ "source", source },
				{ "category", category.ToString() }
			};

			lock (_logLocker)
			{
				if (!_client.DefaultRequestHeaders.Contains("apikey"))
				{
					_client.DefaultRequestHeaders.Add("apikey", _apiKey);
				}
			}

			HttpResponseMessage response = null;

			for (int i = 0; i < n; i++)
			{
				parameters["message"] = $"Hello, world {DateTime.UtcNow}";

				response = await _client.PostAsync(_url, new FormUrlEncodedContent(parameters));
			}

			return response;
		}
	}
}
