using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;

namespace StatusMonitor.Tests.IntegrationTests
{
	public partial class ApiControllerTest
	{
		[Theory]
		[InlineData("getdata", HttpMethods.GET)]
		[InlineData("cpuload", HttpMethods.POST)]
		[InlineData("useraction", HttpMethods.POST)]
		[InlineData("compilation", HttpMethods.POST)]
		[InlineData("logdata", HttpMethods.POST)]
		[InlineData("logmessage", HttpMethods.POST)]
		[InlineData("removemetric", HttpMethods.DELETE)]
		[InlineData("metricupdate", HttpMethods.PATCH)]
		/// <summary>
		/// Check if given endpoint returns status code 400 as per documentation
		/// </summary>
		/// <param name="endpoint">api endpoint, eq: /api/{endpoint}</param>
		/// <param name="method">expected Http methods, eq: GET</param>
		private async Task BadRequestCheck(string endpoint, HttpMethods method)
		{
			var _url = $"/api/{endpoint}";

			// Check Bad Request
			var parameters = new Dictionary<string, string> { };

			_client.DefaultRequestHeaders.Add("apikey", _apiKey);

			HttpResponseMessage badRequest = null;

			switch (method)
			{
				case HttpMethods.GET:
					badRequest = await _client.GetAsync(QueryHelpers.AddQueryString(_url, parameters));
					break;
				case HttpMethods.POST:
					badRequest = await _client.PostAsync(_url, new FormUrlEncodedContent(parameters));
					break;
				case HttpMethods.PATCH:
					badRequest = await _client.PatchAsync(_url, new FormUrlEncodedContent(parameters));
					break;
				case HttpMethods.DELETE:
					badRequest = await _client.DeleteAsync(QueryHelpers.AddQueryString(_url, parameters));
					break;
			}

			Assert.Equal(HttpStatusCode.BadRequest, badRequest.StatusCode);
		}
	}
}
