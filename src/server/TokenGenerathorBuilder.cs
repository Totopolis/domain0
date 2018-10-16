using Autofac;
using Domain0.Service;
using Microsoft.IdentityModel.Tokens;
using Nancy;
using System.Linq;

namespace Domain0.Nancy.Infrastructure
{
    internal class TokenGeneratorBuilder
    {
        public const string ACCEPT_TOKEN_ALGORITHM_HEADER = "Accept-Token-Algorithm";

        public static ITokenGenerator Build(ILifetimeScope requestContainer, NancyContext context)
        {
            ITokenGenerator tokenGenerator = null;

            if (context.Request != null
                && context.Request.Headers[ACCEPT_TOKEN_ALGORITHM_HEADER] != null
                && context.Request.Headers[ACCEPT_TOKEN_ALGORITHM_HEADER].Contains(SecurityAlgorithms.RsaSha256))
            {
                tokenGenerator = requestContainer.ResolveKeyed<ITokenGenerator>(SecurityAlgorithms.RsaSha256);
            }
            else
            {
                var settings = requestContainer.Resolve<TokenGeneratorSettings>();
                if (string.IsNullOrWhiteSpace(settings.Alg))
                    tokenGenerator = requestContainer.ResolveKeyed<ITokenGenerator>(SecurityAlgorithms.HmacSha256);
                else
                    tokenGenerator = requestContainer.ResolveKeyed<ITokenGenerator>(settings.Alg);
            }

            return tokenGenerator;
        }
    }
}
