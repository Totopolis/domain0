using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Sdl.Topshelf.Nancy
{
    public static class CommandHelper
    {
        public static bool OpenFirewallPorts(string rule, params int[] ports)
            => RunElevated("netsh",
                $"advfirewall firewall add rule name=\"{rule}\" dir=in protocol=TCP localport=\"{string.Join(",", ports)}\" action=allow",
                out var output);

        public static bool RemoveUrlReservation(Uri uri)
            => RunElevated("netsh", 
                $"http delete urlacl url={uri.Scheme}://{uri.Host}:{uri.Port}/".Replace("localhost", "+"),
                out var text);

        public static bool AddUrlReservation(Uri uri, string user)
            => RunElevated("netsh", 
                $"http add urlacl url={uri.Scheme}://{uri.Host}:{uri.Port}/ user=\"{user}\"".Replace("localhost", "+"),
                out var text);

        public static bool AddSslCertificate(Uri uri, X509Certificate2 cert)
            => RunElevated("netsh",
                $"http add sslcert ipport=0.0.0.0:{uri.Port} certhash={Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(cert.Thumbprint))} appid={{{Guid.NewGuid()}}}",
                out var text);

        public static bool DeleteSslCertificate(Uri uri)
            => RunElevated("netsh",
                $"http delete sslcert ipport=0.0.0.0:{uri.Port}",
                out var text);

        public static bool RunElevated(string file, string args, out string output)
        {
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
            output = process.StandardOutput.ReadToEnd();
            return process.ExitCode == 0;
        }
    }
}
