using Autofac;
using Domain0.Repository.Settings;

namespace Domain0.Repository.PostgreSql
{
    public class DatabaseModule : Module
    {
        private readonly DbSettings _settings;

        public DatabaseModule(DbSettings settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register<IDbConnectionProvider>(c =>
                new DbConnectionProvider(_settings.ConnectionString));

            builder.RegisterType<AccountRepository>().As<IAccountRepository>().SingleInstance();
            builder.RegisterType<ApplicationRepository>().As<IApplicationRepository>().SingleInstance();
            builder.RegisterType<MessageTemplateRepository>().As<IMessageTemplateRepository>().SingleInstance();
            //builder.RegisterType<PermissionRepository>().As<IPermissionRepository>().SingleInstance();
            //builder.RegisterType<RoleRepository>().As<IRoleRepository>().SingleInstance();
            builder.RegisterType<SmsRequestRepository>().As<ISmsRequestRepository>().SingleInstance();
            builder.RegisterType<EmailRequestRepository>().As<IEmailRequestRepository>().SingleInstance();
            builder.RegisterType<TokenRegistrationRepository>().As<ITokenRegistrationRepository>().SingleInstance();
            builder.RegisterType<AccessLogRepository>().As<IAccessLogRepository>().SingleInstance();
            builder.RegisterType<EnvironmentRepository>().As<IEnvironmentRepository>().SingleInstance();
        }
    }
}