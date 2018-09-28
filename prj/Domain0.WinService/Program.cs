using Autofac;
using Domain0.Nancy;
using Domain0.WinService.Certificate;
using Domain0.WinService.Infrastructure;
using Nancy;
using NLog;
using System;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Nancy.Hosting.Self;
using Topshelf;
using NLog.Common;
using Domain0.FastSql;

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

        public static readonly ILogger Logger;

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
                Run();
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, "cannot start service");
            }

#if DEBUG
            Console.ReadKey(true);
#endif
        }

        static void Run()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["Database"]?.ConnectionString ?? DefaultConnectionString;
            var uri = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["Url"])
                ? new Uri(ConfigurationManager.AppSettings["Url"])
                : new Uri(CertificateHelper.HasX509CertificateSettings() ? DefaultHttpsUri : DefaultHttpUri);
            Logger.Info("Use Uri={0}", uri);
            Logger.Info("Use ConnectionString={0}", connectionString);

            var container = CreateContainer(connectionString);
            var dbManager = container.Resolve<DbManager>();
            dbManager.Initialize();

            var code = HostFactory.Run(x =>
            {
                x.SetDisplayName(ServiceName);
                x.SetDescription($"{ServiceName} auth service based on JWT");
                x.SetServiceName($"{ServiceName} service");
                x.StartAutomatically();
                x.EnableServiceRecovery(r => r.RestartService(0));
                x.RunAsNetworkService();
                x.EnableShutdown();
                x.OnException(ex => Logger.Fatal(ex, "unhandled exception"));

                var bootstrapper = new Domain0Bootstrapper(container);
                var configuration = new HostConfiguration
                {
                    UrlReservations = new UrlReservations
                    {
                        CreateAutomatically = true
                    },
                    AllowChunkedEncoding = false,
                    UnhandledExceptionCallback = ex => Logger.Fatal(ex, "unhandled nancy exception")
                };
                x.WithNancy(uri, configuration, bootstrapper, CertificateHelper.GetX509Cert(uri));
            });
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
    }
}
