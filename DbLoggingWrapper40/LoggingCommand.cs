using System;
using System.Data;
using System.Diagnostics;

namespace DbLoggingWrapper
{
    public class LoggingCommand : IDbCommand
    {
        public LoggingCommand(IDbCommand command)
        {
            BaseCommand = command;
        }

        public IDbCommand BaseCommand { get; private set; }

        public string CommandText
        {
            get { return BaseCommand.CommandText; }
            set { BaseCommand.CommandText = value; }
        }

        public int CommandTimeout
        {
            get { return BaseCommand.CommandTimeout; }
            set { BaseCommand.CommandTimeout = value; }
        }

        public CommandType CommandType
        {
            get { return BaseCommand.CommandType; }
            set { BaseCommand.CommandType = value; }
        }

        public IDbConnection Connection
        {
            get
            {
                return BaseCommand.Connection;
            }
            set
            {
                var logging = value as LoggingConnection;
                BaseCommand.Connection = logging == null ? value : logging.BaseConnection;
            }
        }

        public IDataParameterCollection Parameters
        {
            get { return BaseCommand.Parameters; }
        }

        public IDbTransaction Transaction
        {
            get
            {
                return BaseCommand.Transaction;
            }
            set
            {
                var logging = value as LoggingTransaction;
                BaseCommand.Transaction = logging == null ? value : logging.BaseTransaction;
            }
        }

        public UpdateRowSource UpdatedRowSource
        {
            get { return BaseCommand.UpdatedRowSource; }
            set { BaseCommand.UpdatedRowSource = value; }
        }

        public void Cancel()
        {
            try
            {
                var sw = Stopwatch.StartNew();
                BaseCommand.Cancel();
                LoggingWrapper.Trace("コマンドをキャンセルしました。", sw);
            }
            catch (Exception ex)
            {
                LoggingWrapper.Error(this, "キャンセルに失敗", ex);
                throw;
            }
        }

        public IDbDataParameter CreateParameter()
        {
            return BaseCommand.CreateParameter();
        }

        public void Dispose()
        {
            BaseCommand.Dispose();
            LoggingWrapper.Trace("コマンドが破棄(Dispose)されました。");
        }

        public int ExecuteNonQuery()
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var result = BaseCommand.ExecuteNonQuery();
                LoggingWrapper.Trace(() => string.Format("ExecuteNonQuery. result={0}", result), BaseCommand, sw);
                return result;
            }
            catch (Exception ex)
            {
                LoggingWrapper.Error(this, "クエリ実行に失敗", ex);
                throw;
            }
        }

        public IDataReader ExecuteReader(CommandBehavior behavior)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var result = BaseCommand.ExecuteReader(behavior);
                LoggingWrapper.Trace(() => string.Format("ExecuteReader({0}).", behavior), BaseCommand, sw);
                return result;
            }
            catch (Exception ex)
            {
                LoggingWrapper.Error(this, "クエリ実行に失敗", ex);
                throw;
            }
        }

        public IDataReader ExecuteReader()
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var result = BaseCommand.ExecuteReader();
                LoggingWrapper.Trace("ExecuteReader.", BaseCommand, sw);
                return result;
            }
            catch (Exception ex)
            {
                LoggingWrapper.Error(this, "クエリ実行に失敗", ex);
                throw;
            }
        }

        public object ExecuteScalar()
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var result = BaseCommand.ExecuteScalar();
                LoggingWrapper.Trace(() => string.Format("ExecuteScalar. result={0}", result), BaseCommand, sw);
                return result;
            }
            catch (Exception ex)
            {
                LoggingWrapper.Error(this, "クエリ実行に失敗", ex);
                throw;
            }
        }

        public void Prepare()
        {
            try
            {
                var sw = Stopwatch.StartNew();
                BaseCommand.Prepare();
                LoggingWrapper.Trace("コマンドをコンパイル(Prepare)しました。", sw);
            }
            catch (Exception ex)
            {
                LoggingWrapper.Error(this, "コンパイル(Prepare)に失敗", ex);
                throw;
            }
        }

        public override string ToString()
        {
            return LoggingWrapper.Dump(this);
        }
    }
}