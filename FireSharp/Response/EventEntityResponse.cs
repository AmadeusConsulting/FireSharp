using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using FireSharp.EventStreaming;
using FireSharp.Extensions;
using FireSharp.Interfaces;
using FireSharp.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FireSharp.Response
{
    public sealed class EventEntityResponse<T> : EventStreamResponseBase<T>
    {
        #region Static Fields

        private static readonly Regex EntityKeyAndPathRegex = new Regex(@"/(?<Key>[^/]+)(/(?<Path>[^/]+))*");

        #endregion

        #region Fields

        private readonly EntityAddedEventHandler<T> _added;

        private readonly string _basePath;

        private readonly EntityChangedEventHandler<T> _changed;

        private readonly EntityRemovedEventHandler<T> _removed;

        private readonly IRequestManager _requestManager;

        private readonly IFirebaseConfig _config;

        private readonly ILog _log;

        #endregion

        #region Constructors and Destructors

        internal EventEntityResponse(
            HttpResponseMessage httpResponse, 
            string basePath, 
            EntityAddedEventHandler<T> added, 
            EntityChangedEventHandler<T> changed, 
            EntityRemovedEventHandler<T> removed, 
            IEventStreamResponseCache<T> cache, 
            IRequestManager requestManager,
            IFirebaseConfig config)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            if (requestManager == null)
            {
                throw new ArgumentNullException(nameof(requestManager));
            }

            Cache = cache;
            _basePath = basePath ?? string.Empty;
            _added = added;
            _changed = changed;
            _removed = removed;
            _requestManager = requestManager;
            _config = config;
            _log = config.LogManager.GetLogger(this);

            CancellationTokenSource = new CancellationTokenSource();
            PollingTask = ReadLoop(httpResponse, CancellationTokenSource.Token);
        }

        #endregion

        #region Methods

        protected override async Task ReadLoop(HttpResponseMessage httpResponse, CancellationToken token)
        {
            _log.Debug($"Starting read loop for Entity Event Streaming for path {_basePath}");

            await Task.Factory.StartNew(
                async () =>
                    {
                        using (httpResponse)
                        using (var content = await httpResponse.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        using (var sr = new StreamReader(content))
                        {
                            string eventName = null;

                            while (true)
                            {
                                CancellationTokenSource.Token.ThrowIfCancellationRequested();

                                var read = await sr.ReadLineAsync().ConfigureAwait(false);

                                _log.Debug(read);

                                if (read.StartsWith(EventPrefix))
                                {
                                    eventName = read.Substring(EventPrefix.Length);
                                    continue;
                                }

                                if (eventName == StreamingEventType.KeepAlive)
                                {
                                    // ignore the data line for the keep-alive event (it's always null)
                                    eventName = null;
                                    _log.Debug("Keep-Alive event detected -- skipping data line");
                                    continue;
                                }

                                if (eventName == StreamingEventType.Cancel)
                                {
                                    // security rules have changed such that we no longer have read access to the requested location to be revoked
                                    // TODO: throw an exception that can be handled upstream
                                    _log.Error("Cancel Event received from server. Exiting Read Loop!");
                                    break;
                                }

                                if (eventName == StreamingEventType.AuthRevoked)
                                {
                                    // our auth token is no longer valid
                                    // TODO: throw an exception that can be handled upstream
                                    _log.Error("Firebase Auth Revoked!  Exiting Read Loop!");
                                    break;
                                }

                                try
                                {
                                    if (read.StartsWith(DataPrefix))
                                    {
                                        if (string.IsNullOrEmpty(eventName))
                                        {
                                            throw new InvalidOperationException("Payload data was received but an event did not preceed it.");
                                        }

                                        var match = DataRegex.Match(read.Substring(DataPrefix.Length));
                                        if (match.Success)
                                        {
                                            var path = match.Groups["Path"].Value;
                                            var dataJson = match.Groups["Data"].Value;
                                            if (path == "/" && _added != null)
                                            {
                                                // this is an addition of multiple entities, which occurs 
                                                // when first listening to a path with existing entites
                                                var entityDict = dataJson.ReadAs<Dictionary<string, JToken>>();
                                                foreach (var key in entityDict.Keys)
                                                {
                                                    var jtoken = entityDict[key];
                                                    try
                                                    {
                                                        var entity = jtoken.ToObject<T>();
                                                        await Cache.AddOrUpdate(key, entity);
                                                        _added(this, key, entity);
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        _log.Error(
                                                            $"Error converting entity list value to {typeof(T).Name}.  The value was: \n{jtoken}",
                                                            ex);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                await HandleEntityUpdate(path, eventName, dataJson);
                                            }
                                        }
                                        else
                                        {
                                            _log.Error($"Bad Data Format! \n {read.Substring(DataPrefix.Length)}");
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _log.Error($"Unhandled Exception in Read Loop: {ex.Message}", ex);
                                }

                                // start over
                                eventName = null;
                            }
                        }
                    }, 
                token, 
                TaskCreationOptions.LongRunning, 
                TaskScheduler.Default);
        }

        private static void WriteElement(JsonWriter writer, PathElement elem, string finalValue)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(elem.Name);
            if (elem.Child != null)
            {
                WriteElement(writer, elem.Child, finalValue);
            }
            else
            {
                writer.WriteValue(finalValue);
            }

            writer.WriteEndObject();
        }

        private async Task<T> FetchEntity(string key)
        {
            var fullEntityPath = string.Format("{0}/{1}", _basePath.EndsWith("/") ? _basePath.Substring(0, _basePath.Length - 1) : _basePath, key);
            var response = await _requestManager.RequestAsync(HttpMethod.Get, fullEntityPath, null);
            var jsonStr = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return jsonStr.ReadAs<T>();
        }

        private async Task HandleEntityUpdate(string path, string eventName, string dataJson)
        {
            var keyAndPathMatch = EntityKeyAndPathRegex.Match(path);
            if (!keyAndPathMatch.Success)
            {
                Debug.WriteLine("Bad PATH format!!!");
                Debug.WriteLine(path);
                throw new FormatException($"Invalid Key and Path format: {path}");
            }

            var key = keyAndPathMatch.Groups["Key"].Value;
            var pathCaptures = keyAndPathMatch.Groups["Path"].Captures;
            if (pathCaptures.Count == 0)
            {
                // path was key-only without any nested property path.
                // this represents the addition of a new entity or a patch operation
                if (eventName == StreamingEventType.Put)
                {
                    if (dataJson == "null" && _removed != null)
                    {
                        // entity deleted
                        var toRemove = await Cache.Get(key);
                        await Cache.Remove(key).ConfigureAwait(false);
                        if (_removed != null)
                        {
                            _removed(this, key, toRemove);
                        }

                        return;
                    }

                    var entity = dataJson.ReadAs<T>();

                    await Cache.AddOrUpdate(key, entity).ConfigureAwait(false);

                    if (_added != null)
                    {
                        // cache the entity
                        _added(this, key, entity);
                    }
                }
                else if (eventName == StreamingEventType.Patch)
                {
                    var entity = await Cache.Get(key).ConfigureAwait(false);

                    if (entity == null)
                    {
                        // entity is not cached, we will need to fetch it prior to updating
                        entity = await FetchEntity(key);
                    }

                    var oldValue = JsonConvert.DeserializeObject<T>(entity.ToJson()); // cheap clone operation

                    JsonConvert.PopulateObject(dataJson, entity);

                    await Cache.AddOrUpdate(key, entity);

                    if (_changed != null)
                    {
                        _changed(this, key, entity, oldValue);
                    }
                }
            }
            else
            {
                if (eventName == StreamingEventType.Patch)
                {
                    // ??? does this mean we're patching a nested object?  I presume so
                    // We'd have to do a bit of reflecting to get the coresponding property from the path
                    // For now, we'll just bail and do the easy thing ... get the new value directly from the 
                    // database
                    var entity = await FetchEntity(key);
                    var oldValue = await Cache.Get(key);

                    await Cache.AddOrUpdate(key, entity);

                    if (_changed != null)
                    {
                        _changed(this, key, entity, oldValue);
                    }
                }
                else
                {
                    // build a json document from the path
                    var finalValue = dataJson.ReadAs<string>();

                    PathElement last = null;
                    var pathElements = pathCaptures.Cast<Capture>().Reverse().Select(
                        c =>
                            {
                                var elem = new PathElement
                                               {
                                                   Name = c.Value, 
                                                   Child = last
                                               };
                                last = elem;
                                return elem;
                            }).ToList();

                    var firstElement = pathElements.Last();

                    var sb = new StringBuilder();
                    using (var writer = new JsonTextWriter(new StringWriter(sb)))
                    {
                        WriteElement(writer, firstElement, finalValue);
                    }

                    var entity = await Cache.Get(key);

                    if (entity == null)
                    {
                        entity = await FetchEntity(key);
                    }

                    var oldValue = JsonConvert.DeserializeObject<T>(entity.ToJson()); // cheap clone operation

                    // BUG: this throws an exception if the target property is an array and we're updating one value within the array
                    JsonConvert.PopulateObject(sb.ToString(), entity);

                    await Cache.AddOrUpdate(key, entity);

                    if (_changed != null)
                    {
                        _changed(this, key, entity, oldValue);
                    }
                }
            }
        }

        #endregion

        private class PathElement
        {
            #region Public Properties

            public PathElement Child { get; set; }

            public string Name { get; set; }

            #endregion
        }
    }
}