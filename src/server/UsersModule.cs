using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using Domain0.Exceptions;
using Domain0.Model;
using Domain0.Nancy.Infrastructure;
using Domain0.Service;
using Domain0.Service.Throttling;
using Domain0.Service.Tokens;
using Nancy;
using Nancy.Security;
using Nancy.Swagger.Annotations.Attributes;
using NLog;
using Swagger.ObjectModel;

namespace Domain0.Nancy
{
    public sealed class UsersModule : NancyModule
    {
        public const string GetMyProfileUrl = "/api/profile";
        public const string ChangeMyPasswordUrl = "/api/profile/ChangePassword";
        public const string GetUsersByFilterUrl = "/api/profile/filter";

        public const string GetUserByPhoneUrl = "/api/users/sms/{phone}";
        public const string GetUserByIdUrl = "/api/users/{id}";
        public const string PostUserUrl = "/api/users/{id}";

        public const string DeleteUserUrl = "/api/sms/{phone}";

        public UsersModule(
            IAccountService accountServiceInstance,
            ILogger loggerInstance,
            IRequestContext requestContextInstance,
            IRequestThrottleManager requestThrottleManagerInstance)
        {
            accountService = accountServiceInstance;
            logger = loggerInstance;
            requestContext = requestContextInstance;
            requestThrottleManager = requestThrottleManagerInstance;

            Get("/", ctx => HttpStatusCode.NoContent);

            Get(GetMyProfileUrl, ctx => GetMyProfile(), name: nameof(GetMyProfile));
            Post(ChangeMyPasswordUrl, ctx => ChangeMyPassword(), name: nameof(ChangeMyPassword));
            Post(GetUsersByFilterUrl, ctx => GetUserByFilter(), name: nameof(GetUserByFilter));
            Get(GetUserByPhoneUrl, ctx => GetUserByPhone(), name: nameof(GetUserByPhone));
            Get(GetUserByIdUrl, ctx => GetUserById(), name: nameof(GetUserById));
            Post(PostUserUrl, ctx => UpdateUser(), name: nameof(UpdateUser));
            Delete(DeleteUserUrl, ctx => DeleteUser(), name: nameof(DeleteUser));
        }

