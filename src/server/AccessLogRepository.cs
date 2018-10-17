using Domain0.Repository;
using Domain0.Repository.Model;
using Gerakul.FastSql.Common;
using System;

namespace Domain0.FastSql
{
    public class AccessLogRepository : RepositoryBase<long, AccessLogEntry>, IAccessLogRepository
    {
        public AccessLogRepository(Func<DbContext> getContextFunc)
            : base(getContextFunc)
        { 
            TableName = "[log].[Access]";
            KeyName = "Id";
        }
    }
}