namespace FireSharp.Logging
{
    public class NoOpLogManager : ILogManager
    {
        public ILog GetLogger<T>()
        {
            return new NoOpLogger();
        }

        public ILog GetLogger<T>(T clazz) where T : class
        {
            return new NoOpLogger();
        }

        public ILog GetLogger(string name)
        {
            return new NoOpLogger();
        }
    }
}