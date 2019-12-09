using System;
using System.Linq;
using System.Threading.Tasks;
using Domain0.Repository.Model;
using Gerakul.FastSql.Common;
using Gerakul.FastSql.SqlServer;
using NLog;

namespace Domain0.Repository.SqlServer
{
    public class AccessLogRepository : IAccessLogRepository
    {
        private readonly Func<DbContext> _getContext;
        private readonly ILogger _logger;

        public AccessLogRepository(
            Func<DbContext> getContextFunc,
            ILogger loggerInstance)
        {
            _getContext = getContextFunc;
            _logger = loggerInstance;
        }

        public async Task<long> Insert(AccessLogEntry entity)
        {
            var id = await (_getContext() as ISqlCommandCreator)
                .CreateInsertAndGetID("[log].[Access]", entity, ignoreFields: "Id")
                .ExecuteQueryFirstColumnAsync<long>()
                .First();

            _logger.Debug($"{entity.Action} | {entity.ClientIp} | {entity.ProcessingTime}");
            return id;
        }
    }
}