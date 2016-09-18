using FireSharp.Logging;

namespace FireSharp.Tests
{
    public class DebugLogManager : ILogManager
    {
        public ILog GetLogger<T>()
        {
            return new DebugLogger(typeof(T).FullName);
        }

        public ILog GetLogger<T>(T clazz) where T : class
        {
            return GetLogger<T>();
        }

        public ILog GetLogger(string name)
        {
            return new DebugLogger(name);
        }
    }
}