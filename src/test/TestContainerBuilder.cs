using System;
using Autofac;
using Domain0.Repository;
using Domain0.Service;
using Domain0.Test.Infrastructure;
using Moq;
using Newtonsoft.Json;
using NLog;
using NLog.Config;
using NLog.Targets;
using Sdl.Domain0.Shared;
using LogLevel = NLog.LogLevel;

namespace Domain0.Test
{

    public class TestContainerBuilder
    {
        public static IContainer GetContainer(Action<ContainerBuilder> upgrade = null)
        {
            var builder = new ContainerBuilder();

            var config = new LoggingConfiguration();
            config.AddTarget("console", new ColoredConsoleTarget());
            config.AddRule(LogLevel.Error, LogLevel.Fatal, "console");
            LogManager.Configuration = config;

            builder.Register(c => LogManager.GetCurrentClassLogger()).As<ILogger>().InstancePerDependency();
            builder.RegisterSource(new MoqRegistrationSource());
            builder.RegisterModule<ApplicationModule>();
            builder.RegisterInstance(new Mock<ITokenGenerator>().Object).As<ITokenGenerator>();
            builder.RegisterInstance(new Mock<IPasswordGenerator>().Object).As<IPasswordGenerator>().SingleInstance();
            builder.RegisterInstance(new Mock<IRequestContext>().Object).As<IRequestContext>().SingleInstance();
            builder.RegisterInstance(new Mock<ICultureRequestContext>().Object).As<ICultureRequestContext>()
                .SingleInstance();
            builder.RegisterInstance(new Mock<IAccountRepository>().Object).As<IAccountRepository>().SingleInstance();
            builder.RegisterInstance(new Mock<IApplicationRepository>().Object).As<IApplicationRepository>()
                .SingleInstance();
            builder.RegisterInstance(new Mock<ISmsClient>().Object).As<ISmsClient>().SingleInstance();
            builder.RegisterInstance(new Mock<IEmailClient>().Object).As<IEmailClient>().SingleInstance();

            upgrade?.Invoke(builder);
            return builder.Build();
        }


        public static string BuildToken(
            IContainer container,
            int userId,
            params string[] permissions)
        {
            var tokenGenerator = container.Resolve<ITokenGenerator>();
            var secret = Convert.FromBase64String("kiHLSfGebYvXGTDx0vWb53JhyUpnw6HvgRwOJ6h/hUs=");
            var userPermission = permissions ?? new[] { "test1", "test2" };
            var issueTime = DateTime.UtcNow;
            var accessToken = new JsonWebToken().Encode(new
            {
                typ = "access_token",
                sub = $"{userId}",
                permissions = JsonConvert.SerializeObject(permissions),
                exp = new DateTimeOffset(issueTime.AddMinutes(15)).ToUnixTimeSeconds(),
                iat = new DateTimeOffset(issueTime).ToUnixTimeSeconds(),
                iss = "issuer",
                aud = "*",
            }, secret, JwtHashAlgorithm.HS256);
            return accessToken;
        }
    }
}