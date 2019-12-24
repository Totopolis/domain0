using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Domain0.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Domain0.Auth.AspNet
{
    public static class AspNetExtensions
    {
        public static void AddDomain0Auth(this IServiceCollection services, TokenValidationSettings settings)
        {
            var defaultPolicy = new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build();
            services.AddAuthorizationCore(opt => opt.AddPolicy(Domain0Auth.Policy, defaultPolicy));

            services.AddAuthentication().AddJwtBearer(options =>
            {
                // Automatically disable the HTTPS requirement for development scenarios.
                options.RequireHttpsMetadata = false; //!env.IsDevelopment();
                options.ConfigurationManager = null;
                options.MetadataAddress = null;
                options.Authority = null;

                options.TokenValidationParameters = settings.BuildTokenValidationParameters();

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context => Task.FromResult(0),
                    OnTokenValidated = context =>
                    {
                        //TODO use constants
                        var claimsIdentity = (ClaimsIdentity) context.Principal.Identity;
                        claimsIdentity.AddClaim(new Claim("id_token",
                            context.Request.Headers["Authorization"][0].Substring(
                                context.Scheme.Name.Length + 1)));

                        foreach (var role in context.Principal.FindAll("permissions"))
                        {
                            foreach (var permission in JsonConvert.DeserializeObject<string[]>(role.Value))
                            {
                                claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, permission));
                            }
                        }

                        var subClaim = context.Principal.FindFirst(
                            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
                        if (subClaim != null)
                        {
                            claimsIdentity.AddClaim(new Claim(ClaimTypes.Name, subClaim.Value));
                        }

                        return Task.FromResult(0);
                    },
                    OnChallenge = context =>
                    {
                        //TODO replace
                        if (string.Compare(context.Request.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase) != 0)
                            return Task.FromResult(0);
                        context.Response.StatusCode = 200;
                        context.Response.Headers.Add("Access-Control-Allow-Origin",
                            context.Request.Headers["Origin"][0]);
                        context.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
                        context.Response.Headers.Add("Access-Control-Allow-Headers",
                            context.Request.Headers["Access-Control-Request-Headers"][0]);
                        return Task.FromResult(0);
                    }
                };
            });
        }
    }
}