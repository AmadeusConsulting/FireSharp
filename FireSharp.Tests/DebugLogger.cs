using System;

using FireSharp.Logging;

using DebugLog= System.Diagnostics.Debug;

namespace FireSharp.Tests
{
    public class DebugLogger : ILog
    {
        private readonly string _loggerName;

        public DebugLogger(string loggerName)
        {
            if (loggerName == null)
            {
                throw new ArgumentNullException(nameof(loggerName));
            }

            _loggerName = loggerName;
        }

        public bool IsDebugEnabled { get; }

        public void Debug(string message)
        {
            DebugLog.WriteLine($"{DateTime.Now.ToString("u")} [{_loggerName}] DEBUG {message}");
        }

        public void Info(string message)
        {
            DebugLog.WriteLine($"{DateTime.Now.ToString("u")} [{_loggerName}] INFO {message}");
        }

        public void Warn(string message)
        {
            DebugLog.WriteLine($"{DateTime.Now.ToString("u")} [{_loggerName}] WARN {message}");
        }

        public void Warn(string message, Exception ex)
        {
            if (ex == null)
            {
                Warn(message);
                return;
            }

            DebugLog.WriteLine($"{DateTime.Now.ToString("u")} [{_loggerName}] WARN {message} \n{ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}");
        }

        public void Error(string message)
        {
            DebugLog.WriteLine($"{DateTime.Now.ToString("u")} [{_loggerName}] ERROR {message}");
        }

        public void Error(string message, Exception ex)
        {
            if (ex == null)
            {
                Error(message);
                return;
            }

            DebugLog.WriteLine($"{DateTime.Now.ToString("u")} [{_loggerName}] ERROR {message} \n{ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}");
        }
    }
}