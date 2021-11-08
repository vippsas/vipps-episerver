using EPiServer.Logging;
using System;
using System.Collections.Generic;

namespace Vipps.Data
{
    public class IndexMigrator : DbExecutorBase
    {
        private const string _existsQuery = @"SELECT COUNT([name]) FROM [sys].[indexes]
                                              WHERE [name] = '{0}' AND [object_id] = OBJECT_ID('dbo.{1}')";

        private const string _createQuery = @"CREATE NONCLUSTERED INDEX [{0}]
                                              ON [dbo].[{1}]
	                                          ([{2}] ASC) ON [PRIMARY]";

        private readonly ILogger _logger;

        public IndexMigrator() : base("EcfSqlConnection")
        {
            _logger = LogManager.GetLogger(typeof(IndexMigrator));
        }

        public void CreateIndexIfNotExists(string metaClassName, string columnName)
        {
            CreateIndexIfNotExists($"IDX_{metaClassName}_{columnName}", $"OrderGroup_{metaClassName}", columnName);
        }

        private void CreateIndexIfNotExists(string indexName, string tableName, string columnName)
        {
            var parameters = new KeyValuePair<string, object>[0];
            
            var existsQuery = string.Format(_existsQuery, indexName, tableName, columnName);
            var indexCount = ExecuteScalar(existsQuery, parameters, (result) => Convert.ToInt32(result));
            if (indexCount > 0)
                return;

            _logger.Debug($"Creating index named {indexName} on table {tableName}");

            ExecuteNonQuery(string.Format(_createQuery, indexName, tableName, columnName), parameters);
        }
    }
}
