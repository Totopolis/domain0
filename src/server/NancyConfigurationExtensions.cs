using Nancy.Bootstrapper;
using System;
using System.Security.Cryptography.X509Certificates;
using Topshelf;
using Topshelf.HostConfigurators;

namespace Domain0.WinService.Infrastructure
{
    public static class NancyConfigurationExtensions
    {
        public static void WithNancy<T>(this HostConfigurator configurator, Uri uri, X509Certificate2 x509cert)
            where T : INancyBootstrapper, new()
            => WithNancy(configurator, uri, new T(), x509cert);

        public static void WithNancy(this HostConfigurator configurator, Uri uri, INancyBootstrapper bootstrapper, X509Certificate2 x509cert)
        {
            configurator.BeforeInstall(() => NancyService.InstallService(uri, x509cert));
            configurator.BeforeUninstall(() => NancyService.UninstallService(uri));
            configurator.Service(settings => new NancyService(uri, bootstrapper, x509cert));
        }
    }
}
