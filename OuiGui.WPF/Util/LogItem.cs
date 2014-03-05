using NLog;
using System;

namespace OuiGui.WPF.Util
{
    public class LogItem
    {
        public string Message { get; set; }

        public DateTime Timestamp { get; set; }

        public LogLevel Severity { get; set; }

        public string Logger { get; set; }
    }
}
