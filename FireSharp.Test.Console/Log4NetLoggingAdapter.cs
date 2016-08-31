using System;

using FireSharp.Logging;

namespace FireSharp.Test.Console
{
    public class Log4NetLoggingAdapter : ILog
    {
        private readonly log4net.ILog _logger;

        public Log4NetLoggingAdapter(log4net.ILog logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
            _logger = logger;
        }

        public bool IsDebugEnabled => _logger.IsDebugEnabled;

        public void Debug(string message)
        {
            _logger.Debug(message);
        }

        public void Info(string message)
        {
            _logger.Info(message);
        }

        public void Warn(string message)
        {
            _logger.Warn(message);
        }

        public void Warn(string message, Exception ex)
        {
            _logger.Warn(message, ex);
        }

        public void Error(string message)
        {
            _logger.Error(message);
        }

        public void Error(string message, Exception ex)
        {
            _logger.Error(message, ex);
        }
    }
}