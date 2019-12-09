using System.Data;
using System.Data.SqlClient;

namespace Domain0.Repository.SqlServer
{
    public class DbConnectionProvider : IDbConnectionProvider
    {
        private readonly string _connectionString;
        public DbConnectionProvider(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection Connection => new SqlConnection(_connectionString);
    }
}