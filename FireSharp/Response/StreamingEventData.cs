namespace FireSharp.Response
{
    internal class StreamingEventData<T>
    {
        public string Path { get; set; }

        public T Data { get; set; }
    }
}