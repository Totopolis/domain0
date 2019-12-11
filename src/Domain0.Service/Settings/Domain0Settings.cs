using Domain0.Nancy.Service;
using Domain0.Nancy.Service.Ldap;
using Domain0.Repository.Settings;
using Domain0.Tokens;

namespace Domain0.Service
{
    public class Domain0Settings
    {
        public DbSettings Db { get; set; }
        public CultureContextSettings CultureContext { get; set; }
        public TokenGeneratorSettings Token { get; set; }
        public EmailClientSettings Email { get; set; }
        public SqlQueueSmsClientSettings SmsQueueClient { get; set; }
        public SmsGatewaySettings SmsGateway { get; set; }
        public AccountServiceSettings AccountService { get; set; }
        public LdapSettings Ldap { get; set; }
        public ThresholdSettings Threshold { get; set; }
    }
}