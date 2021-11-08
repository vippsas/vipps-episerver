using EPiServer.Logging;
using System.Collections.Generic;

namespace Vipps.Data
{
    public class ColumnMigrator : DbExecutorBase
    {
        private const string _alterColumnQuery = @"ALTER TABLE [dbo].[{0}]
                                                   ALTER COLUMN [{1}] {2}";

        private readonly ILogger _logger;

        public ColumnMigrator() : base("EcfSqlConnection")
        {
            _logger = LogManager.GetLogger(typeof(ColumnMigrator));
        }

        public void MigrateColumn(string metaClassName, string columnName, string columnType)
        {
            AlterColumn($"OrderGroup_{metaClassName}", columnName, columnType);
            AlterColumn($"OrderGroup_{metaClassName}_Localization", columnName, columnType);
        }

        private int AlterColumn(string tableName, string columnName, string columnType)
        {
            var parameters = new KeyValuePair<string, object>[0];

            _logger.Debug($"Altering column {columnName} in table {tableName} to have data type {columnType}");

            return ExecuteNonQuery(string.Format(_alterColumnQuery, tableName, columnName, columnType), parameters);
        }
    }
}
