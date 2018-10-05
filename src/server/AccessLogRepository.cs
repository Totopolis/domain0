using Domain0.Repository;
using Domain0.Repository.Model;

namespace Domain0.FastSql
{
    public class AccessLogRepository : RepositoryBase<long, AccessLogEntry>, IAccessLogRepository
    {
        public AccessLogRepository(string connectionString) : base(connectionString)
        {
            TableName = "[log].[Access]";
            KeyName = "Id";
        }
    }
}