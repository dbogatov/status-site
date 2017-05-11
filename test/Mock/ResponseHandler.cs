using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace StatusMonitor.Tests.Mock
{
	public enum HttpResponseOption
	{
		Success, Timeout, ServiceUnavailable
	}

	public class ResponseHandler : DelegatingHandler
	{
		private readonly Dictionary<Uri, HttpResponseOption> _urls = new Dictionary<Uri, HttpResponseOption>();
		private readonly Dictionary<Uri, Action> _actions = new Dictionary<Uri, Action>();

		public void AddAction(Uri uri, Action action)
		{
			_actions.Add(uri, action);
		}

		public void RemoveAction(Uri uri)
		{
			_actions.Remove(uri);
		}

		public void AddHandler(Uri uri, HttpResponseOption option)
		{
			_urls.Add(uri, option);
		}

		public void RemoveHandler(Uri uri)
		{
			_urls.Remove(uri);
		}

		protected async override Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken
		)
		{
			if (_actions.Count > 0)
			{
				if (_actions.ContainsKey(request.RequestUri))
				{
					_actions[request.RequestUri].Invoke();
					return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
				}
				else
				{
					return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
				}
			}

			if (_urls.Count > 0)
			{
				if (_urls.ContainsKey(request.RequestUri))
				{
					switch (_urls[request.RequestUri])
					{
						case HttpResponseOption.Success:
							return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
						case HttpResponseOption.Timeout:
							await Task.Delay(5000);
							return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
						case HttpResponseOption.ServiceUnavailable:
							return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
					}

					return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
				}
				else
				{
					return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
				}
			}

			throw new InvalidOperationException("Mo option or action was registered");
		}
	}

}
