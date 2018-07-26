using System.Diagnostics;
using System.Threading.Tasks;
using Domain0.Service;

namespace Domain0.Nancy.Service
{
    public class FakeSmsClient : ISmsClient
    {
        public async Task Send(decimal phone, string message)
        {
            Trace.TraceInformation($"sms to: { phone }, message: { message }");            
        }
    }
}
