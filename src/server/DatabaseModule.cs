using Autofac;

namespace Domain0.Database
{
    public class DatabaseModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DbManager>().WithParameter((p, c) => p.ParameterType == typeof(string),
                (p, c) => c.ResolveNamed<string>("connectionString")).AsSelf();
        }
    }
}
