using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DbLoggingWrapper
{
    public partial class LoggingCommand : ICloneable
    {
        public object Clone()
        {
            var tail = _command as ICloneable;
            if (tail == null) throw new NotSupportedException($"Underlying {_command.GetType().Name} is not cloneable");
            return new LoggingCommand((System.Data.Common.DbCommand)tail.Clone(), _connection, _logger);
        }
    }
}
