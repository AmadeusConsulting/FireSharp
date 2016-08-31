using System;

namespace FireSharp.Logging
{
    public class NoOpLogger : ILog
    {
        public bool IsDebugEnabled { get; } = false;

        public void Debug(string message)
        {
        }

        public void Info(string message)
        {
        }

        public void Warn(string message)
        {
        }

        public void Warn(string message, Exception ex)
        {
        }

        public void Error(string message)
        {
        }

        public void Error(string message, Exception ex)
        {
        }
    }
}