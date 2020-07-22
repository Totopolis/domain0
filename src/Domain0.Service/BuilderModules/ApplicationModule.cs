using System.Collections.Generic;
using Autofac;
using Autofac.Core;
using AutoMapper;
using Domain0.Nancy.Infrastructure;
using Domain0.Nancy.Service;
using Domain0.Nancy.Service.Ldap;
using Microsoft.IdentityModel.Tokens;

namespace Domain0.Service.BuilderModules
{
    public class ApplicationModule : Module
    {
        private Domain0Settings _settings;
        public ApplicationModule(Domain0Settings settings)
        {
            _settings = settings;
        }
        public ApplicationModule()
        {
           
        }
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

            if (_settings==null || _settings.SmsProvider == null ||
                string.IsNullOrEmpty(_settings.SmsProvider.Provider) ||
                _settings.SmsProvider.Provider.ToLower() == "smsgateway")
            {
                builder.RegisterType<SmsGatewayClient>().As<ISmsClient>();
            }
            else if (_settings.SmsProvider.Provider.ToLower() == "smsc")
            {
                builder.RegisterType<SmscClient>().As<ISmsClient>();
            }
            else
            {
                builder.RegisterType<SmsGatewayClient>().As<ISmsClient>();
            }

        
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