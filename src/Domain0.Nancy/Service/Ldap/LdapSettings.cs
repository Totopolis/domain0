namespace Domain0.Nancy.Service.Ldap
{
    public class LdapSettings
    {
        public string DomainControllerName { get; set; }
        public int LdapPort { get; set; }
        public bool UseSecureSocketLayer { get; set; }
        public int LdapProtocolVersion { get; set; }
        public string EmailAttributeName { get; set; }
    }
}
