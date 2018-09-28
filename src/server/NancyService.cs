using Nancy.Bootstrapper;
using Nancy.Hosting.Self;
using System;
using System.Diagnostics;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using NLog;
using Topshelf;
using Topshelf.Hosts;

namespace Domain0.WinService.Infrastructure
{
    public class NancyService : ServiceControl
    {
        public const string FirewallRule = "domain0-rule-https";

        private readonly Uri uri;

        private readonly X509Certificate2 x509Cert;

        private readonly NancyHost host;

        private readonly Logger logger;

        public NancyService(
            Uri hostUri, 
            HostConfiguration configuration, 
            INancyBootstrapper bootstrapper, 
            X509Certificate2 hostX509Cert)
        {
            uri = hostUri;
            x509Cert = hostX509Cert;
            host = new NancyHost(uri, bootstrapper, configuration);
            logger = LogManager.GetCurrentClassLogger();

        }

        public bool Start(HostControl hostControl)
        {
            if (!(hostControl is ConsoleRunHost))
            {
                logger.Info("InstallService... ");
                InstallService(uri, x509Cert);
            }

            try
            {
                host.Start();
            }
            catch (Exception ex)
            {
                logger.Info($"Starting nancy host exception {ex}");
                throw;
            }

            if (hostControl is ConsoleRunHost)
            {
                logger.Info("Opening default page... ");
                Process.Start($"{uri}swagger-ui/index.html");
            }

            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            host.Stop();

            if (!(hostControl is ConsoleRunHost))
            {
                logger.Info("UninstallService... ");
                UninstallService(uri);
            }

            return true;
        }

        public static void InstallService(Uri uri, X509Certificate2 x509Cert)
        {
            NetshHelper.RemoveFirewallRule(FirewallRule);
            if (!NetshHelper.AddFirewallRule(FirewallRule, uri.Port))
                throw new SecurityException("could not execute firewall rule look at Monik");
            NetshHelper.RemoveUrlReservation(uri);
            if (!NetshHelper.AddUrlReservation(uri, $"{Environment.UserDomainName}\\{Environment.UserName}"))
                throw new SecurityException("could not add url reservation look at Monik");
            if (uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                if (x509Cert == null)
                    throw new SecurityException("could not load x509 cert for https");

                NetshHelper.RemoveSslCertificate(uri);
                if (!NetshHelper.AddSslCertificate(uri, x509Cert))
                    throw new SecurityException("could not set ssl cert look at Monik");
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
