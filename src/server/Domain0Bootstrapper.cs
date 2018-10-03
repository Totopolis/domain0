using Autofac;
using Autofac.Core;
using Domain0.Exceptions;
using Domain0.Model;
using Domain0.Nancy.Infrastructure;
using Domain0.Service;
using Nancy;
using Nancy.Authentication.Stateless;
using Nancy.Bootstrapper;
using Nancy.Bootstrappers.Autofac;
using Nancy.Configuration;
using Nancy.Conventions;
using Nancy.ModelBinding;
using Nancy.Responses.Negotiation;
using Nancy.Swagger;
using Nancy.Swagger.Annotations;
using Nancy.Swagger.Services;
using Nancy.Validation;
using NLog;
using Swagger.ObjectModel;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Domain0.Nancy
{

    public class Domain0Bootstrapper : AutofacNancyBootstrapper
    {
        public Domain0Bootstrapper(IContainer rootContainer)
        {
            container = rootContainer;
            logger = container.Resolve<ILogger>();
        } 

        protected override ILifetimeScope GetApplicationContainer() => container;

        protected override void ApplicationStartup(ILifetimeScope applicationContainer, IPipelines pipelines)
        {
            // Add swagger info
            SwaggerMetadataProvider.SetInfo("Domain0", "v1", "Domain0 JWT auth service", new Contact
            {
                Name = "domain0"
            });

            var modelCatalog = applicationContainer.Resolve<ISwaggerModelCatalog>();
            modelCatalog.AddModel<IEnumerable<ModelValidationError>>();
            modelCatalog.AddModel<ModelValidationError>();
            modelCatalog.AddModel<RolePermission>();
            modelCatalog.AddModel<UserPermission>();

            SwaggerAnnotationsConfig.ShowOnlyAnnotatedRoutes = true;
            applicationContainer.Update(builder =>
            {
                builder.RegisterType<SwaggerAnnotationsProvider>().As<ISwaggerMetadataProvider>();
            });

            pipelines.EnableCors();
        }

        protected override void RequestStartup(ILifetimeScope requestContainer, IPipelines pipelines, NancyContext context)
        {
            base.RequestStartup(requestContainer, pipelines, context);
            pipelines.BeforeRequest.AddItemToEndOfPipeline(ctx => null);

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
                    case ModelBindingException binding:
                        return new Negotiator(ctx)
                            .WithStatusCode(HttpStatusCode.BadRequest)
                            .WithHeader("X-Status-Reason", "validation error")
                            .WithReasonPhrase("validation error")
                            .WithMediaRangeModel("application/json", new List<ModelValidationError> { new ModelValidationError(binding.BoundType.Name, "couldnt deserialize")});
                    default:
                        logger.Error(ex, ex.ToString());
                        break;
                }

                return null;
            });

            StatelessAuthentication.Enable(
                pipelines, 
                requestContainer
                    .Resolve<IAuthenticationConfigurationBuilder>()
                    .Build());

        }

        protected override void ConfigureConventions(NancyConventions nancyConventions)
        {
            // Add swagger
            nancyConventions.StaticContentsConventions.AddEmbeddedDirectory<Domain0Bootstrapper>("/swagger-ui", "Swagger-UI");
        }

        protected override void ConfigureRequestContainer(
            ILifetimeScope requestContainer, 
            NancyContext context)
        {
            requestContainer.Update(builder =>
            {
                builder
                    .RegisterType<JwtAuthenticationRequestContext>()
                    .As<IRequestContext>()
                    .WithParameter(
                        new ResolvedParameter(
                            (pi, ctx) => pi.ParameterType == typeof(NancyContext),
                            (pi, ctx) => context))
                    .InstancePerLifetimeScope();

                builder
                    .RegisterType<CultureRequestContext>()
                    .As<ICultureRequestContext>()
                    .WithParameter(
                        new ResolvedParameter(
                            (pi, ctx) => pi.ParameterType == typeof(NancyContext),
                            (pi, ctx) => context))
                    .InstancePerLifetimeScope();
            });

            base.ConfigureRequestContainer(requestContainer, context);
        }

        public override void Configure(INancyEnvironment environment)
        {
            var supportedCultures = CultureInfo.GetCultures(
                    CultureTypes.AllCultures & ~CultureTypes.SpecificCultures)
                .Select(x => x.Name)
                .ToArray();
            environment.Globalization(supportedCultures, "en-US");

            base.Configure(environment);
        }

        private readonly IContainer container;

        private readonly ILogger logger;
    }
}
