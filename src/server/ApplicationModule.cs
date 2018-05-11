using System;
using System.Collections.Generic;
using System.Configuration;
using Autofac;
using Autofac.Core;
using AutoMapper;
using Domain0.FastSql;
using Domain0.Repository;
using Domain0.Service;
using Microsoft.IdentityModel.Tokens;

namespace Domain0
{
    public class ApplicationModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<MapperProfile>().As<Profile>().SingleInstance();
            builder.RegisterType<PasswordGenerator>().As<IPasswordGenerator>().SingleInstance();
            builder.RegisterType<TokenGenerator>().As<ITokenGenerator>().SingleInstance();
            builder.RegisterType<AccountService>().As<IAccountService>().InstancePerLifetimeScope();

            var connectionString = ConfigurationManager.ConnectionStrings["Database"].ConnectionString;
            builder.RegisterInstance(connectionString).Named<string>(nameof(connectionString));

            builder.RegisterType<AccountRepository>().As<IAccountRepository>()
                .WithParameter(new ResolvedParameter((pi, ctx) => pi.ParameterType == typeof(string),
                    (pi, ctx) => ctx.ResolveNamed<string>(nameof(connectionString)))).SingleInstance();
            builder.RegisterType<MessageTemplateRepository>().As<IMessageTemplateRepository>().SingleInstance();
            builder.RegisterType<PermissionRepository>().As<IPermissionRepository>().SingleInstance();
            builder.RegisterType<RoleRepository>().As<IRoleRepository>().SingleInstance();
            builder.RegisterType<SmsRequestRepository>().As<ISmsRequestRepository>().SingleInstance();
            builder.RegisterType<TokenRegistrationRepository>().As<ITokenRegistrationRepository>().SingleInstance();

            var settings = new TokenGeneratorSettings
            {
                Audience = "*",
                Issuer = "sdl",
                Lifetime = TimeSpan.FromMinutes(15),
                Secret = "kiHLSfGebYvXGTDx0vWb53JhyUpnw6HvgRwOJ6h/hUs=",
                Alg = SecurityAlgorithms.HmacSha256,
            };
            builder.RegisterInstance(settings);
            builder.Register(container =>
            {
                var profiles = container.Resolve<IEnumerable<Profile>>();
                var mapper = new MapperConfiguration(c =>
                {
                    foreach (var profile in profiles)
                        c.AddProfile(profile);
                }).CreateMapper();
                return mapper;

            }).As<IMapper>().SingleInstance();
        }
    }
}