        [Route(nameof(GetMyProfile))]
        [Route(HttpMethod.Get, GetMyProfileUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "UserProfile" }, Summary = "Method for receive own profile")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(UserProfile))]
        [SwaggerResponse(HttpStatusCode.BadRequest, "Incorrect ids format or unsupported auth type")]
        [SwaggerResponse(HttpStatusCode.Unauthorized, "Provide domain0 auth token")]
        [SwaggerResponse(HttpStatusCode.NotFound, "Profile not found")]
        public async Task<object> GetMyProfile()
        {
            this.RequiresAuthentication();

            var profile = await accountService.GetMyProfile();
            return profile;
        }

        [Route(nameof(GetUserByPhone))]
        [Route(HttpMethod.Get, GetUserByPhoneUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Users" }, Summary = "Method for receive profile by phone")]
        [RouteParam(ParamIn = ParameterIn.Path, Name = "phone", ParamType = typeof(long), Required = true, Description = "User phone")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(UserProfile))]
        [SwaggerResponse(HttpStatusCode.NotFound, "User with this profile id wasn't found")]
        [SwaggerResponse(HttpStatusCode.BadRequest, "incorrect id format or unsupported auth type")]
        [SwaggerResponse(HttpStatusCode.Unauthorized, "Provide domain0 auth token")]
        [SwaggerResponse(HttpStatusCode.Forbidden, "you need 'domain0.viewUsers' permission")]
        public async Task<object> GetUserByPhone()
        {
            this.RequiresAuthentication();
            this.RequiresClaims(c =>
                c.Type == TokenClaims.CLAIM_PERMISSIONS
                && c.Value.Contains(TokenClaims.CLAIM_PERMISSIONS_VIEW_USERS));

            if (!decimal.TryParse(Context.Parameters.phone.ToString(), out decimal phone))
            {
                ModelValidationResult.Errors.Add(nameof(phone), "bad format");
                throw new BadModelException(ModelValidationResult);
            }

            var profile = await accountService.GetProfileByPhone(phone);
            return profile;
        }

        [Route(nameof(GetUserById))]
        [Route(HttpMethod.Get, GetUserByIdUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Users" }, Summary = "Method for receive profile by user id")]
        [RouteParam(ParamIn = ParameterIn.Path, Name = "id", ParamType = typeof(int), Required = true, Description = "User id")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(UserProfile))]
        [SwaggerResponse(HttpStatusCode.NotFound, "User with this profile id for the auth type wasn't found")]
        [SwaggerResponse(HttpStatusCode.BadRequest, "incorrect id format")]
        [SwaggerResponse(HttpStatusCode.Unauthorized, "Provide domain0 auth token")]
        [SwaggerResponse(HttpStatusCode.Forbidden, "you need 'domain0.viewUsers' permission")]
        public async Task<object> GetUserById()
        {
            this.RequiresAuthentication();
            this.RequiresClaims(c =>
                c.Type == TokenClaims.CLAIM_PERMISSIONS
                && c.Value.Contains(TokenClaims.CLAIM_PERMISSIONS_VIEW_USERS));

            var id = Context.Parameters.id;
            var profile = await accountService.GetProfileByUserId(id);
            return profile;
        }

        [Route(nameof(UpdateUser))]
        [Route(HttpMethod.Post, PostUserUrl)]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Produces = new string[] { })]
        [Route(Tags = new[] { "Users" }, Summary = "Method for change user data")]
        [RouteParam(
            ParamIn = ParameterIn.Path,
            Name = "id",
            ParamType = typeof(int),
            Required = true,
            Description = "User id")]
        [RouteParam(
            ParamIn = ParameterIn.Body, 
            Name = "request", 
            ParamType = typeof(UserProfile), 
            Required = true, 
            Description = "user data")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(UserProfile))]
        [SwaggerResponse(HttpStatusCode.NotFound, "User with this profile id for the auth type wasn't found")]
        [SwaggerResponse(HttpStatusCode.BadRequest, "incorrect input format")]
        [SwaggerResponse(HttpStatusCode.Unauthorized, "Provide domain0 auth token")]
        [SwaggerResponse(HttpStatusCode.Forbidden, "you need 'domain0.editUsers' permission")]
        public async Task<object> UpdateUser()
        {
            this.RequiresAuthentication();
            this.RequiresClaims(c =>
                c.Type == TokenClaims.CLAIM_PERMISSIONS
                && c.Value.Contains(TokenClaims.CLAIM_PERMISSIONS_EDIT_USERS));

            var request = this.BindAndValidateModel<UserProfile>();
            request.Id = (int)Context.Parameters.id;

            return await accountService.UpdateUser(request);
        }

        [Route(nameof(DeleteUser))]
        [Route(HttpMethod.Post, DeleteUserUrl)]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Produces = new string[] { })]
        [Route(Tags = new[] { "Sms" }, Summary = "Method for delete user")]
        [RouteParam(
            ParamIn = ParameterIn.Path,
            Name = "phone",
            ParamType = typeof(long),
            Required = true,
            Description = "phone")]
        [SwaggerResponse(HttpStatusCode.NoContent, Message = "Success")]
        [SwaggerResponse(HttpStatusCode.BadRequest, "wrong phone format")]
        [SwaggerResponse(HttpStatusCode.InternalServerError, "internal error during request execution")]
        [SwaggerResponse(HttpStatusCode.NotFound, "User with this profile id for the auth type wasn't found")]
        [SwaggerResponse(HttpStatusCode.Unauthorized, "Provide domain0 auth token")]
        [SwaggerResponse(HttpStatusCode.Forbidden, "you need 'domain0.editUsers' permission")]
        public async Task<object> DeleteUser()
        {
            this.RequiresAuthentication();
            this.RequiresClaims(c =>
                c.Type == TokenClaims.CLAIM_PERMISSIONS
                && c.Value.Contains(TokenClaims.CLAIM_PERMISSIONS_BASIC));

            var phone = (long)Context.Parameters.phone;

            var profile = await accountService.GetProfileByPhone(phone);
            if (profile == null)
            {
                return HttpStatusCode.NotFound;
            }

            if (requestContext.UserId != profile.Id)
            {
                return HttpStatusCode.Forbidden;
            }

            await accountService.DeleteUser(profile.Id);

            return HttpStatusCode.NoContent;
        }

        [Route(nameof(ChangeMyPassword))]
        [Route(HttpMethod.Post, ChangeMyPasswordUrl)]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "UserProfile" }, Summary = "Method for change password")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "request",
            ParamType = typeof(ChangePasswordRequest),
            Required = true,
            Description = "parameters for change password")]
        [SwaggerResponse(HttpStatusCode.BadRequest, "wrong old password or too easy new password")]
        [SwaggerResponse(HttpStatusCode.InternalServerError, "internal error during request execution")]
        [SwaggerResponse(HttpStatusCode.NoContent, "operation completes successfully")]
        [SwaggerResponse(HttpStatusCode.Unauthorized, "authentication required. jwt token in header")]
        [SwaggerResponse(HttpStatusCode.Forbidden, "domain0.basic permission required")]
        public async Task<object> ChangeMyPassword()
        {
            this.RequiresAuthentication();
            this.RequiresClaims(c =>
                c.Type == TokenClaims.CLAIM_PERMISSIONS
                && c.Value.Contains(TokenClaims.CLAIM_PERMISSIONS_BASIC));

            var request = this.BindAndValidateModel<ChangePasswordRequest>();
            try
            {
                await accountService.ChangePassword(request);
            }
            catch (SecurityException)
            {
                ModelValidationResult.Errors.Add("oldPassword", "password is not valid");
                throw new BadModelException(ModelValidationResult);
            }

            return HttpStatusCode.NoContent;
        }


        [Route(nameof(GetUserByFilter))]
        [Route(HttpMethod.Post, GetUsersByFilterUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "UserProfile" }, Summary = "Method for receive profiles by user ids")]
        [RouteParam(ParamIn = ParameterIn.Body, Name = "request", ParamType = typeof(UserProfileFilter), Required = true, Description = "Profile filter")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(IEnumerable<UserProfile>))]
        [SwaggerResponse(HttpStatusCode.NotFound, "User with this profile id for the auth type wasn't found")]
        [SwaggerResponse(HttpStatusCode.BadRequest, "incorrect ids format or unsupported auth type")]
        [SwaggerResponse(HttpStatusCode.Unauthorized, "Provide domain0 auth token")]
        [SwaggerResponse(HttpStatusCode.Forbidden, "you need 'domain0.viewProfile' permission")]
        public async Task<object> GetUserByFilter()
        {
            this.RequiresAuthentication();
            this.RequiresClaims(c =>
                c.Type == TokenClaims.CLAIM_PERMISSIONS
                && c.Value.Contains(TokenClaims.CLAIM_PERMISSIONS_VIEW_PROFILE));

            var filter = this.BindAndValidateModel<UserProfileFilter>();
            return await accountService.GetProfilesByFilter(filter);
        }


        private readonly IAccountService accountService;

        private readonly ILogger logger;

        private readonly IRequestThrottleManager requestThrottleManager;

        private readonly IRequestContext requestContext;
    }
}
