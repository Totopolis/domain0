using Microsoft.Extensions.Configuration;
using Sdl.Domain0.Infrastructure;
using Sdl.Domain0.Nancy;
using Sdl.Topshelf.Nancy;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Topshelf;

namespace Sdl.Domain0
{
    class Program
    {
        static IConfiguration Configuration { get; set; }

        static void Main(string[] args)
        {
            M.Init();

            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json")
                .Build();

            var uri = new Uri(Configuration["Uri"]);
            var code = HostFactory.Run(x =>
            {
                x.SetDisplayName(Configuration["App:Name"]);
                x.SetDescription(Configuration["App:Desc"]);
                x.SetServiceName(Configuration["App:Service"]);
                x.StartAutomatically();
                x.EnableServiceRecovery(r => r.RestartService(0));
                x.RunAsNetworkService();
                x.EnableShutdown();
                x.OnException(ex =>
                {
                    Monik.Client.M.ApplicationError(ex.Message);
                });

                x.WithNancy<Domain0Bootstrapper>(uri, GetX509Cert(uri));
            });
        }

        static X509Certificate2 GetX509Cert(Uri uri)
        {
            IX509CertificateProvider provider = null;

            var fileSettings = Configuration.GetSection("X509Provider").Get<X509FileSettings>();
            if (!string.IsNullOrEmpty(fileSettings.FilePath))
            {
                provider = new X509FileProvider(fileSettings);
            }
            else
            {
                var storeSettings = Configuration.GetSection("X509Provider").Get<X509StoreSettings>();
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
