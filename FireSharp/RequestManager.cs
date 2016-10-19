using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using FireSharp.Exceptions;
using FireSharp.Interfaces;
using FireSharp.Logging;
using FireSharp.Security;

namespace FireSharp
{
    public class RequestManager : IRequestManager
    {
        private readonly IHttpClientProvider _httpClientProvider;

        private readonly ISerializer _serializer;

        private readonly ILogManager _logManager;

        private readonly IRequestAuthenticator _requestAuthenticator;

        public static readonly HttpMethod Patch = new HttpMethod("PATCH");

        private readonly ILog _log;

        private Uri _baseUri;

        private TimeSpan _requestTimeout;

        public RequestManager(
            Uri baseUri,
            IHttpClientProvider httpClientProvider,
            ISerializer serializer,
            ILogManager logManager,
            IRequestAuthenticator requestAuthenticator,
            TimeSpan? requestTimeout = null)
        {
            if (baseUri == null)
            {
                throw new ArgumentNullException(nameof(baseUri));
            }
            if (httpClientProvider == null)
            {
                throw new ArgumentNullException(nameof(httpClientProvider));
            }
            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }
            if (logManager == null)
            {
                throw new ArgumentNullException(nameof(logManager));
            }
            if (requestAuthenticator == null)
            {
                throw new ArgumentNullException(nameof(requestAuthenticator));
            }

            _requestTimeout = requestTimeout ?? TimeSpan.FromSeconds(30);
            _baseUri = baseUri;
            _httpClientProvider = httpClientProvider;
            _serializer = serializer;
            _logManager = logManager;
            _requestAuthenticator = requestAuthenticator;
            _log = _logManager.GetLogger<RequestManager>();
        }

        public void Dispose()
        {
        }

        public async Task<HttpResponseMessage> ListenAsync(string path)
        {
            var request = PrepareEventStreamRequest(path, null);

            var response = await _httpClientProvider.GetHttpClient(_baseUri, _requestTimeout).SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            await CheckStatusCode(response).ConfigureAwait(false);

            return response;
        }

        public async Task<HttpResponseMessage> ListenAsync(string path, QueryBuilder queryBuilder)
        {
            var request = PrepareEventStreamRequest(path, queryBuilder);

            try
            {
                var response = await _httpClientProvider.GetHttpClient(_baseUri, _requestTimeout).SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

                await CheckStatusCode(response).ConfigureAwait(false);

                return response;
            }
            catch (WebException ex)
            {
                _log.Error($"Failed to complete request to {request.RequestUri}", ex);
                throw new FirebaseApiException("Firebase API Request Failed.", null, ex);
            }
        }

        private async Task CheckStatusCode(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                _log.Error($"Request status code was not between 200 and 299: \n\n{responseContent}");

                throw new FirebaseApiException(
                    $"The response status code does not indicate success.\n\n The server responded with: {responseContent}",
                    response);
            }
        }

        public Task<HttpResponseMessage> RequestAsync(HttpMethod method, string path, object payload = null, bool formatPayload = false)
        {
            return RequestAsync(method, path, null, payload, formatPayload);
        }

        public Task<HttpResponseMessage> RequestAsync(HttpMethod method, string path, QueryBuilder queryBuilder, object payload = null, bool formatPayload = false)
        {
            try
            {
                var uri = PrepareUri(path, queryBuilder);
                var request = PrepareRequest(method, uri, payload, formatPayload);
                
                return _httpClientProvider.GetHttpClient(_baseUri, _requestTimeout).SendAsync(request, HttpCompletionOption.ResponseContentRead);
            }
            catch (Exception ex)
            {
                throw new FirebaseException(
                    $"An error occured while execute request. Path : {path} , Method : {method}", ex);
            }
        }

        private HttpRequestMessage PrepareEventStreamRequest(string path, QueryBuilder queryBuilder)
        {
            var uri = PrepareUri(path, queryBuilder);

            var request = PrepareRequest(HttpMethod.Get, uri);

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

            return request;
        }

        private HttpRequestMessage PrepareRequest(HttpMethod method, Uri uri, object payload = null, bool formatted = false)
        {
            var request = new HttpRequestMessage(method, uri);

            if (payload != null)
            {
                var json = _serializer.Serialize(payload, formatted);
                request.Content = new StringContent(json);
            }

            _requestAuthenticator.AddAuthentication(request);

            return request;
        }

        private Uri PrepareUri(string path, QueryBuilder queryBuilder)
        {
            return new Uri($"{path}.json?{(queryBuilder != null ? queryBuilder.ToQueryString() : string.Empty)}", UriKind.Relative);
        }
    }
}