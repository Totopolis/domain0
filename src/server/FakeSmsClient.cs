using System.Diagnostics;
using System.Threading.Tasks;
using Domain0.Service;

namespace Domain0.Nancy.Service
{
    public class FakeSmsClient : ISmsClient
    {
        public async Task Send(decimal phone, string message, string environment)
        {
            Trace.TraceInformation($"sms to: { phone }, message: { message }, environment: { environment }");
            await Task.CompletedTask;
        }
    }
}
