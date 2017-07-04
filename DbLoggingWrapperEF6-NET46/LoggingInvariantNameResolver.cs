using DbLoggingWrapper;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Infrastructure.DependencyResolution;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DbLoggingWrapperEF6
{
    public class LoggingInvariantNameResolver : IDbDependencyResolver
    {
        private readonly ConcurrentDictionary<DbProviderFactory, IProviderInvariantName> _providerInvariantNameCache = new ConcurrentDictionary<DbProviderFactory, IProviderInvariantName>();

        public object GetService(Type type, object key)
        {
            if (type != typeof(IProviderInvariantName))
            {
                return null;
            }
            var factory = key is LoggingProviderFactory logging ? logging.BaseProviderFactory : key as DbProviderFactory;
            if (factory == null)
            {
                return null;
            }
            return _providerInvariantNameCache.GetOrAdd(factory, GetProviderInvariantNameViaRefrection);
        }

        public IEnumerable<object> GetServices(Type type, object key)
        {
            var service = GetService(type, key);
            return service == null ? Enumerable.Empty<object>() : new[] { service };
        }

        private IProviderInvariantName GetProviderInvariantNameViaRefrection(DbProviderFactory factory)
        {
            try
            {
                var extensionsType = Type.GetType("System.Data.Entity.Utilities.DbProviderFactoryExtensions, EntityFramework");
                var getProviderInvariantNameMethod = extensionsType.GetMethod("GetProviderInvariantName", BindingFlags.Static | BindingFlags.Public);
                var providerInvariantName = (string)getProviderInvariantNameMethod.Invoke(null, new[] { factory });
                return new ProviderInvariantName(providerInvariantName);
            }
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException != null)
                {
                    throw ex.InnerException;
                }
                throw;
            }
        }

        private class ProviderInvariantName : IProviderInvariantName
        {
            public ProviderInvariantName(string name) => this.Name = name;
            public string Name { get; }
        }
    }
}
