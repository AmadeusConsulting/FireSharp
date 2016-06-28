namespace FireSharp.Response
{
    public static class StreamingEventType
    {
        public const string Put = "put";

        public const string Patch = "patch";

        public const string KeepAlive = "keep-alive";

        public const string Cancel = "cancel";

        public const string AuthRevoked = "auth_revoked";
    }
}