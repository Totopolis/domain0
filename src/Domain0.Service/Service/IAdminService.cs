using System.Threading.Tasks;
using Domain0.Model;

namespace Domain0.Nancy.Service
{
    public interface IAdminService
    {
        Task<Application[]> GetByFilter(ApplicationFilter filter);

        Task<int> Insert(Application application);

        Task Update(Application application);

        Task DeleteApplication(int id);

        Task<Environment[]> GetByFilter(EnvironmentFilter filter);

        Task<int> Insert(Environment application);

        Task Update(Environment application);

        Task DeleteEnvironment(int id);

        Task<MessageTemplate[]> GetByFilter(MessageTemplateFilter filter);
        
        Task<int> Insert(MessageTemplate messageTemplate);

        Task Update(MessageTemplate messageTemplate);

        Task DeleteMessageTemplate(int id);


        Task<Permission[]> GetByFilter(PermissionFilter filter);

        Task<RolePermission[]> GetByFilter(RolePermissionFilter filter);

        Task<UserPermission[]> GetByFilter(UserPermissionFilter filter);

        Task<int> Insert(Permission permission);

        Task Update(Permission permission);

        Task DeletePermission(int id);


        Task<Role[]> GetByFilter(RoleFilter filter);

        Task<UserRole[]> GetByFilter(RoleUserFilter filter);

        Task<int> Insert(Role role);

        Task Update(Role role);

        Task DeleteRole(int id);

        Task AddRolePermissions(int roleId, int[] ids);

        Task RemoveRolePermissions(int roleId, int[] ids);


        Task AddUserPermissions(int userId, int[] ids);

        Task RemoveUserPermissions(int userId, int[] ids);

        Task AddUserRoles(int userId, int[] ids);

        Task RemoveUserRole(int userId, int[] ids);
    }
}