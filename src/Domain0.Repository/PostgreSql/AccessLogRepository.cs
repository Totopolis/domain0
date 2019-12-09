using System.Threading.Tasks;
using Dapper;
using Domain0.Repository.Model;

namespace Domain0.Repository.PostgreSql
{
    public class AccessLogRepository : IAccessLogRepository
    {
        private readonly IDbConnectionProvider _connectionProvider;

        public AccessLogRepository(IDbConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        public Task<long> Insert(AccessLogEntry entity)
        {
            const string query = @"
insert into log.""Access""
(""Action"", ""Method"", ""ClientIp"", ""ProcessedAt"", ""StatusCode"", ""UserAgent"", ""UserId"", ""Referer"", ""ProcessingTime"")
values
(@Action, @Method, @ClientIp, @ProcessedAt, @StatusCode, @UserAgent, @UserId, @Referer, @ProcessingTime)
returning ""Id""
";

            using (var con = _connectionProvider.Connection)
            {
                return con.ExecuteScalarAsync<long>(query, entity);
            }
        }
    }
}