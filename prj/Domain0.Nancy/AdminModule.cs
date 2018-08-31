using Domain0.Nancy.Infrastructure;
using Domain0.Model;
using Domain0.Nancy.Service;
using Nancy;
using Nancy.Swagger.Annotations.Attributes;
using NLog;
using Swagger.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain0.Nancy
{
    public class AdminModule : NancyModule
    {
        public const string LoadApplicationsByFilterUri = "api/admin/Application/ByFilter";
        public const string LoadApplicationUri = "api/admin/Application/{id}";
        public const string CreateApplicationUri = "api/admin/Application";
        public const string UpdateApplicationUri = "api/admin/Application";
        public const string RemoveApplicationUri = "api/admin/Application/{id}";

        public const string LoadMessageTemplatesByFilterUri = "api/admin/MessageTemplate/ByFilter";
        public const string LoadMessageTemplateUri = "api/admin/MessageTemplate/{id}";
        public const string CreateMessageTemplateUri = "api/admin/MessageTemplate";
        public const string UpdateMessageTemplateUri = "api/admin/MessageTemplate";
        public const string RemoveMessageTemplateUri = "api/admin/MessageTemplate/{id}";

        public const string LoadPermissionsByFilterUri = "api/admin/Permission/ByFilter";
        public const string LoadPermissionUri = "api/admin/Permission/{id}";
        public const string CreatePermissionUri = "api/admin/Permission";
        public const string UpdatePermissionUri = "api/admin/Permission";
        public const string RemovePermissionUri = "api/admin/Permission/{id}";


        public const string LoadRolesByFilterUri = "api/admin/Role/ByFilter";
        public const string LoadRoleUri = "api/admin/Role/{id}";
        public const string CreateRoleUri = "api/admin/Role";
        public const string UpdateRoleUri = "api/admin/Role";
        public const string RemoveRoleUri = "api/admin/Role/{id}";
        public const string LoadRolePermissionsUri = "api/admin/Role/{id}/Permissions";
        public const string AddRolePermissionsUri = "api/admin/Role/{id}/Permissions";
        public const string RemoveRolePermissionsUri = "api/admin/Role/{id}/Permissions";


        public AdminModule(
            ILogger loggerInstance,
            IAdminService adminServiceInstance)
        {
            logger = loggerInstance;
            adminService = adminServiceInstance;

            Post(LoadApplicationsByFilterUri,
                ctx => LoadApplicationsByFilter(),
                name: nameof(LoadApplicationsByFilterUri));
            Get(LoadApplicationUri,
                ctx => LoadApplication(),
                name: nameof(LoadApplicationUri));
            Put(CreateApplicationUri,
                ctx => CreateApplication(),
                name: nameof(CreateApplicationUri));
            Post(UpdateApplicationUri,
                ctx => UpdateApplication(),
                name: nameof(UpdateApplicationUri));
            Delete(RemoveApplicationUri,
                ctx => RemoveApplication(),
                name: nameof(RemoveApplicationUri));


            Post(
                LoadMessageTemplatesByFilterUri,
                ctx => LoadMessageTemplatesByFilter(),
                name: nameof(LoadMessageTemplatesByFilterUri));
            Get(LoadMessageTemplateUri,
                ctx => LoadMessageTemplate(),
                name: nameof(LoadMessageTemplateUri));
            Put(CreateMessageTemplateUri,
                ctx => CreateMessageTemplate(),
                name: nameof(CreateMessageTemplateUri));
            Post(UpdateMessageTemplateUri,
                ctx => UpdateMessageTemplate(),
                name: nameof(UpdateMessageTemplateUri));
            Delete(RemoveMessageTemplateUri,
                ctx => RemoveMessageTemplate(),
                name: nameof(RemoveMessageTemplateUri));


            Post(LoadPermissionsByFilterUri,
                ctx => LoadPermissionsByFilter(),
                name: nameof(LoadPermissionsByFilterUri));
            Get(LoadPermissionUri,
                ctx => LoadPermission(),
                name: nameof(LoadPermissionUri));
            Put(CreatePermissionUri,
                ctx => CreatePermission(),
                name: nameof(CreatePermissionUri));
            Post(UpdatePermissionUri,
                ctx => UpdatePermission(),
                name: nameof(UpdatePermissionUri));
            Delete(RemovePermissionUri,
                ctx => RemovePermission(),
                name: nameof(RemovePermissionUri));


            Post(LoadRolesByFilterUri,
                ctx => LoadRolesByFilter(),
                name: nameof(LoadRolesByFilterUri));
            Get(LoadRoleUri,
                ctx => LoadRole(),
                name: nameof(LoadRoleUri));
            Put(CreateRoleUri,
                ctx => CreateRole(),
                name: nameof(CreateRoleUri));
            Post(UpdateRoleUri,
                ctx => UpdateRole(),
                name: nameof(UpdateRoleUri));
            Delete(RemoveRoleUri,
                ctx => RemoveRole(),
                name: nameof(RemoveRoleUri));

            Post(LoadRolePermissionsUri,
                ctx => LoadRolePermissions(),
                name: nameof(LoadRolePermissionsUri));
            Put(AddRolePermissionsUri,
                ctx => AddRolePermissions(),
                name: nameof(AddRolePermissionsUri));
            Delete(RemoveRolePermissionsUri,
                ctx => RemoveRolePermissions(),
                name: nameof(RemoveRolePermissionsUri));
        }

        #region Application
        [Route(nameof(LoadApplicationsByFilterUri))]
        [Route(HttpMethod.Post, LoadApplicationsByFilterUri)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for receive Applications by filter")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "request",
            ParamType = typeof(ApplicationFilter),
            Required = true,
            Description = "Applications filter")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(IEnumerable<Application>))]
        public async Task<object> LoadApplicationsByFilter()
        {
            var filter = this.BindAndValidateModel<ApplicationFilter>();
            return await adminService.GetByFilter(filter);
        }

        [Route(nameof(LoadApplicationUri))]
        [Route(HttpMethod.Get, LoadApplicationUri)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for get Application")]
        [RouteParam(
            ParamIn = ParameterIn.Path,
            Name = "id",
            ParamType = typeof(int),
            Required = true,
            Description = "Application id")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(int))]
        public async Task<object> LoadApplication()
        {
            var id = Context.Parameters.id;
            return await adminService.GetByFilter(new ApplicationFilter(id));
        }

        [Route(nameof(CreateApplicationUri))]
        [Route(HttpMethod.Put, CreateApplicationUri)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for create Application")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "request",
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

        [Route(nameof(UpdateApplicationUri))]
        [Route(HttpMethod.Post, UpdateApplicationUri)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for update Application")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "request",
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

        [Route(nameof(RemoveApplicationUri))]
        [Route(HttpMethod.Delete, RemoveApplicationUri)]
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
        [Route(nameof(LoadMessageTemplatesByFilterUri))]
        [Route(HttpMethod.Post, LoadMessageTemplatesByFilterUri)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for receive MessageTemplates by filter")]
        [RouteParam(
            ParamIn = ParameterIn.Body, 
            Name = "request", 
            ParamType = typeof(MessageTemplateFilter), 
            Required = true, 
            Description = "Message template filter")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(IEnumerable<MessageTemplate>))]
        public async Task<object> LoadMessageTemplatesByFilter()
        {
            var filter = this.BindAndValidateModel<MessageTemplateFilter>();
            return await adminService.GetByFilter(filter);
        }

        [Route(nameof(LoadMessageTemplateUri))]
        [Route(HttpMethod.Get, LoadMessageTemplateUri)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for get MessageTemplate")]
        [RouteParam(
            ParamIn = ParameterIn.Path,
            Name = "id",
            ParamType = typeof(int),
            Required = true,
            Description = "message template id")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(int))]
        public async Task<object> LoadMessageTemplate()
        {
            var id = Context.Parameters.id;
            return await adminService.GetByFilter(new MessageTemplateFilter
            {
                MessageTemplatesIds = new List<int> { id }
            });
        }

        [Route(nameof(CreateMessageTemplateUri))]
        [Route(HttpMethod.Put, CreateMessageTemplateUri)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for create MessageTemplate")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "request",
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

        [Route(nameof(UpdateMessageTemplateUri))]
        [Route(HttpMethod.Post, UpdateMessageTemplateUri)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for update MessageTemplate")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "request",
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

        [Route(nameof(RemoveMessageTemplateUri))]
        [Route(HttpMethod.Delete, RemoveMessageTemplateUri)]
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
        [Route(nameof(LoadPermissionsByFilterUri))]
        [Route(HttpMethod.Post, LoadPermissionsByFilterUri)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for receive Permissions by filter")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "request",
            ParamType = typeof(PermissionFilter),
            Required = true,
            Description = "Permissions filter")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(IEnumerable<Permission>))]
        public async Task<object> LoadPermissionsByFilter()
        {
            var filter = this.BindAndValidateModel<PermissionFilter>();
            return await adminService.GetByFilter(filter);
        }

        [Route(nameof(LoadPermissionUri))]
        [Route(HttpMethod.Get, LoadPermissionUri)]
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

        [Route(nameof(CreatePermissionUri))]
        [Route(HttpMethod.Put, CreatePermissionUri)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for create Permission")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "request",
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

        [Route(nameof(UpdatePermissionUri))]
        [Route(HttpMethod.Post, UpdatePermissionUri)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for update Permission")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "request",
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

        [Route(nameof(RemovePermissionUri))]
        [Route(HttpMethod.Delete, RemovePermissionUri)]
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
        [Route(nameof(LoadRolesByFilterUri))]
        [Route(HttpMethod.Post, LoadRolesByFilterUri)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for receive Roles by filter")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "request",
            ParamType = typeof(RoleFilter),
            Required = true,
            Description = "Roles filter")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(IEnumerable<Role>))]
        public async Task<object> LoadRolesByFilter()
        {
            var filter = this.BindAndValidateModel<RoleFilter>();
            return await adminService.GetByFilter(filter);
        }

        [Route(nameof(LoadRoleUri))]
        [Route(HttpMethod.Get, LoadRoleUri)]
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

        [Route(nameof(CreateRoleUri))]
        [Route(HttpMethod.Put, CreateRoleUri)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for create Role")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "request",
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

        [Route(nameof(UpdateRoleUri))]
        [Route(HttpMethod.Post, UpdateRoleUri)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for update Role")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "request",
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

        [Route(nameof(RemoveRoleUri))]
        [Route(HttpMethod.Delete, RemoveRoleUri)]
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

        [Route(nameof(LoadRolePermissionsUri))]
        [Route(HttpMethod.Post, LoadRolePermissionsUri)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for receive Role Permissions")]
        [RouteParam(
            ParamIn = ParameterIn.Path,
            Name = "request",
            ParamType = typeof(int),
            Required = true,
            Description = "Role id")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(IEnumerable<Permission>))]
        public async Task<object> LoadRolePermissions()
        {
            var id = Context.Parameters.id;
            return await adminService.GetByFilter(new PermissionFilter
            {
                RoleId = id
            });
        }

        [Route(nameof(AddRolePermissionsUri))]
        [Route(HttpMethod.Put, AddRolePermissionsUri)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for add role permissions")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "ids",
            ParamType = typeof(int[]),
            Required = true,
            Description = "permission ids to add")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success")]
        public async Task<object> AddRolePermissions()
        {
            var roleId = Context.Parameters.id;
            var ids = this.BindAndValidateModel<int[]>();
            await adminService.AddRolePermissions(roleId, ids);
            return HttpStatusCode.NoContent;
        }

        [Route(nameof(RemoveRolePermissionsUri))]
        [Route(HttpMethod.Delete, RemoveRolePermissionsUri)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Admin" }, Summary = "Method for remove role permissions")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "ids",
            ParamType = typeof(int[]),
            Required = true,
            Description = "permission ids to remove")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success")]
        public async Task<object> RemoveRolePermissions()
        {
            var roleId = Context.Parameters.id;
            var ids = this.BindAndValidateModel<int[]>();
            await adminService.RemoveRolePermissions(roleId, ids);
            return HttpStatusCode.NoContent;
        }

        #endregion

        private readonly ILogger logger;
        private readonly IAdminService adminService;        
    }
}