using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Domain0.Service;
using System.Text;

namespace Domain0.Nancy.Service
{
    public class SmscClient : ISmsClient
    {
        private SmscSettings _settingsInstance;
        public SmscClient(SmscSettings settingsInstance)
        {
           _settingsInstance = settingsInstance;
        }

        public async Task Send(decimal phone, string message, string environment)
        {
            var url = FormatSendCommand(phone.ToString(), message, environment);
            var content = await new HttpClient().GetStringAsync(url);
        }
        public virtual string GetNaming(string environment)
        {
            return _settingsInstance.Naming ?? "";
        }

        public string FormatSendCommand(string phone, string text, string environment)
        {

            var builder = new StringBuilder(_settingsInstance.Host);
            builder.Append($"&login={Uri.EscapeDataString(_settingsInstance.Login)}&psw={Uri.EscapeDataString(_settingsInstance.Password)}");
            builder.Append($"&phones={phone}&mes={Uri.EscapeDataString(text)}");
            if (!string.IsNullOrEmpty(environment))
            {
                builder.Append($"&sender={Uri.EscapeDataString(GetNaming(environment))}");
            }
            
             return builder.ToString();
        }

    }
}
