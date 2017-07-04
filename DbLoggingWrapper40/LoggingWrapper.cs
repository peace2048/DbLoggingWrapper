using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace DbLoggingWrapper
{
    public class LoggingWrapper : ILoggingWriter
    {
        private static ILoggingWriter _writer;

        public static ILoggingWriter DefaultLogWriter
        {
            get
            {
                if (_writer == null)
                {
                    _writer = new LoggingWrapper();
                }
                return _writer;
            }
            set
            {
                _writer = value;
            }
        }

        private Common.Logging.ILog _logger = Common.Logging.LogManager.GetLogger(nameof(LoggingWrapper));

        public Common.Logging.ILog Logger { get => _logger; set => _logger = value; }


        public void BeginTransactionFailed(DbConnection connection, Exception ex)
        {
            _logger.Error($"Begin transaction failed.\n{connection.ConnectionString}", ex);
        }

        public void BeginTransactionSuccessful(DbConnection connection, DbTransaction transaction)
        {
            _logger.Trace($"Begin transaction. {connection.ConnectionString}");
        }

        public void ChangeDatabaseFailed(TimeSpan elapsed, DbConnection connection, string databaseName, Exception ex)
        {
            _logger.Error($"Change database failed. database name is {databaseName}.", ex);
        }

        public void ChangeDatabaseSuccessful(TimeSpan elapsed, DbConnection connection, string databaseName)
        {
            _logger.Trace($"Databse changed. new database is {databaseName}. {Dump(elapsed)} elapsed.");
        }

        public void CloseFailed(DbConnection connection, Exception ex)
        {
            _logger.Error($"Close connection failed. {connection.ConnectionString}", ex);
        }

        public void CloseSuccessful(DbConnection connection)
        {
            _logger.Trace($"Connection closed. {connection.ConnectionString}");
        }

        public void CommitFailed(TimeSpan elapsed, DbTransaction transaction, Exception ex)
        {
            _logger.Error($"Commit transaction failed.", ex);
        }

        public void CommitSuccessful(TimeSpan elapsed, DbTransaction transaction)
        {
            _logger.Trace($"Transaction commited. {Dump(elapsed)} elapsed.");
        }

        public void ExecuteNonQueryFailed(TimeSpan elapsed, DbCommand command, Exception ex)
        {
            _logger.Error($"Execute non query faild. {Dump(elapsed)} elapsed.{Dump(command)}", ex);
        }

        public void ExecuteNonQuerySuccessful(TimeSpan elapsed, DbCommand command, int result)
        {
            _logger.Trace($"Executed non query. {result} records processed. {Dump(elapsed)} elapsed.{Dump(command)}");
        }

        public void ExecuteReaderFailed(TimeSpan elapsed, DbCommand command, Exception ex)
        {
            _logger.Error($"Execute reader failed. {Dump(elapsed)} elapsed.{Dump(command)}", ex);
        }

        public void ExecuteReaderSuccessful(TimeSpan elapsed, DbCommand command)
        {
            _logger.Trace($"Executed reader. {Dump(elapsed)} elapsed.{Dump(command)}");
        }

        public void ExecuteScalarFailed(TimeSpan elapsed, DbCommand command, Exception ex)
        {
            _logger.Error($"Execute scalar failed. {Dump(elapsed)} elapsed.{Dump(command)}", ex);
        }

        public void ExecuteScalarSuccessful(TimeSpan elapsed, DbCommand command, object result)
        {
            _logger.Trace($"Executed scalar. result={result}, {Dump(elapsed)} elapsed.{Dump(command)}");
        }

        public void OpenFailed(TimeSpan elapsed, DbConnection connection, Exception ex)
        {
            _logger.Error($"Open connection failed. {Dump(elapsed)} elapsed. {connection.ConnectionString}", ex);
        }

        public void OpenSuccessful(TimeSpan elapsed, DbConnection connection)
        {
            _logger.Trace($"Open connection. {Dump(elapsed)} elapsed.");
        }

        public void RollbackFailed(TimeSpan elapsed, DbTransaction transaction, Exception ex)
        {
            _logger.Error($"Rollback transaction failed.", ex);
        }

        public void RollbackSuccessful(TimeSpan elapsed, DbTransaction transaction)
        {
            _logger.Warn($"Rollback transaction successed.");
        }

        public string Dump(TimeSpan elapsed)
        {
            if (elapsed.TotalSeconds < 1)
            {
                return $"{elapsed.TotalMilliseconds} milliseconds";
            }
            return $"{elapsed.TotalSeconds} seconds";
        }

        public string Dump(DbCommand command)
        {
            return $"\n{command.CommandText}{Dump(command.Parameters)}";
        }

        public string Dump(DbParameterCollection parameters)
        {
            if (parameters.Count == 0)
            {
                return null;
            }
            return "\n--- parameters ---\n" + string.Join("\n", parameters.Cast<DbParameter>().Select(_ => $"{_.ParameterName} = {_.Value} [{_.DbType}]").ToArray());
        }
    }
}