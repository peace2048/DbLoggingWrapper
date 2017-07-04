using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace DbLoggingWrapper
{
    public interface ILoggingWriter
    {
        void CommitSuccessful(TimeSpan elapsed, DbTransaction transaction);
        void CommitFailed(TimeSpan elapsed, DbTransaction transaction, Exception ex);
        void ChangeDatabaseSuccessful(TimeSpan elapsed, DbConnection connection, string databaseName);
        void ChangeDatabaseFailed(TimeSpan elapsed, DbConnection connection, string databaseName, Exception ex);
        void CloseSuccessful(DbConnection connection);
        void CloseFailed(DbConnection connection, Exception ex);
        void OpenSuccessful(TimeSpan elapsed, DbConnection connection);
        void OpenFailed(TimeSpan elapsed, DbConnection connection, Exception ex);
        void BeginTransactionSuccessful(DbConnection connection, DbTransaction transaction);
        void BeginTransactionFailed(DbConnection connection, Exception ex);
        void ExecuteReaderSuccessful(TimeSpan elapsed, DbCommand command);
        void ExecuteReaderFailed(TimeSpan elapsed, DbCommand command, Exception ex);
        void ExecuteNonQuerySuccessful(TimeSpan elapsed, DbCommand command, int result);
        void ExecuteNonQueryFailed(TimeSpan elapsed, DbCommand command, Exception ex);
        void ExecuteScalarFailed(TimeSpan elapsed, DbCommand command, Exception ex);
        void RollbackSuccessful(TimeSpan elapsed, DbTransaction transaction);
        void RollbackFailed(TimeSpan elapsed, DbTransaction transaction, Exception ex);
        void ExecuteScalarSuccessful(TimeSpan elapsed, DbCommand command, object result);
    }
}
