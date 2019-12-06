using Autofac;
using Domain0.FastSql.Settings;
using Domain0.Repository;
using Gerakul.FastSql.Common;
using Gerakul.FastSql.SqlServer;

namespace Domain0.FastSql
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
            builder.Register<DbContext>(c =>
                SqlContextProvider.DefaultInstance.CreateContext(_settings.ConnectionString));

            builder.RegisterType<AccountRepository>().As<IAccountRepository>().SingleInstance();
            builder.RegisterType<ApplicationRepository>().As<IApplicationRepository>().SingleInstance();
            builder.RegisterType<MessageTemplateRepository>().As<IMessageTemplateRepository>().SingleInstance();
            builder.RegisterType<PermissionRepository>().As<IPermissionRepository>().SingleInstance();
            builder.RegisterType<RoleRepository>().As<IRoleRepository>().SingleInstance();
            builder.RegisterType<SmsRequestRepository>().As<ISmsRequestRepository>().SingleInstance();
            builder.RegisterType<EmailRequestRepository>().As<IEmailRequestRepository>().SingleInstance();
            builder.RegisterType<TokenRegistrationRepository>().As<ITokenRegistrationRepository>().SingleInstance();
            builder.RegisterType<AccessLogRepository>().As<IAccessLogRepository>().SingleInstance();
            builder.RegisterType<EnvironmentRepository>().As<IEnvironmentRepository>().SingleInstance();
        }
    }
}