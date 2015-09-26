using System;
using System.Data;
using System.Diagnostics;
using System.Text;

namespace DbLoggingWrapper
{
    public static class LoggingWrapper
    {
        private static Common.Logging.ILog _logger;

        public static Common.Logging.ILog Logger
        {
            get
            {
                if (_logger == null)
                {
                    _logger = Common.Logging.LogManager.GetLogger("DbWrapper");
                }
                return _logger;
            }
            set
            {
                _logger = value;
            }
        }

        public static TCommand TryCast<TCommand>(this IDbCommand command) where TCommand : IDbCommand
        {
            try
            {
                var logging = command as LoggingCommand;
                if (logging != null)
                {
                    return (TCommand)logging.BaseCommand;
                }
                return (TCommand)command;
            }
            catch (InvalidCastException)
            {
                return default(TCommand);
            }
        }

        internal static void Dump(IDbConnection connection, StringBuilder sb)
        {
            sb.Append("--- ").Append(connection.GetType().Name).AppendLine(" ---");
            sb.Append("ConnectionString  = ").Append(connection.ConnectionString).AppendLine();
            sb.Append("ConnectionTimeout = ").Append(connection.ConnectionTimeout).AppendLine();
            sb.Append("Database = ").Append(connection.Database).AppendLine();
            sb.Append("State = ").Append(connection.State).AppendLine();
        }

        internal static string Dump(IDbConnection connection)
        {
            var sb = new StringBuilder();
            Dump(connection, sb);
            return sb.ToString();
        }

        internal static void Dump(IDbTransaction transaction, StringBuilder sb)
        {
            if (transaction.Connection != null)
            {
                Dump(transaction.Connection, sb);
            }
            sb.Append("--- ").Append(transaction.GetType().Name).AppendLine(" ---");
            sb.Append("IsolationLevel = ").Append(transaction.IsolationLevel).AppendLine();
        }

        internal static string Dump(IDbTransaction transaction)
        {
            var sb = new StringBuilder();
            Dump(transaction, sb);
            return sb.ToString();
        }

        internal static void Dump(IDbCommand command, StringBuilder sb)
        {
            if (command.Transaction != null)
            {
                Dump(command.Transaction, sb);
            }
            else if (command.Connection != null)
            {
                Dump(command.Connection, sb);
            }
            sb.Append("--- ").Append(command.GetType().Name).AppendLine(" ---");
            sb.Append("CommandTimeout   = ").Append(command.CommandTimeout).AppendLine();
            sb.Append("CommandType      = ").Append(command.CommandType).AppendLine();
            sb.Append("UpdatedRowSource = ").Append(command.UpdatedRowSource).AppendLine();
            sb.AppendLine("--- CommandText ---");
            sb.Append(command.CommandText).AppendLine();
            sb.AppendLine("--- Parameters ---");
            foreach (IDbDataParameter item in command.Parameters)
            {
                sb.Append(item.ParameterName).Append("(").Append(item.DbType).Append(")=").Append(item.Value).AppendLine();
            }
        }

        internal static string Dump(IDbCommand command)
        {
            var sb = new StringBuilder();
            Dump(command, sb);
            return sb.ToString();
        }

        internal static void Error(object wrapper, string message, Exception ex)
        {
            if (Logger.IsErrorEnabled)
            {
                var sb = new StringBuilder();
                sb.AppendLine(message);
                sb.Append(wrapper.ToString());
                Logger.Error(sb.ToString(), ex);
            }
        }

        internal static void Error(object wrapper, Func<string> message, Exception ex)
        {
            if (Logger.IsErrorEnabled)
            {
                Error(wrapper, message(), ex);
            }
        }

        internal static void Trace(string message)
        {
            Logger.Trace(message);
        }

        internal static void Trace(string message, Stopwatch stopwatch)
        {
            Logger.Trace(f => f("{0} 実行時間:{1}", message, stopwatch.Elapsed));
        }

        internal static void Trace(Func<string> message, Stopwatch stopwatch)
        {
            if (Logger.IsTraceEnabled)
            {
                Trace(message(), stopwatch);
            }
        }

        internal static void Trace(string message, IDbCommand command, Stopwatch stopwatch)
        {
            if (Logger.IsTraceEnabled)
            {
                var sb = new StringBuilder(message);
                sb.Append(" 実行時間:").Append(stopwatch.Elapsed).AppendLine();
                sb.AppendLine("--- CommandText ---");
                sb.AppendLine(command.CommandText);
                sb.AppendLine("--- Parameters ---");
                foreach (IDbDataParameter item in command.Parameters)
                {
                    sb.Append(item.ParameterName).Append("(").Append(item.DbType).Append(")=").Append(item.Value).AppendLine();
                }
                Logger.Trace(sb.ToString());
            }
        }

        internal static void Trace(Func<string> message, IDbCommand command, Stopwatch stopwatch)
        {
            if (Logger.IsTraceEnabled)
            {
                Trace(message(), command, stopwatch);
            }
        }
    }
}