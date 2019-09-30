using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using NLog;
using NLog.Common;
using NLog.Web;

namespace Domain0.Nancy.Kestrel
{
    using Microsoft.AspNetCore.Hosting;

    public class Program
    {
        private static readonly ILogger Logger;

        static Program()
        {
            LogManager.ThrowExceptions = true;
            InternalLogger.LogFile = "error.log";
            InternalLogger.LogLevel = LogLevel.Error;

            Logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
        }

        public static void Main()
        {
            Logger.Info($"Starting version: {Assembly.GetExecutingAssembly().GetName().Version}");
            Logger.Info($"Use BasePath: {AppContext.BaseDirectory}");

            var host = new HostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseKestrel(serverOptions => { serverOptions.AllowSynchronousIO = true; })
                        .UseUrls(Settings.Uri.ToString().Replace("localhost", "*"))
                        .UseStartup<Startup>();
                })
                .Build();

            host.Run();
        }
    }
}