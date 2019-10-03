using System;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using NLog;
using NLog.Common;

namespace Domain0.Nancy.Kestrel
{
    public class Program
    {
        private static readonly ILogger Logger;

        static Program()
        {
            LogManager.ThrowExceptions = true;
            InternalLogger.LogFile = "error.log";
            InternalLogger.LogLevel = LogLevel.Error;

            Logger = LogManager.GetCurrentClassLogger();
        }

        public static void Main()
        {
            Logger.Info($"Starting version: {Assembly.GetExecutingAssembly().GetName().Version}");
            Logger.Info($"Use BasePath: {AppContext.BaseDirectory}");
            
            Logger.Info("Use Uri={0}", Settings.Uri);
            Logger.Info("Use ConnectionString={0}", Settings.ConnectionString);

            var host = new HostBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseKestrel(serverOptions => { serverOptions.AllowSynchronousIO = true; })
                        .UseUrls(Settings.Uri.ToString().Replace("localhost", "*"))
                        .UseStartup<Startup>();
                })
                .Build();

            host.Run();

            Logger.Warn("Stopped");
        }
    }
}