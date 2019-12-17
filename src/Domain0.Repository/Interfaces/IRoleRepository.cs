using Domain0.Repository.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain0.Repository
{
    public interface IRoleRepository
    {
        Task<int> Insert(Role entity);

        Task Update(Role entity);

        Task Delete(int id);

        Task<Role[]> FindByIds(IEnumerable<int> ids);

        Task<Role[]> GetByRoleNames(params string[] roleNames);

        Task AddUserToRoles(int userId, params string[] roles);

        Task AddUserToDefaultRoles(int userId);

        Task AddUserRoles(int userId, int[] ids);

        Task RemoveUserRole(int userId, int[] ids);

        Task<UserRole[]> FindByUserIds(IEnumerable<int> userIds);
    }
}
