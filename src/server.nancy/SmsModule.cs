using Domain0.Model;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Swagger.Annotations.Attributes;
using Swagger.ObjectModel;
using System.Collections.Generic;
using System.Linq;

namespace Domain0.Nancy
{
    public class SmsModule : NancyModule
    {
        public SmsModule()
        {
            Put("/api/sms/Register", ctx => Register(), name: nameof(Register));
            Put("/api/sms/ForceCreateUser", ctx => ForceCreateUser(), name: nameof(ForceCreateUser));
            Post("/api/sms/Login", ctx => Login(), name: nameof(Login));
            Post("/api/sms/ChangePassword", ctx => ChangePassword(), name: nameof(ChangePassword));
            Post("/api/sms/RequestResetPassword", ctx => RequestResetPassword(), name: nameof(RequestResetPassword));
            Post("/api/sms/ForceChangePhone", ctx => ForceChangePhone(), name: nameof(ForceChangePhone));
            Get("/api/sms/DoesUserExist", ctx => DoesUserExist(), name: nameof(DoesUserExist));
            Get("/api/sms/PhoneByUserId", ctx => PhoneByUserId(), name: nameof(PhoneByUserId));
            Get("/api/Refresh/{refreshToken}", ctx => Refresh(), name: nameof(Refresh));
            Get("/api/profile", ctx => GetMyProfile(), name: nameof(GetMyProfile));
            Get("/api/users/sms/{phone}", ctx => GetUserByPhone(), name: nameof(GetUserByPhone));
            Post("/api/profile/filter", ctx => UserFilter(), name: nameof(UserFilter));
            Post("/api/users/{id}", ctx => GetUserById(), name: nameof(GetUserById));
        }

        [Route(nameof(Register))]
        [Route(HttpMethod.Put, "/api/sms/Register")]
        [Route(Consumes = new[] { "application/json" })]
        [Route(Produces = new string[] { })]
        [Route(Tags = new[] { "Sms" }, Summary = "Method for registration by phone")]
        [RouteParam(ParamIn = ParameterIn.Body, Name = "phone", ParamType = typeof(long), Required = true, Description = "user's phone with single number, started from 7 for Russia, 79162233224 for example")]
        [SwaggerResponse(HttpStatusCode.NoContent, Message = "Success")]
        public object Register()
        {
            var phone = this.Bind<long>();
            return HttpStatusCode.NoContent;
        }

