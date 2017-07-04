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
    public class LoggingProviderFactoryResolver : IDbProviderFactoryResolver
    {
        private readonly IDbProviderFactoryResolver _resolver;

        public LoggingProviderFactoryResolver(IDbProviderFactoryResolver resolver) => _resolver = resolver;

        public DbProviderFactory ResolveProviderFactory(DbConnection connection) => _resolver.ResolveProviderFactory(connection is LoggingConnection logging ? logging.BaseConnection : connection);
    }
}
