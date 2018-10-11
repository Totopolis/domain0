using System.IO;
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
    public sealed class EmailModule : NancyModule
    {
        public const string RegisterByEmailUrl = "/api/email/Register";
        public const string LoginByEmailUrl = "/api/email/Login";
        public const string RequestResetPasswordByEmailUrl = "/api/email/RequestResetPassword";
        public const string DoesUserExistByEmailUrl = "/api/email/DoesUserExist";

        public const string ForceChangeEmailUrl = "/api/email/ForceChangeEmail";
        public const string ForceCreateUserUrl = "/api/email/ForceCreateUser";
        public const string ForceResetPasswordUrl = "/api/email/ForceResetPassword";

        public const string RequestChangeEmailUrl = "/api/email/RequestChangeEmail";
        public const string CommitChangeEmailUrl = "/api/email/CommitChangeEmail";


        public EmailModule(
            IAccountService accountServiceInstance,
            ILogger loggerInstance,
            IRequestContext requestContextInstance,
            IRequestThrottleManager requestThrottleManagerInstance)
        {
            accountService = accountServiceInstance;
            requestContext = requestContextInstance;
            requestThrottleManager = requestThrottleManagerInstance;
            logger = loggerInstance;

            Put(RegisterByEmailUrl, ctx => RegisterByEmail(), name: nameof(RegisterByEmail));
            Post(LoginByEmailUrl, ctx => LoginByEmail(), name: nameof(LoginByEmail));
            Post(DoesUserExistByEmailUrl, ctx => DoesUserExistByEmail(), name: nameof(DoesUserExistByEmail));
            Post(RequestResetPasswordByEmailUrl, ctx => RequestResetPasswordByEmail(), name: nameof(RequestResetPasswordByEmail));

            Post(ForceChangeEmailUrl, ctx => ForceChangeEmail(), name: nameof(ForceChangeEmail));
            Put(ForceCreateUserUrl, ctx => ForceCreateUser(), name: nameof(ForceCreateUser));
            Post(ForceResetPasswordUrl, ctx => ForceResetPassword(), name: nameof(ForceResetPassword));

            Post(RequestChangeEmailUrl, ctx => RequestChangeEmail(), name: nameof(RequestChangeEmail));
            Post(CommitChangeEmailUrl, ctx => CommitChangeEmail(), name: nameof(CommitChangeEmail));
        }

        [Route(nameof(RegisterByEmail))]
        [Route(HttpMethod.Put, RegisterByEmailUrl)]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Email" }, Summary = "Method for registration by email")]
        [RouteParam(
            ParamIn = ParameterIn.Body, 
            Name = "email", 
            ParamType = typeof(RegisterRequest), 
            Required = true, 
            Description = "user's email")]
        [SwaggerResponse(HttpStatusCode.NoContent, Message = "Success")]
        [SwaggerResponse(HttpStatusCode.BadRequest, "wrong email format or user with this email already existed")]
        [SwaggerResponse(HttpStatusCode.InternalServerError, "internal error during request execution")]
        public async Task<object> RegisterByEmail()
        {
            requestThrottleManager.RequiresThrottling(
                this, ThrottlingProperties.Path | ThrottlingProperties.RemoteIp,
                ThrottlingPeriod.Minute, requestCountLimit: 100);

            requestThrottleManager.RequiresThrottling(
                this, ThrottlingProperties.Path | ThrottlingProperties.RemoteIp,
                ThrottlingPeriod.Hour, requestCountLimit: 300);

            var request = this.BindAndValidateModel<RegisterRequest>();
            try
            {
                await accountService.Register(request.Email);
            }
            catch (SecurityException ex)
            {
                ModelValidationResult.Errors.Add(nameof(request.Email), ex.Message);
                throw new BadModelException(ModelValidationResult);
            }

            return HttpStatusCode.NoContent;
        }

        [Route(nameof(LoginByEmail))]
        [Route(HttpMethod.Post, LoginByEmailUrl)]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Email" }, Summary = "Method for login by email")]
        [RouteParam(
            ParamIn = ParameterIn.Body, 
            Name = "request", 
            ParamType = typeof(EmailLoginRequest), 
            Required = true, Description = "parameters for login")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(AccessTokenResponse))]
        [SwaggerResponse(HttpStatusCode.BadRequest, "wrong email format / wrong email and password pair")]
        [SwaggerResponse(HttpStatusCode.InternalServerError, "internal error during request execution")]
        public async Task<object> LoginByEmail()
        {
            var request = this.BindAndValidateModel<EmailLoginRequest>();

            requestThrottleManager.RequiresThrottling(
                this, ThrottlingProperties.Path,
                ThrottlingPeriod.Minute, requestCountLimit: 20,
                requestKeys: request.Email);

            requestThrottleManager.RequiresThrottling(
                this, ThrottlingProperties.Path,
                ThrottlingPeriod.Hour, requestCountLimit: 100,
                requestKeys: request.Email);

            var result = await accountService.Login(request);
            if (result == null)
            {
                ModelValidationResult.Errors.Add(nameof(request.Email), "user or password incorrect");
                throw new BadModelException(ModelValidationResult);
            }

            return result;
        }

        [Route(nameof(DoesUserExistByEmail))]
        [Route(HttpMethod.Post, DoesUserExistByEmailUrl)]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Email" }, Summary = "Method for check user exists")]
        [RouteParam(
            ParamIn = ParameterIn.Body, 
            Name = "email", 
            ParamType = typeof(RegisterRequest), 
            Required = true, 
            Description = "user's email")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "True if user exists else false", Model = typeof(bool))]
        [SwaggerResponse(HttpStatusCode.BadRequest, "wrong email format")]
        [SwaggerResponse(HttpStatusCode.InternalServerError, "internal error during request execution")]
        public async Task<object> DoesUserExistByEmail()
        {
            var request = this.BindAndValidateModel<RegisterRequest>();

            requestThrottleManager.RequiresThrottling(
                this, ThrottlingProperties.Path,
                ThrottlingPeriod.Minute, requestCountLimit: 100);

            requestThrottleManager.RequiresThrottling(
                this, ThrottlingProperties.Path,
                ThrottlingPeriod.Hour, requestCountLimit: 300);

            var result = await accountService.DoesUserExists(request.Email);
            return result;
        }

        [Route(nameof(RequestResetPasswordByEmail))]
        [Route(HttpMethod.Post, RequestResetPasswordByEmailUrl)]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Email" }, Summary = "Method for reset password")]
        [RouteParam(
            ParamIn = ParameterIn.Body, 
            Name = "email", 
            ParamType = typeof(RegisterRequest), 
            Required = true, 
            Description = "user's email")]
        [SwaggerResponse(HttpStatusCode.BadRequest, "user with this email doesn't exist")]
        [SwaggerResponse(HttpStatusCode.InternalServerError, "internal error during request execution")]
        [SwaggerResponse(HttpStatusCode.NoContent, "operation completes successfully, code has been sent")]
        public async Task<object> RequestResetPasswordByEmail()
        {
            var request = this.BindAndValidateModel<RegisterRequest>();

            requestThrottleManager.RequiresThrottling(
                this, ThrottlingProperties.Path,
                ThrottlingPeriod.Minute, requestCountLimit: 1,
                requestKeys: request.Email);

            requestThrottleManager.RequiresThrottling(
                this, ThrottlingProperties.Path,
                ThrottlingPeriod.Hour, requestCountLimit: 10,
                requestKeys: request.Email);

            await accountService.RequestResetPassword(request.Email);
            return HttpStatusCode.NoContent;
        }

        [Route(nameof(ForceChangeEmail))]
        [Route(HttpMethod.Post, ForceChangeEmailUrl)]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Email" }, Summary = "Method for force change phone only administrator")]
        [RouteParam(
            ParamIn = ParameterIn.Body, 
            Name = "Change email request", 
            ParamType = typeof(ChangeEmailRequest), 
            Required = true, 
            Description = "parameters for change email")]
        [SwaggerResponse(HttpStatusCode.NoContent, "operation completes successfully, email was changed")]
        [SwaggerResponse(HttpStatusCode.BadRequest, "wrong code/user pair")]
        [SwaggerResponse(HttpStatusCode.InternalServerError, "internal error during request execution")]
        [SwaggerResponse(HttpStatusCode.Unauthorized, "authentication required. jwt token in header")]
        [SwaggerResponse(HttpStatusCode.Forbidden, "domain0.forceChangePhone permission required")]
        public async Task<object> ForceChangeEmail()
        {
            this.RequiresAuthentication();
            this.RequiresClaims(c =>
                c.Type == TokenClaims.CLAIM_PERMISSIONS
                && c.Value.Contains(TokenClaims.CLAIM_PERMISSIONS_FORCE_CHANGE_EMAIL));

            var request = this.BindAndValidateModel<ChangeEmailRequest>();
            await accountService.ForceChangeEmail(request);
            return HttpStatusCode.NoContent;
        }

        [Route(nameof(ForceCreateUser))]
        [Route(HttpMethod.Put, ForceCreateUserUrl)]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Email" }, Summary = "Method for force registration by email")]
        [RouteParam(
            ParamIn = ParameterIn.Body, 
            Name = "request", 
            ParamType = typeof(ForceCreateEmailUserRequest), 
            Required = true, 
            Description = "parameters for force create")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(UserProfile))]
        [SwaggerResponse(HttpStatusCode.BadRequest, "wrong email or user with this email already created")]
        [SwaggerResponse(HttpStatusCode.InternalServerError, "internal error during request execution")]
        [SwaggerResponse(HttpStatusCode.Unauthorized, "authentication required. jwt token in header")]
        [SwaggerResponse(HttpStatusCode.Forbidden, "domain0.forceCreateUser permission required")]
        public async Task<object> ForceCreateUser()
        {
            this.RequiresAuthentication();
            this.RequiresClaims(c =>
                c.Type == TokenClaims.CLAIM_PERMISSIONS
                && c.Value.Contains(TokenClaims.CLAIM_PERMISSIONS_FORCE_CREATE_USER));

            var request = this.BindAndValidateModel<ForceCreateEmailUserRequest>();
            try
            {
                return await accountService.CreateUser(request);
            }
            catch (SecurityException)
            {
                ModelValidationResult.Errors.Add(nameof(request.Email), "user exists");
                throw new BadModelException(ModelValidationResult);
            }
        }

        [Route(nameof(ForceResetPassword))]
        [Route(HttpMethod.Post, ForceResetPasswordUrl)]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Produces = new string[] { })]
        [Route(Tags = new[] { "Email" }, Summary = "Method for force reset password only administrator")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "email",
            ParamType = typeof(string),
            Required = true,
            Description = "user's email")]
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

            var email = await Context.Request.Body.AsString();
            await accountService.ForceResetPassword(email);
            return HttpStatusCode.NoContent;
        }

        [Route(nameof(RequestChangeEmail))]
        [Route(HttpMethod.Post, RequestChangeEmailUrl)]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Produces = new string[] { })]
        [Route(Tags = new[] { "Email" }, Summary = "Method for reset email")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "change email request",
            ParamType = typeof(ChangeEmailUserRequest),
            Required = true,
            Description = "request with password and new email")]
        [SwaggerResponse(HttpStatusCode.NoContent, "operation completes successfully, validation code sent to email")]
        [SwaggerResponse(HttpStatusCode.BadRequest, "wrong password or incorrect new email")]
        [SwaggerResponse(HttpStatusCode.InternalServerError, "internal error during request execution")]
        [SwaggerResponse(HttpStatusCode.Unauthorized, "authentication required. jwt token in header")]
        [SwaggerResponse(HttpStatusCode.Forbidden, "domain0.basic permission required")]
        public async Task<object> RequestChangeEmail()
        {
            var changeEmailRequest = this.BindAndValidateModel<ChangeEmailUserRequest>();

            requestThrottleManager.RequiresThrottling(
                this, ThrottlingProperties.Path,
                ThrottlingPeriod.Minute, requestCountLimit: 1,
                requestKeys: changeEmailRequest.Email);

            requestThrottleManager.RequiresThrottling(
                this, ThrottlingProperties.Path,
                ThrottlingPeriod.Hour, requestCountLimit: 10,
                requestKeys: changeEmailRequest.Email);

            this.RequiresAuthentication();
            this.RequiresClaims(c =>
                c.Type == TokenClaims.CLAIM_PERMISSIONS
                && c.Value.Contains(TokenClaims.CLAIM_PERMISSIONS_BASIC));

            await accountService.RequestChangeEmail(changeEmailRequest);
            return HttpStatusCode.NoContent;
        }

        [Route(nameof(CommitChangeEmail))]
        [Route(HttpMethod.Post, CommitChangeEmailUrl)]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Produces = new string[] { })]
        [Route(Tags = new[] { "Email" }, Summary = "Method for commit change email request")]
        [RouteParam(
            ParamIn = ParameterIn.Query,
            Name = "code",
            ParamType = typeof(long),
            Required = true,
            Description = "user's pin code for change email")]
        [SwaggerResponse(HttpStatusCode.NoContent, "operation completes successfully, email was changed")]
        [SwaggerResponse(HttpStatusCode.BadRequest, "wrong code/user pair")]
        [SwaggerResponse(HttpStatusCode.InternalServerError, "internal error during request execution")]
        [SwaggerResponse(HttpStatusCode.Unauthorized, "authentication required. jwt token in header")]
        [SwaggerResponse(HttpStatusCode.Forbidden, "domain0.basic permission required")]
        public async Task<object> CommitChangeEmail()
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

            await accountService.CommitChangeEmail(code);
            return HttpStatusCode.NoContent;
        }

        private readonly IAccountService accountService;
        private readonly IRequestThrottleManager requestThrottleManager;
        private readonly ILogger logger;
        private readonly IRequestContext requestContext;
    }
}
