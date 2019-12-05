using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
            InternalLogger.LogLevel = NLog.LogLevel.Error;
            LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration("configs/NLog.config");

            var logger = LogManager.GetCurrentClassLogger();

            logger.Info($"Starting version: {Assembly.GetExecutingAssembly().GetName().Version}");
            logger.Info($"Use BasePath: {AppContext.BaseDirectory}");

            await CreateHostBuilder(args).Build().RunAsync();

            logger.Warn("Stopped");
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            new HostBuilder()
                .UseSystemd()
                .UseWindowsService()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureLogging(logging => { logging.ClearProviders(); })
                .ConfigureHostConfiguration(config => { config.AddEnvironmentVariables("ASPNETCORE_"); })
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config
                        .AddJsonFile("configs/appsettings.json",
                            optional: true, reloadOnChange: true)
                        .AddJsonFile($"configs/appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json",
                            optional: true, reloadOnChange: true);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseConfiguration(
                            new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddJsonFile("configs/hosting.json", optional: true)
                                .Build()
                        )
                        .UseKestrel(serverOptions => { serverOptions.AllowSynchronousIO = true; })
                        .UseStartup<Startup>();
                });
    }
}