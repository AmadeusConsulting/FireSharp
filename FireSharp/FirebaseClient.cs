using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using FireSharp.EventStreaming;
using FireSharp.Exceptions;
using FireSharp.Extensions;
using FireSharp.Interfaces;
using FireSharp.Logging;
using FireSharp.Response;

using Newtonsoft.Json.Linq;

namespace FireSharp
{
    public class FirebaseClient : IFirebaseClient, IDisposable
    {
        #region Constants

        private const string DatabaseRulesPath = ".settings/rules";

        #endregion

        #region Fields

        private readonly IEventStreamCacheProvider _cacheProvider;

        private readonly Action<HttpStatusCode, string> _defaultErrorHandler = (statusCode, body) =>
            {
                if (statusCode < HttpStatusCode.OK || statusCode >= HttpStatusCode.BadRequest)
                {
                    throw new FirebaseException(statusCode, body);
                }
            };

        private readonly ILogManager _logManager;

        private readonly IRequestManager _requestManager;

        #endregion

        #region Constructors and Destructors

        public FirebaseClient(IFirebaseConfig config)
            : this(
                new RequestManager(
                    new Uri(config.BasePath),
                    config.HttpClientProvider,
                    config.Serializer,
                    config.LogManager,
                    config.RequestAuthenticator,
                    config.RequestTimeout),
                config.CacheProvider,
                config.LogManager)
        {}

        public FirebaseClient(IRequestManager requestManager, IEventStreamCacheProvider cacheProvider, ILogManager logManager)
        {
            if (requestManager == null)
            {
                throw new ArgumentNullException(nameof(requestManager));
            }
            if (cacheProvider == null)
            {
                throw new ArgumentNullException(nameof(cacheProvider));
            }

            if (logManager == null)
            {
                throw new ArgumentNullException(nameof(logManager));
            }

            _requestManager = requestManager;
            _cacheProvider = cacheProvider;
            _logManager = logManager;
        }

        ~FirebaseClient()
        {
            Dispose(false);
        }

        #endregion

        #region Public Methods and Operators

        public FirebaseResponse Delete(string path)
        {
            try
            {
                using (var response = _requestManager.RequestAsync(HttpMethod.Delete, path).Result)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    HandleIfErrorResponse(response.StatusCode, content);
                    return new FirebaseResponse(content, response.StatusCode);
                }
            }
            catch (HttpRequestException ex)
            {
                throw new FirebaseException(ex);
            }
        }

        public async Task<FirebaseResponse> DeleteAsync(string path)
        {
            try
            {
                using (var response = await _requestManager.RequestAsync(HttpMethod.Delete, path).ConfigureAwait(false))
                {
                    var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    HandleIfErrorResponse(response.StatusCode, content);
                    return new FirebaseResponse(content, response.StatusCode);
                }
            }
            catch (HttpRequestException ex)
            {
                throw new FirebaseException(ex);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public FirebaseResponse Get(string path)
        {
            try
            {
                using (var response = _requestManager.RequestAsync(HttpMethod.Get, path).Result)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    HandleIfErrorResponse(response.StatusCode, content);
                    return new FirebaseResponse(content, response.StatusCode);
                }
            }
            catch (HttpRequestException ex)
            {
                throw new FirebaseException(ex);
            }
        }

        public FirebaseResponse Get(string path, QueryBuilder queryBuilder)
        {
            try
            {
                using (var response = _requestManager.RequestAsync(HttpMethod.Get, path, queryBuilder).Result)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    HandleIfErrorResponse(response.StatusCode, content);
                    return new FirebaseResponse(content, response.StatusCode);
                }
            }
            catch (HttpRequestException ex)
            {
                throw new FirebaseException(ex);
            }
        }

        public async Task<FirebaseResponse> GetAsync(string path, QueryBuilder queryBuilder)
        {
            try
            {
                using (var response = await _requestManager.RequestAsync(HttpMethod.Get, path, queryBuilder).ConfigureAwait(false))
                {
                    var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    HandleIfErrorResponse(response.StatusCode, content);
                    return new FirebaseResponse(content, response.StatusCode);
                }
            }
            catch (HttpRequestException ex)
            {
                throw new FirebaseException(ex);
            }
        }

        public async Task<FirebaseResponse> GetAsync(string path)
        {
            try
            {
                using (var response = await _requestManager.RequestAsync(HttpMethod.Get, path).ConfigureAwait(false))
                {
                    var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    HandleIfErrorResponse(response.StatusCode, content);
                    return new FirebaseResponse(content, response.StatusCode);
                }
            }
            catch (HttpRequestException ex)
            {
                throw new FirebaseException(ex);
            }
        }

        public async Task<DatabaseRules> GetDatabaseRulesAsync()
        {
            try
            {
                using (var response = await _requestManager.RequestAsync(HttpMethod.Get, DatabaseRulesPath).ConfigureAwait(false))
                {
                    var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    HandleIfErrorResponse(response.StatusCode, content);
                    var rules = content.ReadAs<JObject>();

                    return rules["rules"].ToObject<DatabaseRules>();
                }
            }
            catch (HttpRequestException ex)
            {
                throw new FirebaseException(ex);
            }
        }

        [Obsolete("This method is obsolete use OnAsync instead.")]
        public async Task<IEventStreamResponse> ListenAsync(
            string path,
            ValueAddedEventHandler added = null,
            ValueChangedEventHandler changed = null,
            ValueRemovedEventHandler removed = null)
        {
            return new EventStreamResponse(
                       path,
                       await _requestManager.ListenAsync(path).ConfigureAwait(false),
                       new CancellationTokenSource(),
                       _logManager,
                       added,
                       changed,
                       removed);
        }

