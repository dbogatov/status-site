using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
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
		public async Task GetDataEndpointNotFound()
		{
			// Arrange

			var _url = "/api/getdata";

			var parameters = new Dictionary<string, string> {
				{ "metrictype", Metrics.CpuLoad.ToString() },
				{ "source", "the-source" }
			 };

			// Act

			var notFound = await _client.GetAsync(QueryHelpers.AddQueryString(_url, parameters));

			// Assert

			Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
		}

		[Fact]
		public async Task GetDataEndpointOKPublic()
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

			_client.DefaultRequestHeaders.Remove("apikey");

			var _url = "/api/getdata";

			var parameters = new Dictionary<string, string> {
				{ "metrictype", Metrics.CpuLoad.ToString() },
				{ "source", "the-source" }
			 };

			// Act
			var ok = await _client.GetAsync(QueryHelpers.AddQueryString(_url, parameters));

			// Assert

			Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

			var data = JsonConvert.DeserializeObject(
				await ok.Content.ReadAsStringAsync()
			);
			Assert.NotNull(data);
		}

		[Fact]
		public async Task GetDataEndpointOKPrivate()
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

			var _url = "/api/getdata";

			var parameters = new Dictionary<string, string> {
				{ "metrictype", Metrics.CpuLoad.ToString() },
				{ "source", "the-source" }
			 };

			// Act
			var ok = await _client.GetAsync(QueryHelpers.AddQueryString(_url, parameters));

			// Assert
			Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

			var data = JsonConvert.DeserializeObject(
				await ok.Content.ReadAsStringAsync()
			);
			Assert.NotNull(data);
		}

		[Fact]
		public async Task GetDataEndpointNoContent()
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

			_client.DefaultRequestHeaders.Remove("apikey");

			var _url = "/api/getdata";

			var parameters = new Dictionary<string, string> {
				{ "metrictype", Metrics.CpuLoad.ToString() },
				{ "source", "the-source" },
				{ "timeperiod", 1.ToString() }
			 };

			// Act
			Thread.Sleep(1000); // Sleep so that there is no data requested withing timeframe

			var noContent = await _client.GetAsync(QueryHelpers.AddQueryString(_url, parameters));

			// Assert
			Assert.Equal(HttpStatusCode.NoContent, noContent.StatusCode);
		}
	}
}
