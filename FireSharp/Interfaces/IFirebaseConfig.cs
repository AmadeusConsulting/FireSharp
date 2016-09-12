using System;
using System.Diagnostics.Contracts;

using FireSharp.EventStreaming;
using FireSharp.Logging;

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
    }
}