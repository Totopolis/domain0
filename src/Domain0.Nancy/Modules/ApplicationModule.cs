using System;
using System.Collections.Generic;
using System.Configuration;
using Autofac;
using AutoMapper;
using Domain0.Nancy.Infrastructure;
using Domain0.Nancy.Service;
using Domain0.Nancy.Service.Ldap;
using Domain0.Service;
using Domain0.Tokens;
using Microsoft.IdentityModel.Tokens;

namespace Domain0
{
    public class ApplicationModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(ReadTokenSettings());
            builder
                .RegisterType<AsymmetricKeyPairTokenGenerator>()
                .Keyed<ITokenGenerator>(SecurityAlgorithms.RsaSha256);
            builder
                .RegisterType<SymmetricKeyTokenGenerator>()
                .Keyed<ITokenGenerator>(SecurityAlgorithms.HmacSha256)
                .SingleInstance();

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
                .InstancePerLifetimeScope();

            builder.RegisterInstance(ReadEmailClientSettings());
            builder.RegisterType<EmailClient>().As<IEmailClient>();

            builder.RegisterInstance<ThresholdSettings>(ReadThresholdSettings());

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

            builder.RegisterInstance(ReadLdapSettings());
            builder.RegisterType<LdapClient>().As<ILdapClient>().SingleInstance();
        }

        private AccountServiceSettings ReadAccountServiceSettings()
        {
            return new AccountServiceSettings
            {
                MessagesResendCooldown = TimeSpan.FromMinutes(
                    double.Parse(ConfigurationManager.AppSettings["AccountService_MessagesResendCooldown"] ?? "1")),

                PinExpirationTime = TimeSpan.FromMinutes(
                    double.Parse(ConfigurationManager.AppSettings["AccountService_PinExpirationTime"] ?? "15")),

                EmailCodeExpirationTime = TimeSpan.FromMinutes(
                    double.Parse(ConfigurationManager.AppSettings["AccountService_EmailCodeExpirationTime"] ?? "120")),
            };
        }

        private static ThresholdSettings ReadThresholdSettings()
        {
            var settings = new ThresholdSettings
            {
                HourlyRequestsLimitByActionByIP =
                    int.Parse(ConfigurationManager.AppSettings["ThresholdSettings_HourlyRequestsLimitByActionByIP"] ?? "6000"),

                MinuteRequestsLimitByActionByIP =
                    int.Parse(ConfigurationManager.AppSettings["ThresholdSettings_MinuteRequestsLimitByActionByIP"] ?? "300"),

                CacheLimitMB =
                    int.Parse(ConfigurationManager.AppSettings["ThresholdSettings_CacheLimitMB"] ?? "512")
            };

            return settings;
        }

        private static TokenGeneratorSettings ReadTokenSettings()
        {
            var settings = new TokenGeneratorSettings
            {
                Audience = ConfigurationManager.AppSettings["Token_Audience"] ?? "*",
                RefreshAudience = ConfigurationManager.AppSettings["Token_RefreshAudience"] ?? "*",
                Issuer = ConfigurationManager.AppSettings["Token_Issuer"] ?? "issuer",
                Lifetime = TimeSpan.FromMinutes(double.Parse(ConfigurationManager.AppSettings["Token_LifeTime"] ?? "15")),
                RefreshLifetime = TimeSpan.FromMinutes(double.Parse(ConfigurationManager.AppSettings["Token_RefreshLifeTime"] ?? "480")),
                Secret = string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["Token_Secret"])
                    ? "kiHLSfGebYvXGTDx0vWb53JhyUpnw6HvgRwOJ6h/hUs="
                    : ConfigurationManager.AppSettings["Token_Secret"],
                Private = string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["Token_Private"])
                    ? "PFJTQUtleVZhbHVlPjxNb2R1bHVzPng1Y2tLak5NQzUvVTRETnZ2ZEhTRnkzc1ZoUDgxRmZrZ1lmZ3o5RjRXK05mUVpXK3k2Mk43NmZwRjRMOGh0cVNoSjFRcjJuRUtFNjdPZ2V0R3FjZWJ6dlppaXU3UXlpTFlzN1VJK2sxUGhiN1B1WDFTNUFBelpkY2tRSDdiNXZiV2lXVmdyUG9xYjM3M1BnRTJDai8vNFZmYUR4TEhWOThBbCtXbTV5VEd3WjhPWk45MVFSSmdmQkVseXB0ZEw1MDVRbkVzZGQ3aEZUU0VkUTdlUDRaOE1JK1REaVk5VU10WFh4cU1haUY0UWw1RFR5bUMvUWpiSi9aaVZLbXZXeCtFRkZFSHdUTFZYSHNxZUp5bTg5SzNma3Zvdm11NHNhOGN3ZUovWlRwUnJDM0R0M2YrZnd0NFFBRTNEOGxZNjBmNjhEQkdteWNQVDVtRDliUzlYYWxoUT09PC9Nb2R1bHVzPjxFeHBvbmVudD5BUUFCPC9FeHBvbmVudD48UD55Z1lHR00zcFZzemhMWTh1WEZ6cFh5TERxbWFVQVI0aU5nL09yVnNWWkZ5TGVVWnFzQ21WUCsyQjZMcDN0MzNYUS8wc3FISzNmK2p4SlRCRVY0L1lKNi9xTFk1Wi93ekozT0RPZUQ2SFJUMlBOSGFGNEQxem9TUDI4UGx1YjJDZm5yKzNoMTEzQndWZnFiM3ZDVmhTUStjNVpLbWZCdExZL2RmRHg4SGlNcTg9PC9QPjxRPi9PcXlQTnBPL0FST2NMT0crR01IaXVPNDRaZGR6VXE2RndxY3VXSUhITDZ0QkJkQ0tuNFZwTUt6d0dHRlZJWmg1YUlhaml3N1NqYjJwOGRCclMwRUxVNkU0MW5kRHBNVEtLYjNLUnhpTFhkelFTVTdWNStRS1NQSnB2YmhuUVJnT3hQWERwMmY0anhoOWpuYVNudDRkTDNvNndkWUhoS0xVVlNRMURHbkNBcz08L1E+PERQPm1zdVMwRjYyZERQNE5oaUh4VzNMdzRHM3UxRnVCbzA0V1lRek1OR2h5b3krc2VZcXlXQ1RZN0J3NGdvK3dQSkRoMnp1VXdQVFpzYnVQemlFcFRNcXhQNGR5VnBSeXdQWmlNMFlaenBDRytQWFhyT1NVUWZGR2F1ZEEzZWNEdXRTWXlrelR4MW1ucEtYZ2xCdVlCSzB0aUx0N3h1cEptbFlxdWd3czFiMEl6RT08L0RQPjxEUT55MWFkNmxMU0FjZ3NrR3VsN2ZrZ1RVZjhrbEt4OWFWSXE5RzZZMGt1MHF0eVNzR3dUcDJFSlN6c2U1VnNMcUxEL20wdjBISTdVTldUeFJ4cjd5RXNKSWptU2lzcmtOWXFKeHJseDhXc0lVWFNBZVEvSzVsN3U3ZXNIbktLdlVTUllhMzN6eVpuTHVyQkQ4Yy9lM1o5Ujg2UGZyWXU0QzZrbDhUWGsrSS9talU9PC9EUT48SW52ZXJzZVE+V1liRmtGdGFhbm1mZUxmNkVOSVZpcVlRTGdyTmRIZHB4TXlYWXdDbi9tVXlzbzZiTktVUmhtT2VpL0poYVErZzhkWks3emRpdEdPelZSYU91S0Q3VUJZeGIzRHJTdFA4NVZRZm5BdHQzd1JKN0hrU0p1bjFKZnc3VjJNMEtlU29LNnJIUG9sUVV2N1h2OUJnUUJvSGhVRWRlb0tGUkYxM2NoQmZFRTVySkNJPTwvSW52ZXJzZVE+PEQ+aHN1UHRDU0MxbEx5clJ4ditnM0x5clhNS0hKRlRZK3lscnlTMnlmSUZwN2Z4V2FCdmdNUG1leVg0clluSUZoYm5jNjFJRkxaRkxQZmhKaU1relNNdkdqNlNYT3hlL0RVK2oxZlRvV0EzTmNlaGVNK24vSzRhQ0V5ZGdpVnJGSGhlZWRxS3lTZ0hJdGZuMk90dWVNdXlYNWs2ZnZXYXhjQ3BJTzBMcEkzVTRjQmZCdnU3Um92R0Q1ejF4UmlSREdRK1M3OVBabi9LRTlqMWNmcDdKMjIzM2l5WVNqbHRIZ0ZsV2owbkZMNzhXS0tVbVB4QmZoa1BBa05EQ2Jjdm02ZUhpTGJuY2FqdWcrL2JlMXljVmNKZnRqcTJGZU5Jc24vVGJVVFlsekFwSGdUWWF2eCtJRVVzRXV1VGhFNEgrbXpBQ0dmeks1MWxGS0pKTXZicEtTWWZRPT08L0Q+PC9SU0FLZXlWYWx1ZT4="
                    : ConfigurationManager.AppSettings["Token_Private"],
                Public = string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["Token_Public"])
                    ? "PFJTQUtleVZhbHVlPjxNb2R1bHVzPng1Y2tLak5NQzUvVTRETnZ2ZEhTRnkzc1ZoUDgxRmZrZ1lmZ3o5RjRXK05mUVpXK3k2Mk43NmZwRjRMOGh0cVNoSjFRcjJuRUtFNjdPZ2V0R3FjZWJ6dlppaXU3UXlpTFlzN1VJK2sxUGhiN1B1WDFTNUFBelpkY2tRSDdiNXZiV2lXVmdyUG9xYjM3M1BnRTJDai8vNFZmYUR4TEhWOThBbCtXbTV5VEd3WjhPWk45MVFSSmdmQkVseXB0ZEw1MDVRbkVzZGQ3aEZUU0VkUTdlUDRaOE1JK1REaVk5VU10WFh4cU1haUY0UWw1RFR5bUMvUWpiSi9aaVZLbXZXeCtFRkZFSHdUTFZYSHNxZUp5bTg5SzNma3Zvdm11NHNhOGN3ZUovWlRwUnJDM0R0M2YrZnd0NFFBRTNEOGxZNjBmNjhEQkdteWNQVDVtRDliUzlYYWxoUT09PC9Nb2R1bHVzPjxFeHBvbmVudD5BUUFCPC9FeHBvbmVudD48L1JTQUtleVZhbHVlPg =="
                    : ConfigurationManager.AppSettings["Token_Public"],
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

        private static LdapSettings ReadLdapSettings()
        {
            return new LdapSettings
            {
                DomainControllerName = ConfigurationManager.AppSettings["DomainControllerName"] ?? "domain.local",
                LdapPort = int.Parse(ConfigurationManager.AppSettings["LdapPort"] ?? "636"),
                UseSecureSocketLayer = bool.Parse(ConfigurationManager.AppSettings["UseSecureSocketLayer"] ?? "true"),
                LdapProtocolVersion = int.Parse(ConfigurationManager.AppSettings["LdapProtocolVersion"] ?? "3"),
                LdapAuthType = ConfigurationManager.AppSettings["LdapAuthType"] ?? "GSSAPI",
                EmailAttributeName = ConfigurationManager.AppSettings["EmailAttributeName"] ?? "mail"
            };
        }
    }
}
