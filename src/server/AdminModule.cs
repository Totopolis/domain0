using Domain0.Nancy.Infrastructure;
using Domain0.Model;
using Domain0.Nancy.Service;
using Nancy;
using Nancy.Swagger.Annotations.Attributes;
using NLog;
using Swagger.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain0.Service.Throttling;
using Nancy.Security;
using Domain0.Service.Tokens;

namespace Domain0.Nancy
{
    public sealed class AdminModule : NancyModule
    {
        public const string LoadApplicationsByFilterUrl = "api/admin/Application/ByFilter";
        public const string LoadApplicationUrl = "api/admin/Application/{id}";
        public const string CreateApplicationUrl = "api/admin/Application";
        public const string UpdateApplicationUrl = "api/admin/Application";
        public const string RemoveApplicationUrl = "api/admin/Application/{id}";

        public const string LoadMessageTemplatesByFilterUrl = "api/admin/MessageTemplate/ByFilter";
        public const string LoadMessageTemplateUrl = "api/admin/MessageTemplate/{id}";
        public const string CreateMessageTemplateUrl = "api/admin/MessageTemplate";
        public const string UpdateMessageTemplateUrl = "api/admin/MessageTemplate";
        public const string RemoveMessageTemplateUrl = "api/admin/MessageTemplate/{id}";

        public const string LoadPermissionsByFilterUrl = "api/admin/Permission/ByFilter";
        public const string LoadPermissionsByUserFilterUrl = "api/admin/Permission/ByUserFilter";
        public const string LoadPermissionsByRoleFilterUrl = "api/admin/Permission/ByRoleFilter";

        public const string LoadPermissionUrl = "api/admin/Permission/{id}";
        public const string CreatePermissionUrl = "api/admin/Permission";
        public const string UpdatePermissionUrl = "api/admin/Permission";
        public const string RemovePermissionUrl = "api/admin/Permission/{id}";


        public const string LoadRolesByFilterUrl = "api/admin/Role/ByFilter";
        public const string LoadRolesByUserFilterUrl = "api/admin/Role/ByUserFilter";
        public const string LoadRoleUrl = "api/admin/Role/{id}";
        public const string CreateRoleUrl = "api/admin/Role";
        public const string UpdateRoleUrl = "api/admin/Role";
        public const string RemoveRoleUrl = "api/admin/Role/{id}";
        public const string LoadRolePermissionsUrl = "api/admin/Role/{id}/Permissions";
        public const string AddRolePermissionsUrl = "api/admin/Role/{id}/Permissions";
        public const string RemoveRolePermissionsUrl = "api/admin/Role/{id}/Permissions";

        public const string AddUserPermissionsUrl = "api/admin/User/{id}/Permissions";
        public const string RemoveUserPermissionsUrl = "api/admin/User/{id}/Permissions";
        public const string AddUserRolesUrl = "api/admin/User/{id}/Roles";
        public const string RemoveUserRolesUrl = "api/admin/User/{id}/Roles";

