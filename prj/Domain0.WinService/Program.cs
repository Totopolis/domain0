using Autofac;
using Domain0.Nancy;
using Domain0.WinService.Infrastructure;
using NLog;
using System;
using System.Security.Cryptography.X509Certificates;
using Nancy.Hosting.Self;
using Topshelf;
using NLog.Common;
using Domain0.FastSql;
using Nancy.Bootstrapper;

namespace Domain0.WinService
{
    internal class Program
    {
        static Program()
        {
            LogManager.ThrowExceptions = true;
            InternalLogger.LogFile = "error.log";
            InternalLogger.LogLevel = LogLevel.Error;

            Logger = LogManager.GetCurrentClassLogger();
        }

        private static void Main()
        {
            Logger.Info($"Use BasePath: {AppContext.BaseDirectory}");
            try
            {
                Initialize();

                Logger.Info("Host creating...");
                var host = CreateHost();

                Logger.Info("Host running...");
                var code = host.Run();

                Logger.Info($"Stoped with code: {code.ToString()}");
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, "cannot start service");
            }
        }

        private static Host CreateHost()
        {
            var host = HostFactory.New(x =>
            {
                x.SetDisplayName(Settings.ServiceName);
                x.SetDescription($"{Settings.ServiceName} auth service based on JWT");
                x.SetServiceName($"{Settings.ServiceName} service");
                x.StartAutomatically();
                x.EnableServiceRecovery(r => r.RestartService(0));
                x.RunAsNetworkService();
                x.EnableShutdown();
                x.OnException(ex => Logger.Fatal(ex, "unhandled exception"));

                var configuration = new HostConfiguration
                {
                    AllowChunkedEncoding = false,
                    UnhandledExceptionCallback = ex => Logger.Fatal(ex, "unhandled nancy exception")
                };

                Logger.Info("Initialize nancy...");
                x.WithNancy(Settings.Uri, configuration, bootstrapper, cert);
            });
            return host;
        }

        private static void Initialize()
        {
            Logger.Info("Use Uri={0}", Settings.Uri);
            Logger.Info("Use ConnectionString={0}", Settings.ConnectionString);

            Logger.Info("Making container...");
            container = CreateContainer();

            Logger.Info("Initialize database...");
            var dbManager = container.Resolve<DbManager>();
            dbManager.Initialize();


            Logger.Info("Making Domain0Bootstrapper...");
            bootstrapper = new Domain0Bootstrapper(container);

            Logger.Info("Load certificate...");
            cert = CertificateHelper.GetX509Cert(Settings.Uri);
            if (cert != null)
                Logger.Info($"Found certificate: {cert.Thumbprint}");
            else
                Logger.Warn("certificate not found!");

        }

        private static IContainer CreateContainer()
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(Settings.ConnectionString).Named<string>("connectionString");
            builder.Register(c => LogManager.GetCurrentClassLogger()).As<ILogger>().InstancePerDependency();
            builder.RegisterModule<DatabaseModule>();
            builder.RegisterModule<ApplicationModule>();

            return builder.Build();
        }

        private static readonly ILogger Logger;

        private static INancyBootstrapper bootstrapper;

        private static X509Certificate2 cert;

        private static IContainer container;
    }
}
