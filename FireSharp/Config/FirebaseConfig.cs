using FireSharp.Interfaces;
using System;

using FireSharp.Logging;

namespace FireSharp.Config
{
    public class FirebaseConfig : IFirebaseConfig
    {
        private string _basePath;

        private ILogManager _logManager;

        private IHttpClientProvider _httpClientProvider;

        private IEventStreamCacheProvider _cacheProvider;

        public FirebaseConfig()
        {
            Serializer = new JsonNetSerializer();
            _basePath = string.Empty;
        }

        public string BasePath
        {
            get
            {
                return _basePath.EndsWith("/") ? _basePath : $"{_basePath}/";
            }
            set { _basePath = value; }
        }

        public string Host { get; set; }
        public string AuthSecret { get; set; }

        public TimeSpan? RequestTimeout { get; set; }

        public ISerializer Serializer { get; set; }

        public ILogManager LogManager
        {
            get
            {
                return _logManager ?? (_logManager = new NoOpLogManager());
            }
            set
            {
                _logManager = value;
            }
        }

        public IHttpClientProvider HttpClientProvider
        {
            get
            {
                return _httpClientProvider ?? (_httpClientProvider = new DefaulFireSharpHttpClientProvider());
            }
            set
            {
                _httpClientProvider = value;
            }
        }

        public IEventStreamCacheProvider CacheProvider
        {
            get
            {
                return _cacheProvider ?? (_cacheProvider = new InMemoryEventStreamCacheProvider());
            }
            set
            {
                _cacheProvider = value;
            }
        }
    }
}
