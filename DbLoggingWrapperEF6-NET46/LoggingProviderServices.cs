using DbLoggingWrapper;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Spatial;

namespace DbLoggingWrapperEF6
{
    public class LoggingProviderServices : DbProviderServices
    {
        private DbProviderServices _tail;

        public LoggingProviderServices(DbProviderServices providerServices)
        {
            _tail = providerServices ??
                throw new ArgumentException("providerServices cannot be null. Please check that your .config defines a <DbProviderFactories> section <system.data>.", nameof(providerServices));
        }

        public override DbCommandDefinition CreateCommandDefinition(DbCommand prototype) => _tail.CreateCommandDefinition(prototype);

        protected override DbCommandDefinition CreateDbCommandDefinition(DbProviderManifest providerManifest, DbCommandTree commandTree)
        {
            var def = _tail.CreateCommandDefinition(providerManifest, commandTree);
            var cmd = def.CreateCommand();
            return CreateCommandDefinition(new LoggingCommand(cmd, cmd.Connection, LoggingWrapper.DefaultLogWriter));
        }

        protected override DbProviderManifest GetDbProviderManifest(string manifestToken) => _tail.GetProviderManifest(manifestToken);

        protected override string GetDbProviderManifestToken(DbConnection connection) => _tail.GetProviderManifestToken(GetRealConnection(connection));

        protected override void DbCreateDatabase(DbConnection connection, int? commandTimeout, StoreItemCollection storeItemCollection) => _tail.CreateDatabase(GetRealConnection(connection), commandTimeout, storeItemCollection);

        protected override void DbDeleteDatabase(DbConnection connection, int? commandTimeout, StoreItemCollection storeItemCollection) => _tail.DeleteDatabase(GetRealConnection(connection), commandTimeout, storeItemCollection);

        protected override string DbCreateDatabaseScript(string providerManifestToken, StoreItemCollection storeItemCollection) => _tail.CreateDatabaseScript(providerManifestToken, storeItemCollection);

        protected override bool DbDatabaseExists(DbConnection connection, int? commandTimeout, Lazy<StoreItemCollection> storeItemCollection) => _tail.DatabaseExists(GetRealConnection(connection), commandTimeout, storeItemCollection);

        private DbConnection GetRealConnection(DbConnection connection) => connection is LoggingConnection logging ? logging.BaseConnection : connection;

        public override object GetService(Type type, object key) => _tail.GetService(type, key);

        public override IEnumerable<object> GetServices(Type type, object key) => _tail.GetServices(type, key);

        protected override DbSpatialDataReader GetDbSpatialDataReader(DbDataReader fromReader, string manifestToken) => _tail.GetSpatialDataReader(fromReader is LoggingDataReader logging ? logging.BaseDataReader : fromReader, manifestToken);

        [Obsolete("Retrun DbSpatialServices from the GetService method.")]
        protected override DbSpatialServices DbGetSpatialServices(string manifestToken) => _tail.GetSpatialServices(manifestToken);
    }
}
