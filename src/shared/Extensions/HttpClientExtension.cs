using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

public static class HttpClientExtensions
{
	/// <summary>
	/// Wrapper around PatchAsync which takes Uri, not string
	/// </summary>
	/// <param name="client">The client from which to send</param>
	/// <param name="requestUri">URI to which to send</param>
	/// <param name="iContent">Data to include in the request</param>
	/// <returns>Response object</returns>
	public static async Task<HttpResponseMessage> PatchAsync(this HttpClient client, string requestUrl, HttpContent iContent)
	{
		return await client.PatchAsync(new Uri(requestUrl, UriKind.Relative), iContent);
	}

	/// <summary>
	/// Sends a PATCH HTTP request
	/// </summary>
	/// <param name="client">The client from which to send</param>
	/// <param name="requestUri">URI to which to send</param>
	/// <param name="iContent">Data to include in the request</param>
	/// <returns>Response object</returns>
	public static async Task<HttpResponseMessage> PatchAsync(this HttpClient client, Uri requestUri, HttpContent iContent)
	{
		var method = new HttpMethod("PATCH");
		var request = new HttpRequestMessage(method, requestUri)
		{
			Content = iContent
		};

		HttpResponseMessage response = new HttpResponseMessage();
		try
		{
			response = await client.SendAsync(request);
		}
		catch (TaskCanceledException e)
		{
			Debug.WriteLine("ERROR: " + e.ToString());
		}

		return response;
	}
}
