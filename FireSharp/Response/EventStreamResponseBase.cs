using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using FireSharp.EventStreaming;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FireSharp.Response
{
    public abstract class EventStreamResponseBase<T> : IDisposable
    {
        protected static readonly Regex DataRegex = new Regex(
            @"\{\s* <?# Start JSON Object>
                ""path"":\s*""(?<Path>[^""]+)""\s*,\s* <?# Path Property with string value >
                ""data"":\s*(?<Data>(\{.*\}|""(([^""]|(?<=\\)"")+)?""|\d+(\.\d+)?|false|true|null))\s* <?# data property with object, number, string, boolean, or null value >
              \} <?# End JSON Object >", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);

        protected const string EventPrefix = "event: ";

        protected const string DataPrefix = "data: ";

        protected const string EventKeepAlive = "keep-alive";

        #region Constructors and Destructors

        ~EventStreamResponseBase()
        {
            Dispose(false);
        }

        #endregion

        #region Properties

        protected IEventStreamResponseCache<T> Cache { get; set; }

        protected CancellationTokenSource CancellationTokenSource { get; set; }

        protected Task PollingTask { get; set; }

        #endregion

        #region Public Methods and Operators

        public void Cancel()
        {
            CancellationTokenSource.Cancel();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Methods

        protected virtual void Dispose(bool disposing)
        {
            Cancel();

            if (disposing)
            {
                Cache.Dispose();
                CancellationTokenSource.Dispose();
            }
        }

        protected abstract Task ReadLoop(HttpResponseMessage httpResponse, CancellationToken token);

        protected JsonReader ReadToNamedPropertyValue(JsonReader reader, string property)
        {
            ReadToNextProperty(reader);
            
            var prop = reader.Value.ToString();
            if (property != prop)
            {
                throw new InvalidOperationException("Error parsing response.  Expected json property named: " + property);
            }

            return reader;
        }


        protected JsonReader ReadToNextProperty(JsonReader reader)
        {
            while (reader.Read() && reader.TokenType != JsonToken.PropertyName)
            {
                // skip the property
            }

            return reader;
        }
        #endregion
    }
}