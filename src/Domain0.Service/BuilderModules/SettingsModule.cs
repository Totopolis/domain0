using Autofac;

namespace Domain0.Service.BuilderModules
{
    public class SettingsModule : Module
    {
        private readonly Domain0Settings _settings;

        public SettingsModule(Domain0Settings settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_settings);
            builder.RegisterInstance(_settings.Db);
            builder.RegisterInstance(_settings.CultureContext);
            builder.RegisterInstance(_settings.AccountService);
            builder.RegisterInstance(_settings.Threshold);
            builder.RegisterInstance(_settings.Token);
            builder.RegisterInstance(_settings.Email);
            builder.RegisterInstance(_settings.SmsQueueClient);
            builder.RegisterInstance(_settings.SmsGateway);
            builder.RegisterInstance(_settings.Ldap);
        }
    }
}