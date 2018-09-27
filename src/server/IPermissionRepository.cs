using Domain0.Repository.Model;
using System.Threading.Tasks;

namespace Domain0.Repository
{
    public interface IPermissionRepository : IRepository<int, Permission>
    {
        Task<Permission[]> GetByUserId(int userId);

        Task<Permission[]> GetByRoleId(int userId);

        Task<Permission[]> FindByFilter(Domain0.Model.PermissionFilter filter);

        Task<UserPermission[]> FindByFilter(Domain0.Model.UserPermissionFilter filter);

        Task<RolePermission[]> FindByFilter(Domain0.Model.RolePermissionFilter filter);

        Task AddUserPermission(int userId, int[] ids);

        Task RemoveUserPermissions(int userId, int[] ids);
    }
}
