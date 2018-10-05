using System.Configuration;
using Autofac;
using Autofac.Core;
using Domain0.Repository;

namespace Domain0.FastSql
{
    public class DatabaseModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["Database"].ConnectionString;
            builder.RegisterInstance(connectionString).Named<string>(nameof(connectionString));

            builder.RegisterType<DbManager>().WithParameter((p, c) => p.ParameterType == typeof(string),
                (p, c) => c.ResolveNamed<string>(nameof(connectionString))).AsSelf();
            builder.RegisterType<AccountRepository>().As<IAccountRepository>()
                .WithParameter(new ResolvedParameter((pi, ctx) => pi.ParameterType == typeof(string),
                    (pi, ctx) => ctx.ResolveNamed<string>(nameof(connectionString)))).SingleInstance();
            builder.RegisterType<ApplicationRepository>().As<IApplicationRepository>()
                .WithParameter(new ResolvedParameter((pi, ctx) => pi.ParameterType == typeof(string),
                    (pi, ctx) => ctx.ResolveNamed<string>(nameof(connectionString)))).SingleInstance();
            builder.RegisterType<MessageTemplateRepository>().As<IMessageTemplateRepository>()
                .WithParameter(new ResolvedParameter((pi, ctx) => pi.ParameterType == typeof(string),
                    (pi, ctx) => ctx.ResolveNamed<string>(nameof(connectionString)))).SingleInstance();
            builder.RegisterType<PermissionRepository>().As<IPermissionRepository>()
                .WithParameter(new ResolvedParameter((pi, ctx) => pi.ParameterType == typeof(string),
                    (pi, ctx) => ctx.ResolveNamed<string>(nameof(connectionString)))).SingleInstance();
            builder.RegisterType<RoleRepository>().As<IRoleRepository>()
                .WithParameter(new ResolvedParameter((pi, ctx) => pi.ParameterType == typeof(string),
                    (pi, ctx) => ctx.ResolveNamed<string>(nameof(connectionString)))).SingleInstance();
            builder.RegisterType<SmsRequestRepository>().As<ISmsRequestRepository>()
                .WithParameter(new ResolvedParameter((pi, ctx) => pi.ParameterType == typeof(string),
                    (pi, ctx) => ctx.ResolveNamed<string>(nameof(connectionString)))).SingleInstance();
            builder.RegisterType<EmailRequestRepository>().As<IEmailRequestRepository>()
                .WithParameter(new ResolvedParameter((pi, ctx) => pi.ParameterType == typeof(string),
                    (pi, ctx) => ctx.ResolveNamed<string>(nameof(connectionString)))).SingleInstance();
            builder.RegisterType<TokenRegistrationRepository>().As<ITokenRegistrationRepository>()
                .WithParameter(new ResolvedParameter((pi, ctx) => pi.ParameterType == typeof(string),
                    (pi, ctx) => ctx.ResolveNamed<string>(nameof(connectionString)))).SingleInstance();

            builder.RegisterType<AccessLogRepository>().As<IAccessLogRepository>()
                .WithParameter(new ResolvedParameter((pi, ctx) => pi.ParameterType == typeof(string),
                    (pi, ctx) => ctx.ResolveNamed<string>(nameof(connectionString)))).SingleInstance();
            
        }
    }
}
