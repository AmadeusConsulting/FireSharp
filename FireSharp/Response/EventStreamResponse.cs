using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using FireSharp.EventStreaming;
using FireSharp.Logging;

using Newtonsoft.Json;

namespace FireSharp.Response
{
    public sealed class EventStreamResponse : EventStreamResponseBase<JsonReader>
    {
        #region Constructors and Destructors

        internal EventStreamResponse(
            string path,
            HttpResponseMessage httpResponse,
            CancellationTokenSource cancellationTokenSource,
            ILogManager logManager,
            ValueAddedEventHandler added = null,
            ValueChangedEventHandler changed = null,
            ValueRemovedEventHandler removed = null,
            object context = null)
            : base(path, httpResponse, cancellationTokenSource, logManager, new TemporaryCache())
        {
            var cache = (TemporaryCache)Cache;

            if (added != null)
            {
                cache.Added += added;
            }

            if (changed != null)
            {
                cache.Changed += changed;
            }

            if (removed != null)
            {
                cache.Removed += removed;
            }

            if (context != null)
            {
                cache.Context = context;
            }
        }

        #endregion

        protected override Task HandleReadLoopDataAsync(string eventName, string path, string dataJson)
        {
            switch (eventName)
            {
                case "put":
                case "patch":
                    using (var dataReader = new JsonTextReader(new StringReader(dataJson)))
                    {
                        if (eventName == "put")
                        {
                            Cache.AddOrUpdate(path, dataReader);
                        }
                        else
                        {
                            Cache.AddOrUpdate(path, dataReader);
                        }
                    }

                    break;
            }

            return Task.FromResult(0);
        }
    }
}