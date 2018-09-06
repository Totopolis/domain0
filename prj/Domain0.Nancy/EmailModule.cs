using System.Security;
using System.Threading.Tasks;
using Domain0.Exceptions;
using Domain0.Model;
using Domain0.Nancy.Infrastructure;
using Domain0.Nancy.Model;
using Domain0.Service;
using Domain0.Service.Tokens;
using Nancy;
using Nancy.Security;
using Nancy.Swagger.Annotations.Attributes;
using NLog;
using Swagger.ObjectModel;

namespace Domain0.Nancy
{
    public class EmailModule : NancyModule
    {
        public const string RegisterUrl = "/api/email/Register";
        public const string LoginUrl = "/api/email/Login";
        public const string ChangePasswordUrl = "/api/email/ChangePassword";
        public const string RequestResetPasswordUrl = "/api/email/RequestResetPassword";
        public const string ForceChangeEmailUrl = "/api/email/ForceChangeEmail";
        public const string DoesUserExistUrl = "/api/email/DoesUserExist";

        public EmailModule(
            IAccountService accountServiceInstance,
            ILogger loggerInstance)
        {
            accountService = accountServiceInstance;
            logger = loggerInstance;

            Put(RegisterUrl, ctx => Register(), name: nameof(Register));
            Post(LoginUrl, ctx => Login(), name: nameof(Login));
            Post(ChangePasswordUrl, ctx => ChangePassword(), name: nameof(ChangePassword));
            Post(DoesUserExistUrl, ctx => DoesUserExist(), name: nameof(DoesUserExist));
            Post(RequestResetPasswordUrl, ctx => RequestResetPassword(), name: nameof(RequestResetPassword));
            Post(ForceChangeEmailUrl, ctx => ForceChangeEmail(), name: nameof(ForceChangeEmail));
        }

        [Route(nameof(Register))]
        [Route(HttpMethod.Put, RegisterUrl)]
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
        public async Task<object> Register()
        {
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

        [Route(nameof(Login))]
        [Route(HttpMethod.Post, LoginUrl)]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Email" }, Summary = "Method for login by email")]
        [RouteParam(
            ParamIn = ParameterIn.Body, 
            Name = "request", 
            ParamType = typeof(EmailLoginRequest), 
            Required = true, Description = "parameters for login")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(AccessTokenResponse))]
        public async Task<object> Login()
        {
            var request = this.BindAndValidateModel<EmailLoginRequest>();

            var result = await accountService.Login(request);
            if (result == null)
            {
                ModelValidationResult.Errors.Add(nameof(request.Email), "user or password incorrect");
                throw new BadModelException(ModelValidationResult);
            }

            return result;
        }

        [Route(nameof(ChangePassword))]
        [Route(HttpMethod.Post, ChangePasswordUrl)]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Email" }, Summary = "Method for change password")]
        [RouteParam(
            ParamIn = ParameterIn.Body, 
            Name = "request", 
            ParamType = typeof(ChangePasswordRequest), 
            Required = true, 
            Description = "parameters for change password")]
        [SwaggerResponse(HttpStatusCode.NoContent, Message = "Success")]
        public async Task<object> ChangePassword()
        {
            this.RequiresAuthentication();
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

        [Route(nameof(DoesUserExist))]
        [Route(HttpMethod.Post, DoesUserExistUrl)]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Email" }, Summary = "Method for check user exists")]
        [RouteParam(
            ParamIn = ParameterIn.Query, 
            Name = "email", 
            ParamType = typeof(RegisterRequest), 
            Required = true, 
            Description = "user's email")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "True if user exists else false", Model = typeof(bool))]
        public async Task<object> DoesUserExist()
        {
            var request = this.BindAndValidateModel<RegisterRequest>();
            var result = await accountService.DoesUserExists(request.Email);
            return result;
        }

        [Route(nameof(RequestResetPassword))]
        [Route(HttpMethod.Post, RequestResetPasswordUrl)]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Email" }, Summary = "Method for reset password")]
        [RouteParam(
            ParamIn = ParameterIn.Body, 
            Name = "email", 
            ParamType = typeof(RegisterRequest), 
            Required = true, 
            Description = "user's email")]
        [SwaggerResponse(HttpStatusCode.NoContent, Message = "Success")]
        public async Task<object> RequestResetPassword()
        {
            this.RequiresAuthentication();
            var request = this.BindAndValidateModel<RegisterRequest>();
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
        [SwaggerResponse(HttpStatusCode.NoContent, Message = "Success")]
        public async Task<object> ForceChangeEmail()
        {
            this.RequiresAuthentication();
            this.RequiresClaims(c =>
                c.Type == TokenClaims.CLAIM_PERMISSIONS
                && c.Value.Contains(TokenClaims.CLAIM_PERMISSIONS_ADMIN));

            var request = this.BindAndValidateModel<ChangeEmailRequest>();
            await accountService.ForceChangeEmail(request);
            return HttpStatusCode.NoContent;
        }


        private readonly IAccountService accountService;
        private readonly ILogger logger;
    }
}
