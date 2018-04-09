using Nancy.Bootstrapper;
using Nancy.Hosting.Self;
using System;
using System.Diagnostics;
using Topshelf;

namespace Sdl.Topshelf.Nancy
{
    public class NancyService : ServiceControl
    {
        private readonly Uri _uri;

        private readonly NancyHost _host;

        public NancyService(Uri uri, INancyBootstrapper bootstrapper)
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
            _host = new NancyHost(uri, bootstrapper, configuration);
        }

        public bool Start(HostControl hostControl)
        {
            _host.Start();
            Debug.WriteLine($"nancy start on {_uri}");
            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            _host.Stop();
            Debug.WriteLine($"nancy stop on {_uri}");
            return false;
        }
    }
}
