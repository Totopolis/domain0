using System;

namespace Domain0.Tokens
{
    public class TokenGeneratorSettings
    {
        public string Issuer { get; set; }

        public string Audience { get; set; }

        public string RefreshAudience { get; set; }

        public TimeSpan Lifetime { get; set; }

        public TimeSpan RefreshLifetime { get; set; }

        public string Secret { get; set; }

        public string Private { get; set; }

        public string Public { get; set; }

        public string Alg { get; set; }
    }
}