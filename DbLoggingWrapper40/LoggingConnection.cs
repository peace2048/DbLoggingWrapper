using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Threading;
#if NET40 || NET46
using System.Threading.Tasks;
#endif

namespace DbLoggingWrapper
{
    public class LoggingConnection : DbConnection
    {
        private readonly DbConnection _connection;
        private readonly ILoggingWriter _logger;

        public LoggingConnection(DbConnection connection, ILoggingWriter logger)
        {
            _connection = RealConnection(connection) ?? throw new ArgumentNullException(nameof(connection));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connection.StateChange += Connection_StateChange;
        }

        private void Connection_StateChange(object sender, StateChangeEventArgs e)
        {
            OnStateChange(e);
        }

        private static DbConnection RealConnection(DbConnection connection) => connection is LoggingConnection wrapped ? wrapped.BaseConnection : connection;

        public DbConnection BaseConnection => _connection;

        public override string ConnectionString { get => _connection.ConnectionString; set => _connection.ConnectionString = value; }

        public override int ConnectionTimeout => _connection.ConnectionTimeout;

        public override string Database => _connection.Database;

        public override string DataSource => _connection.DataSource;

        public override string ServerVersion => _connection.ServerVersion;

        public override ConnectionState State => _connection.State;

        public override void ChangeDatabase(string databaseName)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                _connection.ChangeDatabase(databaseName);
                try { _logger.ChangeDatabaseSuccessful(sw.Elapsed, _connection, databaseName); } catch { }
            }
            catch (Exception ex)
            {
                try { _logger.ChangeDatabaseFailed(sw.Elapsed, _connection, databaseName, ex); } catch { }
                throw;
            }
        }

        public override void Close()
        {
            try
            {
                _connection.Close();
                try { _logger.CloseSuccessful(_connection); } catch { }
            }
            catch (Exception ex)
            {
                try { _logger.CloseFailed(_connection, ex); } catch { }
                throw;
            }
        }

        public override void Open()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                _connection.Open();
                try { _logger.OpenSuccessful(sw.Elapsed, _connection); } catch { }
            }
            catch (Exception ex)
            {
                try { _logger.OpenFailed(sw.Elapsed, _connection, ex); } catch { }
                throw;
            }
        }

#if NET46
        public override async Task OpenAsync(CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                await _connection.OpenAsync(cancellationToken);
                try { _logger.OpenSuccessful(sw.Elapsed, _connection); } catch { }
            }
            catch (Exception ex)
            {
                try { _logger.OpenFailed(sw.Elapsed, _connection, ex); } catch { }
                throw;
            }
        }
#endif

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            try
            {
                var transaction = _connection.BeginTransaction(isolationLevel);
                try { _logger.BeginTransactionSuccessful(_connection, transaction); } catch { }
                return new LoggingTransaction(transaction, this, _logger);
            }
            catch (Exception ex)
            {
                try { _logger.BeginTransactionFailed(_connection, ex); } catch { }
                throw;
            }
        }

        protected override DbCommand CreateDbCommand() => new LoggingCommand(_connection.CreateCommand(), this, _logger);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _connection.StateChange -= Connection_StateChange;
                _connection.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override bool CanRaiseEvents => true;

        public override void EnlistTransaction(System.Transactions.Transaction transaction) => _connection.EnlistTransaction(transaction);

        public override DataTable GetSchema() => _connection.GetSchema();

        public override DataTable GetSchema(string collectionName) => _connection.GetSchema(collectionName);

        public override DataTable GetSchema(string collectionName, string[] restrictionValues) => _connection.GetSchema(collectionName, restrictionValues);
    }
}