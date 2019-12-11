using System.Data;
using Npgsql;

namespace Domain0.Repository.PostgreSql
{
    public class DbConnectionProvider : IDbConnectionProvider
    {
        private readonly string _connectionString;

        public DbConnectionProvider(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection Connection => new NpgsqlConnection(_connectionString);
    }
}