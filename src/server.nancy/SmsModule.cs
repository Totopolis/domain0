﻿using Domain0.Model;
using Domain0.Service;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Swagger.Annotations.Attributes;
using Swagger.ObjectModel;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using Domain0.Exceptions;
using Domain0.Nancy.Infrastructure;
using Nancy.Security;
using Domain0.Service.Tokens;

namespace Domain0.Nancy
{
    public class SmsModule : NancyModule
    {
        public const string RegisterUrl = "/api/sms/Register";
        public const string ForceCreateUserUrl = "/api/sms/ForceCreateUser";
        public const string LoginUrl = "/api/sms/Login";
        public const string ChangePasswordUrl = "/api/sms/ChangePassword";
        public const string RequestResetPasswordUrl = "/api/sms/RequestResetPassword";
        public const string ForceChangePhoneUrl = "/api/sms/ForceChangePhone";
        public const string DoesUserExistUrl = "/api/sms/DoesUserExist";
        public const string PhoneByUserIdUrl = "/api/sms/PhoneByUserId";
        public const string RefreshUrl = "/api/Refresh/{refreshToken}";
        public const string GetMyProfileUrl = "/api/profile";
        public const string GetUserByPhoneUrl = "/api/users/sms/{phone}";
        public const string GetUsersByFilterUrl = "/api/profile/filter";
        public const string GetUserByIdUrl = "/api/users/{id}";

        private readonly IAccountService _accountService;

        public SmsModule(IAccountService accountService)
        {
            _accountService = accountService;

            Put(RegisterUrl, ctx => Register(), name: nameof(Register));
            Put(ForceCreateUserUrl, ctx => ForceCreateUser(), name: nameof(ForceCreateUser));
            Post(LoginUrl, ctx => Login(), name: nameof(Login));
            Post(ChangePasswordUrl, ctx => ChangePassword(), name: nameof(ChangePassword));
            Post(RequestResetPasswordUrl, ctx => RequestResetPassword(), name: nameof(RequestResetPassword));
            Post(ForceChangePhoneUrl, ctx => ForceChangePhone(), name: nameof(ForceChangePhone));
            Get(DoesUserExistUrl, ctx => DoesUserExist(), name: nameof(DoesUserExist));
            Get(PhoneByUserIdUrl, ctx => PhoneByUserId(), name: nameof(PhoneByUserId));
            Get(RefreshUrl, ctx => Refresh(), name: nameof(Refresh));
            Get(GetMyProfileUrl, ctx => GetMyProfile(), name: nameof(GetMyProfile));
            Get(GetUserByPhoneUrl, ctx => GetUserByPhone(), name: nameof(GetUserByPhone));
            Post(GetUsersByFilterUrl, ctx => GetUserByFilter(), name: nameof(GetUserByFilter));
            Get(GetUserByIdUrl, ctx => GetUserById(), name: nameof(GetUserById));
        }

        [Route(nameof(Register))]
        [Route(HttpMethod.Put, RegisterUrl)]
        [Route(Consumes = new[] { "application/json" })]
        [Route(Produces = new string[] { })]
        [Route(Tags = new[] { "Sms" }, Summary = "Method for registration by phone")]
        [RouteParam(ParamIn = ParameterIn.Body, Name = "phone", ParamType = typeof(long), Required = true, Description = "user's phone with single number, started from 7 for Russia, 71231234567 for example")]
        [SwaggerResponse(HttpStatusCode.NoContent, Message = "Success")]
        public async Task<object> Register()
        {
            var phone = this.Bind<long>();
            try
            {
                await _accountService.Register(phone);
            }
            catch (SecurityException ex)
            {
                ModelValidationResult.Errors.Add(nameof(phone), ex.Message);
                throw new BadModelException(ModelValidationResult);
            }

            return HttpStatusCode.NoContent;
        }

