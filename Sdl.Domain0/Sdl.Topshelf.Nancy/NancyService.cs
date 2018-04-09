using Nancy.Bootstrapper;
using Nancy.Hosting.Self;
using System;
using System.Diagnostics;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using Topshelf;
using Topshelf.Hosts;

namespace Sdl.Topshelf.Nancy
{
    public class NancyService : ServiceControl
    {
        public const string FirewallRule = "domain0-rule-https";

        private readonly Uri _uri;

        private readonly X509Certificate2 _x509cert;

        private readonly NancyHost _host;

        public NancyService(Uri uri, INancyBootstrapper bootstrapper, X509Certificate2 x509cert)
        {
            var configuration = new HostConfiguration()
            {
                UrlReservations = new UrlReservations
                {
                    // do not create url reservation
                    CreateAutomatically = false
                },
                AllowChunkedEncoding = false,
                UnhandledExceptionCallback = ex => { }
            };
            _uri = uri;
            _x509cert = x509cert;
            _host = new NancyHost(uri, bootstrapper, configuration);
        }

        public bool Start(HostControl hostControl)
        {
            if (hostControl is ConsoleRunHost)
                InstallService(_uri, _x509cert);

            _host.Start();
            Debug.WriteLine($"nancy started on {_uri}");
            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            _host.Stop();
            Debug.WriteLine($"nancy stoped on {_uri}");

            if (hostControl is ConsoleRunHost)
                UninstallService(_uri);

            return false;
        }

        public static void InstallService(Uri uri, X509Certificate2 x509cert)
        {
            CommandHelper.RemoveFirewallRule(FirewallRule);
            if (!CommandHelper.AddFirewallRule(FirewallRule, uri.Port))
                throw new SecurityException("couldnot execute firewall rule look at Monik");
            CommandHelper.RemoveUrlReservation(uri);
            if (!CommandHelper.AddUrlReservation(uri, "NT AUTHORITY\\NETWORK SERVICE"))
                throw new SecurityException("couldnot add url reservation look at Monik");
            if (uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                if (x509cert == null)
                    throw new SecurityException("couldnot load x509 cert for https");

                CommandHelper.RemoveSslCertificate(uri);
                if (!CommandHelper.AddSslCertificate(uri, x509cert))
                    throw new SecurityException("couldnot set ssl cert look at Monik");
            }
        }

        public static void UninstallService(Uri uri)
        {
            CommandHelper.RemoveFirewallRule(FirewallRule);
            CommandHelper.RemoveUrlReservation(uri);
            if (uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                CommandHelper.RemoveSslCertificate(uri);

        }
    }
}
