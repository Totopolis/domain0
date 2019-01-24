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

            Post(LoginByDomainUserUrl, ctx => LoginByDomainUser(), name: nameof(LoginByDomainUser));
        }

        [Route(nameof(LoginByDomainUser))]
        [Route(HttpMethod.Post, LoginByDomainUserUrl)]
        [Route(Consumes = new[] { "application/json", "application/x-protobuf" })]
        [Route(Produces = new[] { "application/json", "application/x-protobuf" })]
        [Route(Tags = new[] { "UserName" }, Summary = "Method for login by domain user")]
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

            var result = await _accountServiceInstance.Login(request);
            if (result == null)
            {
                ModelValidationResult.Errors.Add(nameof(request.UserName), "user or password incorrect");
                throw new BadModelException(ModelValidationResult);
            }

            return result;
        }


    }
}
