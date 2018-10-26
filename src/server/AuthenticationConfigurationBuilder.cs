using Domain0.Service;
using Nancy.Authentication.Stateless;
using System;

namespace Domain0.Nancy.Infrastructure
{
    internal class AuthenticationConfigurationBuilder : IAuthenticationConfigurationBuilder
    {
        public AuthenticationConfigurationBuilder(ITokenGenerator tokenGeneratorInstance)
        {
            tokenGenerator = tokenGeneratorInstance;
        }

        public StatelessAuthenticationConfiguration Build()
        {
            var configuration = new StatelessAuthenticationConfiguration(ctx =>
            {
                var jwtToken = ctx.Request.Headers.Authorization;

                if (string.IsNullOrWhiteSpace(jwtToken))
                    return null;

                try
                {
                    return tokenGenerator.Parse(jwtToken
                        .Replace("Bearer ", "")
                        .Replace("bearer ", ""));
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
