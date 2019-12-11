using Autofac;
using Domain0.Nancy;
using Domain0.Repository.Settings;
using Domain0.Service.BuilderModules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Nancy.Owin;
using NLog;

namespace Domain0.Service
{
    public class Startup
    {

        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env,
            IConfiguration config)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger.json", "Api");
                c.RoutePrefix = "swagger-ui";
            });

            app.UseOwin(x => x.UseNancy(opt =>
            {
                var container = CreateContainer(config);
                var bootstrapper = new Domain0Bootstrapper(container);
                opt.Bootstrapper = bootstrapper;
            }));
        }

        private static IContainer CreateContainer(IConfiguration config)
        {
            var builder = new ContainerBuilder();

            builder.Register(c => LogManager.GetCurrentClassLogger()).As<ILogger>().InstancePerDependency();
            
            var settings = config.Get<Domain0Settings>();
            builder.RegisterModule(new SettingsModule(settings));

            builder.RegisterModule<ApplicationModule>();

            switch (settings.Db.Provider)
            {
                case DbProvider.SqlServer:
                    builder.RegisterModule(new Repository.SqlServer.DatabaseModule(settings.Db));
                    break;
                case DbProvider.PostgreSql:
                    builder.RegisterModule(new Repository.PostgreSql.DatabaseModule(settings.Db));
                    break;
            }
            
            return builder.Build();
        }
    }
}
