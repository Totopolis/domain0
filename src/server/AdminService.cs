using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Domain0.Model;
using Domain0.Repository;

namespace Domain0.Nancy.Service
{
    public class AdminService : IAdminService
    {
        public AdminService(
            IApplicationRepository applicationRepositoryInstance,
            IEnvironmentRepository environmentRepositoryInstance,
            IMapper mapperInstance,
            IMessageTemplateRepository messageTemplateRepositoryInstance,
            IPermissionRepository permissionRepositoryInstance,
            IRoleRepository roleRepositoryInstance)
        {
            applicationRepository = applicationRepositoryInstance;
            environmentRepository = environmentRepositoryInstance;
            mapper = mapperInstance;
            messageTemplateRepository = messageTemplateRepositoryInstance;
            permissionRepository = permissionRepositoryInstance;
            roleRepository = roleRepositoryInstance;
        }

        #region MessageTemplate
        public async Task<MessageTemplate[]> GetByFilter(MessageTemplateFilter filter)
        {
            var messageTemplates = await messageTemplateRepository.FindByIds(filter.MessageTemplatesIds);

            return mapper.Map<MessageTemplate[]>(messageTemplates);
        }

        public async Task<int> Insert(MessageTemplate messageTemplate)
        {
            var messageTemplateEntity = mapper.Map<Repository.Model.MessageTemplate>(messageTemplate);

            return (int)await messageTemplateRepository.Insert(messageTemplateEntity);
        }

        public async Task Update(MessageTemplate messageTemplate)
        {
            var messageTemplateEntity = mapper.Map<Repository.Model.MessageTemplate>(messageTemplate);

            await messageTemplateRepository.Update(messageTemplateEntity);
        }

        public async Task DeleteMessageTemplate(int id)
        {
            await messageTemplateRepository.Delete(id);
        }
        #endregion

        #region Permission
        public async Task<Permission[]> GetByFilter(PermissionFilter filter)
        {
            var permissions = await permissionRepository.FindByIds(filter.PermissionIds);

            return mapper.Map<Permission[]>(permissions);
        }

        public async Task<RolePermission[]> GetByFilter(RolePermissionFilter filter)
        {
            var permissions = await permissionRepository.FindRolePermissionsByRoleIds(filter.RoleIds);

            return mapper.Map<RolePermission[]>(permissions);
        }

        public async Task<UserPermission[]> GetByFilter(UserPermissionFilter filter)
        {
            var permissions = await permissionRepository.FindUserPermissionsByUserIds(filter.UserIds);

            return mapper.Map<UserPermission[]>(permissions);
        }


        public async Task<int> Insert(Permission permission)
        {
            var permissionEntity = mapper.Map<Repository.Model.Permission>(permission);

            return (int)await permissionRepository.Insert(permissionEntity);
        }

        public async Task Update(Permission permission)
        {
            var permissionEntity = mapper.Map<Repository.Model.Permission>(permission);

            await permissionRepository.Update(permissionEntity);
        }

        public async Task DeletePermission(int id)
        {
            await permissionRepository.Delete(id);
        }
        #endregion

        #region Application
        public async Task<Application[]> GetByFilter(ApplicationFilter filter)
        {
            var applications = await applicationRepository.FindByIds(filter.ApplicationIds);

            return mapper.Map<Application[]>(applications);
        }

        public async Task<int> Insert(Application application)
        {
            var applicationEntity = mapper.Map<Repository.Model.Application>(application);

            return (int)await applicationRepository.Insert(applicationEntity);
        }

        public async Task Update(Application application)
        {
            var applicationEntity = mapper.Map<Repository.Model.Application>(application);

            await applicationRepository.Update(applicationEntity);
        }

        public async Task DeleteApplication(int id)
        {
            await applicationRepository.Delete(id);
        }
        #endregion

        #region Role
        public async Task<Role[]> GetByFilter(RoleFilter filter)
        {
            var roles = await roleRepository.FindByIds(filter.RoleIds);

            return mapper.Map<Role[]>(roles);
        }

        public async Task<UserRole[]> GetByFilter(RoleUserFilter filter)
        {
            var roles = await roleRepository.FindByUserIds(filter.UserIds);

            return mapper.Map<UserRole[]>(roles);
        }

        public async Task<int> Insert(Role role)
        {
            var roleEntity = mapper.Map<Repository.Model.Role>(role);

            return (int)await roleRepository.Insert(roleEntity);
        }

        public async Task Update(Role role)
        {
            var roleEntity = mapper.Map<Repository.Model.Role>(role);

            await roleRepository.Update(roleEntity);
        }

        public async Task DeleteRole(int id)
        {
            await roleRepository.Delete(id);
        }

        public async Task AddRolePermissions(int roleId, int[] ids)
        {
            await roleRepository.AddRolePermissions(roleId, ids);
        }

        public async Task RemoveRolePermissions(int roleId, int[] ids)
        {
            await roleRepository.RemoveRolePermissions(roleId, ids);
        }

        #endregion

        #region User
        public async Task AddUserPermissions(int userId, int[] ids)
        {
            await permissionRepository.AddUserPermission(userId, ids);
        }

        public async Task RemoveUserPermissions(int userId, int[] ids)
        {
            await permissionRepository.RemoveUserPermissions(userId, ids);
        }

        public async Task AddUserRoles(int userId, int[] ids)
        {
            await roleRepository.AddUserRoles(userId, ids);
        }

        public async Task RemoveUserRole(int userId, int[] ids)
        {
            await roleRepository.RemoveUserRole(userId, ids);
        }
        #endregion

        #region Environment
        public async Task<Environment[]> GetByFilter(EnvironmentFilter filter)
        {
            if (filter.LoadAll.GetValueOrDefault())
            {
                var environments = await environmentRepository.FindByIds(Enumerable.Empty<int>());
                return mapper.Map<Environment[]>(environments);
            }

            if (filter.Ids.Any())
            {
                var environments = await environmentRepository.FindByIds(filter.Ids);
                return mapper.Map<Environment[]>(environments);
            }

            return new Environment[0];
        }

        public async Task<int> Insert(Environment environment)
        {
            var environmentEntity = mapper.Map<Repository.Model.Environment>(environment);

            return (int)await environmentRepository.Insert(environmentEntity);
        }

        public async Task Update(Environment environment)
        {
            var environmentEntity = mapper.Map<Repository.Model.Environment>(environment);

            await environmentRepository.Update(environmentEntity);
        }

        public async Task DeleteEnvironment(int id)
        {
            await environmentRepository.Delete(id);
        }
        #endregion

        private readonly IApplicationRepository applicationRepository;
        private readonly IEnvironmentRepository environmentRepository;
        private readonly IMapper mapper;
        private readonly IMessageTemplateRepository messageTemplateRepository;
        private readonly IPermissionRepository permissionRepository;
        private readonly IRoleRepository roleRepository;
    }
}
