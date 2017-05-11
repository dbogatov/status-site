using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace StatusMonitor.Tests.IntegrationTests
{
	public partial class ApiControllerTest
	{
		private readonly TestServer _server;
		private readonly HttpClient _client;

		private readonly string _apiKey = "set-by-config";

		public ApiControllerTest()
		{
			_server = new TestServer(
				new WebHostBuilder()
					.UseStartup<Web.Startup>()
			);

			_client = _server.CreateClient();

			_apiKey =
				new ConfigurationBuilder()
				.AddJsonFile("appsettings.json", optional: false)
				.Build()
				["Secrets:ApiKey"];
		}
	}

	/// <summary>
	/// Set of Http methods needed for testing
	/// </summary>
	public enum HttpMethods
	{
		GET, POST, PUT, DELETE, HEAD, PATCH
	}
}
