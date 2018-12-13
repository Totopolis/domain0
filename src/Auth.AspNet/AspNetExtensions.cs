using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Domain0.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Domain0.Auth.AspNet
{
    public static class AspNetExtensions
    {
        public static void AddDomain0Auth(this IServiceCollection services)
        {
            var defaultPolicy = new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build();
            services.AddAuthorization(opt => opt.AddPolicy(Domain0Auth.Policy, defaultPolicy));
        }

        public static void UseDomain0Auth(this IApplicationBuilder builder)
        {
            var settings = builder.ApplicationServices.GetRequiredService<TokenValidationSettings>();

            builder.UseJwtBearerAuthentication(new JwtBearerOptions
            {
                AutomaticChallenge = true,
                AutomaticAuthenticate = true,

                // Automatically disable the HTTPS requirement for development scenarios.
                RequireHttpsMetadata = false, //!env.IsDevelopment();
                ConfigurationManager = null,
                MetadataAddress = null,
                Authority = null,

                TokenValidationParameters = settings.BuildTokenValidationParameters(),
                
                Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context => Task.FromResult(0),
                    OnTokenValidated = context =>
                    {
                        //TODO use constants
                        var claimsIdentity = (ClaimsIdentity)context.Ticket.Principal.Identity;
                        claimsIdentity.AddClaim(new Claim("id_token",
                            context.Request.Headers["Authorization"][0].Substring(
                                context.Ticket.AuthenticationScheme.Length + 1)));

                        foreach (var role in context.Ticket.Principal.FindAll("permissions"))
                        {
                            foreach (var permission in JsonConvert.DeserializeObject<string[]>(role.Value))
                            {
                                claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, permission));
                            }
                        }
                        var subClaim = context.Ticket.Principal.FindFirst(
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
                }
            });
        }
    }
}
