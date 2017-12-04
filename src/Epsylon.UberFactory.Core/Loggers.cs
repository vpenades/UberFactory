using System;
using System.Collections.Generic;
using System.Text;

namespace Epsylon.UberFactory
{
    public class BasicLogger : SDK.ILogger
    {
        private readonly List<string> _Lines = new List<string>();

        public void LogCritical(string categoryName, string message)
        {
            _Lines.Add(message);
        }

        public void LogDebug(string categoryName, string message)
        {
            _Lines.Add(message);
        }

        public void LogError(string categoryName, string message)
        {
            _Lines.Add(message);
        }

        public void LogInfo(string categoryName, string message)
        {
            _Lines.Add(message);
        }

        public void LogTrace(string categoryName, string message)
        {
            _Lines.Add(message);
        }

        public void LogWarning(string categoryName, string message)
        {
            _Lines.Add(message);
        }
    }
}
