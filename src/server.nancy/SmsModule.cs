using Domain0.Model;
using Domain0.Service;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Swagger.Annotations.Attributes;
using Swagger.ObjectModel;
using System.Security;
using System.Threading.Tasks;
using Domain0.Exceptions;
using Domain0.Nancy.Infrastructure;
using Domain0.Service.Throttling;
using Nancy.Security;
using Domain0.Service.Tokens;
using System;

namespace Domain0.Nancy
{
    public sealed class SmsModule : NancyModule
    {
        public const string RegisterUrl = "/api/sms/Register";
        public const string RegisterWithEnvironmentUrl = "/api/sms/Register/{EnvironmentToken}";
        public const string LoginUrl = "/api/sms/Login";
        public const string ChangePasswordUrl = "/api/sms/ChangePassword";
        public const string DoesUserExistUrl = "/api/sms/DoesUserExist";
        public const string PhoneByUserIdUrl = "/api/sms/PhoneByUserId";
        public const string RefreshUrl = "/api/Refresh/{refreshToken}";

        public const string RequestResetPasswordUrl = "/api/sms/RequestResetPassword";

        public const string ForceCreateUserUrl = "/api/sms/ForceCreateUser";
        public const string ForceChangePhoneUrl = "/api/sms/ForceChangePhone";
        public const string ForceResetPasswordUrl = "/api/sms/ForceResetPassword";

        public const string RequestChangePhoneUrl = "/api/sms/RequestChangePhone";
        public const string CommitChangePhoneUrl = "/api/sms/CommitChangePhone";

        private readonly IAccountService accountService;
        private readonly IRequestThrottleManager requestThrottleManager;
        private readonly IRequestContext requestContext;

        public SmsModule(
            IAccountService accountServiceInstance,
            IRequestContext requestContextInstance,
            IRequestThrottleManager requestThrottleManagerInstance)
        {
            accountService = accountServiceInstance;
            requestContext = requestContextInstance;
            requestThrottleManager = requestThrottleManagerInstance;

            Put(RegisterUrl, ctx => Register(), name: nameof(Register));
            Put(RegisterWithEnvironmentUrl, ctx => RegisterWithEnvironment(), name: nameof(RegisterWithEnvironment));
            Post(LoginUrl, ctx => Login(), name: nameof(Login));

            Get(DoesUserExistUrl, ctx => DoesUserExist(), name: nameof(DoesUserExist));
            Get(PhoneByUserIdUrl, ctx => PhoneByUserId(), name: nameof(PhoneByUserId));
            Get(RefreshUrl, ctx => Refresh(), name: nameof(Refresh));

            Post(RequestResetPasswordUrl, ctx => RequestResetPassword(), name: nameof(RequestResetPassword));
            Post(ChangePasswordUrl, ctx => ChangePassword(), name: nameof(ChangePassword));

            Put(ForceCreateUserUrl, ctx => ForceCreateUser(), name: nameof(ForceCreateUser));
            Post(ForceChangePhoneUrl, ctx => ForceChangePhone(), name: nameof(ForceChangePhone));
            Post(ForceResetPasswordUrl, ctx => ForceResetPassword(), name: nameof(ForceResetPassword));

            Post(RequestChangePhoneUrl, ctx => RequestChangePhone(), name: nameof(RequestChangePhone));
            Post(CommitChangePhoneUrl, ctx => CommitChangePhone(), name: nameof(CommitChangePhone));
        }

