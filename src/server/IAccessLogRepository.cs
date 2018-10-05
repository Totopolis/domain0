using Domain0.Repository.Model;

namespace Domain0.Repository
{
    public interface IAccessLogRepository : IRepository<long, AccessLogEntry>
    {
    }
}