        public AdminModule(
            IAdminService adminServiceInstance,
            ILogger loggerInstance,
            IRequestThrottleManager requestThrottleManagerInstance)
        {
            logger = loggerInstance;
            adminService = adminServiceInstance;
            requestThrottleManager = requestThrottleManagerInstance;

            this.RequiresAuthentication();
            this.RequiresClaims(c => 
                c.Type == TokenClaims.CLAIM_PERMISSIONS
                && c.Value.Contains(TokenClaims.CLAIM_PERMISSIONS_ADMIN));


            Post(LoadApplicationsByFilterUrl,
                ctx => LoadApplicationsByFilter(),
                name: nameof(LoadApplicationsByFilter));
            Get(LoadApplicationUrl,
                ctx => LoadApplication(),
                name: nameof(LoadApplication));
            Post(CreateApplicationUrl,
                ctx => CreateApplication(),
                name: nameof(CreateApplication));
            Put(UpdateApplicationUrl,
                ctx => UpdateApplication(),
                name: nameof(UpdateApplication));
            Delete(RemoveApplicationUrl,
                ctx => RemoveApplication(),
                name: nameof(RemoveApplication));


            Post(
                LoadMessageTemplatesByFilterUrl,
                ctx => LoadMessageTemplatesByFilter(),
                name: nameof(LoadMessageTemplatesByFilter));
            Get(LoadMessageTemplateUrl,
                ctx => LoadMessageTemplate(),
                name: nameof(LoadMessageTemplate));
            Post(CreateMessageTemplateUrl,
                ctx => CreateMessageTemplate(),
                name: nameof(CreateMessageTemplate));
            Put(UpdateMessageTemplateUrl,
                ctx => UpdateMessageTemplate(),
                name: nameof(UpdateMessageTemplate));
            Delete(RemoveMessageTemplateUrl,
                ctx => RemoveMessageTemplate(),
                name: nameof(RemoveMessageTemplate));


            Post(LoadPermissionsByUserFilterUrl,
                ctx => LoadPermissionsByUserFilter(),
                name: nameof(LoadPermissionsByUserFilter));
            Post(LoadPermissionsByRoleFilterUrl,
                ctx => LoadPermissionsByRoleFilter(),
                name: nameof(LoadPermissionsByRoleFilter));
            Post(LoadPermissionsByFilterUrl,
                ctx => LoadPermissionsByFilter(),
                name: nameof(LoadPermissionsByFilter));
            Get(LoadPermissionUrl,
                ctx => LoadPermission(),
                name: nameof(LoadPermission));
            Post(CreatePermissionUrl,
                ctx => CreatePermission(),
                name: nameof(CreatePermission));
            Put(UpdatePermissionUrl,
                ctx => UpdatePermission(),
                name: nameof(UpdatePermission));
            Delete(RemovePermissionUrl,
                ctx => RemovePermission(),
                name: nameof(RemovePermission));


            Post(LoadRolesByFilterUrl,
                ctx => LoadRolesByFilter(),
                name: nameof(LoadRolesByFilter));
            Post(LoadRolesByUserFilterUrl,
                ctx => LoadRolesByUserFilter(),
                name: nameof(LoadRolesByUserFilter));
            Get(LoadRoleUrl,
                ctx => LoadRole(),
                name: nameof(LoadRole));
            Post(CreateRoleUrl,
                ctx => CreateRole(),
                name: nameof(CreateRole));
            Put(UpdateRoleUrl,
                ctx => UpdateRole(),
                name: nameof(UpdateRole));
            Delete(RemoveRoleUrl,
                ctx => RemoveRole(),
                name: nameof(RemoveRole));

            Post(LoadRolePermissionsUrl,
                ctx => LoadRolePermissions(),
                name: nameof(LoadRolePermissions));
            Put(AddRolePermissionsUrl,
                ctx => AddRolePermissions(),
                name: nameof(AddRolePermissions));
            Delete(RemoveRolePermissionsUrl,
                ctx => RemoveRolePermissions(),
                name: nameof(RemoveRolePermissions));


            Put(AddUserPermissionsUrl,
                ctx => AddUserPermissions(),
                name: nameof(AddUserPermissions));
            Delete(RemoveUserPermissionsUrl,
                ctx => RemoveUserPermissions(),
                name: nameof(RemoveUserPermissions));

            Put(AddUserRolesUrl,
                ctx => AddUserRoles(),
                name: nameof(AddUserRoles));
            Delete(RemoveUserRolesUrl,
                ctx => RemoveUserRoles(),
                name: nameof(RemoveUserRoles));
        }

        #region Application
        [Route(nameof(LoadApplicationsByFilter))]
        [Route(HttpMethod.Post, LoadApplicationsByFilterUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for receive Applications by filter")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "applicationFilter",
            ParamType = typeof(ApplicationFilter),
            Required = true,
            Description = "Applications filter")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(IEnumerable<Application>))]
        public async Task<object> LoadApplicationsByFilter()
        {
            var filter = this.BindAndValidateModel<ApplicationFilter>();
            return await adminService.GetByFilter(filter);
        }

