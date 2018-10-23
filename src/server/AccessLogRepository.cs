using Domain0.Repository;
using Domain0.Repository.Model;
using Gerakul.FastSql.Common;
using NLog;
using System;
using System.Threading.Tasks;

namespace Domain0.FastSql
{
    public class AccessLogRepository : RepositoryBase<long, AccessLogEntry>, IAccessLogRepository
    {
        public AccessLogRepository(
            Func<DbContext> getContextFunc,
            ILogger loggerInstance)
            : base(getContextFunc)
        { 
            TableName = "[log].[Access]";
            KeyName = "Id";
            Logger = loggerInstance;
        }


        public override async Task<decimal> Insert(AccessLogEntry entity)
        {
            var id = await base.Insert(entity);
            Logger.Info($"{entity.Action} | {entity.ClientIp} | {entity.ProcessingTime }");
            return id;
        }

        private readonly ILogger Logger;
    }
}