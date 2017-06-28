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
		public async Task UserActionEndpoint()
		{
			// Arrange
			_client.DefaultRequestHeaders.Add("apikey", _apiKey);
			
			var _url = "/api/useraction";

			var parameters = new Dictionary<string, string> {
				{ "action", "Login" },
				{ "count", 10.ToString() },
				{ "source", "existing-source" }
			};

			// Act
			var ok = await _client.PostAsync(_url, new FormUrlEncodedContent(parameters));

			// Assert
			Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
		}
	}
}
