using System;
using System.Reflection;
using System.Threading.Tasks;
using Domain0.Nancy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using NLog;
using NLog.Common;

namespace Domain0.Service
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            LogManager.ThrowExceptions = true;
            InternalLogger.LogFile = "error.log";
            InternalLogger.LogLevel = LogLevel.Error;

            var logger = LogManager.GetCurrentClassLogger();

            logger.Info($"Starting version: {Assembly.GetExecutingAssembly().GetName().Version}");
            logger.Info($"Use BasePath: {AppContext.BaseDirectory}");

            logger.Info("Use Uri={0}", Settings.Uri);
            logger.Info("Use ConnectionString={0}", Settings.ConnectionString);

            await CreateHostBuilder(args).Build().RunAsync();

            logger.Warn("Stopped");
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSystemd()
                .UseWindowsService()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseKestrel(serverOptions => { serverOptions.AllowSynchronousIO = true; })
                        .UseUrls(Settings.Uri.ToString().Replace("localhost", "*"))
                        .UseStartup<Startup>();
                });
    }
}