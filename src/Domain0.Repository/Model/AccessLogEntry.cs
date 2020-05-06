using System;

namespace Domain0.Repository.Model
{
    public class AccessLogEntry
    {
        public long? Id { get; set; }

        public string Action { get; set; }

        public string Method { get; set; }

        public string ClientIp { get; set; }

        public DateTime ProcessedAt { get; set; }

        public int? StatusCode { get; set; }

        public string UserAgent { get; set; }

        public string UserId { get; set; }

        public string Referer { get; set; }

        public int? ProcessingTime { get; set; }

        public string AcceptLanguage { get; set; }
    }
}
