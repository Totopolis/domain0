namespace Domain0.Nancy.Service.Ldap
{
    public class LdapSettings
    {
        public string DomainName { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public bool UseSecureSocketLayer { get; set; }
        public bool TlsReqCertAllow { get; set; }
        public int ProtocolVersion { get; set; }
        public string EmailAttributeName { get; set; }
    }
}
