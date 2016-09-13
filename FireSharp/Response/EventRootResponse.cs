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

        private readonly ValueRemovedEventHandler _removed;

        private readonly IRequestManager _requestManager;

        internal EventRootResponse(
            HttpResponseMessage httpResponse,
            ValueRootAddedEventHandler<T> added,
            IRequestManager requestManager,
            string path,
            ILogManager logManager,
            CancellationTokenSource cancellationTokenSource,
            ValueRemovedEventHandler removed = null)
            : base(path, httpResponse, cancellationTokenSource, logManager, new TemporaryCache())
        {
            if (added == null)
            {
                throw new ArgumentNullException(nameof(added));
            }
            if (requestManager == null)
            {
                throw new ArgumentNullException(nameof(requestManager));
            }
          
            _added = added;
            _requestManager = requestManager;
            _removed = removed;
        }

        protected override async Task HandleReadLoopDataAsync(string eventName, string path, string dataJson)
        {
            var data = JsonConvert.DeserializeObject(dataJson);

            if (data == null)
            {
                _removed?.Invoke(this, new ValueRemovedEventArgs(path), null);
            }
            else
            {
                // Every change on child, will get entire object again.
                Log.Debug($"Getting {Path} to fetch updated object");
                var request = await _requestManager.RequestAsync(HttpMethod.Get, Path);
                var jsonStr = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
                Log.Debug($"Fetched upcated object: \n{jsonStr}");

                _added(this, jsonStr.ReadAs<T>()); 
            }
        }
    }
}
