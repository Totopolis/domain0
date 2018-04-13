using Autofac;
using Domain0.Nancy;
using Domain0.WinService.Certificate;
using Domain0.WinService.Infrastructure;
using Nancy;
using System;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Topshelf;

namespace Sdl.Domain0
{
    class Program
    {
        public const string ServiceName = "domain0";

        static void Main(string[] args)
        {
            var fields = typeof(TypeResolveStrategies).GetRuntimeFields().Where(f => f.Name.Contains("ExcludeNancy"));
            foreach (var field in fields)
                field.SetValue(null, (TypeResolveStrategy)(type => !string.Equals(type.FullName, $"Nancy.{type.Name}")));

            var container = CreateContainer();

            var uri = new Uri(ConfigurationManager.AppSettings["Url"] ?? "127.0.0.1");

            var code = HostFactory.Run(x =>
            {
                x.SetDisplayName(ServiceName);
                x.SetDescription($"{ServiceName} auth service based on JWT");
                x.SetServiceName($"{ServiceName} service");
                x.StartAutomatically();
                x.EnableServiceRecovery(r => r.RestartService(0));
                x.RunAsNetworkService();
                x.EnableShutdown();
                x.OnException(ex => Monik.Client.M.ApplicationError(ex.Message));

                var bootstrapper = new Domain0Bootstrapper(container);
                x.WithNancy(uri, bootstrapper, GetX509Cert(uri));
            });

            Thread.Sleep(TimeSpan.FromSeconds(5));
        }

        static IContainer CreateContainer()
        {
            M.Init();

            var builder = new ContainerBuilder();
            builder.RegisterInstance(ConfigurationManager.ConnectionStrings["Database"].ConnectionString).Named<string>("connectionString");
            return builder.Build();
        }

        static X509Certificate2 GetX509Cert(Uri uri)
        {
            if (!string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
                return null;

            IX509CertificateProvider provider = null;

            var fileSettings = new X509FileSettings
            {
                FilePath = ConfigurationManager.AppSettings["X509_Filepath"],
                Password = ConfigurationManager.AppSettings["X509_Password"]
            };
            if (!string.IsNullOrEmpty(fileSettings.FilePath))
            {
                provider = new X509FileProvider(fileSettings);
            }
            else
            {
                if (!Enum.TryParse(ConfigurationManager.AppSettings["X509_Location"], out StoreLocation location))
                    throw new ArgumentException(nameof(location), "Couldnt parse app.config X509_Location as StoreLocation");
                if (!Enum.TryParse(ConfigurationManager.AppSettings["X509_StoreName"], out StoreName storeName))
                    throw new ArgumentException(nameof(location), "Couldnt parse app.config X509_Location as StoreLocation");

                var storeSettings = new X509StoreSettings
                {
                    Location = location,
                    Name = storeName,
                    Subject = ConfigurationManager.AppSettings["X509_Subject"]
                };
                if (!string.IsNullOrEmpty(storeSettings.Subject))
                    provider = new X509StoreProvider(storeSettings);
            }

            X509Certificate2 x509cert = null;
            if (uri.Scheme == "https")
                x509cert = provider?.GetCert();
            return x509cert;
        }
    }
}
