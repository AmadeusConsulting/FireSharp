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
            ILogManager logManager,
            CancellationTokenSource cancellationTokenSource)
            : base(basePath, httpResponse, cancellationTokenSource, logManager, cache)
        {
            if (requestManager == null)
            {
                throw new ArgumentNullException(nameof(requestManager));
            }
            
            _basePath = basePath ?? string.Empty;
            _added = added;
            _changed = changed;
            _removed = removed;
            _requestManager = requestManager;
        }

        #endregion

        #region Methods

        protected override async Task HandleReadLoopDataAsync(string eventName, string path, string dataJson)
        {
            if (string.IsNullOrEmpty(dataJson))
            {
                throw new ArgumentNullException(nameof(dataJson));
            }

            if (path == "/")
            {
                var dataDictionary = dataJson != "null" ? dataJson.ReadAs<Dictionary<string, JToken>>() : null;

                if (eventName == StreamingEventType.Put)
                {
                    if (dataDictionary != null)
                    {
                        // this is an addition of multiple entities, which occurs 
                        // when first listening to a path with existing entites
                        foreach (var key in dataDictionary.Keys)
                        {
                            var jtoken = dataDictionary[key];
                            try
                            {
                                var entity = jtoken.ToObject<T>();
                                await Cache.AddOrUpdate(key, entity);
                                _added(this, key, entity);
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"Error converting entity list value to {typeof(T).Name}.  The value was: \n{jtoken}", ex);
                                throw;
                            }
                        }
                    }
                    else
                    {
                        if (_removed != null)
                        {
                            var deleted = await Cache.GetAllAsync();
                            foreach (var deletedItem in deleted)
                            {
                                _removed(this, _basePath, deletedItem);
                            }
                        }

                        await Cache.RemoveAllAsync();
                    }
                }
                else if (eventName == StreamingEventType.Patch)
                {
                    if (dataDictionary == null)
                    {
                        throw new FormatException("Expected to get JSON Object as data for patch to Entity List Root, instead got null");
                    }

                    foreach (var key in dataDictionary.Keys)
                    {
                        var jtoken = dataDictionary[key];
                        await PatchEntity(key, jtoken.ToObject<JObject>());
                    }
                }
            }
            else
            {
                await HandleEntityUpdate(path, eventName, dataJson);
            }
        }
        
        private static void WriteElement(JsonWriter writer, PathElement elem, string finalValueJson)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(elem.Name);
            if (elem.Child != null)
            {
                WriteElement(writer, elem.Child, finalValueJson);
            }
            else
            {
                writer.WriteRawValue(finalValueJson);
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
                Log.Debug("Bad PATH format!!!");
                Log.Debug(path);
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
                        _removed.Invoke(this, key, toRemove);

                        return;
                    }

                    var entity = dataJson.ReadAs<T>();

                    // cache the entity
                    await Cache.AddOrUpdate(key, entity).ConfigureAwait(false);
                    
                    _added?.Invoke(this, key, entity);
                }
                else if (eventName == StreamingEventType.Patch)
                {
                    await PatchEntity(key, dataJson.ReadAs<JObject>());
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
                        var pathString = GetPathString(pathCaptures);

                        _changed(this, key, new [] { pathString }, entity, oldValue);
                    }
                }
                else
                {
                    // build a json document from the path
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
                        WriteElement(writer, firstElement, dataJson);
                    }
                    
                    await PatchEntity(key, sb.ToString().ReadAs<JObject>());
                }
            }
        }

        private async Task PatchEntity(string key, JObject data)
        {
            var entity = CheapClone(await Cache.Get(key).ConfigureAwait(false));
            var dataJson = data.ToJson();
            
            if (entity == null)
            {
                // entity is not cached, we will need to fetch it prior to updating
                entity = await FetchEntity(key);
            }

            var oldValue = CheapClone(entity);

            // BUG: this throws an exception if the target property is an array and we're updating one index within the array
            JsonConvert.PopulateObject(dataJson, entity);

            await Cache.AddOrUpdate(key, entity);

            var paths = data.Properties().Select(p => p.Name).ToList();

            Log.Debug($"Entity Patched:\nPaths={paths}\nOldvalue:{oldValue}\nNewValue:{entity}");
            
            _changed?.Invoke(this, key, paths, entity, oldValue);
        }

        private TObj CheapClone<TObj>(TObj obj)
        {
            if (Object.ReferenceEquals(obj, null))
            {
                return default(TObj);
            }

            var settings = new JsonSerializerSettings
                               {
                                   ObjectCreationHandling = ObjectCreationHandling.Replace
                               };

            return JsonConvert.DeserializeObject<TObj>(JsonConvert.SerializeObject(obj), settings);
        }

        private static string GetPathString(CaptureCollection pathCaptures)
        {
            return pathCaptures.Cast<Capture>()
                .Aggregate(string.Empty, (str, pc) => $"{str}{(!string.IsNullOrEmpty(str) ? $"/{pc.Value}" : pc.Value)}")
                .ToLowerInvariant();
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