        [Route(nameof(LoadApplication))]
        [Route(HttpMethod.Get, LoadApplicationUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for get Application")]
        [RouteParam(
            ParamIn = ParameterIn.Path,
            Name = "id",
            ParamType = typeof(int),
            Required = true,
            Description = "Application id")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(IEnumerable<Application>))]
        public async Task<object> LoadApplication()
        {
            var id = Context.Parameters.id;
            return await adminService.GetByFilter(new ApplicationFilter(id));
        }

        [Route(nameof(CreateApplication))]
        [Route(HttpMethod.Post, CreateApplicationUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for create Application")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "application",
            ParamType = typeof(Application),
            Required = true,
            Description = "Create Application")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(int))]
        public async Task<object> CreateApplication()
        {
            var application = this.BindAndValidateModel<Application>();
            var id = await adminService.Insert(application);
            return Response.AsJson(id, HttpStatusCode.OK);
        }

        [Route(nameof(UpdateApplication))]
        [Route(HttpMethod.Put, UpdateApplicationUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for update Application")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "application",
            ParamType = typeof(Application),
            Required = true,
            Description = "Update Application")]
        [SwaggerResponse(HttpStatusCode.NoContent, Message = "Success")]
        public async Task<object> UpdateApplication()
        {
            var application = this.BindAndValidateModel<Application>();
            await adminService.Update(application);
            return HttpStatusCode.NoContent;
        }

