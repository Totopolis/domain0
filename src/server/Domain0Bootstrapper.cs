using Autofac;
using Autofac.Core;
using Domain0.Nancy.Infrastructure;
using Domain0.Service;
using Nancy;
using Nancy.Authentication.Stateless;
using Nancy.Bootstrapper;
using Nancy.Bootstrappers.Autofac;
using Nancy.Configuration;
using Nancy.Conventions;
using Nancy.Swagger;
using Nancy.Swagger.Annotations;
using Nancy.Swagger.Services;
using Nancy.Validation;
using Swagger.ObjectModel;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Domain0.Model;

namespace Domain0.Nancy
{

    public class Domain0Bootstrapper : AutofacNancyBootstrapper
    {
        public Domain0Bootstrapper(IContainer rootContainer)
        {
            container = rootContainer;
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

            NancyExceptionHandling.Enable(
                requestContainer, 
                pipelines, 
                context);

            StatelessAuthentication.Enable(
                pipelines, 
                requestContainer
                    .Resolve<IAuthenticationConfigurationBuilder>()
                    .Build());

            NancySecurityCriticalActionsHandling.Enable(
                requestContainer,
                pipelines);
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
    }
}
