using System;

using FireSharp.Logging;

using log4net;

using ILog = FireSharp.Logging.ILog;

namespace FireSharp.Tests.Logging
{
    public class Log4NetLogManager : ILogManager
    {
        public ILog GetLogger<T>()
        {
            log4net.ILog logger = LogManager.GetLogger(typeof(T));
            
            return new Log4NetLoggingAdapter(logger);
        }

        public ILog GetLogger<T>(T clazz) where T : class
        {
            return GetLogger<T>();
        }

        public ILog GetLogger(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            var logger = LogManager.GetLogger(name);

            return new Log4NetLoggingAdapter(logger);
        }
    }
}
