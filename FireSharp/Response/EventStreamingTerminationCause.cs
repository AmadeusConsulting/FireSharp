namespace FireSharp.Response
{
    public enum EventStreamingTerminationCause
    {
        Unknown = 0,
        StreamingCanceled = 1,
        AuthorizationRevoked = 2,
        ServerCancel = 3,
        UnhandledException = 4
    }
}