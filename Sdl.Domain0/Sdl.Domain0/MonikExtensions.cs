using Monik.Client;
using System;
using System.Diagnostics;

namespace Sdl.Domain0
{
    public static class M
    {
        public const string MONIK_SOURCE = nameof(MONIK_SOURCE);

        public static void Init()
        {
            var azureSender = new AzureSender("", "");
            var instanceName = "Dev";
            var isDebug = AppContext.BaseDirectory.IndexOf("debug", 0, StringComparison.InvariantCultureIgnoreCase) >= 0;
            var isProduction = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Production", StringComparison.InvariantCultureIgnoreCase) ?? false;
            if (isProduction && !isDebug)
                instanceName = "Azure";

            var source = Environment.GetEnvironmentVariable(MONIK_SOURCE);
            Trace.WriteLine($"BasePath: {AppContext.BaseDirectory}");
            Trace.WriteLine($"Monik: {source} {instanceName}");
            Monik.Client.M.Initialize(azureSender, source, instanceName, aAutoKeepAliveEnable: true);
        }
    }
}
