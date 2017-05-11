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
		[Theory]
		[InlineData("cpuload")]
		[InlineData("useraction")]
		[InlineData("compilation")]
		[InlineData("logdata")]
		[InlineData("logmessage")]
		private async Task MalformedSourceCheck(string endpoint)
		{
			// Arrange

			var _url = $"/api/{endpoint}";

			// Check Bad Request
			var parameters = new Dictionary<string, string> {
				{ "stage", CompilationStages.M4.ToString() },
				{ "sourcesize", 15.ToString() },
				{ "compiletime", 78.ToString() },
				{ "action", UserActions.Login.ToString() },
				{ "count", 10.ToString() },
				{ "severity", LogEntrySeverities.Error.ToString() },
				{ "message", "Hello" },
				{ "value", 50.ToString() }
			 };

			_client.DefaultRequestHeaders.Add("apikey", _apiKey);

			// Act

			parameters["source"] = "";
			var emptySource = await _client.PostAsync(_url, new FormUrlEncodedContent(parameters));

			parameters["source"] = "source-&";
			var specialChars = await _client.PostAsync(_url, new FormUrlEncodedContent(parameters));

			parameters["source"] = "source-<script>alert('lol');</script>";
			var script = await _client.PostAsync(_url, new FormUrlEncodedContent(parameters));

			parameters["source"] = "source-source-source-source-source-source";
			var tooLong = await _client.PostAsync(_url, new FormUrlEncodedContent(parameters));

			parameters["source"] = "staging-3.makerchip.com";
			var good = await _client.PostAsync(_url, new FormUrlEncodedContent(parameters));

			// Assert

			Assert.Equal(HttpStatusCode.BadRequest, emptySource.StatusCode);
			Assert.Equal(HttpStatusCode.BadRequest, specialChars.StatusCode);
			Assert.Equal(HttpStatusCode.BadRequest, script.StatusCode);
			Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
			Assert.Equal(HttpStatusCode.OK, good.StatusCode);
		}
	}
}
