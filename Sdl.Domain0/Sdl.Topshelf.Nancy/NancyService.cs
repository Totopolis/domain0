using Nancy.Bootstrapper;
using Nancy.Hosting.Self;
using System;
using Topshelf;

namespace Sdl.Topshelf.Nancy
{
    public class NancyService : ServiceControl
    {
        private readonly NancyHost _host;

        public NancyService(Uri uri, INancyBootstrapper bootstrapper)
        {
            var configuration = new HostConfiguration()
            {
                UrlReservations = new UrlReservations
                {
                    CreateAutomatically = true,
                    User = "NT AUTHORITY\\NETWORK SERVICE"
                },
                AllowChunkedEncoding = false,
                UnhandledExceptionCallback = ex => { }
            };
            _host = new NancyHost(uri, bootstrapper, configuration);
        }

        public bool Start(HostControl hostControl)
        {
            _host.Start();
            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            _host.Stop();
            return false;
        }
    }
}
