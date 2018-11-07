using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Domain0.Api.Client
{
    public interface IDomain0ClientScope : IClientScope<IDomain0Client>
    {
        string HostUrl { get; set; }
    }

    public class Domain0ClientScope : ClientLockScope<IDomain0Client>, IDomain0ClientScope
    {
        public Domain0ClientScope() : this(new HttpClient())
        {
        }

        public Domain0ClientScope(HttpClient http)
        {
            httpClient = http;
            domain0Client = new Domain0Client(null, httpClient);
        }

        public override IDomain0Client Client => domain0Client;

        public override string Token
        {
            get => base.Token;
            set
            {
                using (RequestSetupLock.WriterLock())
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        TokenValue = null;
                    }
                    else
                    {
                        TokenValue = value;
                    }

                    httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", TokenValue);
                }
            }
        }

        public string HostUrl
        {
            get
            {
                using (RequestSetupLock.ReaderLock())
                {
                    return domain0Client.BaseUrl;
                }
            }
            set
            {
                using (RequestSetupLock.WriterLock())
                {
                    domain0Client.BaseUrl = value;
                    AdjustConnectionsLimit(value);
                }
            }
        }

        private void AdjustConnectionsLimit(string baseUrl)
        {
            var delayServicePoint = ServicePointManager.FindServicePoint(
                new Uri(baseUrl));
            delayServicePoint.ConnectionLimit = 15;
        }

        private readonly HttpClient httpClient;
        private readonly Domain0Client domain0Client;
    }
}