using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Domain0.Service.Tokens;
using Domain0.Tokens;
using Nancy.Authentication.Stateless;
using Nancy.Bootstrapper;
using Newtonsoft.Json;

namespace Domain0.Auth.Nancy
{
    public static class NancyExtensions
    {
        public static void AddDomain0Auth(this IPipelines pipelines, TokenValidationSettings settings)
        {
            StatelessAuthentication.Enable(
                pipelines,
                BuildAuthConfiguration(settings));
        }

        private const string TokenPrefix = "Bearer ";

        private static StatelessAuthenticationConfiguration BuildAuthConfiguration(TokenValidationSettings settings)
        {
            var configuration = new StatelessAuthenticationConfiguration(
                ctx =>
                {
                    try
                    {
                        var authorization = ctx.Request.Headers.Authorization;

                        if (string.IsNullOrWhiteSpace(authorization))
                            return null;

                        if (!authorization.StartsWith(TokenPrefix, StringComparison.OrdinalIgnoreCase))
                            return null;

                        var jwtToken = authorization.Remove(0, TokenPrefix.Length);

                        var handler = new JwtSecurityTokenHandler {SetDefaultTimesOnTokenCreation = false};

                        var principal = handler.ValidateToken(
                            jwtToken,
                            settings.BuildTokenValidationParameters(),
                            out _);

                        ParsePermissions(principal, jwtToken);

                        return principal;
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                });

            return configuration;
        }

        private static void ParsePermissions(ClaimsPrincipal principal, string jwtToken)
        {
            var identity = (ClaimsIdentity) principal.Identity;
            identity.AddClaim(new Claim("id_token", jwtToken));
            foreach (var role in principal.FindAll(TokenClaims.CLAIM_PERMISSIONS))
            {
                foreach (var permission in JsonConvert.DeserializeObject<string[]>(role.Value))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, permission));
                }
            }

            var subClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            if (subClaim != null)
                identity.AddClaim(new Claim(ClaimTypes.Name, subClaim.Value));
        }
    }
}