        [Route(nameof(Register))]
        [Route(HttpMethod.Put, RegisterUrl)]
        [Route(Consumes = new[] { "application/json" })]
        [Route(Produces = new string[] { })]
        [Route(Tags = new[] { "Sms" }, Summary = "Method for registration by phone")]
        [RouteParam(ParamIn = ParameterIn.Body, Name = "phone", ParamType = typeof(long), Required = true, Description = "user's phone with single number, started from 7 for Russia, 71231234567 for example")]
        [SwaggerResponse(HttpStatusCode.NoContent, Message = "Success")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success")]
        [SwaggerResponse(HttpStatusCode.BadRequest, "wrong phone format or user with this phone already existed")]
        [SwaggerResponse(HttpStatusCode.InternalServerError, "internal error during request execution")]
        public async Task<object> Register()
        {
            return await RegisterInternal();
        }

        [Route(nameof(RegisterWithEnvironment))]
        [Route(HttpMethod.Put, RegisterWithEnvironmentUrl)]
        [Route(Consumes = new[] { "application/json" })]
        [Route(Produces = new string[] { })]
        [Route(Tags = new[] { "Sms" }, Summary = "Method for registration by phone with environment scope")]
        [RouteParam(
            ParamIn = ParameterIn.Body, 
            Name = "phone", 
            ParamType = typeof(long), 
            Required = true, 
            Description = "user's phone with single number, started from 7 for Russia, 71231234567 for example")]
        [RouteParam(
            ParamIn = ParameterIn.Path,
            Name = "environmentToken",
            ParamType = typeof(string),
            Required = false,
            Description = "user's environment token")]
        [SwaggerResponse(HttpStatusCode.NoContent, Message = "Success")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success")]
        [SwaggerResponse(HttpStatusCode.BadRequest, "wrong phone format or user with this phone already existed")]
        [SwaggerResponse(HttpStatusCode.InternalServerError, "internal error during request execution")]
        public async Task<object> RegisterWithEnvironment()
        {
            return await RegisterInternal();
        }

        private async Task<object> RegisterInternal()
        {
            var phone = this.Bind<long>();

            requestThrottleManager.RequiresThrottling(
                this, ThrottlingProperties.Path,
                ThrottlingPeriod.Day, requestCountLimit: 20,
                requestKeys: phone.ToString());

            requestThrottleManager.RequiresThrottling(
                this, ThrottlingProperties.Path | ThrottlingProperties.RemoteIp,
                ThrottlingPeriod.Minute, requestCountLimit: 50);

            requestThrottleManager.RequiresThrottling(
                this, ThrottlingProperties.Path | ThrottlingProperties.RemoteIp,
                ThrottlingPeriod.Hour, requestCountLimit: 300);

            try
            {
                var environmentToken = (string)Context.Parameters.EnvironmentToken;
                await accountService.Register(phone, environmentToken);
            }
            catch (SecurityException ex)
            {
                ModelValidationResult.Errors.Add(nameof(phone), ex.Message);
                throw new BadModelException(ModelValidationResult);
            }
            catch (ArgumentException ex)
            {
                ModelValidationResult.Errors.Add("wrong data: ", ex.Message);
                throw new BadModelException(ModelValidationResult);
            }
            return HttpStatusCode.OK;
        }

        [Route(nameof(Login))]
        [Route(HttpMethod.Post, LoginUrl)]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Sms" }, Summary = "Method for registration by phone")]
        [RouteParam(ParamIn = ParameterIn.Body, Name = "request", ParamType = typeof(SmsLoginRequest), Required = true, Description = "parameters for login")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(AccessTokenResponse))]
        [SwaggerResponse(HttpStatusCode.BadRequest, "wrong phone format / wrong phone and password pair")]
        [SwaggerResponse(HttpStatusCode.InternalServerError, "internal error during request execution")]
        public async Task<object> Login()
        {
            var request = this.BindAndValidateModel<SmsLoginRequest>();

            requestThrottleManager.RequiresThrottling(
                this, ThrottlingProperties.Path,
                ThrottlingPeriod.Minute, requestCountLimit: 20,
                requestKeys: request.Phone.ToString());

            requestThrottleManager.RequiresThrottling(
                this, ThrottlingProperties.Path,
                ThrottlingPeriod.Hour, requestCountLimit: 100,
                requestKeys: request.Phone.ToString());

            var result = await accountService.Login(request);
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
        [SwaggerResponse(HttpStatusCode.BadRequest, "wrong old password or too easy new password")]
        [SwaggerResponse(HttpStatusCode.InternalServerError, "internal error during request execution")]
        [SwaggerResponse(HttpStatusCode.Unauthorized, "authentication required. jwt token in header")]
        [SwaggerResponse(HttpStatusCode.Forbidden, "domain0.basic permission required")]
        public async Task<object> ChangePassword()
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

        [Route(nameof(RequestResetPassword))]
        [Route(HttpMethod.Post, RequestResetPasswordUrl)]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Produces = new string[] { })]
        [Route(Tags = new[] { "Sms" }, Summary = "Method for reset password")]
        [RouteParam(ParamIn = ParameterIn.Body, Name = "phone", ParamType = typeof(long), Required = true, Description = "user's phone with single number, started from 7 for Russia, 71231234567 for example")]
        [SwaggerResponse(HttpStatusCode.BadRequest, "user with this phone doesn't exist")]
        [SwaggerResponse(HttpStatusCode.InternalServerError, "internal error during request execution")]
        [SwaggerResponse(HttpStatusCode.NoContent, "operation completes successfully, code has been sent")]
        public async Task<object> RequestResetPassword()
        {
            var phone = this.BindAndValidateModel<long>();

            requestThrottleManager.RequiresThrottling(
                this, ThrottlingProperties.Path,
                ThrottlingPeriod.Minute, requestCountLimit: 1,
                requestKeys: phone.ToString());

            requestThrottleManager.RequiresThrottling(
                this, ThrottlingProperties.Path,
                ThrottlingPeriod.Hour, requestCountLimit: 10,
                requestKeys: phone.ToString());

            await accountService.RequestResetPassword(phone);
            return HttpStatusCode.NoContent;
        }

