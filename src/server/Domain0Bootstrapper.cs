using Autofac;
using Nancy.Bootstrappers.Autofac;
using Nancy.Conventions;
using Nancy.Swagger.Services;
using Swagger.ObjectModel;
using Domain0.Nancy.Infrastructure;
using Nancy.Bootstrapper;
using Nancy.Swagger.Annotations;
using Nancy.Swagger;
using Nancy.Validation;
using System.Collections.Generic;
using Nancy;
using Nancy.Responses.Negotiation;
using System.Linq;
using Domain0.Exceptions;
using NLog;

namespace Domain0.Nancy
{

    public class Domain0Bootstrapper : AutofacNancyBootstrapper
    {
        private readonly IContainer _container;

        private readonly ILogger _logger;

        public Domain0Bootstrapper(IContainer container)
        {
            _container = container;
            _logger = _container.Resolve<ILogger>();
        } 

        protected override ILifetimeScope GetApplicationContainer() => _container;

        protected override void ApplicationStartup(ILifetimeScope container, IPipelines pipelines)
        {
            // Add swagger info
            SwaggerMetadataProvider.SetInfo("Domain0", "v1", "Domain0 JWT auth service", new Contact
            {
                Name = "domain0"
            });

            var modelCatalog = container.Resolve<ISwaggerModelCatalog>();
            modelCatalog.AddModel<IEnumerable<ModelValidationError>>();
            modelCatalog.AddModel<ModelValidationError>();

            SwaggerAnnotationsConfig.ShowOnlyAnnotatedRoutes = true;
            container.Update(builder =>
            {
                builder.RegisterType<SwaggerAnnotationsProvider>().As<ISwaggerMetadataProvider>();
            });
        }

        protected override void RequestStartup(ILifetimeScope container, IPipelines pipelines, NancyContext context)
        {
            base.RequestStartup(container, pipelines, context);

            pipelines.OnError.AddItemToStartOfPipeline((ctx, ex) =>
            {
                switch (ex)
                {
                    case BadModelException bad:
                        return new Negotiator(ctx)
                            .WithStatusCode(HttpStatusCode.BadRequest)
                            .WithHeader("X-Status-Reason", "validation error")
                            .WithReasonPhrase("validation error")
                            .WithMediaRangeModel("application/json", bad.ValidationResult.Errors.SelectMany(e => e.Value));
                    case NotFoundException notFound:
                        return new Negotiator(ctx)
                            .WithStatusCode(HttpStatusCode.NotFound)
                            .WithReasonPhrase("not found error");
                    default:
                        _logger.Error(ex, ex.ToString());
                        break;
                }

                return null;
            });
        }

        protected override void ConfigureConventions(NancyConventions nancyConventions)
        {
            // Add swagger
            nancyConventions.StaticContentsConventions.AddEmbeddedDirectory<Domain0Bootstrapper>("/swagger-ui", "Swagger-UI");
        }
    }
}
