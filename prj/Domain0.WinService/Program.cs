using Autofac;
using Domain0.Nancy;
using Domain0.WinService.Infrastructure;
using NLog;
using System;
using System.Configuration;
using System.Security.Cryptography.X509Certificates;
using Nancy.Hosting.Self;
using Topshelf;
using NLog.Common;
using Domain0.FastSql;
using Nancy.Bootstrapper;

namespace Domain0.WinService
{
    class Program
    {
#if DEBUG
        public const string ServiceName = "domain0Debug";
#else
        public const string ServiceName = "domain0";
#endif

#if DEBUG
        public const string DefaultHttpsUri = "https://localhost:4443";
#else
        public const string DefaultHttpsUri = "https://localhost";
#endif

#if DEBUG
        public const string DefaultHttpUri = "http://localhost:8880";
#else
        public const string DefaultHttpUri = "http://localhost";
#endif

        public const string DefaultConnectionString =
            "Data Source=.;Initial Catalog=Telematic;Persist Security Info=True;Integrated Security=True";

        static Program()
        {
            LogManager.ThrowExceptions = true;
            InternalLogger.LogFile = "error.log";
            InternalLogger.LogLevel = LogLevel.Error;

            Logger = LogManager.GetCurrentClassLogger();
        }

        static void Main(string[] args)
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
                x.SetDisplayName(ServiceName);
                x.SetDescription($"{ServiceName} auth service based on JWT");
                x.SetServiceName($"{ServiceName} service");
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
                x.WithNancy(uri, configuration, bootstrapper, cert);
            });
            return host;
        }

        private static void Initialize()
        {
            var connectionString =
                ConfigurationManager.ConnectionStrings["Database"]?.ConnectionString ?? DefaultConnectionString;
            uri = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["Url"])
                ? new Uri(ConfigurationManager.AppSettings["Url"])
                : new Uri(CertificateHelper.HasX509CertificateSettings() ? DefaultHttpsUri : DefaultHttpUri);
            Logger.Info("Use Uri={0}", uri);
            Logger.Info("Use ConnectionString={0}", connectionString);

            Logger.Info("Making container...");
            container = CreateContainer(connectionString);

            Logger.Info("Initialize database...");
            var dbManager = container.Resolve<DbManager>();
            dbManager.Initialize();


            Logger.Info("Making Domain0Bootstrapper...");
            bootstrapper = new Domain0Bootstrapper(container);

            Logger.Info("Load certificate...");
            cert = CertificateHelper.GetX509Cert(uri);
            if (cert != null)
                Logger.Info($"Found certificate: {cert.Thumbprint}");
            else
                Logger.Warn($"{ConfigurationManager.AppSettings["X509_Filepath"]} certificate not found! ");

        }

        static IContainer CreateContainer(string connectionString)
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(connectionString).Named<string>("connectionString");
            builder.Register(c => LogManager.GetCurrentClassLogger()).As<ILogger>().InstancePerDependency();
            builder.RegisterModule<DatabaseModule>();
            builder.RegisterModule<ApplicationModule>();

            return builder.Build();
        }

        private static readonly ILogger Logger;

        private static INancyBootstrapper bootstrapper;

        private static X509Certificate2 cert;

        private static IContainer container;

        private static Uri uri;
    }
}
