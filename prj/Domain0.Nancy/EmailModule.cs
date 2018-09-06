using System.Security;
using System.Threading.Tasks;
using Domain0.Exceptions;
using Domain0.Model;
using Domain0.Nancy.Infrastructure;
using Domain0.Nancy.Model;
using Domain0.Service;
using Nancy;
using Nancy.Swagger.Annotations.Attributes;
using NLog;
using Swagger.ObjectModel;

namespace Domain0.Nancy
{
    public class EmailModule : NancyModule
    {
        public const string RegisterUrl = "/api/email/Register";
        public const string ForceCreateUserUrl = "/api/email/ForceCreateUser";
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
        [RouteParam(ParamIn = ParameterIn.Body, Name = "request", ParamType = typeof(EmailLoginRequest), Required = true, Description = "parameters for login")]
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


        private readonly IAccountService accountService;
        private readonly ILogger logger;
    }
}
