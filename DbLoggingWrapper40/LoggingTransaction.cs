using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;

namespace DbLoggingWrapper
{
    public class LoggingTransaction : DbTransaction
    {
        private readonly DbTransaction _transaction;
        private readonly LoggingConnection _connection;
        private ILoggingWriter _logger;

        public LoggingTransaction(DbTransaction transaction, LoggingConnection connection, ILoggingWriter logger)
        {
            _transaction = RealTransaction(transaction) ?? throw new ArgumentNullException(nameof(transaction));
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _logger = logger;
        }

        private static DbTransaction RealTransaction(DbTransaction transaction) => transaction is LoggingTransaction logging ? logging.BaseTransaction : transaction;

        public DbTransaction BaseTransaction => _transaction;

        protected override DbConnection DbConnection => _connection;

        public override IsolationLevel IsolationLevel => _transaction.IsolationLevel;

        public override void Commit()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                _transaction.Commit();
                try { _logger.CommitSuccessful(sw.Elapsed, _transaction); } catch { }
            }
            catch (Exception ex)
            {
                try { _logger.CommitFailed(sw.Elapsed, _transaction, ex); } catch { }
                throw;
            }
        }

        public override void Rollback()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                _transaction.Rollback();
                try { _logger.RollbackSuccessful(sw.Elapsed, _transaction); } catch { }
            }
            catch (Exception ex)
            {
                try { _logger.RollbackFailed(sw.Elapsed, _transaction, ex); } catch { }
                throw;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _transaction.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}