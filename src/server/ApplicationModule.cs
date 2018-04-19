using Autofac;
using Domain0.Service;

namespace Domain0
{
    public class ApplicationModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<PasswordGenerator>().As<IPasswordGenerator>().SingleInstance();
            builder.RegisterType<AccountService>().As<IAccountService>().InstancePerLifetimeScope();
        }
    }
}
