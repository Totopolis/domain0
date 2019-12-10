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

        Task AddUserToRoles(int userId, params string[] roles);

        Task AddUserToDefaultRoles(int userId);

        Task AddRolePermissions(int roleId, int[] ids);

        Task RemoveRolePermissions(int roleId, int[] ids);

        Task<Role[]> GetByRoleNames(params string[] roleNames);

        Task AddUserRoles(int userId, int[] ids);

        Task RemoveUserRole(int userId, int[] ids);

        Task<UserRole[]> FindByUserIds(IEnumerable<int> userIds);
    }
}
