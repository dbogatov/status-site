using System.Net.Http;

namespace StatusMonitor.Shared.Services.Factories
{
	/// <summary>
	/// Simple factory returning HttpClient
	/// </summary>
	public interface IHttpClientFactory
	{
		/// <summary>
		/// Returns an HttpClient
		/// </summary>
		/// <returns>Newly created HttpClient</returns>
		HttpClient BuildClient();
	}

	public class HttpClientFactory : IHttpClientFactory
	{
		public HttpClient BuildClient()
		{
			return new HttpClient();
		}
	}
}
