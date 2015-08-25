using System;
using System.Data;
using System.Diagnostics;

namespace DbLoggingWrapper
{
    public class LoggingTransaction : IDbTransaction
    {
        public LoggingTransaction(IDbTransaction transaction)
        {
            BaseTransaction = transaction;
            LoggingWrapper.Trace("トランザクションを開始しました。");
        }

        public IDbTransaction BaseTransaction { get; private set; }

        public IDbConnection Connection
        {
            get { return BaseTransaction.Connection; }
        }

        public IsolationLevel IsolationLevel
        {
            get { return BaseTransaction.IsolationLevel; }
        }

        private bool _isExecutedCommitOrRollback = false;

        public void Commit()
        {
            try
            {
                var sw = Stopwatch.StartNew();
                BaseTransaction.Commit();
                LoggingWrapper.Trace("トランザクションをコミットしました。", sw);
                _isExecutedCommitOrRollback = true;
            }
            catch (Exception ex)
            {
                LoggingWrapper.Error(this, "コミットに失敗", ex);
                throw;
            }
        }

        public void Dispose()
        {
            BaseTransaction.Dispose();
            if (_isExecutedCommitOrRollback)
            {
                LoggingWrapper.Trace("トランザクションが破棄(Dispose)されました。");
            }
            else
            {
                LoggingWrapper.Logger.Error("コミットもロールバックも実行されずに、トランザクションが破棄(Dispose)されました。", null);
            }
        }

        public void Rollback()
        {
            try
            {
                var sw = Stopwatch.StartNew();
                BaseTransaction.Rollback();
                LoggingWrapper.Trace("トランザクションをロールバックしました。", sw);
                _isExecutedCommitOrRollback = true;
            }
            catch (Exception ex)
            {
                LoggingWrapper.Error(this, "ロールバックに失敗", ex);
                throw;
            }
        }

        public override string ToString()
        {
            return LoggingWrapper.Dump(BaseTransaction);
        }
    }
}