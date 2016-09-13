using System;

namespace FireSharp.Response
{
    public class EventStreamingTerminatedEventArgs : EventArgs
    {
        public EventStreamingTerminationCause TerminationCause { get; }

        public Exception Exception { get; }

        public EventStreamingTerminatedEventArgs(EventStreamingTerminationCause terminationCause, Exception exception = null)
        {
            TerminationCause = terminationCause;
            Exception = exception;
        }
    }
}