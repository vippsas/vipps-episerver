using EPiServer.Logging;
using Mediachase.MetaDataPlus.Configurator;
using System.Collections.Generic;

namespace Vipps.Data
{
    public class MetaFieldMigrator : DbExecutorBase
    {
        private const string _alterMetaFieldType = @"DECLARE @MetaFieldId int = (SELECT TOP 1 [MetaFieldId] FROM [dbo].[MetaField] WHERE [Name] = '{0}')
                                                     UPDATE [dbo].[MetaField]
                                                        SET DataTypeId = @TargetType, [Length] = @TargetLength
                                                        WHERE [MetaFieldId] = @MetaFieldId";

        private readonly ILogger _logger;

        public MetaFieldMigrator() : base("EcfSqlConnection")
        {
            _logger = LogManager.GetLogger(typeof(MetaFieldMigrator));
        }

        public int AlterMetaField(string metaFieldName, MetaDataType metaDataType, int length)
        {
            var parameters = new []
            {
                new KeyValuePair<string, object>("@TargetType", (int)metaDataType),
                new KeyValuePair<string, object>("@TargetLength", length),
            };

            _logger.Debug($"Altering meta field {metaFieldName} to type {metaDataType} with length {length}");

            return ExecuteNonQuery(string.Format(_alterMetaFieldType, metaFieldName, metaDataType), parameters);
        }
    }
}
