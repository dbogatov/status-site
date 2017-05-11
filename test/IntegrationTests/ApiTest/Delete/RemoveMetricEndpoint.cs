using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using StatusMonitor.Shared.Models.Entities;
using Xunit;

namespace StatusMonitor.Tests.IntegrationTests
{
	public partial class ApiControllerTest
	{
		[Fact]
		public async Task RemoveMetricEndpointOK()
		{
			// Arrange

			_client.DefaultRequestHeaders.Add("apikey", _apiKey);

			await _client.PostAsync(
				"/api/cpuload",
				new FormUrlEncodedContent(
					new Dictionary<string, string> {
						{"value", 20.ToString()},
						{"source", "the-source"}
					 }
				)
			);

			// Act

			var okResponse = await _client.DeleteAsync(
				QueryHelpers.AddQueryString(
					"/api/removemetric",
					new Dictionary<string, string> {
						{"type", Metrics.CpuLoad.ToString() },
						{"source", "the-source"}
					 }
				)
			);

			// Assert

			Assert.Equal(HttpStatusCode.OK, okResponse.StatusCode);
		}

		[Fact]
		public async Task RemoveMetricEndpointNotFound()
		{
			// Arrange

			_client.DefaultRequestHeaders.Add("apikey", _apiKey);

			// Act

			var notFoundResponse = await _client.DeleteAsync(
				QueryHelpers.AddQueryString(
					"/api/removemetric",
					new Dictionary<string, string> {
						{"type", Metrics.CpuLoad.ToString() },
						{"source", "non-existing-source"}
					 }
				)
			);

			// Assert

			Assert.Equal(HttpStatusCode.NotFound, notFoundResponse.StatusCode);
		}
	}
}
