using System;
#if NET40
using System.Collections.Concurrent;
#else
using System.Collections.Generic;
#endif
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace DbLoggingWrapper
{
    public class LoggingCommand : IDbCommand
    {
#if NET40
        private static ConcurrentDictionary<Type, Action<IDbCommand>> _bindByName = new ConcurrentDictionary<Type, Action<IDbCommand>>();
#else
        private static Dictionary<Type, Action<IDbCommand>> _bindByName = new Dictionary<Type, Action<IDbCommand>>();
#endif

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
            return LoggingWrapper.Dump(BaseCommand);
        }

        internal static void SetBindByName(IDbCommand command)
        {
#if NET40
            var setBindByName = _bindByName.GetOrAdd(command.GetType(), cmdType =>
            {
                var propertyInfo = cmdType.GetProperty("BindByName", BindingFlags.Instance | BindingFlags.Public);
                if (propertyInfo != null && propertyInfo.CanWrite)
                {
                    var setMethod = propertyInfo.GetSetMethod();
                    if (setMethod != null)
                    {
                        var method = new DynamicMethod("SetBindByName_", null, new[] { typeof(IDbCommand) });
                        var il = method.GetILGenerator();
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Castclass, cmdType);
                        il.Emit(OpCodes.Ldc_I4_1);
                        il.EmitCall(OpCodes.Callvirt, setMethod, null);
                        il.Emit(OpCodes.Ret);
                        return (Action<IDbCommand>)method.CreateDelegate(typeof(Action<IDbCommand>));
                    }
                }
                return null;
            });
#else
            Action<IDbCommand> setBindByName = null;
            lock (_bindByName)
            {
                if (!_bindByName.TryGetValue(command.GetType(), out setBindByName))
                {
                    var cmdType = command.GetType();
                    var propertyInfo = cmdType.GetProperty("BindByName", BindingFlags.Instance | BindingFlags.Public);
                    if (propertyInfo != null && propertyInfo.CanWrite)
                    {
                        var setMethod = propertyInfo.GetSetMethod();
                        if (setMethod != null)
                        {
                            var method = new DynamicMethod("SetBindByName_", null, new[] { typeof(IDbCommand) });
                            var il = method.GetILGenerator();
                            il.Emit(OpCodes.Ldarg_0);
                            il.Emit(OpCodes.Castclass, cmdType);
                            il.Emit(OpCodes.Ldc_I4_1);
                            il.EmitCall(OpCodes.Callvirt, setMethod, null);
                            il.Emit(OpCodes.Ret);
                            setBindByName = (Action<IDbCommand>)method.CreateDelegate(typeof(Action<IDbCommand>));
                            _bindByName.Add(cmdType, setBindByName);
                        }
                    }
                }
            }
#endif
            if (setBindByName != null)
            {
                setBindByName.Invoke(command);
            }
        }
    }
}