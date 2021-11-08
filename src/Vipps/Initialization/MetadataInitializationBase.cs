using EPiServer.Logging;
using Mediachase.MetaDataPlus;
using Mediachase.MetaDataPlus.Configurator;

namespace Vipps.Initialization
{
    public abstract class MetadataInitializationBase
    {
        protected static readonly ILogger Logger = LogManager.GetLogger(typeof(MetadataInitializationBase));

        protected MetaField GetOrCreateMetaField(MetaDataContext mdContext, string fieldName, MetaDataType metaDataType = MetaDataType.LongString, int length = int.MaxValue)
        {
            var field = MetaField.Load(mdContext, fieldName);
            if (field != null) return field;
            
            return CreateMetaField(mdContext, fieldName, metaDataType, length);
        }

        protected MetaField CreateMetaField(MetaDataContext mdContext, string fieldName, MetaDataType metaDataType = MetaDataType.NVarChar, int length = int.MaxValue)
        {
            Logger.Debug($"Adding meta field '{fieldName}' for vipps integration.");
            return MetaField.Create(mdContext, VippsConstants.OrderNamespace, fieldName, fieldName, string.Empty, metaDataType, length, true, false, false, false);
        }

        protected void JoinField(MetaDataContext mdContext, MetaField field, string metaClassName)
        {
            var cls = MetaClass.Load(mdContext, metaClassName);

            if (MetaFieldIsNotConnected(field, cls))
            {
                cls.AddField(field);
            }
        }

        protected static bool MetaFieldIsNotConnected(MetaField field, MetaClass cls)
        {
            return cls != null && !cls.MetaFields.Contains(field);
        }
    }
}
