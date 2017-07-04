using System;
#if NET40 || NET46
using System.Collections.Concurrent;
using System.Threading.Tasks;
#else
using System.Collections.Generic;
#endif
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace DbLoggingWrapper
{
    public partial class LoggingCommand : DbCommand
    {
#if NET40 || NET46
        private static ConcurrentDictionary<Type, Action<IDbCommand, bool>> _bindByNameCache = new ConcurrentDictionary<Type, Action<IDbCommand, bool>>();
#else
        private static Dictionary<Type, Action<IDbCommand, bool>> _bindByNameCache = new Dictionary<Type, Action<IDbCommand, bool>>();
#endif

        private readonly DbCommand _command;
        private readonly ILoggingWriter _logger;
        private DbConnection _connection;
        private DbTransaction _transaction;
        private bool _bindByName;


        public LoggingCommand(DbCommand command, DbConnection connection, ILoggingWriter logger)
        {
            _command = RealCommand(command) ?? throw new ArgumentNullException(nameof(command));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connection = connection;
        }

        private DbCommand RealCommand(DbCommand command) => command is LoggingCommand logging ? logging.BaseCommand : command;

        public DbCommand BaseCommand => _command;

        public override string CommandText { get => _command.CommandText; set => _command.CommandText = value; }

        public override int CommandTimeout { get => _command.CommandTimeout; set => _command.CommandTimeout = value; }

        public override CommandType CommandType { get => _command.CommandType; set => _command.CommandType = value; }

        protected override DbConnection DbConnection
        {
            get => _connection;
            set
            {
                _connection = value;
                _command.Connection = value is LoggingConnection logging ? logging.BaseConnection : value;
            }
        }

        protected override DbParameterCollection DbParameterCollection => _command.Parameters;

        protected override DbTransaction DbTransaction
        {
            get => _transaction;
            set
            {
                _transaction = value;
                _command.Transaction = _transaction is LoggingTransaction logging ? logging.BaseTransaction : _transaction;
            }
        }

        public override bool DesignTimeVisible { get => _command.DesignTimeVisible; set => _command.DesignTimeVisible = value; }

        public override UpdateRowSource UpdatedRowSource { get => _command.UpdatedRowSource; set => _command.UpdatedRowSource = value; }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var reader = _command.ExecuteReader(behavior);
                try { _logger.ExecuteReaderSuccessful(sw.Elapsed, _command); } catch { }
                return new LoggingDataReader(reader, _logger);
            }
            catch (Exception ex)
            {
                try { _logger.ExecuteReaderFailed(sw.Elapsed, _command, ex); } catch { }
                throw;
            }
        }

        public override int ExecuteNonQuery()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = _command.ExecuteNonQuery();
                try { _logger.ExecuteNonQuerySuccessful(sw.Elapsed, _command, result); } catch { }
                return result;
            }
            catch (Exception ex)
            {
                try { _logger.ExecuteNonQueryFailed(sw.Elapsed, _command, ex); } catch { }
                throw;
            }
        }

        public override object ExecuteScalar()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = _command.ExecuteScalar();
                try { _logger.ExecuteScalarSuccessful(sw.Elapsed, _command, result); } catch { }
                return result;
            }
            catch (Exception ex)
            {
                try { _logger.ExecuteScalarFailed(sw.Elapsed, _command, ex); } catch { }
                throw;
            }
        }

#if NET46
        protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var reader = await _command.ExecuteReaderAsync(behavior, cancellationToken).ConfigureAwait(false);
                try { _logger.ExecuteReaderSuccessful(sw.Elapsed, _command); } catch { }
                return new LoggingDataReader(reader, _logger);
            }
            catch (Exception ex)
            {
                try { _logger.ExecuteReaderFailed(sw.Elapsed, _command, ex); } catch { }
                throw;
            }
        }

        public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = await _command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                try { _logger.ExecuteNonQuerySuccessful(sw.Elapsed, _command, result); } catch { }
                return result;
            }
            catch (Exception ex)
            {
                try { _logger.ExecuteNonQueryFailed(sw.Elapsed, _command, ex); } catch { }
                throw;
            }
        }

        public override async Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = await _command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                try { _logger.ExecuteScalarSuccessful(sw.Elapsed, _command, result); } catch { }
                return result;
            }
            catch (Exception ex)
            {
                try { _logger.ExecuteScalarFailed(sw.Elapsed, _command, ex); } catch { }
                throw;
            }
        }
#endif

        public override void Cancel() => _command.Cancel();

        public override void Prepare() => _command.Prepare();

        protected override DbParameter CreateDbParameter() => _command.CreateParameter();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _command.Dispose();
            }
            base.Dispose(disposing);
        }

        public bool BindByName
        {
            get => _bindByName;
            set
            {
                _bindByName = value;
                SetBindByName(_command, value);
            }
        }

        internal static void SetBindByName(IDbCommand command, bool value)
        {
#if NET40 || NET46
            var setBindByName = _bindByNameCache.GetOrAdd(command.GetType(), commandType => CreateBindByNameSetter(commandType));
#else
            Action<IDbCommand, bool> setBindByName = null;
            var commandType = command.GetType();
            lock (_bindByNameCache)
            {
                if (!_bindByNameCache.TryGetValue(commandType, out setBindByName))
                {
                    setBindByName = CreateBindByNameSetter(commandType);
                    if (setBindByName != null)
                    {
                        _bindByNameCache.Add(commandType, setBindByName);
                    }
                }
            }
#endif
            if (setBindByName != null)
            {
                setBindByName.Invoke(command, value);
            }
        }

        private static Action<IDbCommand, bool> CreateBindByNameSetter(Type commandType)
        {
            try
            {
                var pinfo = commandType.GetProperty("BindByName", BindingFlags.Instance | BindingFlags.Public);
                if (pinfo != null && pinfo.CanWrite)
                {
                    var setter = pinfo.GetSetMethod();
                    if (setter != null)
                    {
                        var method = new DynamicMethod(commandType.Name + "_set_BindByName", null, new[] { typeof(IDbCommand), typeof(bool) });
                        var il = method.GetILGenerator();
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Castclass, commandType);
                        il.Emit(OpCodes.Ldarg_1);
                        il.EmitCall(OpCodes.Callvirt, setter, null);
                        il.Emit(OpCodes.Ret);
                        return (Action<IDbCommand, bool>)method.CreateDelegate(typeof(Action<IDbCommand, bool>));
                    }
                }
            }
            catch { }

            return null;
        }
    }
}