        [Route(nameof(ForceCreateUser))]
        [Route(HttpMethod.Put, ForceCreateUserUrl)]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Produces = new string[] { })]
        [Route(Tags = new[] { "Sms" }, Summary = "Method for registration by phone")]
        [RouteParam(ParamIn = ParameterIn.Body, Name = "request", ParamType = typeof(ForceCreateUserRequest), Required = true, Description = "parameters for force create")]
        [SwaggerResponse(HttpStatusCode.NoContent, Message = "Success")]
        public async Task<object> ForceCreateUser()
        {
            this.RequiresAuthentication();
            this.RequiresClaims(c =>
                c.Type == TokenClaims.CLAIM_PERMISSIONS
                && c.Value.Contains(TokenClaims.CLAIM_PERMISSIONS_ADMIN));

            var request = this.BindAndValidateModel<ForceCreateUserRequest>();
            try
            {
                return await _accountService.CreateUser(request);
            }
            catch (SecurityException)
            {
                ModelValidationResult.Errors.Add(nameof(request.Phone), "user exists");
                throw new BadModelException(ModelValidationResult);
            }
        }

        [Route(nameof(Login))]
        [Route(HttpMethod.Post, LoginUrl)]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Sms" }, Summary = "Method for registration by phone")]
        [RouteParam(ParamIn = ParameterIn.Body, Name = "request", ParamType = typeof(SmsLoginRequest), Required = true, Description = "parameters for login")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(AccessTokenResponse))]
        public async Task<object> Login()
        {
            var request = this.BindAndValidateModel<SmsLoginRequest>();

            var result = await _accountService.Login(request);
            if (result == null)
            {
                ModelValidationResult.Errors.Add(nameof(request.Phone), "user or password incorrect");
                throw new BadModelException(ModelValidationResult);
            }

            return result;
        }

        [Route(nameof(ChangePassword))]
        [Route(HttpMethod.Post, ChangePasswordUrl)]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Produces = new string[] { })]
        [Route(Tags = new[] { "Sms" }, Summary = "Method for registration by phone")]
        [RouteParam(ParamIn = ParameterIn.Body, Name = "request", ParamType = typeof(ChangePasswordRequest), Required = true, Description = "parameters for change password")]
        [SwaggerResponse(HttpStatusCode.NoContent, Message = "Success")]
        public async Task<object> ChangePassword()
        {
            this.RequiresAuthentication();

            var request = this.BindAndValidateModel<ChangePasswordRequest>();
            try
            {
                await _accountService.ChangePassword(request);
            }
            catch (SecurityException)
            {
                ModelValidationResult.Errors.Add("oldPassword", "password is not valid");
                throw new BadModelException(ModelValidationResult);
            }

            return HttpStatusCode.NoContent;
        }

        [Route(nameof(RequestResetPassword))]
        [Route(HttpMethod.Post, RequestResetPasswordUrl)]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Produces = new string[] { })]
        [Route(Tags = new[] { "Sms" }, Summary = "Method for reset password")]
        [RouteParam(ParamIn = ParameterIn.Body, Name = "phone", ParamType = typeof(long), Required = true, Description = "user's phone with single number, started from 7 for Russia, 71231234567 for example")]
        [SwaggerResponse(HttpStatusCode.NoContent, Message = "Success")]
        public async Task<object> RequestResetPassword()
        {
            this.RequiresAuthentication();

            var phone = this.BindAndValidateModel<long>();
            await _accountService.RequestResetPassword(phone);
            return HttpStatusCode.NoContent;
        }

        [Route(nameof(ForceChangePhone))]
        [Route(HttpMethod.Post, ForceChangePhoneUrl)]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Produces = new string[] { })]
        [Route(Tags = new[] { "Sms" }, Summary = "Method for force change phone only administrator")]
        [RouteParam(ParamIn = ParameterIn.Body, Name = "phone", ParamType = typeof(ChangePhoneRequest), Required = true, Description = "parameters for change phone")]
        [SwaggerResponse(HttpStatusCode.NoContent, Message = "Success")]
        public async Task<object> ForceChangePhone()
        {
            this.RequiresAuthentication();
            this.RequiresClaims(c =>
                c.Type == TokenClaims.CLAIM_PERMISSIONS
                && c.Value.Contains(TokenClaims.CLAIM_PERMISSIONS_ADMIN));

            var request = this.BindAndValidateModel<ChangePhoneRequest>();
            await _accountService.ForceChangePhone(request);
            return HttpStatusCode.NoContent;
        }

        [Route(nameof(DoesUserExist))]
        [Route(HttpMethod.Get, DoesUserExistUrl)]
        [Route(Produces = new[] { "application/json" })]
        [Route(Tags = new[] { "Sms" }, Summary = "Method for check user exists")]
        [RouteParam(ParamIn = ParameterIn.Query, Name = "phone", ParamType = typeof(long), Required = true, Description = "user's phone with single number, started from 7 for Russia, 71231234567 for example")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "True if user exists else false", Model = typeof(bool))]
        public async Task<object> DoesUserExist()
        {
            decimal phone;
            if (!decimal.TryParse(Request.Query[nameof(phone)].ToString(), out phone))
            {
                ModelValidationResult.Errors.Add(nameof(phone), "wrong phone format");
                throw new BadModelException(ModelValidationResult);
            }

            var result = await _accountService.DoesUserExists(phone);
            return result;
        }

        [Route(nameof(PhoneByUserId))]
        [Route(HttpMethod.Get, PhoneByUserIdUrl)]
        [Route(Produces = new[] { "application/json" })]
        [Route(Tags = new[] { "Sms" }, Summary = "Method for get phone by user id")]
        [RouteParam(ParamIn = ParameterIn.Query, Name = "id", ParamType = typeof(int), Required = true, Description = "User Id")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "User phone", Model = typeof(long))]
        public async Task<object> PhoneByUserId()
        {
            this.RequiresAuthentication();
            this.RequiresClaims(c =>
                c.Type == TokenClaims.CLAIM_PERMISSIONS
                && c.Value.Contains(TokenClaims.CLAIM_PERMISSIONS_ADMIN));

            int id;
            if (!int.TryParse(Request.Query[nameof(id)], out id))
            {
                ModelValidationResult.Errors.Add(nameof(id), "bad format");
                throw new BadModelException(ModelValidationResult);
            }

            var result = await _accountService.GetProfileByUserId(id);
            if (result.Phone == null)
                throw new NotFoundException(nameof(result.Phone));

            return (long) result.Phone;
        }

        [Route(nameof(Refresh))]
        [Route(HttpMethod.Get, RefreshUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Refresh" }, Summary = "Method for refresh access token")]
        [RouteParam(ParamIn = ParameterIn.Path, Name = "refreshToken", ParamType = typeof(string), Required = true, Description = "Refresh token")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(AccessTokenResponse))]
        public async Task<object> Refresh()
        {
            var refreshToken = Context.Parameters.refreshToken;
            var response = await _accountService.Refresh(refreshToken);
            return response;
        }

        [Route(nameof(GetMyProfile))]
        [Route(HttpMethod.Get, GetMyProfileUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "UserProfile" }, Summary = "Method for receive own profile")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(UserProfile))]
        public async Task<object> GetMyProfile()
        {
            this.RequiresAuthentication();

            var profile = await _accountService.GetMyProfile();
            return profile;
        }

        [Route(nameof(GetUserByPhone))]
        [Route(HttpMethod.Get, GetUserByPhoneUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Users" }, Summary = "Method for receive profile by phone")]
        [RouteParam(ParamIn = ParameterIn.Path, Name = "phone", ParamType = typeof(long), Required = true, Description = "User phone")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(UserProfile))]
        public async Task<object> GetUserByPhone()
        {
            this.RequiresAuthentication();
            this.RequiresClaims(c =>
                c.Type == TokenClaims.CLAIM_PERMISSIONS
                && c.Value.Contains(TokenClaims.CLAIM_PERMISSIONS_ADMIN));

            if (!decimal.TryParse(Context.Parameters.phone.ToString(), out decimal phone))
            {
                ModelValidationResult.Errors.Add(nameof(phone), "bad format");
                throw new BadModelException(ModelValidationResult);
            }

            var profile = await _accountService.GetProfileByPhone(phone);
            return profile;
        }

        [Route(nameof(GetUserByFilter))]
        [Route(HttpMethod.Post, GetUsersByFilterUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "UserProfile" }, Summary = "Method for receive profiles by user ids")]
        [RouteParam(ParamIn = ParameterIn.Body, Name = "request", ParamType = typeof(UserProfileFilter), Required = true, Description = "Profile filter")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(IEnumerable<UserProfile>))]
        public async Task<object> GetUserByFilter()
        {
            this.RequiresAuthentication();
            this.RequiresClaims(c =>
                c.Type == TokenClaims.CLAIM_PERMISSIONS
                && c.Value.Contains(TokenClaims.CLAIM_PERMISSIONS_ADMIN));

            var filter = this.BindAndValidateModel<UserProfileFilter>();
            return await _accountService.GetProfilesByFilter(filter);
        }

        [Route(nameof(GetUserById))]
        [Route(HttpMethod.Get, GetUserByIdUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Users" }, Summary = "Method for receive profile by user id")]
        [RouteParam(ParamIn = ParameterIn.Path, Name = "id", ParamType = typeof(int), Required = true, Description = "User id")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(UserProfile))]
        public async Task<object> GetUserById()
        {
            this.RequiresAuthentication();
            this.RequiresClaims(c =>
                c.Type == TokenClaims.CLAIM_PERMISSIONS
                && c.Value.Contains(TokenClaims.CLAIM_PERMISSIONS_ADMIN));

            var id = Context.Parameters.id;
            var profile = await _accountService.GetProfileByUserId(id);
            return profile;
        }
    }
}
