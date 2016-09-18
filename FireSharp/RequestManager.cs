using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using FireSharp.Exceptions;
using FireSharp.Interfaces;
using FireSharp.Logging;

namespace FireSharp
{
    public class RequestManager : IRequestManager
    {
        private readonly HttpClient _httpClient;

        private readonly ISerializer _serializer;

        private readonly ILogManager _logManager;

        private readonly string _authSecret;

        private readonly string _apiHost;

        public static readonly HttpMethod Patch = new HttpMethod("PATCH");

        private readonly ILog _log;

        public RequestManager(HttpClient httpClient, ISerializer serializer, ILogManager logManager, string authSecret = null, string apiHost = null)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException(nameof(httpClient));
            }
            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }
            if (logManager == null)
            {
                throw new ArgumentNullException(nameof(logManager));
            }

            _httpClient = httpClient;
            _serializer = serializer;
            _logManager = logManager;
            _authSecret = authSecret;
            _apiHost = apiHost;
            _log = _logManager.GetLogger<RequestManager>();
        }

        public void Dispose()
        {
        }

        public async Task<HttpResponseMessage> ListenAsync(string path)
        {
            var request = PrepareEventStreamRequest(path, null);

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            await CheckStatusCode(response).ConfigureAwait(false);

            return response;
        }

        public async Task<HttpResponseMessage> ListenAsync(string path, QueryBuilder queryBuilder)
        {
            var request = PrepareEventStreamRequest(path, queryBuilder);

            try
            {
                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

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

                return _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead);
            }
            catch (Exception ex)
            {
                throw new FirebaseException(
                    $"An error occured while execute request. Path : {path} , Method : {method}", ex);
            }
        }

        public Task<HttpResponseMessage> RequestApiAsync(HttpMethod method, string path, QueryBuilder queryBuilder, object payload = null)
        {
            try
            {
                var uri = PrepareApiUri(path, queryBuilder);
                var request = PrepareRequest(method, uri, payload);

                return _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead);
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

            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

            return request;
        }

        private HttpRequestMessage PrepareRequest(HttpMethod method, Uri uri, object payload, bool prettyPrint = false)
        {
            var request = new HttpRequestMessage(method, uri);

            if (payload != null)
            {
                var json = _serializer.Serialize(payload, prettyPrint);
                request.Content = new StringContent(json);
            }

            return request;
        }

        private Uri PrepareUri(string path, QueryBuilder queryBuilder)
        {
            var authToken = !string.IsNullOrWhiteSpace(_authSecret)
                ? $"{path}.json?auth={_authSecret}"
                : $"{path}.json";

            var queryStr = string.Empty;
            if (queryBuilder != null)
            {
                queryStr = $"&{queryBuilder.ToQueryString()}";
            }

            var url = $"{authToken}{queryStr}";

            return new Uri(url, UriKind.Relative);
        }

        private Uri PrepareApiUri(string path, QueryBuilder queryBuilder)
        {
            string uriString = $"https://auth.firebase.com/v2/{_apiHost}/{path}?{queryBuilder.ToQueryString()}";
            return new Uri(uriString);
        }
    }
}