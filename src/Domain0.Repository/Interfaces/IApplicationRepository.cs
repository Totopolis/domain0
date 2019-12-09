using System.Collections.Generic;
using System.Threading.Tasks;
using Domain0.Repository.Model;

namespace Domain0.Repository
{
    public interface IApplicationRepository
    {
        Task<int> Insert(Application entity);
        Task<Application[]> FindByIds(IEnumerable<int> ids);
        Task Update(Application entity);
        Task Delete(int id);
    }
}