        [Route(nameof(RemoveApplication))]
        [Route(HttpMethod.Delete, RemoveApplicationUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for delete Application by id")]
        [RouteParam(
            ParamIn = ParameterIn.Path,
            Name = "id",
            ParamType = typeof(int),
            Required = true,
            Description = "Delete Application")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success")]
        public async Task<object> RemoveApplication()
        {
            var id = Context.Parameters.id;
            await adminService.DeleteApplication(id);
            return HttpStatusCode.NoContent;

        }

        #endregion

        #region MessageTemplate
        [Route(nameof(LoadMessageTemplatesByFilter))]
        [Route(HttpMethod.Post, LoadMessageTemplatesByFilterUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for receive MessageTemplates by filter")]
        [RouteParam(
            ParamIn = ParameterIn.Body, 
            Name = "messageTemplateFilter", 
            ParamType = typeof(MessageTemplateFilter), 
            Required = true, 
            Description = "Message template filter")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(IEnumerable<MessageTemplate>))]
        public async Task<object> LoadMessageTemplatesByFilter()
        {
            var filter = this.BindAndValidateModel<MessageTemplateFilter>();
            return await adminService.GetByFilter(filter);
        }

        [Route(nameof(LoadMessageTemplate))]
        [Route(HttpMethod.Get, LoadMessageTemplateUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for get MessageTemplate")]
        [RouteParam(
            ParamIn = ParameterIn.Path,
            Name = "id",
            ParamType = typeof(int),
            Required = true,
            Description = "message template id")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(IEnumerable<MessageTemplate>))]
        public async Task<object> LoadMessageTemplate()
        {
            var id = Context.Parameters.id;
            return await adminService.GetByFilter(new MessageTemplateFilter
            {
                MessageTemplatesIds = new List<int> { id }
            });
        }

        [Route(nameof(CreateMessageTemplate))]
        [Route(HttpMethod.Post, CreateMessageTemplateUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for create MessageTemplate")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "messageTemplate",
            ParamType = typeof(MessageTemplate),
            Required = true,
            Description = "Create message template")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(int))]
        public async Task<object> CreateMessageTemplate()
        {
            var messageTemplate = this.BindAndValidateModel<MessageTemplate>();
            var id  = await adminService.Insert(messageTemplate);
            return Response.AsJson(id, HttpStatusCode.OK);
        }

        [Route(nameof(UpdateMessageTemplate))]
        [Route(HttpMethod.Put, UpdateMessageTemplateUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for update MessageTemplate")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "messageTemplate",
            ParamType = typeof(MessageTemplate),
            Required = true,
            Description = "Update message template")]
        [SwaggerResponse(HttpStatusCode.NoContent, Message = "Success")]
        public async Task<object> UpdateMessageTemplate()
        {
            var messageTemplate = this.BindAndValidateModel<MessageTemplate>();
            await adminService.Update(messageTemplate);
            return HttpStatusCode.NoContent;
        }

        [Route(nameof(RemoveMessageTemplate))]
        [Route(HttpMethod.Delete, RemoveMessageTemplateUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for delete MessageTemplate by id")]
        [RouteParam(
            ParamIn = ParameterIn.Path,
            Name = "id",
            ParamType = typeof(int),
            Required = true,
            Description = "Delete message template")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success")]
        public async Task<object> RemoveMessageTemplate()
        {
            var id = Context.Parameters.id;
            await adminService.DeleteMessageTemplate(id);
            return HttpStatusCode.NoContent;
        }
        #endregion

        #region Permission
        [Route(nameof(LoadPermissionsByUserFilter))]
        [Route(HttpMethod.Post, LoadPermissionsByUserFilterUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for receive Permissions by filter")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "permissionFilter",
            ParamType = typeof(UserPermissionFilter),
            Required = true,
            Description = "Permissions filter")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(IEnumerable<UserPermission>))]
        public async Task<object> LoadPermissionsByUserFilter()
        {
            var filter = this.BindAndValidateModel<UserPermissionFilter>();
            return await adminService.GetByFilter(filter);
        }

        [Route(nameof(LoadPermissionsByRoleFilter))]
        [Route(HttpMethod.Post, LoadPermissionsByRoleFilterUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for receive Permissions by filter")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "permissionFilter",
            ParamType = typeof(RolePermissionFilter),
            Required = true,
            Description = "Permissions filter")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(IEnumerable<RolePermission>))]
        public async Task<object> LoadPermissionsByRoleFilter()
        {
            var filter = this.BindAndValidateModel<RolePermissionFilter>();
            return await adminService.GetByFilter(filter);
        }

        [Route(nameof(LoadPermissionsByFilter))]
        [Route(HttpMethod.Post, LoadPermissionsByFilterUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for receive Permissions by filter")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "permissionFilter",
            ParamType = typeof(PermissionFilter),
            Required = true,
            Description = "Permissions filter")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(IEnumerable<Permission>))]
        public async Task<object> LoadPermissionsByFilter()
        {
            var filter = this.BindAndValidateModel<PermissionFilter>();
            return await adminService.GetByFilter(filter);
        }

        [Route(nameof(LoadPermission))]
        [Route(HttpMethod.Get, LoadPermissionUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for get Permission")]
        [RouteParam(
            ParamIn = ParameterIn.Path,
            Name = "id",
            ParamType = typeof(int),
            Required = true,
            Description = "Permission id")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(int))]
        public async Task<object> LoadPermission()
        {
            var id = Context.Parameters.id;
            return await adminService.GetByFilter(new PermissionFilter(id));
        }

        [Route(nameof(CreatePermission))]
        [Route(HttpMethod.Post, CreatePermissionUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for create Permission")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "permission",
            ParamType = typeof(Permission),
            Required = true,
            Description = "Create Permission")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(int))]
        public async Task<object> CreatePermission()
        {
            var permission = this.BindAndValidateModel<Permission>();
            var id = await adminService.Insert(permission);
            return Response.AsJson(id, HttpStatusCode.OK);
        }

        [Route(nameof(UpdatePermission))]
        [Route(HttpMethod.Put, UpdatePermissionUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for update Permission")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "permission",
            ParamType = typeof(Permission),
            Required = true,
            Description = "Update Permission")]
        [SwaggerResponse(HttpStatusCode.NoContent, Message = "Success")]
        public async Task<object> UpdatePermission()
        {
            var permission = this.BindAndValidateModel<Permission>();
            await adminService.Update(permission);
            return HttpStatusCode.NoContent;
        }

        [Route(nameof(RemovePermission))]
        [Route(HttpMethod.Delete, RemovePermissionUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for delete Permission by id")]
        [RouteParam(
            ParamIn = ParameterIn.Path,
            Name = "id",
            ParamType = typeof(int),
            Required = true,
            Description = "Delete Permission")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success")]
        public async Task<object> RemovePermission()
        {
            var id = Context.Parameters.id;
            await adminService.DeletePermission(id);
            return HttpStatusCode.NoContent;
        }

        #endregion

        #region Role
        [Route(nameof(LoadRolesByFilter))]
        [Route(HttpMethod.Post, LoadRolesByFilterUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for receive Roles by filter")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "roleFilter",
            ParamType = typeof(RoleFilter),
            Required = true,
            Description = "Roles filter")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(IEnumerable<Role>))]
        public async Task<object> LoadRolesByFilter()
        {
            var filter = this.BindAndValidateModel<RoleFilter>();
            return await adminService.GetByFilter(filter);
        }

        [Route(nameof(LoadRolesByUserFilter))]
        [Route(HttpMethod.Post, LoadRolesByUserFilterUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for receive Roles by user filter")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "roleUserFilter",
            ParamType = typeof(RoleUserFilter),
            Required = true,
            Description = "Roles filter by user")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(IEnumerable<UserRole>))]
        public async Task<object> LoadRolesByUserFilter()
        {
            var filter = this.BindAndValidateModel<RoleUserFilter>();
            return await adminService.GetByFilter(filter);
        }

        [Route(nameof(LoadRole))]
        [Route(HttpMethod.Get, LoadRoleUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for get Role")]
        [RouteParam(
            ParamIn = ParameterIn.Path,
            Name = "id",
            ParamType = typeof(int),
            Required = true,
            Description = "Role id")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(int))]
        public async Task<object> LoadRole()
        {
            var id = Context.Parameters.id;
            return await adminService.GetByFilter(new RoleFilter(id));
        }

        [Route(nameof(CreateRole))]
        [Route(HttpMethod.Put, CreateRoleUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for create Role")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "role",
            ParamType = typeof(Role),
            Required = true,
            Description = "Create Role")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(int))]
        public async Task<object> CreateRole()
        {
            var role = this.BindAndValidateModel<Role>();
            var id = await adminService.Insert(role);
            return Response.AsJson(id, HttpStatusCode.OK);
        }

        [Route(nameof(UpdateRole))]
        [Route(HttpMethod.Post, UpdateRoleUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for update Role")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "role",
            ParamType = typeof(Role),
            Required = true,
            Description = "Update Role")]
        [SwaggerResponse(HttpStatusCode.NoContent, Message = "Success")]
        public async Task<object> UpdateRole()
        {
            var role = this.BindAndValidateModel<Role>();
            await adminService.Update(role);
            return HttpStatusCode.NoContent;
        }

        [Route(nameof(RemoveRole))]
        [Route(HttpMethod.Delete, RemoveRoleUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for delete Role by id")]
        [RouteParam(
            ParamIn = ParameterIn.Path,
            Name = "id",
            ParamType = typeof(int),
            Required = true,
            Description = "Delete Role")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success")]
        public async Task<object> RemoveRole()
        {
            var id = Context.Parameters.id;
            await adminService.DeleteRole(id);
            return HttpStatusCode.NoContent;
        }

        [Route(nameof(LoadRolePermissions))]
        [Route(HttpMethod.Post, LoadRolePermissionsUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for receive Role Permissions")]
        [RouteParam(
            ParamIn = ParameterIn.Path,
            Name = "id",
            ParamType = typeof(int),
            Required = true,
            Description = "Role id")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(IEnumerable<Permission>))]
        public async Task<object> LoadRolePermissions()
        {
            var id = Context.Parameters.id;
            return await adminService.GetByFilter(new RolePermissionFilter(id));
        }

        [Route(nameof(AddRolePermissions))]
        [Route(HttpMethod.Put, AddRolePermissionsUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for add role permissions")]
        [RouteParam(
            ParamIn = ParameterIn.Path,
            Name = "id",
            ParamType = typeof(int),
            Required = true,
            Description = "role id to add to")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "ids",
            ParamType = typeof(IdArrayRequest),
            Required = true,
            Description = "permission ids to add")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success")]
        public async Task<object> AddRolePermissions()
        {
            var roleId = Context.Parameters.id;
            var ids = this.BindAndValidateModel<IdArrayRequest>().Ids.ToArray();
            await adminService.AddRolePermissions(roleId, ids);
            return HttpStatusCode.NoContent;
        }

        [Route(nameof(RemoveRolePermissions))]
        [Route(HttpMethod.Delete, RemoveRolePermissionsUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for remove role permissions")]
        [RouteParam(
            ParamIn = ParameterIn.Path,
            Name = "id",
            ParamType = typeof(int),
            Required = true,
            Description = "role id to remove from")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "ids",
            ParamType = typeof(IdArrayRequest),
            Required = true,
            Description = "permission ids to remove")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success")]
        public async Task<object> RemoveRolePermissions()
        {
            var roleId = Context.Parameters.id;
            var ids = this.BindAndValidateModel<IdArrayRequest>().Ids.ToArray();
            await adminService.RemoveRolePermissions(roleId, ids);
            return HttpStatusCode.NoContent;
        }

        #endregion

        #region User

        [Route(nameof(AddUserPermissions))]
        [Route(HttpMethod.Put, AddUserPermissionsUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for add user permissions")]
        [RouteParam(
            ParamIn = ParameterIn.Path,
            Name = "id",
            ParamType = typeof(int),
            Required = true,
            Description = "user id to add to")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "ids",
            ParamType = typeof(IdArrayRequest),
            Required = true,
            Description = "permission ids to add")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success")]
        public async Task<object> AddUserPermissions()
        {
            var userId = Context.Parameters.id;
            var ids = this.BindAndValidateModel<IdArrayRequest>().Ids.ToArray();
            await adminService.AddUserPermissions(userId, ids);
            return HttpStatusCode.NoContent;
        }

        [Route(nameof(RemoveUserPermissions))]
        [Route(HttpMethod.Delete, RemoveUserPermissionsUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for remove user permissions")]
        [RouteParam(
            ParamIn = ParameterIn.Path,
            Name = "id",
            ParamType = typeof(int),
            Required = true,
            Description = "user id to remove from")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "ids",
            ParamType = typeof(IdArrayRequest),
            Required = true,
            Description = "permission ids to remove")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success")]
        public async Task<object> RemoveUserPermissions()
        {
            var userId = Context.Parameters.id;
            var ids = this.BindAndValidateModel<IdArrayRequest>().Ids.ToArray();
            await adminService.RemoveUserPermissions(userId, ids);
            return HttpStatusCode.NoContent;
        }


        [Route(nameof(AddUserRoles))]
        [Route(HttpMethod.Put, AddUserRolesUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for add user roles")]
        [RouteParam(
            ParamIn = ParameterIn.Path,
            Name = "id",
            ParamType = typeof(int),
            Required = true,
            Description = "user id to add to")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "ids",
            ParamType = typeof(IdArrayRequest),
            Required = true,
            Description = "permission ids to add")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success")]
        public async Task<object> AddUserRoles()
        {
            var userId = Context.Parameters.id;
            var ids = this.BindAndValidateModel<IdArrayRequest>().Ids.ToArray();
            await adminService.AddUserRoles(userId, ids);
            return HttpStatusCode.NoContent;
        }

        [Route(nameof(RemoveUserRoles))]
        [Route(HttpMethod.Delete, RemoveUserRolesUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for remove user permissions")]
        [RouteParam(
            ParamIn = ParameterIn.Path,
            Name = "id",
            ParamType = typeof(int),
            Required = true,
            Description = "user id to remove from")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "ids",
            ParamType = typeof(IdArrayRequest),
            Required = true,
            Description = "permission ids to remove")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success")]
        public async Task<object> RemoveUserRoles()
        {
            var userId = Context.Parameters.id;
            var ids = this.BindAndValidateModel<IdArrayRequest>().Ids.ToArray();
            await adminService.RemoveUserRole(userId, ids);
            return HttpStatusCode.NoContent;
        }
        #endregion

        private readonly ILogger logger;
        private readonly IAdminService adminService;
        private readonly IRequestThrottleManager requestThrottleManager;
    }
}