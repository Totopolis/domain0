namespace Domain0.Tokens
{
    public class TokenValidationSettings
    {
        public string Audience { get; set; }

        public string Issuer { get; set; }

        public bool ValidateLifetime { get; set; }

        public KeyInfo[] Keys { get; set; }
    }

    public class KeyInfo
    {
        public string Alg { get; set; }

        public string Key { get; set; }

        public int? KeySize { get; set; }
    }
}