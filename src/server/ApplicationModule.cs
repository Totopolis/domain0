using System.Collections.Generic;
using Autofac;
using AutoMapper;
using Domain0.Service;

namespace Domain0
{
    public class ApplicationModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<MapperProfile>().As<Profile>().SingleInstance();
            builder.RegisterType<PasswordGenerator>().As<IPasswordGenerator>().SingleInstance();
            builder.RegisterType<AccountService>().As<IAccountService>().InstancePerLifetimeScope();
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
    }
}
