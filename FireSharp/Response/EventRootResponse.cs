using FireSharp.EventStreaming;
using FireSharp.Extensions;
using FireSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using FireSharp.Logging;

using Newtonsoft.Json;

namespace FireSharp.Response
{
    public sealed class EventRootResponse<T> : EventStreamResponseBase<JsonReader>
    {
        private readonly ValueRootAddedEventHandler<T> _added;
        private readonly string _path;

        private readonly ValueRemovedEventHandler _removed;

        private readonly IRequestManager _requestManager;

        private readonly ILog _log;

        internal EventRootResponse(HttpResponseMessage httpResponse, ValueRootAddedEventHandler<T> added,
            IRequestManager requestManager, string path, ILogManager logManager, ValueRemovedEventHandler removed = null)
        {
            if (added == null)
            {
                throw new ArgumentNullException(nameof(added));
            }
            if (requestManager == null)
            {
                throw new ArgumentNullException(nameof(requestManager));
            }
            if (logManager == null)
            {
                throw new ArgumentNullException(nameof(logManager));
            }
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            _added = added;
            _requestManager = requestManager;
            _path = path;
            _removed = removed;
            _log = logManager.GetLogger(this);

            CancellationTokenSource = new CancellationTokenSource();
            PollingTask = ReadLoop(httpResponse, CancellationTokenSource.Token);
        }

        protected override async Task ReadLoop(HttpResponseMessage httpResponse, CancellationToken token)
        {
            await Task.Factory.StartNew(async () =>
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

                        Debug.WriteLine(read);

                        if (read.StartsWith("event: "))
                        {
                            eventName = read.Substring(7);
                            _log.Debug($"Received event '{eventName}'");
                            continue;
                        }

                        if (read.StartsWith("data: "))
                        {
                            if (string.IsNullOrEmpty(eventName))
                            {
                                throw new InvalidOperationException(
                                    "Payload data was received but an event did not preceed it.");
                            }

                            var json = read.Substring("data: ".Length);

                            _log.Debug($"Received data {json}");

                            var data = JsonConvert.DeserializeObject<IDictionary<string, object>>(json);

                            if (data.ContainsKey("data") && data["data"] == null)
                            {
                                _removed?.Invoke(this, new ValueRemovedEventArgs(data["path"].ToString()), null);
                                continue;
                            }

                            // Every change on child, will get entire object again.
                            _log.Debug($"Getting {_path} to fetch updated object");
                            var request = await _requestManager.RequestAsync(HttpMethod.Get, _path);
                            var jsonStr = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
                            _log.Debug($"Fetched upcated object: \n{jsonStr}");

                            _added(this, jsonStr.ReadAs<T>());
                        }

                        // start over
                        eventName = null;
                    }
                }
            }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }
    }
}
