using Domain0.Service;
using Nancy.Authentication.Stateless;
using System;

namespace Domain0.Nancy.Infrastructure
{
    internal class AuthenticationConfigurationBuilder : IAuthenticationConfigurationBuilder
    {
        private const string TokenPrefix = "Bearer ";

        public AuthenticationConfigurationBuilder(ITokenGenerator tokenGeneratorInstance)
        {
            tokenGenerator = tokenGeneratorInstance;
        }

        public StatelessAuthenticationConfiguration Build()
        {
            var configuration = new StatelessAuthenticationConfiguration(ctx =>
            {
                var authorization = ctx.Request.Headers.Authorization;

                if (string.IsNullOrWhiteSpace(authorization))
                    return null;

                if (!authorization.StartsWith(TokenPrefix, StringComparison.OrdinalIgnoreCase))
                    return null;

                var jwtToken = authorization.Remove(0, TokenPrefix.Length);

                try
                {
                    return tokenGenerator.Parse(jwtToken);
                }
                catch (Exception)
                {
                    return null;
                }
            });

            return configuration;
        }

        private readonly ITokenGenerator tokenGenerator;
    }
}
