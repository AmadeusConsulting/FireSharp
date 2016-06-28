using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using FireSharp.EventStreaming;

using Newtonsoft.Json;

namespace FireSharp.Response
{
    public sealed class EventStreamResponse : EventStreamResponseBase<JsonReader>
    {
        #region Constructors and Destructors

        internal EventStreamResponse(
            HttpResponseMessage httpResponse, 
            ValueAddedEventHandler added = null, 
            ValueChangedEventHandler changed = null, 
            ValueRemovedEventHandler removed = null, 
            object context = null)
        {
            CancellationTokenSource = new CancellationTokenSource();

            var cache = new TemporaryCache();

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

            Cache = cache;
            PollingTask = ReadLoop(httpResponse, CancellationTokenSource.Token);
        }

        #endregion

        #region Methods

        protected override async Task ReadLoop(HttpResponseMessage httpResponse, CancellationToken token)
        {
            await Task.Factory.StartNew(
                async () =>
                    {
                        using (httpResponse)
                        using (var content = await httpResponse.Content.ReadAsStreamAsync())
                        using (var sr = new StreamReader(content))
                        {
                            string eventName = null;

                            while (true)
                            {
                                token.ThrowIfCancellationRequested();

                                var read = await sr.ReadLineAsync();

                                Debug.WriteLine(read);

                                if (read.StartsWith("event: "))
                                {
                                    eventName = read.Substring(7);
                                    continue;
                                }

                                if (read.StartsWith("data: "))
                                {
                                    if (string.IsNullOrEmpty(eventName))
                                    {
                                        throw new InvalidOperationException("Payload data was received but an event did not preceed it.");
                                    }

                                    Update(eventName, read.Substring(6));
                                }

                                // start over
                                eventName = null;
                            }
                        }
                    }, 
                TaskCreationOptions.LongRunning);
        }

        private void Update(string eventName, string p)
        {
            switch (eventName)
            {
                case "put":
                case "patch":
                    using (var reader = new JsonTextReader(new StringReader(p)))
                    {
                        ReadToNamedPropertyValue(reader, "path");
                        reader.Read();
                        var path = reader.Value.ToString();

                        if (eventName == "put")
                        {
                            Cache.AddOrUpdate(path, ReadToNamedPropertyValue(reader, "data"));
                        }
                        else
                        {
                            Cache.AddOrUpdate(path, ReadToNamedPropertyValue(reader, "data"));
                        }
                    }

                    break;
            }
        }

        #endregion
    }
}