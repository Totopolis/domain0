using Nancy.Bootstrapper;
using Nancy.Hosting.Self;
using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using Topshelf;

namespace Sdl.Topshelf.Nancy
{
    public class NancyService : ServiceControl
    {
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
            CommandHelper.OpenFirewallPorts("domain0-port", _uri.Port);
            Debug.WriteLine($"firewall port added for {_uri.Port}");
            CommandHelper.RemoveUrlReservation(_uri);
            CommandHelper.AddUrlReservation(_uri, "NT AUTHORITY\\NETWORK SERVICE");
            Debug.WriteLine($"reservation added for {_uri}");
            if (_uri.Scheme == "https")
            {
                if (_x509cert == null)
                    throw new ArgumentNullException(nameof(_x509cert), "https");

                CommandHelper.DeleteSslCertificate(_uri);
                CommandHelper.AddSslCertificate(_uri, _x509cert);
                Debug.WriteLine($"ssl certificate added for {_uri}");
            }

            _host.Start();
            Debug.WriteLine($"nancy started on {_uri}");
            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            _host.Stop();
            Debug.WriteLine($"nancy stoped on {_uri}");

            CommandHelper.RemoveUrlReservation(_uri);
            Debug.WriteLine($"reservation removed for {_uri}");
            return false;
        }
    }
}
