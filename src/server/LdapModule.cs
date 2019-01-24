using System.Threading.Tasks;
using Domain0.Exceptions;
using Domain0.Model;
using Domain0.Nancy.Infrastructure;
using Domain0.Nancy.Model;
using Domain0.Service;
using Domain0.Service.Throttling;
using Nancy;
using Nancy.Swagger.Annotations.Attributes;
using NLog;
using Swagger.ObjectModel;

namespace Domain0.Nancy
{
    public sealed class LdapModule : NancyModule
    {
        private readonly IAccountService _accountServiceInstance;
        private readonly ILogger _loggerInstance;
        private readonly IRequestContext _requestContextInstance;
        private readonly IRequestThrottleManager _requestThrottleManagerInstance;

        public const string LoginByDomainUserWithEnvironmentUrl = "/api/ldap/Login/{EnvironmentToken}";
        public const string LoginByDomainUserUrl = "/api/ldap/Login";

        public LdapModule(
            IAccountService accountServiceInstance,
            ILogger loggerInstance,
            IRequestContext requestContextInstance,
            IRequestThrottleManager requestThrottleManagerInstance)
        {
            _accountServiceInstance = accountServiceInstance;
            _loggerInstance = loggerInstance;
            _requestContextInstance = requestContextInstance;
            _requestThrottleManagerInstance = requestThrottleManagerInstance;

            Post(LoginByDomainUserWithEnvironmentUrl, ctx => LoginByDomainUserWithEnvironment(), name: nameof(LoginByDomainUserWithEnvironment));
            Post(LoginByDomainUserUrl, ctx => LoginByDomainUser(), name: nameof(LoginByDomainUser));
        }

        [Route(nameof(LoginByDomainUserWithEnvironment))]
        [Route(HttpMethod.Post, LoginByDomainUserWithEnvironmentUrl)]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Ldap" }, Summary = "Method for login by domain user")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "request",
            ParamType = typeof(ActiveDirectoryUserLoginRequest),
            Required = true, Description = "parameters for login")]
        [RouteParam(
            ParamIn = ParameterIn.Path,
            Name = "EnvironmentToken",
            ParamType = typeof(string),
            Required = false,
            Description = "user's environment token")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(AccessTokenResponse))]
        [SwaggerResponse(HttpStatusCode.BadRequest, "wrong user name format / wrong username and password pair")]
        [SwaggerResponse(HttpStatusCode.InternalServerError, "internal error during request execution")]
        public async Task<object> LoginByDomainUserWithEnvironment()
        {
            return await LoginInternal();
        }

        [Route(nameof(LoginByDomainUser))]
        [Route(HttpMethod.Post, LoginByDomainUserUrl)]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "Ldap" }, Summary = "Method for login by domain user")]
        [RouteParam(
            ParamIn = ParameterIn.Body,
            Name = "request",
            ParamType = typeof(ActiveDirectoryUserLoginRequest),
            Required = true, Description = "parameters for login")]
        [SwaggerResponse(HttpStatusCode.OK, Message = "Success", Model = typeof(AccessTokenResponse))]
        [SwaggerResponse(HttpStatusCode.BadRequest, "wrong user name format / wrong username and password pair")]
        [SwaggerResponse(HttpStatusCode.InternalServerError, "internal error during request execution")]
        public async Task<object> LoginByDomainUser()
        {
            return await LoginInternal();
        }


        private async Task<object> LoginInternal()
        {
            var request = this.BindAndValidateModel<ActiveDirectoryUserLoginRequest>();
            request.UserName = request.UserName.Trim();

            _requestThrottleManagerInstance.RequiresThrottling(
                this, ThrottlingProperties.Path,
                ThrottlingPeriod.Minute, requestCountLimit: 20,
                requestKeys: request.UserName);

            _requestThrottleManagerInstance.RequiresThrottling(
                this, ThrottlingProperties.Path,
                ThrottlingPeriod.Hour, requestCountLimit: 100,
                requestKeys: request.UserName);

            var environmentToken = (string)Context.Parameters.EnvironmentToken;

            var result = await _accountServiceInstance.Login(request, environmentToken);
            if (result == null)
            {
                ModelValidationResult.Errors.Add(nameof(request.UserName), "user or password incorrect");
                throw new BadModelException(ModelValidationResult);
            }

            return result;
        }
    }
}
