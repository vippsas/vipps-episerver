using EPiServer.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace Vipps.Data
{
    public abstract class DbExecutorBase
    {
        private readonly ConnectionStringSettings _connectionSettings;
        private readonly ILogger _logger;

        protected DbExecutorBase(string connectionStringName)
        {
            if (string.IsNullOrEmpty(connectionStringName))
                throw new ArgumentException("message", nameof(connectionStringName));

            _connectionSettings = ConfigurationManager.ConnectionStrings[connectionStringName];
            _logger = LogManager.GetLogger(GetType());
        }

        protected virtual TResult ExecuteScalar<TResult>(string query, IEnumerable<KeyValuePair<string, object>> parameters, Func<object, TResult> conversionDelegate)
        {
            return Execute(query, parameters, (command) => conversionDelegate(command.ExecuteScalar()));
        }

        protected virtual int ExecuteNonQuery(string query, IEnumerable<KeyValuePair<string, object>> parameters)
        {
            return Execute(query, parameters, (command) => command.ExecuteNonQuery());
        }

        protected virtual IList<TResult> ExecuteToList<TResult>(string query, IEnumerable<KeyValuePair<string, object>> parameters, Func<DbDataReader, TResult> selectionDelegate)
        {
            return Execute(query, parameters, (command) => {

                var result = new List<TResult>();

                using (var reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            result.Add(selectionDelegate(reader));
                        }
                    }
                }

                return result;
            });
        }

        protected virtual TResult Execute<TResult>(string query, IEnumerable<KeyValuePair<string, object>> parameters, Func<DbCommand, TResult> exectionDelegate)
        {
            using (var connection = GetConnection())
            using (var command = connection.CreateCommand())
            {
                try
                {
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
                    command.CommandText = query;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

                    command.CommandType = CommandType.Text;
                    command.CommandTimeout = 60;

                    foreach (var valuePair in parameters ?? Enumerable.Empty<KeyValuePair<string, object>>())
                    {
                        var parameter = command.CreateParameter();

                        parameter.ParameterName = valuePair.Key;
                        parameter.Value = valuePair.Value;

                        command.Parameters.Add(parameter);
                    }

                    if (!connection.State.Equals(ConnectionState.Open))
                        connection.Open();

                     return exectionDelegate(command);
                }
                catch (SqlException ex)
                {
                    var message = BuildErrorMessage(parameters, ex);
                    _logger.Error(message, ex);
                }
                finally
                {
                    connection.Close();
                }
            }

            return default(TResult);
        }

        protected virtual string BuildErrorMessage(IEnumerable<KeyValuePair<string, object>> parameters, SqlException exception)
        {
            var builder = new StringBuilder("SQL Server failed to execute a statement.");

            builder.Append(Environment.NewLine);
            builder.Append("Server error message: ");
            builder.Append(exception.Message);

            builder.Append(Environment.NewLine);
            builder.Append("Parameters: ");
            builder.Append(string.Join(", ", parameters.Select(x => $"{x.Key}: '{x.Value}'")));

            return builder.ToString();
        }

        protected virtual DbConnection GetConnection()
        {
            return new SqlConnection(_connectionSettings.ConnectionString);
        }
    }
}
