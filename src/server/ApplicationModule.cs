using System;
using System.Collections.Generic;
using System.Configuration;
using Autofac;
using AutoMapper;
using Domain0.Nancy.Infrastructure;
using Domain0.Nancy.Service;
using Domain0.Service;

namespace Domain0
{
    public class ApplicationModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(ReadTokenSettings());
            builder.RegisterType<TokenGenerator>().As<ITokenGenerator>().SingleInstance();

            builder.RegisterType<MapperProfile>().As<Profile>().SingleInstance();
            builder.RegisterType<PasswordGenerator>().As<IPasswordGenerator>().SingleInstance();
            builder.RegisterInstance(ReadAccountServiceSettings());
            builder.RegisterType<AccountService>().As<IAccountService>().InstancePerLifetimeScope();
            builder.RegisterType<AdminService>().As<IAdminService>().InstancePerLifetimeScope();

            builder.RegisterInstance(ReadSmsQueueSettings());
            builder.RegisterInstance(ReadSmsGatewaySettings());
            builder.RegisterType<SmsGatewayClient>().As<ISmsClient>();
            builder
                .RegisterType<AuthenticationConfigurationBuilder>()
                .As<IAuthenticationConfigurationBuilder>()
                .SingleInstance();

            builder.RegisterInstance(ReadEmailClientSettings());
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
        }

        private AccountServiceSettings ReadAccountServiceSettings()
        {
            return new AccountServiceSettings
            {
                PinExpirationTime = TimeSpan.FromMinutes(
                    double.Parse(ConfigurationManager.AppSettings["AccountService_PinExpirationTime"] ?? "15")),

                EmailCodeExpirationTime = TimeSpan.FromMinutes(
                    double.Parse(ConfigurationManager.AppSettings["AccountService_EmailCodeExpirationTime"] ?? "120")),
            };
        }

        private static TokenGeneratorSettings ReadTokenSettings()
        {
            var settings = new TokenGeneratorSettings
            {
                Audience = ConfigurationManager.AppSettings["Token_Audience"] ?? "*",
                Issuer = ConfigurationManager.AppSettings["Token_Issuer"] ?? "issuer",
                Lifetime = TimeSpan.FromMinutes(double.Parse(ConfigurationManager.AppSettings["Token_LifeTime"] ?? "15")),
                RefreshLifetime = TimeSpan.FromMinutes(double.Parse(ConfigurationManager.AppSettings["Token_RefreshLifeTime"] ?? "480")),
                Secret = ConfigurationManager.AppSettings["Token_Secret"] ?? "kiHLSfGebYvXGTDx0vWb53JhyUpnw6HvgRwOJ6h/hUs=",
                Private = ConfigurationManager.AppSettings["Token_Private"],
                Public = ConfigurationManager.AppSettings["Token_Public"],
                Alg = ConfigurationManager.AppSettings["Token_Alg"] ?? "HS256",
            };
            return settings;
        }

        private static EmailClientSettings ReadEmailClientSettings()
        {
            return new EmailClientSettings
            {
                ServerHost = ConfigurationManager.AppSettings["Email_ServerHost"],
                Port = int.Parse(ConfigurationManager.AppSettings["Email_Port"] ?? "587"),
                Email = ConfigurationManager.AppSettings["Email_Address"],
                Username = ConfigurationManager.AppSettings["Email_Username"],
                Password = ConfigurationManager.AppSettings["Email_Password"],
            };
        }

        private static SqlQueueSmsClientSettings ReadSmsQueueSettings()
        {
            return new SqlQueueSmsClientSettings
            {
                ConnectionString = ConfigurationManager.AppSettings["SmsQueueClient_ConnectionString"],
                QueueName = ConfigurationManager.AppSettings["SmsQueueClient_QueueName"]
            };
        }

        private static SmsGatewaySettings ReadSmsGatewaySettings()
        {
            return new SmsGatewaySettings
            {
                Host = ConfigurationManager.AppSettings["SmsGateway_Host"],
                Token = ConfigurationManager.AppSettings["SmsGateway_Token"],
            };
        }
    }
}
