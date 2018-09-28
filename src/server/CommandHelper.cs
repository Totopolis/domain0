using Monik.Client;
using NLog;
using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Domain0.WinService.Infrastructure
{
    public static class NetshHelper
    {
        private const string Netsh = "netsh";

        public static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        public static bool AddFirewallRule(string rule, params int[] ports)
            => RunElevated(Netsh,
                $"advfirewall firewall add rule name=\"{rule}\" dir=in protocol=TCP localport=\"{string.Join(",", ports)}\" action=allow");

        public static bool RemoveFirewallRule(string rule)
            => RunElevated(Netsh,
                $"advfirewall firewall delete rule name=\"{rule}\" dir=in");

        public static bool RemoveUrlReservation(Uri uri)
            => RunElevated(Netsh, 
                $"http delete urlacl url={uri.Scheme}://{uri.Host}:{uri.Port}/".Replace("localhost", "+"));

        public static bool AddUrlReservation(Uri uri, string user)
            => RunElevated(Netsh,
                $"http add urlacl url={uri.Scheme}://{uri.Host}:{uri.Port}/ user=\"{user}\"".Replace("localhost", "+"));

        public static bool AddSslCertificate(Uri uri, X509Certificate2 cert)
            => RunElevated(Netsh,
                $"http add sslcert ipport=0.0.0.0:{uri.Port} certhash={Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(cert.Thumbprint))} appid={{{Guid.NewGuid()}}} clientcertnegotiation=enable");

        public static bool RemoveSslCertificate(Uri uri)
            => RunElevated(Netsh,
                $"http delete sslcert ipport=0.0.0.0:{uri.Port}");

        public static bool RunElevated(string file, string args)
        {
            Logger.Info("{0} {1}", Netsh, args);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    Verb = "runas",
                    Arguments = args,
                    FileName = file,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };
            process.Start();
            process.WaitForExit();
            var output = process.StandardOutput.ReadToEnd();
            if (process.ExitCode == 0)
                return true;

            Logger.Error("{0} {1}: {2}", Netsh, args, output);
            return false;
        }
    }
}
