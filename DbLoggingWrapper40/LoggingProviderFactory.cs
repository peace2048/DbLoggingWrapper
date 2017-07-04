using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Text;

namespace DbLoggingWrapper
{
    public class LoggingProviderFactory : DbProviderFactory
    {
        private DbProviderFactory _factory;

        public readonly static LoggingProviderFactory Instance = new LoggingProviderFactory(null);

        public LoggingProviderFactory(DbProviderFactory providerFactory) => _factory = providerFactory;

        public DbProviderFactory BaseProviderFactory => _factory;

        public override DbCommand CreateCommand()
        {
            var command = _factory.CreateCommand();
            return new LoggingCommand(command, null, LoggingWrapper.DefaultLogWriter);
        }

        public override DbConnection CreateConnection()
        {
            var connection = _factory.CreateConnection();
            return new LoggingConnection(connection, LoggingWrapper.DefaultLogWriter);
        }

        public override DbParameter CreateParameter() => _factory.CreateParameter();

        public void SetBaseProviderFactory(DbProviderFactory factory) => _factory = factory;

        public override bool CanCreateDataSourceEnumerator => _factory.CanCreateDataSourceEnumerator;

        public override DbCommandBuilder CreateCommandBuilder() => _factory.CreateCommandBuilder();

        public override DbDataAdapter CreateDataAdapter() => _factory.CreateDataAdapter();

        public override DbDataSourceEnumerator CreateDataSourceEnumerator() => _factory.CreateDataSourceEnumerator();

        public override CodeAccessPermission CreatePermission(PermissionState state) => _factory.CreatePermission(state);
    }
}
