using Domain0.Repository.Model;
using System.Threading.Tasks;

namespace Domain0.Repository
{
    public interface IRoleRepository
    {
        Task<Role> GetById(string id);

        Task<Role[]> GetByIds(params string[] ids);

        Task AddUserToRoles(int userId, params string[] roles);

        Task AddUserToDefaultRoles(int userId);
    }
}
