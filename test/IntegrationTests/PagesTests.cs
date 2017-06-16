using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.TestHost;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using StatusMonitor.Shared.Services.Factories;
using Xunit;

namespace StatusMonitor.Tests.IntegrationTests
{
	public class PagesTests
	{
		private readonly TestServer _server;
		private readonly HttpClient _client;

		private readonly string _apiKey = "set-by-config";

		public PagesTests()
		{
			var path = PlatformServices.Default.Application.ApplicationBasePath;
			var contentPath = Path.GetFullPath(Path.Combine(path, $@"../../../../src/web"));

			_server = new TestServer(
				new WebHostBuilder()
					.UseContentRoot(contentPath)
					.UseStartup<Web.Startup>()
					.ConfigureServices(services =>
					{
						services.AddSingleton<IHttpClientFactory, HttpClientFactory>();
						services.Configure((RazorViewEngineOptions options) =>
						{
							var previous = options.CompilationCallback;
							options.CompilationCallback = (context) =>
							{
								previous?.Invoke(context);

								var assembly = typeof(Web.Startup).GetTypeInfo().Assembly;
								var assemblies = assembly.GetReferencedAssemblies().Select(x => MetadataReference.CreateFromFile(Assembly.Load(x).Location))
								.ToList();
								assemblies.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("mscorlib")).Location));
								assemblies.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("System.Private.Corelib")).Location));
								assemblies.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("Microsoft.AspNetCore.Razor")).Location));
								assemblies.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("mscorlib")).Location)); 
								assemblies.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("System.Private.Corelib")).Location)); 
								assemblies.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("System.Linq")).Location)); 
								assemblies.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("System.Threading.Tasks")).Location)); 
								assemblies.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("System.Runtime")).Location)); 
								assemblies.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("System.Dynamic.Runtime")).Location)); 
								assemblies.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("Microsoft.AspNetCore.Razor.Runtime")).Location)); 
								assemblies.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("Microsoft.AspNetCore.Mvc")).Location)); 
								assemblies.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("Microsoft.AspNetCore.Razor")).Location)); 
								assemblies.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("Microsoft.AspNetCore.Mvc.Razor")).Location));
								assemblies.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("Microsoft.AspNetCore.Html.Abstractions")).Location)); 
								assemblies.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("System.Text.Encodings.Web")).Location));
								assemblies.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("System.IO")).Location));
								assemblies.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("Microsoft.CSharp")).Location));
								assemblies.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("Microsoft.AspNetCore.Http.Extensions")).Location));
								assemblies.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("System.Private.Uri")).Location));

								context.Compilation = context.Compilation.AddReferences(assemblies);
							};
						});
					})
			);

			_client = _server.CreateClient();

			_apiKey =
				new ConfigurationBuilder()
				.AddYamlFile("appsettings.yml", optional: false)
				.AddYamlFile("appsettings.testing.yml", optional: true)
				.Build()
				["Secrets:ApiKey"];
		}

		[Fact]
		public async Task Scenario()
		{
			// No content initially
			var indexNoContent = await _client.GetAsync("/");

			Assert.Equal(HttpStatusCode.OK, indexNoContent.StatusCode);

			// Add CPU Load data point
			_client.DefaultRequestHeaders.Add("apikey", _apiKey);

			var postOk = await _client.PostAsync(
				"/api/cpuload", 
				new FormUrlEncodedContent(
					new Dictionary<string, string> {
						{ "value", "20" },
						{ "source", "some-source" }
					 }
				)
			);

			Assert.Equal(HttpStatusCode.OK, postOk.StatusCode);

			// Index page OK
			var indexOk = await _client.GetAsync("/");

			Assert.Equal(HttpStatusCode.OK, indexOk.StatusCode);

			// Metric page OK
			var metricOk = await _client.GetAsync("/home/metric/CpuLoad/some-source");

			Assert.Equal(HttpStatusCode.OK, metricOk.StatusCode);

			// Metric NotFound
			var metricNotFound = await _client.GetAsync("/home/metric/CpuLoad/non-existing-source");

			Assert.Equal(HttpStatusCode.NotFound, metricNotFound.StatusCode);

			// Metric BadRequest
			var metricBadRequest = await _client.GetAsync("/home/metric/bad-type/any-source");

			Assert.Equal(HttpStatusCode.BadRequest, metricBadRequest.StatusCode);
		}
	}
}
