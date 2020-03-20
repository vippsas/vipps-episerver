using EPiServer.Logging;
using Mediachase.MetaDataPlus;
using Mediachase.MetaDataPlus.Configurator;
using MetaClass = Mediachase.MetaDataPlus.Configurator.MetaClass;
using MetaField = Mediachase.MetaDataPlus.Configurator.MetaField;

namespace Vipps.Initialization
{
    public abstract class MetadataInitializationBase
    {
        protected static readonly ILogger Logger = LogManager.GetLogger(typeof(MetadataInitializationBase));

        protected MetaField GetOrCreateMetaField(MetaDataContext mdContext, string fieldName, MetaDataType metaDataType = MetaDataType.LongString)
        {

            var f = MetaField.Load(mdContext, fieldName);
            if (f == null)
            {
                Logger.Debug($"Adding meta field '{fieldName}' for vipps integration.");
                f = MetaField.Create(mdContext, VippsConstants.OrderNamespace, fieldName, fieldName, string.Empty, metaDataType, int.MaxValue, true, false, false, false);
            }
            return f;
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