        [Route(nameof(ForceCreateUser))]
        [Route(HttpMethod.Put, "/api/sms/ForceCreateUser")]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Produces = new string[] { })]
        [Route(Tags = new[] { "Sms" }, Summary = "Method for registration by phone")]
        [RouteParam(ParamIn = ParameterIn.Body, Name = "request", ParamType = typeof(ForceCreateUserRequest), Required = true, Description = "parameters for force create")]
        [SwaggerResponse(HttpStatusCode.NoContent, Message = "Success")]
        public object ForceCreateUser()
        {
            var request = this.Bind<ForceCreateUserRequest>();
            return HttpStatusCode.NoContent;
        }

        [Route(nameof(Login))]
        [Route(HttpMethod.Post, "/api/sms/Login")]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Sms" }, Summary = "Method for registration by phone")]
        [RouteParam(ParamIn = ParameterIn.Body, Name = "request", ParamType = typeof(SmsLoginRequest), Required = true, Description = "parameters for login")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(AccessTokenResponse))]
        public object Login()
        {
            var request = this.Bind<SmsLoginRequest>();
            return new AccessTokenResponse
            {
                AccessToken = "access_token",
                RefreshToken = "refresh_token",
                Profile = new UserProfile
                {
                    Id = 1,
                    Name = "name"
                }
            };
        }

        [Route(nameof(ChangePassword))]
        [Route(HttpMethod.Post, "/api/sms/ChangePassword")]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Produces = new string[] { })]
        [Route(Tags = new[] { "Sms" }, Summary = "Method for registration by phone")]
        [RouteParam(ParamIn = ParameterIn.Body, Name = "request", ParamType = typeof(ChangePasswordRequest), Required = true, Description = "parameters for change password")]
        [SwaggerResponse(HttpStatusCode.NoContent, Message = "Success")]
        public object ChangePassword()
        {
            var request = this.Bind<ChangePasswordRequest>();
            return HttpStatusCode.NoContent;
        }

        [Route(nameof(RequestResetPassword))]
        [Route(HttpMethod.Post, "/api/sms/RequestResetPassword")]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Produces = new string[] { })]
        [Route(Tags = new[] { "Sms" }, Summary = "Method for reset password")]
        [RouteParam(ParamIn = ParameterIn.Body, Name = "phone", ParamType = typeof(long), Required = true, Description = "user's phone with single number, started from 7 for Russia, 79162233224 for example")]
        [SwaggerResponse(HttpStatusCode.NoContent, Message = "Success")]
        public object RequestResetPassword()
        {
            var phone = this.Bind<long>();
            return HttpStatusCode.NoContent;
        }

        [Route(nameof(ForceChangePhone))]
        [Route(HttpMethod.Post, "/api/sms/ForceChangePhone")]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Produces = new string[] { })]
        [Route(Tags = new[] { "Sms" }, Summary = "Method for force change phone only administrator")]
        [RouteParam(ParamIn = ParameterIn.Body, Name = "phone", ParamType = typeof(ForceChangePhone), Required = true, Description = "parameters for change phone")]
        [SwaggerResponse(HttpStatusCode.NoContent, Message = "Success")]
        public object ForceChangePhone()
        {
            var request = this.Bind<ForceChangePhone>();
            return HttpStatusCode.NoContent;
        }

        [Route(nameof(DoesUserExist))]
        [Route(HttpMethod.Get, "/api/sms/DoesUserExist")]
        [Route(Produces = new[] { "application/json" })]
        [Route(Tags = new[] { "Sms" }, Summary = "Method for check user exists")]
        [RouteParam(ParamIn = ParameterIn.Query, Name = "phone", ParamType = typeof(long), Required = true, Description = "user's phone with single number, started from 7 for Russia, 79162233224 for example")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "True if user exists else false", Model = typeof(bool))]
        public object DoesUserExist()
        {
            var phone = this.Bind<long>();
            return true;
        }

        [Route(nameof(PhoneByUserId))]
        [Route(HttpMethod.Get, "/api/sms/PhoneByUserId")]
        [Route(Produces = new[] { "application/json" })]
        [Route(Tags = new[] { "Sms" }, Summary = "Method for get phone by user id")]
        [RouteParam(ParamIn = ParameterIn.Query, Name = "id", ParamType = typeof(int), Required = true, Description = "User Id")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "User phone", Model = typeof(long))]
        public object PhoneByUserId()
        {
            var userId = this.Bind<long>();
            return 79160000000;
        }

        [Route(nameof(Refresh))]
        [Route(HttpMethod.Get, "/api/Refresh/{refreshToken}")]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Refresh" }, Summary = "Method for refresh access token")]
        [RouteParam(ParamIn = ParameterIn.Path, Name = "refreshToken", ParamType = typeof(string), Required = true, Description = "Refresh token")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(AccessTokenResponse))]
        public object Refresh()
        {
            var refreshToken = Context.Parameters.refreshToken;
            return new AccessTokenResponse
            {
                AccessToken = "access_token",
                RefreshToken = refreshToken,
                Profile = new UserProfile
                {
                    Id = 1,
                    Name = "name"
                }
            };
        }

        [Route(nameof(GetMyProfile))]
        [Route(HttpMethod.Get, "/api/profile")]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "UserProfile" }, Summary = "Method for receive own profile")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(UserProfile))]
        public object GetMyProfile()
        {
            return new UserProfile
            {
                Id = 1,
                Name = "test"
            };
        }

        [Route(nameof(GetUserByPhone))]
        [Route(HttpMethod.Get, "/api/users/sms/{phone}")]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Users" }, Summary = "Method for receive profile by phone")]
        [RouteParam(ParamIn = ParameterIn.Path, Name = "phone", ParamType = typeof(long), Required = true, Description = "User phone")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(UserProfile))]
        public object GetUserByPhone()
        {
            var phone = Context.Parameters.phone;
            return new UserProfile
            {
                Id = 1,
                Name = "test",
                Phone = phone
            };
        }

        [Route(nameof(UserFilter))]
        [Route(HttpMethod.Get, "/api/profile/filter")]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "UserProfile" }, Summary = "Method for receive profiles by user ids")]
        [RouteParam(ParamIn = ParameterIn.Body, Name = "request", ParamType = typeof(UserProfileFilter), Required = true, Description = "Profile filter")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(IEnumerable<UserProfile>))]
        public object UserFilter()
        {
            var filter = this.Bind<UserProfileFilter>();
            return filter.UserIds.Select(id => new UserProfile
            {
                Id = id,
                Name = "test " + id,
                Phone = 79000000000 + id
            }).ToList();
        }

        [Route(nameof(GetUserById))]
        [Route(HttpMethod.Get, "/api/users/{id}")]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Users" }, Summary = "Method for receive profile by user id")]
        [RouteParam(ParamIn = ParameterIn.Path, Name = "id", ParamType = typeof(int), Required = true, Description = "User id")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(UserProfile))]
        public object GetUserById()
        {
            var id = Context.Parameters.id;
            return new UserProfile
            {
                Id = id,
                Name = "test " + id
            };
        }
    }
}
