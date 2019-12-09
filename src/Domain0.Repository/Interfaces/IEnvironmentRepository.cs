using System.Collections.Generic;
using System.Threading.Tasks;
using Domain0.Repository.Model;

namespace Domain0.Repository
{
    public interface IEnvironmentRepository
    {
        Task<int> Insert(Environment entity);
        Task Update(Environment entity);
        Task Delete(int id);
        Task<Environment> FindById(int id);
        Task<Environment[]> FindByIds(IEnumerable<int> ids);
        Task<Environment> GetByToken(string environmentToken);
        Task<Environment> GetDefault();
        Task<Environment> GetByUser(int userId);
        Task SetUserEnvironment(int userId, int environmentId);
    }
}