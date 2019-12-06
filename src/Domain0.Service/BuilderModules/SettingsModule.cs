using Autofac;
using Domain0.Nancy.Service;
using Domain0.Nancy.Service.Ldap;
using Domain0.Tokens;
using Microsoft.Extensions.Configuration;

namespace Domain0.Service.BuilderModules
{
    public class SettingsModule : Module
    {
        private readonly IConfiguration _config;

        public SettingsModule(IConfiguration config)
        {
            _config = config;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c =>
                    _config.GetSection("Database").Get<string>())
                .Named<string>("connectionString")
                .SingleInstance();
            builder.Register(c =>
                    _config.GetSection("DefaultCulture").Get<string>() ?? string.Empty)
                .Named<string>("defaultCulture")
                .SingleInstance();

            builder.Register(c =>
                    _config.GetSection("AccountService").Get<AccountServiceSettings>())
                .SingleInstance();

            builder.Register(c =>
                    _config.GetSection("Threshold").Get<ThresholdSettings>())
                .SingleInstance();

            builder.Register(c =>
                    _config.GetSection("Token").Get<TokenGeneratorSettings>())
                .SingleInstance();

            builder.Register(c =>
                    _config.GetSection("Email").Get<EmailClientSettings>())
                .SingleInstance();

            builder.Register(c =>
                    _config.GetSection("SmsQueueClient").Get<SqlQueueSmsClientSettings>())
                .SingleInstance();

            builder.Register(c =>
                    _config.GetSection("SmsGateway").Get<SmsGatewaySettings>())
                .SingleInstance();

            builder.Register(c =>
                    _config.GetSection("Ldap").Get<LdapSettings>())
                .SingleInstance();
        }
    }
}