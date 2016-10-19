using System;
using System.Diagnostics.Contracts;

using FireSharp.EventStreaming;
using FireSharp.Logging;
using FireSharp.Security;

namespace FireSharp.Interfaces
{
    public interface IFirebaseConfig
    {
        string BasePath { get; set; }
        string Host { get; set; }
        string AuthSecret { get; set; }
        TimeSpan? RequestTimeout { get; set; }
        ISerializer Serializer { get; set; } 
        ILogManager LogManager { get; set; }
        IHttpClientProvider HttpClientProvider { get; set; }

        IEventStreamCacheProvider CacheProvider { get; set; }

        IRequestAuthenticator RequestAuthenticator { get; set; }
    }
}