using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;
using Common.Logging.Simple;

namespace Tests.DbLoggingWrapper
{
    class TestLogger : AbstractSimpleLogger
    {
        public TestLogger()
            : base("test", LogLevel.All, true, false, false, "yyyy-MM-dd")
        {
            Items = new List<LogItem>();
        }

        public List<LogItem> Items { get; private set; }

        public LogItem Last { get; private set; }

        protected override void WriteInternal(LogLevel level, object message, Exception exception)
        {
            Last = new LogItem { Exception = exception, Level = level, Message = message.ToString() };
            Items.Add(Last);
        }

        public class LogItem
        {
            public LogLevel Level { get; set; }
            public string Message { get; set; }
            public Exception Exception { get; set; }
        }
    }
}
