using System;
using System.Data;
using System.Diagnostics;

namespace DbLoggingWrapper
{
    public class LoggingConnection : IDbConnection
    {
        public LoggingConnection(IDbConnection connection)
        {
            BaseConnection = connection;
        }

        public IDbConnection BaseConnection { get; private set; }

        public string ConnectionString
        {
            get { return BaseConnection.ConnectionString; }
            set { BaseConnection.ConnectionString = value; }
        }

        public int ConnectionTimeout
        {
            get { return BaseConnection.ConnectionTimeout; }
        }

        public string Database
        {
            get { return BaseConnection.Database; }
        }

        public ConnectionState State
        {
            get { return BaseConnection.State; }
        }

        public IDbTransaction BeginTransaction(IsolationLevel il)
        {
            try
            {
                var trans = BaseConnection.BeginTransaction(il);
                return new LoggingTransaction(trans);
            }
            catch (Exception ex)
            {
                LoggingWrapper.Error(this, "トランザクションの開始に失敗", ex);
                throw;
            }
        }

        public IDbTransaction BeginTransaction()
        {
            try
            {
                var trans = BaseConnection.BeginTransaction();
                return trans;
            }
            catch (Exception ex)
            {
                LoggingWrapper.Error(this, "トランザクションの開始に失敗", ex);
                throw;
            }
        }

        public void ChangeDatabase(string databaseName)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                BaseConnection.ChangeDatabase(databaseName);
                LoggingWrapper.Trace(() => string.Format("ベータベース[{0}]に切り替えました。", databaseName), sw);
            }
            catch (Exception ex)
            {
                LoggingWrapper.Error(this, () => string.Format("データベース[{0}]への切り替えに失敗", databaseName), ex);
                throw;
            }
        }

        public void Close()
        {
            try
            {
                BaseConnection.Close();
                LoggingWrapper.Trace("コネクションを閉じました。");
            }
            catch (Exception ex)
            {
                LoggingWrapper.Error(this, "Close に失敗", ex);
                throw;
            }
        }

        public IDbCommand CreateCommand()
        {
            return new LoggingCommand(BaseConnection.CreateCommand());
        }

        public void Dispose()
        {
            BaseConnection.Dispose();
            LoggingWrapper.Trace("コネクションが破棄(Dispose)されました。");
        }

        public void Open()
        {
            try
            {
                var sw = Stopwatch.StartNew();
                BaseConnection.Open();
                LoggingWrapper.Trace("コネクションを接続しました。", sw);
            }
            catch (Exception ex)
            {
                LoggingWrapper.Error(this, "接続に失敗", ex);
                throw;
            }
        }

        public override string ToString()
        {
            return LoggingWrapper.Dump(this);
        }
    }
}