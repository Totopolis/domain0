using System.Threading.Tasks;
using Dapper;
using Domain0.Repository.Model;
using NLog;

namespace Domain0.Repository.PostgreSql
{
    public class AccessLogRepository : IAccessLogRepository
    {
        private readonly IDbConnectionProvider _connectionProvider;
        private readonly ILogger _logger;

        public AccessLogRepository(
            IDbConnectionProvider connectionProvider,
            ILogger logger)
        {
            _connectionProvider = connectionProvider;
            _logger = logger;
        }

        public async Task<long> Insert(AccessLogEntry entity)
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
                var id = await con.ExecuteScalarAsync<long>(query, entity);
                _logger.Debug($"{entity.Action} | {entity.ClientIp} | {entity.ProcessingTime}");
                return id;
            }
        }
    }
}