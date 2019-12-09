using System.Threading.Tasks;
using Domain0.Repository.Model;

namespace Domain0.Repository
{
    public interface IAccessLogRepository
    {
        Task<long> Insert(AccessLogEntry entity);
    }
}