        public async Task<IEventStreamResponse> MonitorEntityListAsync<TEntity>(
            string path,
            EntityAddedEventHandler<TEntity> added,
            EntityChangedEventHandler<TEntity> changed,
            EntityRemovedEventHandler<TEntity> removed,
            QueryBuilder queryBuilder = null)
        {
            return new EventEntityResponse<TEntity>(
                       await _requestManager.ListenAsync(path, queryBuilder).ConfigureAwait(false),
                       path,
                       added,
                       changed,
                       removed,
                       _cacheProvider.GetCache<TEntity>(path),
                       _requestManager,
                       _logManager,
                       new CancellationTokenSource());
        }

        public async Task<IEventStreamResponse> OnAsync(
            string path,
            ValueAddedEventHandler added = null,
            ValueChangedEventHandler changed = null,
            ValueRemovedEventHandler removed = null,
            object context = null)
        {
            return new EventStreamResponse(
                       path,
                       await _requestManager.ListenAsync(path).ConfigureAwait(false),
                       new CancellationTokenSource(),
                       _logManager,
                       added,
                       changed,
                       removed,
                       context);
        }

        public async Task<EventStreamResponse> OnAsync(
            string path,
            QueryBuilder queryBuilder,
            ValueAddedEventHandler added = null,
            ValueChangedEventHandler changed = null,
            ValueRemovedEventHandler removed = null,
            object context = null)
        {
            return new EventStreamResponse(
                       path,
                       await _requestManager.ListenAsync(path, queryBuilder).ConfigureAwait(false),
                       new CancellationTokenSource(),
                       _logManager,
                       added,
                       changed,
                       removed,
                       context);
        }

        public async Task<IEventStreamResponse> OnChangeGetAsync<T>(string path, ValueRootAddedEventHandler<T> added = null)
        {
            return new EventRootResponse<T>(
                       await _requestManager.ListenAsync(path).ConfigureAwait(false),
                       added,
                       _requestManager,
                       path,
                       _logManager,
                       new CancellationTokenSource());
        }

        public PushResponse Push<T>(string path, T data)
        {
            try
            {
                using (var response = _requestManager.RequestAsync(HttpMethod.Post, path, data).Result)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    HandleIfErrorResponse(response.StatusCode, content);
                    return new PushResponse(content, response.StatusCode);
                }
            }
            catch (HttpRequestException ex)
            {
                throw new FirebaseException(ex);
            }
        }

        public async Task<PushResponse> PushAsync<T>(string path, T data)
        {
            try
            {
                using (var response = await _requestManager.RequestAsync(HttpMethod.Post, path, data).ConfigureAwait(false))
                {
                    var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    HandleIfErrorResponse(response.StatusCode, content);
                    return new PushResponse(content, response.StatusCode);
                }
            }
            catch (HttpRequestException ex)
            {
                throw new FirebaseException(ex);
            }
        }

        public SetResponse Set<T>(string path, T data)
        {
            try
            {
                using (var response = _requestManager.RequestAsync(HttpMethod.Put, path, data).Result)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    HandleIfErrorResponse(response.StatusCode, content);
                    return new SetResponse(content, response.StatusCode);
                }
            }
            catch (HttpRequestException ex)
            {
                throw new FirebaseException(ex);
            }
        }

        public async Task<SetResponse> SetAsync<T>(string path, T data)
        {
            try
            {
                using (var response = await _requestManager.RequestAsync(HttpMethod.Put, path, data).ConfigureAwait(false))
                {
                    var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    HandleIfErrorResponse(response.StatusCode, content);
                    return new SetResponse(content, response.StatusCode);
                }
            }
            catch (HttpRequestException ex)
            {
                throw new FirebaseException(ex);
            }
        }

        public async Task<SetResponse> SetDatabaseRulesAsync(IDictionary<string, object> rules)
        {
            try
            {
                using (var response = await _requestManager.RequestAsync(
                                          HttpMethod.Put,
                                          DatabaseRulesPath,
                                          new Dictionary<string, object>
                                              {
                                                      { "rules", rules }
                                              },
                                          formatPayload: true))
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    HandleIfErrorResponse(response.StatusCode, content);
                    return new SetResponse(content, response.StatusCode);
                }
            }
            catch (HttpRequestException ex)
            {
                throw new FirebaseException(ex);
            }
        }

        public FirebaseResponse Update<T>(string path, T data)
        {
            try
            {
                using (var response = _requestManager.RequestAsync(RequestManager.Patch, path, data).Result)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    HandleIfErrorResponse(response.StatusCode, content);
                    return new FirebaseResponse(content, response.StatusCode);
                }
            }
            catch (HttpRequestException ex)
            {
                throw new FirebaseException(ex);
            }
        }

        public async Task<FirebaseResponse> UpdateAsync<T>(string path, T data)
        {
            try
            {
                using (var response = await _requestManager.RequestAsync(RequestManager.Patch, path, data).ConfigureAwait(false))
                {
                    var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    HandleIfErrorResponse(response.StatusCode, content);
                    return new FirebaseResponse(content, response.StatusCode);
                }
            }
            catch (HttpRequestException ex)
            {
                throw new FirebaseException(ex);
            }
        }

        #endregion

        #region Methods

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _requestManager.Dispose();
            }
        }

        private void HandleIfErrorResponse(HttpStatusCode statusCode, string content, Action<HttpStatusCode, string> errorHandler = null)
        {
            if (errorHandler != null)
            {
                errorHandler(statusCode, content);
            }
            else
            {
                _defaultErrorHandler(statusCode, content);
            }
        }

        #endregion
    }
}