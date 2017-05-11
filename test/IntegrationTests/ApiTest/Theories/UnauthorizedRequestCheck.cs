using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace StatusMonitor.Tests.IntegrationTests
{
	public partial class ApiControllerTest
	{
		[Theory]
		[InlineData("getlogmessages", HttpMethods.GET)]
		[InlineData("cpuload", HttpMethods.POST)]
		[InlineData("useraction", HttpMethods.POST)]
		[InlineData("compilation", HttpMethods.POST)]
		[InlineData("logdata", HttpMethods.POST)]
		[InlineData("logmessage", HttpMethods.POST)]
		[InlineData("removemetric", HttpMethods.DELETE)]
		[InlineData("metricupdate", HttpMethods.PATCH)]
		/// <summary>
		/// Check if given endpoint returns status code 401 as per documentation
		/// </summary>
		/// <param name="endpoint">api endpoint, eq: /api/{endpoint}</param>
		/// <param name="method">expected Http methods, eq: GET</param>
		private async Task UnauthorizedRequestCheck(string endpoint, HttpMethods method)
		{
			var _url = $"/api/{endpoint}";

			HttpResponseMessage unauthorized = null;

			// Check Unauthorized
			switch (method)
			{
				case HttpMethods.GET:
					unauthorized = await _client.GetAsync(_url);
					break;
				case HttpMethods.POST:
					unauthorized =
						await _client.PostAsync(
							_url,
							new FormUrlEncodedContent(new Dictionary<string, string> { })
						);
					break;
				case HttpMethods.PATCH:
					unauthorized =
						await _client.PatchAsync(
							_url,
							new FormUrlEncodedContent(new Dictionary<string, string> { })
						);
					break;
				case HttpMethods.DELETE:
					unauthorized = await _client.DeleteAsync(_url);
					break;
			}

			Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
		}
	}
}
