using Autofac;
using Nancy.Bootstrappers.Autofac;
using Nancy.Conventions;
using Nancy.Swagger.Services;
using Swagger.ObjectModel;
using Domain0.Nancy.Infrastructure;
using Nancy.Bootstrapper;
using Domain0.Database;

namespace Domain0.Nancy
{

    public class Domain0Bootstrapper : AutofacNancyBootstrapper
    {
        private readonly IContainer _container;

        public Domain0Bootstrapper(IContainer container) => _container = container;

        protected override ILifetimeScope GetApplicationContainer() => _container;

        protected override void ApplicationStartup(ILifetimeScope container, IPipelines pipelines)
        {
            // Add swagger info
            SwaggerMetadataProvider.SetInfo("Domain0", "v1", "Domain0 JWT auth service", new Contact
            {
                Name = "domain0"
            });

            container.Update(builder =>
            {
                builder.RegisterModule<DatabaseModule>();
            });

            var dbManager = container.Resolve<DbManager>();
            dbManager.Initialize();
        }

        protected override void ConfigureConventions(NancyConventions nancyConventions)
        {
            // Add swagger
            nancyConventions.StaticContentsConventions.AddEmbeddedDirectory<Domain0Bootstrapper>("/swagger-ui", "Swagger-UI");
        }
    }
}
