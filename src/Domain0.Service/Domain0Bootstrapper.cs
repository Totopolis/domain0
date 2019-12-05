using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Autofac;
using Autofac.Core;
using Domain0.Model;
using Domain0.Nancy.Infrastructure;
using Domain0.Service;
using Domain0.Service.Throttling;
using Microsoft.Extensions.Caching.Memory;
using Nancy;
using Nancy.Authentication.Stateless;
using Nancy.Bootstrapper;
using Nancy.Bootstrappers.Autofac;
using Nancy.Configuration;
using Nancy.Responses.Negotiation;
using Nancy.Swagger;
using Nancy.Swagger.Annotations;
using Nancy.Swagger.Services;
using Nancy.Validation;
using Swagger.ObjectModel;

namespace Domain0.Nancy
{

    public class Domain0Bootstrapper : AutofacNancyBootstrapper
    {
        public Domain0Bootstrapper(IContainer rootContainer)
        {
            container = rootContainer;
            thresholdSettings = rootContainer.Resolve<ThresholdSettings>();
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
            modelCatalog.AddModel<UserRole>();

            SwaggerAnnotationsConfig.ShowOnlyAnnotatedRoutes = true;
            applicationContainer.Update(builder =>
            {
                builder
                    .RegisterType<SwaggerAnnotationsProvider>()
                    .As<ISwaggerMetadataProvider>();

                builder
                    .RegisterInstance(new MemoryCache(
                        new MemoryCacheOptions
                        {
                            ExpirationScanFrequency = TimeSpan.FromMinutes(5),
                            SizeLimit = thresholdSettings.CacheLimitMB * 1024 * 1024
                        }))
                    .As<IMemoryCache>()
                    .SingleInstance();


                builder
                    .RegisterType<RequestThrottleManager>()
                    .As<IRequestThrottleManager>()
                    .SingleInstance();
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

            var requestThrottleManager = requestContainer.Resolve<IRequestThrottleManager>();

            requestThrottleManager.RequiresThrottlingByPathAndIpOnlyUnauthorized(
                pipelines, ThrottlingPeriod.Minute, requestCountLimit: thresholdSettings.MinuteRequestsLimitByActionByIP);
            requestThrottleManager.RequiresThrottlingByPathAndIpOnlyUnauthorized(
                pipelines, ThrottlingPeriod.Hour, requestCountLimit: thresholdSettings.HourlyRequestsLimitByActionByIP);

            StatelessAuthentication.Enable(
                pipelines, 
                requestContainer
                    .Resolve<IAuthenticationConfigurationBuilder>()
                    .Build());

            NancySecurityCriticalActionsHandling.Enable(
                requestContainer,
                pipelines);



        }

        protected override void ConfigureRequestContainer(
            ILifetimeScope requestContainer, 
            NancyContext context)
        {
            var tokenGenerator = TokenGeneratorBuilder.Build(requestContainer, context);

            requestContainer.Update(builder =>
            {
                builder.RegisterInstance(tokenGenerator)
                    .As<ITokenGenerator>();
                
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

                builder
                    .RegisterType<CultureRequestContext>()
                    .As<ICultureRequestContext>()
                    .WithParameter(
                        new ResolvedParameter(
                            (pi, ctx) => pi.ParameterType == typeof(NancyContext),
                            (pi, ctx) => context))
                    .InstancePerLifetimeScope();

                builder
                    .RegisterType<EnvironmentRequestContext>()
                    .As<IEnvironmentRequestContext>()
                    .WithParameter(
                        new ResolvedParameter(
                            (pi, ctx) => pi.ParameterType == typeof(NancyContext),
                            (pi, ctx) => context))
                    .InstancePerLifetimeScope();

                builder
                    .RegisterType<MessageBuilder>()
                    .As<IMessageBuilder>()
                    .InstancePerLifetimeScope();
            });

            base.ConfigureRequestContainer(requestContainer, context);
        }

        public override void Configure(INancyEnvironment environment)
        {
            var supportedCultures = CultureInfo.GetCultures(CultureTypes.AllCultures)
                .Select(x => x.Name)
                .ToArray();
            environment.Globalization(supportedCultures, "en-US");

            base.Configure(environment);
        }

        protected override Func<ITypeCatalog, NancyInternalConfiguration> InternalConfiguration
        {
            get
            {
                return NancyInternalConfiguration
                    .WithOverrides(x =>
                    {
                        x.ResponseProcessors = availableResponseProcessors;
                        x.Serializers = availableSerializers;
                    });
            }
        }

        private readonly Type[] availableResponseProcessors = {
            typeof(ProtobufResponseProcessor),
            typeof(JsonProcessor),
            typeof(DefaultResponseProcessor)
        };

        private readonly Type[] availableSerializers = {
            typeof(JsonNetSerializer)
        };

        private readonly IContainer container;
        private readonly ThresholdSettings thresholdSettings;
    }
}