        [Route(nameof(DoesUserExist))]
        [Route(HttpMethod.Get, DoesUserExistUrl)]
        [Route(Produces = new[] { "application/json" })]
        [Route(Tags = new[] { "Sms" }, Summary = "Method for check user exists")]
        [RouteParam(ParamIn = ParameterIn.Query, Name = "phone", ParamType = typeof(long), Required = true, Description = "user's phone with single number, started from 7 for Russia, 71231234567 for example")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "True if user exists else false", Model = typeof(bool))]
        [SwaggerResponse(HttpStatusCode.BadRequest, "wrong phone format")]
        [SwaggerResponse(HttpStatusCode.InternalServerError, "internal error during request execution")]
        public async Task<object> DoesUserExist()
        {
            requestThrottleManager.RequiresThrottling(
                this, ThrottlingProperties.Path,
                ThrottlingPeriod.Minute, requestCountLimit: 100);

            requestThrottleManager.RequiresThrottling(
                this, ThrottlingProperties.Path,
                ThrottlingPeriod.Hour, requestCountLimit: 300);

            decimal phone;
            if (!decimal.TryParse(Request.Query[nameof(phone)].ToString(), out phone))
            {
                ModelValidationResult.Errors.Add(nameof(phone), "wrong phone format");
                throw new BadModelException(ModelValidationResult);
            }

            var result = await accountService.DoesUserExists(phone);
            return result;
        }

