using DbLoggingWrapper;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;

namespace DbLoggingWrapperEF6
{
    public static class LoggingWrapperEF6
    {
        private static readonly ConcurrentDictionary<object, DbProviderServices> _providerServicesCache = new ConcurrentDictionary<object, DbProviderServices>();
        private static readonly ConcurrentDictionary<object, DbProviderFactory> _providerFactoryCache = new ConcurrentDictionary<object, DbProviderFactory>();
        private static readonly ConcurrentDictionary<object, IDbProviderFactoryResolver> _providerFactoryResolverCache = new ConcurrentDictionary<object, IDbProviderFactoryResolver>();
        private static readonly ConcurrentDictionary<object, IDbConnectionFactory> _connectionFactoryCache = new ConcurrentDictionary<object, IDbConnectionFactory>();
        private static readonly object _nullKeyPlaceholder = new object();

        public static void Initialize()
        {
            try
            {
                DbConfiguration.Loaded += (_, a) =>
                {
                    a.ReplaceService((DbProviderServices inner, object key) => _providerServicesCache.GetOrAdd(key ?? _nullKeyPlaceholder, __ => new LoggingProviderServices(inner)));
                    a.ReplaceService((DbProviderFactory inner, object key) => _providerFactoryCache.GetOrAdd(key ?? _nullKeyPlaceholder, __ => new LoggingProviderFactory(inner)));
                    a.ReplaceService((IDbProviderFactoryResolver inner, object key) => _providerFactoryResolverCache.GetOrAdd(key ?? _nullKeyPlaceholder, __ => new LoggingProviderFactoryResolver(inner)));
                    a.ReplaceService((IDbConnectionFactory inner, object key) => _connectionFactoryCache.GetOrAdd(key ?? _nullKeyPlaceholder, __ => new LoggingConnecttionFactory(inner)));
                    a.AddDependencyResolver(new LoggingInvariantNameResolver(), false);
                };
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                if (!ex.Message.Contains("Invalid column name 'ContextKey'"))
                {
                    throw;
                }
            }
        }
    }
}
