using Autofac;
using Domain0.Nancy;
using Domain0.WinService.Certificate;
using Domain0.WinService.Infrastructure;
using System;
using System.Configuration;
using System.Security.Cryptography.X509Certificates;
using Domain0.Database;
using Topshelf;

namespace Sdl.Domain0
{
    class Program
    {
        static void Main(string[] args)
        {
            var container = CreateContainer();

            var uri = new Uri(ConfigurationManager.AppSettings["Url"]);
            var code = HostFactory.Run(x =>
            {
                x.SetDisplayName("domain0");
                x.SetDescription("domain0 auth service based on JWT");
                x.SetServiceName("domain0 service");
                x.StartAutomatically();
                x.EnableServiceRecovery(r => r.RestartService(0));
                x.RunAsNetworkService();
                x.EnableShutdown();
                x.OnException(ex => Monik.Client.M.ApplicationError(ex.Message));

                var bootstrapper = new Domain0Bootstrapper(container);
                x.WithNancy(uri, bootstrapper, GetX509Cert(uri));
            });
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
