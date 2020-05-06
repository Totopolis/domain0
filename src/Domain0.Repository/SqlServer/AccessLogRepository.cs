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

        public async Task Insert(AccessLogEntry entity)
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
           ,[ProcessingTime]
           ,[AcceptLanguage])
     VALUES
           (@Action
           ,@Method
           ,@ClientIp
           ,@ProcessedAt
           ,@StatusCode
           ,@UserAgent
           ,@UserId
           ,@Referer
           ,@ProcessingTime
           ,@AcceptLanguage)
";

            using (var con = _connectionProvider.Connection)
            {
                await con.ExecuteAsync(query, entity);
                _logger.Debug($"{entity.Action} | {entity.ClientIp} | {entity.ProcessingTime}");
            }
        }
    }
}