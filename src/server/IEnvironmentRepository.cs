using System.Threading.Tasks;
using Domain0.Repository.Model;

namespace Domain0.Repository
{
    public interface IEnvironmentRepository : IRepository<int, Environment>
    {
        Task<Environment> GetByToken(string environmentToken);
        Task<Environment> GetDefault();
        Task<Environment> GetByUser(int userId);
        Task SetUserEnvironment(int userId, int environmentId);
    }
}
