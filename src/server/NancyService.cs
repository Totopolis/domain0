using Nancy.Bootstrapper;
using Nancy.Hosting.Self;
using System;
using System.Diagnostics;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using Topshelf;
using Topshelf.Hosts;

namespace Domain0.WinService.Infrastructure
{
    public class NancyService : ServiceControl
    {
        public const string FirewallRule = "domain0-rule-https";

        private readonly Uri _uri;

        private readonly X509Certificate2 _x509cert;

        private readonly NancyHost _host;

        public NancyService(Uri uri, HostConfiguration configuration, INancyBootstrapper bootstrapper, X509Certificate2 x509cert)
        {
            _uri = uri;
            _x509cert = x509cert;
            _host = new NancyHost(uri, bootstrapper, configuration);
        }

        public bool Start(HostControl hostControl)
        {
            if (hostControl is ConsoleRunHost)
                InstallService(_uri, _x509cert);

            _host.Start();

            if(hostControl is ConsoleRunHost)
                Process.Start($"{_uri}swagger-ui/index.html");

            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            _host.Stop();

            if (hostControl is ConsoleRunHost)
                UninstallService(_uri);

            return true;
        }

        public static void InstallService(Uri uri, X509Certificate2 x509cert)
        {
            NetshHelper.RemoveFirewallRule(FirewallRule);
            if (!NetshHelper.AddFirewallRule(FirewallRule, uri.Port))
                throw new SecurityException("couldnot execute firewall rule look at Monik");
            NetshHelper.RemoveUrlReservation(uri);
            if (!NetshHelper.AddUrlReservation(uri, $"{Environment.UserDomainName}\\{Environment.UserName}"))
                throw new SecurityException("couldnot add url reservation look at Monik");
            if (uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                if (x509cert == null)
                    throw new SecurityException("couldnot load x509 cert for https");

                NetshHelper.RemoveSslCertificate(uri);
                if (!NetshHelper.AddSslCertificate(uri, x509cert))
                    throw new SecurityException("couldnot set ssl cert look at Monik");
            }
        }

        public static void UninstallService(Uri uri)
        {
            NetshHelper.RemoveFirewallRule(FirewallRule);
            NetshHelper.RemoveUrlReservation(uri);
            if (uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                NetshHelper.RemoveSslCertificate(uri);

        }
    }
}
