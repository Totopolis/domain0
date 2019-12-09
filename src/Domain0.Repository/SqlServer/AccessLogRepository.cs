using System.Threading.Tasks;
using Dapper;
using Domain0.Repository.Model;
using NLog;

namespace Domain0.Repository.SqlServer
{
    public class AccessLogRepository : IAccessLogRepository
    {
        private readonly IDbConnectionProvider _connectionProvider;
        private readonly ILogger _logger;

        public AccessLogRepository(
            IDbConnectionProvider connectionProvider,
            ILogger loggerInstance)
        {
            _connectionProvider = connectionProvider;
            _logger = loggerInstance;
        }

        public async Task<long> Insert(AccessLogEntry entity)
        {
            const string query = @"
INSERT INTO [log].[Access]
           ([Action]
           ,[Method]
           ,[ClientIp]
           ,[ProcessedAt]
           ,[StatusCode]
           ,[UserAgent]
           ,[UserId]
           ,[Referer]
           ,[ProcessingTime])
     VALUES
           (@Action
           ,@Method
           ,@ClientIp
           ,@ProcessedAt
           ,@StatusCode
           ,@UserAgent
           ,@UserId
           ,@Referer
           ,@ProcessingTime)
;select SCOPE_IDENTITY() Id";

            using (var con = _connectionProvider.Connection)
            {
                var id = await con.ExecuteScalarAsync<long>(query, entity);
                _logger.Debug($"{entity.Action} | {entity.ClientIp} | {entity.ProcessingTime}");
                return id;
            }
        }
    }
}