using System;

namespace FireSharp.Response
{
    public interface IEventStreamResponse
    {
        event EventHandler<EventStreamingTerminatedEventArgs> StreamingTerminated;

        string Path { get; }

        bool IsStreaming { get; }

        void Cancel();
    }
}