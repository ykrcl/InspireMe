using System;
using System.Data;
using Npgsql;
using System.Threading.Tasks;
using D = Dapper;

namespace InspireMe.Data
{
    /// <summary>
    /// 
    /// </summary>
    public class PostgresConnectionFactory : IDatabaseConnectionFactory
    {
        private readonly string _connectionString;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString">Database connection string</param>
        public PostgresConnectionFactory(string connectionString) => _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));

        /// <inheritdoc/>
        public IDbConnection CreateConnection() 
        {
            var sqlConnection = new NpgsqlConnection(_connectionString);
            D.DefaultTypeMap.MatchNamesWithUnderscores = true;
            sqlConnection.Open();
            return sqlConnection;
        }

        public async Task<IDbConnection> CreateConnectionAsync()
        {
            var sqlConnection = new NpgsqlConnection(_connectionString);
            D.DefaultTypeMap.MatchNamesWithUnderscores = true;
            await sqlConnection.OpenAsync();
            return sqlConnection;
        }
    }
}
