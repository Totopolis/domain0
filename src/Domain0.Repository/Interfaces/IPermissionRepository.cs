using System.Collections.Generic;
using Domain0.Repository.Model;
using System.Threading.Tasks;

namespace Domain0.Repository
{
    public interface IPermissionRepository
    {
        Task<int> Insert(Permission entity);

        Task Update(Permission entity);

        Task Delete(int id);

        Task<Permission[]> FindByIds(IEnumerable<int> ids);


        Task AddUserPermission(int userId, int[] ids);

        Task RemoveUserPermissions(int userId, int[] ids);

        Task<Permission[]> GetByUserId(int userId);
        
        Task<UserPermission[]> FindUserPermissionsByUserIds(List<int> ids);


        Task<Permission[]> GetByRoleId(int roleId);

        Task AddRolePermissions(int roleId, int[] ids);

        Task RemoveRolePermissions(int roleId, int[] ids);

        Task<RolePermission[]> FindRolePermissionsByRoleIds(List<int> ids);
    }
}
