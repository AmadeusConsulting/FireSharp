using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using FireSharp.EventStreaming;
using FireSharp.Logging;

using Newtonsoft.Json;

namespace FireSharp.Response
{
    public abstract class EventStreamResponseBase<T> : IDisposable
    {
        #region Constants

        private const string DataPrefix = "data: ";

        private const string EventPrefix = "event: ";

        #endregion

        #region Static Fields

        private static readonly Regex DataRegex = new Regex(@"\{\s* <?# Start JSON Object>
                ""path"":\s*""(?<Path>[^""]+)""\s*,\s* <?# Path Property with string value >
                ""data"":\s*(?<Data>(\{.*\}|""(([^""]|(?<=\\)"")+)?""|\d+(\.\d+)?|false|true|null))\s* <?# data property with object, number, string, boolean, or null value >
              \} <?# End JSON Object >", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);

        #endregion

        #region Fields

        private readonly ILog _log;

        #endregion

        #region Constructors and Destructors

        protected EventStreamResponseBase(
            string path, 
            HttpResponseMessage httpResponseMessage, 
            CancellationTokenSource cancellationTokenSource, 
            ILogManager logManager, 
            IEventStreamResponseCache<T> cache)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (httpResponseMessage == null)
            {
                throw new ArgumentNullException(nameof(httpResponseMessage));
            }

            if (logManager == null)
            {
                throw new ArgumentNullException(nameof(logManager));
            }

            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            Cache = cache;
            Path = path;
            CancellationTokenSource = cancellationTokenSource;
            LogManager = logManager;
            _log = LogManager.GetLogger(this);

            PollingTask = ReadLoop(httpResponseMessage, CancellationTokenSource.Token);
        }

        ~EventStreamResponseBase()
        {
            Dispose(false);
        }

        #endregion

        #region Public Events

        public event EventHandler<EventStreamingTerminatedEventArgs> StreamingTerminated;

        #endregion

        #region Properties

        protected IEventStreamResponseCache<T> Cache { get; }

        protected CancellationTokenSource CancellationTokenSource { get; }

        protected ILogManager LogManager { get; }

        protected string Path { get; }

        private Task PollingTask { get; set; }

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

        protected abstract Task HandleReadLoopDataAsync(string eventName, string path, string dataJson);

        protected async Task ReadLoop(HttpResponseMessage httpResponse, CancellationToken token)
        {
            _log.Debug($"Starting read loop for Entity Event Streaming for path {Path}");

            try
            {
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
                                        _log.Error("Cancel Event received from server. Exiting Read Loop!");
                                        OnEventStreamingTerminated(EventStreamingTerminationCause.ServerCancel);
                                        break;
                                    }

                                    if (eventName == StreamingEventType.AuthRevoked)
                                    {
                                        // our auth token is no longer valid
                                        _log.Error("Firebase Auth Revoked!  Exiting Read Loop!");
                                        OnEventStreamingTerminated(EventStreamingTerminationCause.AuthorizationRevoked);
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
                                                await HandleReadLoopDataAsync(eventName, path, dataJson);
                                            }
                                            else
                                            {
                                                _log.Error($"Bad Data Format! \n {read.Substring(DataPrefix.Length)}");
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _log.Error($"Unhandled Exception Deserializing Data in Read Loop: {ex.Message}", ex);
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
            catch (Exception ex)
            {
                var isCanceled = ex is OperationCanceledException;

                OnEventStreamingTerminated(
                    isCanceled ? EventStreamingTerminationCause.StreamingCanceled : EventStreamingTerminationCause.UnhandledException, 
                    ex);

                throw;
            }
        }

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

        private void OnEventStreamingTerminated(EventStreamingTerminationCause terminationCause, Exception exception = null)
        {
            StreamingTerminated?.Invoke(this, new EventStreamingTerminatedEventArgs(terminationCause, exception));
        }

        #endregion
    }
}