        [Route(nameof(PhoneByUserId))]
        [Route(HttpMethod.Get, PhoneByUserIdUrl)]
        [Route(Produces = new[] { "application/json" })]
        [Route(Tags = new[] { "Sms" }, Summary = "Method for get phone by user id")]
        [RouteParam(ParamIn = ParameterIn.Query, Name = "id", ParamType = typeof(int), Required = true, Description = "User Id")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "User phone", Model = typeof(long))]
        [SwaggerResponse(HttpStatusCode.BadRequest, "wrong phone format")]
        [SwaggerResponse(HttpStatusCode.InternalServerError, "internal error during request execution")]
        [SwaggerResponse(HttpStatusCode.Unauthorized, "authentication required. jwt token in header")]
        [SwaggerResponse(HttpStatusCode.Forbidden, "domain0.viewUsers permission required")]
        public async Task<object> PhoneByUserId()
        {
            this.RequiresAuthentication();
            this.RequiresClaims(c =>
                c.Type == TokenClaims.CLAIM_PERMISSIONS
                && c.Value.Contains(TokenClaims.CLAIM_PERMISSIONS_VIEW_USERS));

            int id;
            if (!int.TryParse(Request.Query[nameof(id)], out id))
            {
                ModelValidationResult.Errors.Add(nameof(id), "bad format");
                throw new BadModelException(ModelValidationResult);
            }

            var result = await accountService.GetProfileByUserId(id);
            if (result.Phone == null)
                throw new NotFoundException(nameof(result.Phone));

            return long.Parse(result.Phone);
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
            var response = await accountService.Refresh(refreshToken);
            return response;
        }

        [Route(nameof(ForceCreateUser))]
        [Route(HttpMethod.Put, ForceCreateUserUrl)]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Sms" }, Summary = "Method for registration by phone")]
        [RouteParam(ParamIn = ParameterIn.Body, Name = "request", ParamType = typeof(ForceCreateUserRequest), Required = true, Description = "parameters for force create")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(UserProfile))]
        [SwaggerResponse(HttpStatusCode.BadRequest, "wrong phone or user with this phone already created")]
        [SwaggerResponse(HttpStatusCode.InternalServerError, "internal error during request execution")]
        [SwaggerResponse(HttpStatusCode.Unauthorized, "authentication required. jwt token in header")]
        [SwaggerResponse(HttpStatusCode.Forbidden, "domain0.forceCreateUser permission required")]
        public async Task<object> ForceCreateUser()
        {
            this.RequiresAuthentication();
            this.RequiresClaims(c =>
                c.Type == TokenClaims.CLAIM_PERMISSIONS
                && c.Value.Contains(TokenClaims.CLAIM_PERMISSIONS_FORCE_CREATE_USER));

            var request = this.BindAndValidateModel<ForceCreateUserRequest>();
            try
            {
                return await accountService.CreateUser(request);
            }
            catch (SecurityException)
            {
                ModelValidationResult.Errors.Add(nameof(request.Phone), "user exists");
                throw new BadModelException(ModelValidationResult);
            }
            catch (ArgumentException ex)
            {
                ModelValidationResult.Errors.Add("wrong data: ", ex.Message);
                throw new BadModelException(ModelValidationResult);
            }
        }

        [Route(nameof(ForceChangePhone))]
        [Route(HttpMethod.Post, ForceChangePhoneUrl)]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Produces = new string[] { })]
        [Route(Tags = new[] { "Sms" }, Summary = "Method for force change phone only administrator")]
        [RouteParam(ParamIn = ParameterIn.Body, Name = "phone", ParamType = typeof(ChangePhoneRequest), Required = true, Description = "parameters for change phone")]
        [SwaggerResponse(HttpStatusCode.NoContent, "operation completes successfully, phone was changed")]
        [SwaggerResponse(HttpStatusCode.BadRequest, "wrong code/user pair")]
        [SwaggerResponse(HttpStatusCode.InternalServerError, "internal error during request execution")]
        [SwaggerResponse(HttpStatusCode.Unauthorized, "authentication required. jwt token in header")]
        [SwaggerResponse(HttpStatusCode.Forbidden, "domain0.forceChangePhone permission required")]
        public async Task<object> ForceChangePhone()
        {
            this.RequiresAuthentication();
            this.RequiresClaims(c =>
                c.Type == TokenClaims.CLAIM_PERMISSIONS
                && c.Value.Contains(TokenClaims.CLAIM_PERMISSIONS_FORCE_CHANGE_PHONE));

            var request = this.BindAndValidateModel<ChangePhoneRequest>();
            await accountService.ForceChangePhone(request);
            return HttpStatusCode.NoContent;
        }

        [Route(nameof(ForceResetPassword))]
        [Route(HttpMethod.Post, ForceResetPasswordUrl)]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Produces = new string[] { })]
        [Route(Tags = new[] { "Sms" }, Summary = "Method for force reset password only administrator")]
        [RouteParam(
            ParamIn = ParameterIn.Body, 
            Name = "phone", 
            ParamType = typeof(long), 
            Required = true, 
            Description = "user's phone with single number, started from 7 for Russia, 71234561234 for example")]
        [SwaggerResponse(HttpStatusCode.NoContent, "operation completes successfully, new password sent to user")]
        [SwaggerResponse(HttpStatusCode.BadRequest, "wrong phone or user with this phone not found")]
        [SwaggerResponse(HttpStatusCode.InternalServerError, "internal error during request execution")]
        [SwaggerResponse(HttpStatusCode.Unauthorized, "authentication required. jwt token in header")]
        [SwaggerResponse(HttpStatusCode.Forbidden, "domain0.forceResetPassword permission required")]
        public async Task<object> ForceResetPassword()
        {
            this.RequiresAuthentication();
            this.RequiresClaims(c =>
                c.Type == TokenClaims.CLAIM_PERMISSIONS
                && c.Value.Contains(TokenClaims.CLAIM_PERMISSIONS_FORCE_PASSWORD_RESET));

            var phone = this.BindAndValidateModel<long>();
            await accountService.ForceResetPassword(phone);
            return HttpStatusCode.NoContent;
        }

        [Route(nameof(RequestChangePhone))]
        [Route(HttpMethod.Post, RequestChangePhoneUrl)]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Produces = new string[] { })]
        [Route(Tags = new[] { "Sms" }, Summary = "Method for change phone")]
        [RouteParam(
            ParamIn = ParameterIn.Body, 
            Name = "change phone request", 
            ParamType = typeof(ChangePhoneUserRequest), 
            Required = true, 
            Description = "request with password and new phone number")]
        [SwaggerResponse(HttpStatusCode.NoContent, "operation completes successfully, validation code sent to phone")]
        [SwaggerResponse(HttpStatusCode.BadRequest, "wrong password or incorrect new phone")]
        [SwaggerResponse(HttpStatusCode.InternalServerError, "internal error during request execution")]
        [SwaggerResponse(HttpStatusCode.Unauthorized, "authentication required. jwt token in header")]
        [SwaggerResponse(HttpStatusCode.Forbidden, "domain0.basic permission required")]
        public async Task<object> RequestChangePhone()
        {
            var changePhoneRequest = this.BindAndValidateModel<ChangePhoneUserRequest>();

            requestThrottleManager.RequiresThrottling(
                this, ThrottlingProperties.Path,
                ThrottlingPeriod.Minute, requestCountLimit: 1,
                requestKeys: changePhoneRequest.Phone.ToString());

            requestThrottleManager.RequiresThrottling(
                this, ThrottlingProperties.Path,
                ThrottlingPeriod.Hour, requestCountLimit: 10,
                requestKeys: changePhoneRequest.Phone.ToString());

            this.RequiresAuthentication();
            this.RequiresClaims(c =>
                c.Type == TokenClaims.CLAIM_PERMISSIONS
                && c.Value.Contains(TokenClaims.CLAIM_PERMISSIONS_BASIC));

            await accountService.RequestChangePhone(changePhoneRequest);
            return HttpStatusCode.NoContent;
        }

        [Route(nameof(CommitChangePhone))]
        [Route(HttpMethod.Post, CommitChangePhoneUrl)]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Produces = new string[] { })]
        [Route(Tags = new[] { "Sms" }, Summary = "Method for commit change phone request")]
        [RouteParam(
            ParamIn = ParameterIn.Query,
            Name = "code",
            ParamType = typeof(long),
            Required = true,
            Description = "user's pin code for change phone")]
        [SwaggerResponse(HttpStatusCode.NoContent, "operation completes successfully, phone was changed")]
        [SwaggerResponse(HttpStatusCode.BadRequest, "wrong code/user pair")]
        [SwaggerResponse(HttpStatusCode.InternalServerError, "internal error during request execution")]
        [SwaggerResponse(HttpStatusCode.Unauthorized, "authentication required. jwt token in header")]
        [SwaggerResponse(HttpStatusCode.Forbidden, "domain0.basic permission required")]
        public async Task<object> CommitChangePhone()
        {
            this.RequiresAuthentication();
            this.RequiresClaims(c =>
                c.Type == TokenClaims.CLAIM_PERMISSIONS
                && c.Value.Contains(TokenClaims.CLAIM_PERMISSIONS_BASIC));

            requestThrottleManager.RequiresThrottling(
                this, ThrottlingProperties.Path,
                ThrottlingPeriod.Minute, requestCountLimit: 10,
                requestKeys: requestContext.UserId.ToString());

            requestThrottleManager.RequiresThrottling(
                this, ThrottlingProperties.Path,
                ThrottlingPeriod.Hour, requestCountLimit: 100,
                requestKeys: requestContext.UserId.ToString());

            long code;
            if (!long.TryParse(Request.Query[nameof(code)].ToString(), out code))
            {
                ModelValidationResult.Errors.Add(nameof(code), "wrong pin code format");
                throw new BadModelException(ModelValidationResult);
            }

            await accountService.CommitChangePhone(code);
            return HttpStatusCode.NoContent;
        }

    }
}
