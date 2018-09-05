using System.Diagnostics;
using System.Threading.Tasks;

namespace Domain0.Service
{
    public class FakeEmailClient : IEmailClient
    {
        public FakeEmailClient(EmailClientSettings settingsInstance)
        {
            settings = settingsInstance;
        }

        public async Task Send(string subject, string emailTo, string message)
        {
            Trace.TraceInformation($"email to: { emailTo }, subject: { subject } message: { message }");
            await Task.CompletedTask;
        }

        private readonly EmailClientSettings settings;
    }
}
