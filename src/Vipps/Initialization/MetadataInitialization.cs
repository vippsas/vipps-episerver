using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Logging;
using Mediachase.Commerce.Catalog;
using Mediachase.MetaDataPlus.Configurator;
using Vipps.Data;

namespace Vipps.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Commerce.Initialization.InitializationModule))]
    internal class MetadataInitialization : MetadataInitializationBase, IInitializableModule
    {
        private readonly IndexMigrator _indexMigrator;
        private readonly ColumnMigrator _columnMigrator;
        private readonly MetaFieldMigrator _metaFieldMigrator;
        private readonly ILogger _logger;

        public MetadataInitialization()
        {
            _indexMigrator = new IndexMigrator();
            _columnMigrator = new ColumnMigrator();
            _metaFieldMigrator = new MetaFieldMigrator();
            _logger = LogManager.GetLogger(typeof(MetadataInitialization));
        }

        public void Initialize(InitializationEngine context)
        {
            _logger.Information("Running migrations for Vipps");

            // Add or migrate purchase order meta fields
            var mdContext = CatalogContext.MetaDataContext;
            var idField = MetaField.Load(mdContext, VippsConstants.VippsOrderIdField);
            
            // Note: See https://vipps.no/developers-documentation/ecom/documentation/#orderid-recommendations
            var idFieldMaxLength = 50;

            if (idField != null && (idField.DataType != MetaDataType.ShortString || idField.Length != idFieldMaxLength))
            {
                _metaFieldMigrator.AlterMetaField(VippsConstants.VippsOrderIdField, MetaDataType.ShortString, idFieldMaxLength);

                var columnType = $"nvarchar({idFieldMaxLength})";

                _columnMigrator.MigrateColumn(VippsConstants.PurchaseOrderClass, VippsConstants.VippsOrderIdField, columnType);
                _columnMigrator.MigrateColumn(VippsConstants.CartClass, VippsConstants.VippsOrderIdField, columnType);
            }
            else
            {
                JoinField(mdContext, GetOrCreateMetaField(mdContext, VippsConstants.VippsOrderIdField, MetaDataType.ShortString, 50), VippsConstants.PurchaseOrderClass);
                JoinField(mdContext, GetOrCreateMetaField(mdContext, VippsConstants.VippsOrderIdField, MetaDataType.ShortString, 50), VippsConstants.CartClass);
            }
            
            JoinField(mdContext, GetOrCreateMetaField(mdContext, VippsConstants.VippsPaymentTypeField), VippsConstants.PurchaseOrderClass);
            JoinField(mdContext, GetOrCreateMetaField(mdContext, VippsConstants.VippsPaymentTypeField), VippsConstants.CartClass);
            JoinField(mdContext, GetOrCreateMetaField(mdContext, VippsConstants.VippsIsProcessingOrderField, MetaDataType.Boolean), VippsConstants.CartClass);

            _indexMigrator.CreateIndexIfNotExists(VippsConstants.PurchaseOrderClass, VippsConstants.VippsOrderIdField);
            _indexMigrator.CreateIndexIfNotExists(VippsConstants.CartClass, VippsConstants.VippsOrderIdField);
        }

        public void Uninitialize(InitializationEngine context)
        {

        }
    }
}
