using Nancy.Bootstrapper;
using System;
using System.Security.Cryptography.X509Certificates;
using Topshelf;
using Topshelf.HostConfigurators;

namespace Sdl.Topshelf.Nancy
{
    public static class NancyConfigurationExtensions
    {
        public static void WithNancy<T>(this HostConfigurator configurator, Uri uri, X509Certificate2 certificate)
            where T : INancyBootstrapper, new()
        {
            configurator.BeforeInstall(() =>
            {
                CommandHelper.OpenFirewallPorts("domain0-port", uri.Port);
                CommandHelper.AddUrlReservation(uri, "NT AUTHORITY\\NETWORK SERVICE");
                if (uri.Scheme == "https" && certificate != null)
                    CommandHelper.AddSslCertificate(uri, certificate);
            });
            configurator.BeforeUninstall(() =>
            {
                CommandHelper.RemoveUrlReservation(uri);
            });

            configurator.Service(settings => new NancyService(uri, new T()));
        }
    }
}
