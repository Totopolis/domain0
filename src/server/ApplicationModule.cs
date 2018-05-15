using System;
using System.Collections.Generic;
using Autofac;
using AutoMapper;
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
