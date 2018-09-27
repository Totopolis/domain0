using System.Security;
using System.Threading.Tasks;
using Domain0.Exceptions;
using Domain0.Model;
using Domain0.Nancy.Infrastructure;
using Domain0.Service;
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
        public const string ForceChangeEmailUrl = "/api/email/ForceChangeEmail";
        public const string DoesUserExistByEmailUrl = "/api/email/DoesUserExist";

        public EmailModule(
            IAccountService accountServiceInstance,
            ILogger loggerInstance)
        {
            accountService = accountServiceInstance;
            logger = loggerInstance;

            Put(RegisterByEmailUrl, ctx => RegisterByEmail(), name: nameof(RegisterByEmail));
            Post(LoginByEmailUrl, ctx => LoginByEmail(), name: nameof(LoginByEmail));
            Post(DoesUserExistByEmailUrl, ctx => DoesUserExistByEmail(), name: nameof(DoesUserExistByEmail));
            Post(RequestResetPasswordByEmailUrl, ctx => RequestResetPasswordByEmail(), name: nameof(RequestResetPasswordByEmail));
            Post(ForceChangeEmailUrl, ctx => ForceChangeEmail(), name: nameof(ForceChangeEmail));
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
        public async Task<object> RegisterByEmail()
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
        public async Task<object> LoginByEmail()
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
        public async Task<object> DoesUserExistByEmail()
        {
            var request = this.BindAndValidateModel<RegisterRequest>();
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
        [SwaggerResponse(HttpStatusCode.NoContent, Message = "Success")]
        public async Task<object> RequestResetPasswordByEmail()
        {
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
