using Monik.Client;
using Sdl.Domain0.Nancy;
using Sdl.Topshelf.Nancy;
using System;
using Topshelf;

namespace Sdl.Domain0
{
    class Program
    {
        static void Main(string[] args)
        {
            var application = new ApplicationInfo
            {
                Name = "Domain0",
                Description = "Domain0 auth service"
            };
            var uri = new Uri("https://+:443");
            application.UseMonik();

            var code = HostFactory.Run(x =>
            {
                x.SetServiceName($"{application.Name}Service");
                x.SetDisplayName(application.Name);
                x.SetDescription(application.Description);
                x.StartAutomatically();
                x.EnableServiceRecovery(r =>
                {
                    r.RestartService(0);
                });
                x.RunAsNetworkService();
                x.EnableShutdown();
                x.OnException(ex =>
                {
                    M.ApplicationError(ex.Message);
                });

                x.WithNancy<Domain0Bootstrapper>(uri, );
            });
        }
    }
}
