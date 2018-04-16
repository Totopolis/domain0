using Monik.Client;
using System;
using System.Diagnostics;

namespace Sdl.Domain0
{
    public class ApplicationInfo
    {
        public string Name { get; set; }

        public string Description { get; set; }
    }

    public static class MonikExtensions
    {

        public static void UseMonik(this ApplicationInfo applicationInfo)
        {
            var azureSender = new AzureSender("", "");
            var instanceName = "Dev";
            var isDebug = AppContext.BaseDirectory.IndexOf("debug", 0, StringComparison.InvariantCultureIgnoreCase) >= 0;
            var isProduction = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Production", StringComparison.InvariantCultureIgnoreCase) ?? false;
            if (isProduction && !isDebug)
                instanceName = "Azure";

            Trace.WriteLine($"BasePath: {AppContext.BaseDirectory}");
            Trace.WriteLine($"Monik: {applicationInfo.Name} {instanceName}");
            M.Initialize(azureSender, applicationInfo.Name, instanceName, aAutoKeepAliveEnable: true);
        }
    }
}
