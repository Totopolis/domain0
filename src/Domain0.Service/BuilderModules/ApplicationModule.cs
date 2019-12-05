using System.Collections.Generic;
using Autofac;
using AutoMapper;
using Domain0.Nancy.Infrastructure;
using Domain0.Nancy.Service;
using Domain0.Nancy.Service.Ldap;
using Microsoft.IdentityModel.Tokens;

namespace Domain0.Service.BuilderModules
{
    public class ApplicationModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<AsymmetricKeyPairTokenGenerator>()
                .Keyed<ITokenGenerator>(SecurityAlgorithms.RsaSha256);
            builder
                .RegisterType<SymmetricKeyTokenGenerator>()
                .Keyed<ITokenGenerator>(SecurityAlgorithms.HmacSha256)
                .SingleInstance();

            builder.RegisterType<MapperProfile>().As<Profile>().SingleInstance();
            builder.RegisterType<PasswordGenerator>().As<IPasswordGenerator>().SingleInstance();
            builder.RegisterType<AccountService>().As<IAccountService>().InstancePerLifetimeScope();
            builder.RegisterType<AdminService>().As<IAdminService>().InstancePerLifetimeScope();

            builder.RegisterType<SmsGatewayClient>().As<ISmsClient>();
            builder
                .RegisterType<AuthenticationConfigurationBuilder>()
                .As<IAuthenticationConfigurationBuilder>()
                .InstancePerLifetimeScope();

            builder.RegisterType<EmailClient>().As<IEmailClient>();

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

            builder.RegisterType<LdapClient>().As<ILdapClient>().SingleInstance();
        }
    }
}