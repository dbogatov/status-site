using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using StatusMonitor.Shared.Models.Entities;
using Xunit;

namespace StatusMonitor.Tests.IntegrationTests
{
	public partial class ApiControllerTest
	{
		[Fact]
		public async Task GetLogMessagesEndpointOK()
		{
			// Arrange
			_client.DefaultRequestHeaders.Add("apikey", _apiKey);

			var _url = "/api/getLogMessages";

			await _client.PostAsync(
				"/api/logMessage",
				new FormUrlEncodedContent(
					new Dictionary<string, string> {
						{ "severity", LogEntrySeverities.Info.ToString() },
						{ "source", "the-source" },
						{ "message", "Hello, world" }
					 }
				)
			);

			await _client.PostAsync(
				"/api/logMessage",
				new FormUrlEncodedContent(
					new Dictionary<string, string> {
						{ "severity", LogEntrySeverities.Warn.ToString() },
						{ "source", "the-source" },
						{ "message", "Hello again!" }
					 }
				)
			);

			// Act
			var ok = await _client.GetAsync(_url);

			Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

			var data = JsonConvert.DeserializeObject(
				await ok.Content.ReadAsStringAsync()
			);
			Assert.NotNull(data);
		}

		[Fact]
		public async Task GetLogMessagesEndpointOKWithFilter()
		{
			// Arrange
			_client.DefaultRequestHeaders.Add("apikey", _apiKey);

			var _url = "/api/getLogMessages";

			await _client.PostAsync(
				"/api/logMessage",
				new FormUrlEncodedContent(
					new Dictionary<string, string> {
						{ "severity", LogEntrySeverities.Info.ToString() },
						{ "source", "the-source" },
						{ "message", "Hello, world" }
					 }
				)
			);

			await _client.PostAsync(
				"/api/logMessage",
				new FormUrlEncodedContent(
					new Dictionary<string, string> {
						{ "severity", LogEntrySeverities.Warn.ToString() },
						{ "source", "the-source" },
						{ "message", "Hello again!" }
					 }
				)
			);

			var parameters = new Dictionary<string, string> {
				{ "severity", LogEntrySeverities.Warn.ToString() }
			 };

			// Act
			var ok = await _client.GetAsync(QueryHelpers.AddQueryString(_url, parameters));

			Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

			var data = JsonConvert.DeserializeObject(
				await ok.Content.ReadAsStringAsync()
			);
			Assert.NotNull(data);
		}

		[Fact]
		public async Task GetLogMessagesEndpointNoContent()
		{
			// Arrange
			_client.DefaultRequestHeaders.Add("apikey", _apiKey);

			var _url = "/api/getLogMessages";

			// Act
			var noContent = await _client.GetAsync(_url);

			// Assert
			Assert.Equal(HttpStatusCode.NoContent, noContent.StatusCode);
		}
	}
}
