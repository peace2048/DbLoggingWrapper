using DbLoggingWrapper;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbLoggingWrapperEF6
{
    public class LoggingConnecttionFactory : IDbConnectionFactory
    {
        private readonly IDbConnectionFactory _factory;

        public LoggingConnecttionFactory(IDbConnectionFactory factory) => _factory = factory;

        public DbConnection CreateConnection(string nameOrConnectionString)
        {
            var connection = _factory.CreateConnection(nameOrConnectionString);
            if (connection is LoggingConnection)
            {
                return connection;
            }
            return new LoggingConnection(connection, LoggingWrapper.DefaultLogWriter);
        }
    }
}
