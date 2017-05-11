using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using StatusMonitor.Shared.Models.Entities;
using Xunit;

namespace StatusMonitor.Tests.IntegrationTests
{
	public partial class ApiControllerTest
	{
		[Fact]
		public async Task MetricUpdateEndpointOK()
		{
			// Arrange
			_client.DefaultRequestHeaders.Add("apikey", _apiKey);
			
			await _client.PostAsync(
				"/api/cpuload",
				new FormUrlEncodedContent(
					new Dictionary<string, string> {
						{"value", "20"},
						{"source", "the-source"}
					 }
				)
			);

			var _url = "/api/metricupdate";

			var parameters = new Dictionary<string, string> {
				{ "ManualLabelId", ((int)ManualLabels.Investigating).ToString() },
				{ "Public", true.ToString() },
				{ "source", "the-source" },
				{ "type", Metrics.CpuLoad.ToString() }
			};

			// Act
			var ok = await _client.PatchAsync(new Uri(_url, UriKind.Relative), new FormUrlEncodedContent(parameters));

			// Assert
			Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
		}

		[Fact]
		public async Task MetricUpdateEndpointLabelNotFound()
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

			var _url = "/api/metricupdate";

			var parameters = new Dictionary<string, string> {
				{ "ManualLabelId", (-1).ToString() },
				{ "Public", true.ToString() },
				{ "source", "the-source" },
				{ "type", Metrics.CpuLoad.ToString() }
			};

			// Act
			var notFound = await _client.PatchAsync(_url, new FormUrlEncodedContent(parameters));

			// Assert
			Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
		}

		[Fact]
		public async Task MetricUpdateEndpointMetricNotFound()
		{
			// Arrange
			_client.DefaultRequestHeaders.Add("apikey", _apiKey);

			var _url = "/api/metricupdate";

			var parameters = new Dictionary<string, string> {
				{ "ManualLabelId", ((int)ManualLabels.Investigating).ToString() },
				{ "Public", true.ToString() },
				{ "source", "non-existing-source" },
				{ "type", Metrics.CpuLoad.ToString() }
			};

			// Act
			var notFound = await _client.PatchAsync(new Uri(_url, UriKind.Relative), new FormUrlEncodedContent(parameters));

			// Assert
			Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
		}
	}
}
