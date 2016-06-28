using System;

using FireSharp.EventStreaming;

namespace FireSharp.Interfaces
{
    public interface IFirebaseConfig
    {
        string BasePath { get; set; }
        string Host { get; set; }
        string AuthSecret { get; set; }
        TimeSpan? RequestTimeout { get; set; }
        ISerializer Serializer { get; set; } 
    }
}