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
		public async Task GetMetricsEndpointOKPublic()
		{
			// Arrange
			_client.DefaultRequestHeaders.Add("apikey", _apiKey);

			await _client.PostAsync(
				"/api/cpuload",
				new FormUrlEncodedContent(
					new Dictionary<string, string> {
						{ "value", 20.ToString() },
						{ "source", "the-source" }
					 }
				)
			);

			_client.DefaultRequestHeaders.Remove("apikey");

			var _url = "/api/getmetrics";

			// Act
			var ok = await _client.GetAsync(_url);

			// Assert
			Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

			var data = JsonConvert.DeserializeObject(
				await ok.Content.ReadAsStringAsync()
			);
			Assert.NotNull(data);
		}

		[Fact]
		public async Task GetMetricsEndpointOKPrivate()
		{
			// Arrange
			_client.DefaultRequestHeaders.Add("apikey", _apiKey);

			await _client.PostAsync(
				"/api/cpuload",
				new FormUrlEncodedContent(
					new Dictionary<string, string> {
						{ "value", 20.ToString() },
						{ "source", "the-source" }
					 }
				)
			);

			await _client.PatchAsync(
				"/api/metricupdate",
				new FormUrlEncodedContent(
					new Dictionary<string, string> {
						{ "ManualLabelId", ((int)ManualLabels.Investigating).ToString() },
						{ "Public", false.ToString() },
						{ "source", "the-source" },
						{ "type", Metrics.CpuLoad.ToString() }
					}
				)
			);

			var _url = "/api/getmetrics";

			// Act
			var ok = await _client.GetAsync(_url);

			// Assert
			Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

			var data = JsonConvert.DeserializeObject(
				await ok.Content.ReadAsStringAsync()
			);
			Assert.NotNull(data);
		}

		[Fact]
		public async Task GetMetricsEndpointOKWithFilter()
		{
			// Arrange
			_client.DefaultRequestHeaders.Add("apikey", _apiKey);

			var _url = "/api/getmetrics";

			await _client.PostAsync(
				"/api/cpuload",
				new FormUrlEncodedContent(
					new Dictionary<string, string> {
						{ "value", 20.ToString() },
						{ "source", "the-source" }
					 }
				)
			);

			await _client.PostAsync(
				"/api/cpuload",
				new FormUrlEncodedContent(
					new Dictionary<string, string> {
						{ "value", 20.ToString() },
						{ "source", "the-other-source" }
					 }
				)
			);

			await _client.PostAsync(
				"/api/useraction",
				new FormUrlEncodedContent(
					new Dictionary<string, string> {
						{ "action", UserActions.Login.ToString() },
						{ "count", 20.ToString() },
						{ "source", "the-source" }
					 }
				)
			);

			_client.DefaultRequestHeaders.Remove("apikey");

			// Act

			// Type
			var parametersType = new Dictionary<string, string> {
				{ "type", Metrics.CpuLoad.ToString() }
			};

			var okType = await _client.GetAsync(QueryHelpers.AddQueryString(_url, parametersType));

			// Source
			var parametersSource = new Dictionary<string, string> {
				{ "source", "the-source" }
			};

			var okSource = await _client.GetAsync(QueryHelpers.AddQueryString(_url, parametersSource));
			
			// Type and source
			var parametersSourceType = new Dictionary<string, string> {
				{ "type", Metrics.CpuLoad.ToString() },
				{ "source", "the-source" }
			};

			var okSourceType = await _client.GetAsync(QueryHelpers.AddQueryString(_url, parametersSourceType));

			// Assert

			// Type
			Assert.Equal(HttpStatusCode.OK, okType.StatusCode);

			var dataType = JsonConvert.DeserializeObject(
				await okType.Content.ReadAsStringAsync()
			);
			Assert.NotNull(dataType);

			// Source
			Assert.Equal(HttpStatusCode.OK, okSource.StatusCode);

			var dataSource = JsonConvert.DeserializeObject(
				await okSource.Content.ReadAsStringAsync()
			);
			Assert.NotNull(dataSource);

			// Type and source
			Assert.Equal(HttpStatusCode.OK, okSource.StatusCode);

			var dataSourceType = JsonConvert.DeserializeObject(
				await okSourceType.Content.ReadAsStringAsync()
			);
			Assert.NotNull(dataSourceType);
		}

		[Fact]
		public async Task GetMetricsEndpointNoContent()
		{
			// Arrange
			var _url = "/api/getmetrics";

			// Act
			var noContent = await _client.GetAsync(_url);

			// Assert
			Assert.Equal(HttpStatusCode.NoContent, noContent.StatusCode);
		}

		[Fact]
		public async Task GetMetricsEndpointUnauthorized()
		{
			// Arrange
			_client.DefaultRequestHeaders.Add("apikey", _apiKey);

			await _client.PostAsync(
				"/api/cpuload",
				new FormUrlEncodedContent(
					new Dictionary<string, string> {
						{"value", 20.ToString() },
						{"source", "the-source"}
					 }
				)
			);

			await _client.PatchAsync(
				"/api/metricupdate",
				new FormUrlEncodedContent(
					new Dictionary<string, string> {
						{ "ManualLabelId", ((int)ManualLabels.Investigating).ToString() },
						{ "Public", false.ToString() },
						{ "source", "the-source" },
						{ "type", Metrics.CpuLoad.ToString() }
					}
				)
			);

			_client.DefaultRequestHeaders.Remove("apikey");

			var _url = "/api/getmetrics";

			// Act
			var noContent = await _client.GetAsync(_url);

			// Assert
			Assert.Equal(HttpStatusCode.NoContent, noContent.StatusCode);
		}
	}
}
