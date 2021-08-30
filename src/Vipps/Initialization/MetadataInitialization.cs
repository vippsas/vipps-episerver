using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using Mediachase.Commerce.Catalog;
using Mediachase.MetaDataPlus.Configurator;

namespace Vipps.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Commerce.Initialization.InitializationModule))]
    internal class MetadataInitialization : MetadataInitializationBase, IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            var mdContext = CatalogContext.MetaDataContext;
            
            // Purchase order meta fields
            JoinField(mdContext, GetOrCreateMetaField(mdContext, VippsConstants.VippsOrderIdField, MetaDataType.VarChar), VippsConstants.PurchaseOrderClass);
            JoinField(mdContext, GetOrCreateMetaField(mdContext, VippsConstants.VippsOrderIdField, MetaDataType.VarChar), VippsConstants.CartClass);
            JoinField(mdContext, GetOrCreateMetaField(mdContext, VippsConstants.VippsPaymentTypeField), VippsConstants.PurchaseOrderClass);
            JoinField(mdContext, GetOrCreateMetaField(mdContext, VippsConstants.VippsPaymentTypeField), VippsConstants.CartClass);
        }

        public void Uninitialize(InitializationEngine context)
        {

        }
    }
}
