using Nancy.Bootstrapper;
using System;
using System.Security.Cryptography.X509Certificates;
using Topshelf;
using Topshelf.HostConfigurators;

namespace Sdl.Topshelf.Nancy
{
    public static class NancyConfigurationExtensions
    {
        public static void WithNancy<T>(this HostConfigurator configurator, Uri uri, X509Certificate2 x509cert)
            where T : INancyBootstrapper, new()
        {
            configurator.BeforeInstall(() =>
            {
                CommandHelper.OpenFirewallPorts("domain0-port", uri.Port);
                CommandHelper.RemoveUrlReservation(uri);
                CommandHelper.AddUrlReservation(uri, "NT AUTHORITY\\NETWORK SERVICE");
                if (uri.Scheme == "https")
                {
                    if (x509cert == null)
                        throw new ArgumentNullException(nameof(x509cert), "https");

                    CommandHelper.DeleteSslCertificate(uri);
                    CommandHelper.AddSslCertificate(uri, x509cert);
                }
            });
            configurator.BeforeUninstall(() =>
            {
                CommandHelper.RemoveUrlReservation(uri);
            });

            configurator.Service(settings => new NancyService(uri, new T(), x509cert));
        }
    }
}
