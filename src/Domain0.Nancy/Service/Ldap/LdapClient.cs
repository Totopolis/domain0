using System;
using System.Linq;
using System.Threading.Tasks;
using Domain0.Nancy.Model;
using NLog;
using Novell.Directory.Ldap;

namespace Domain0.Nancy.Service.Ldap
{
    public class LdapClient : ILdapClient
    {
        private readonly ILogger _logger;
        private readonly LdapSettings _ldapSettings;
        private readonly string _baseDn;

        public LdapClient(ILogger logger, LdapSettings ldapSettings)
        {
            _logger = logger;
            _ldapSettings = ldapSettings;
            _baseDn = string.Join(",",
                _ldapSettings.Host
                    .Split(new[] {'.'}, StringSplitOptions.RemoveEmptyEntries)
                    .Select(dc => $"dc={dc}"));
        }

        public Task<LdapUser> Authorize(string username, string pwd)
        {
            try
            {
                using (var ldapConnection = GetLdapConnection(username, pwd))
                {
                    var response = ldapConnection.Search(_baseDn,
                        LdapConnection.ScopeSub,
                        $"(&(objectCategory=person)(objectClass=user)(SAMAccountName={username}))",
                        null, false)?.ToList();

                    if (response == null || response.Count == 0)
                        return null;

                    var entry = response.First();
                    var attr = entry.GetAttribute(_ldapSettings.EmailAttributeName);

                    var result = attr != null
                        ? new LdapUser {Email = attr.StringValue}
                        : null;

                    return Task.FromResult(result);
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Ldap login failed: {e}");
                return Task.FromResult((LdapUser) null);
            }
        }

        private LdapConnection GetLdapConnection(string username, string pwd)
        {
            var ldapConnection = new LdapConnection
            {
                SecureSocketLayer = _ldapSettings.UseSecureSocketLayer,
            };

            var cons = ldapConnection.Constraints;
            cons.ReferralFollowing = true;
            ldapConnection.Constraints = cons;

            if (_ldapSettings.TlsReqCertAllow)
            {
                ldapConnection.UserDefinedServerCertValidationDelegate +=
                    (sender, certificate, chain, errors) => true;
            }

            ldapConnection.Connect(_ldapSettings.Host, _ldapSettings.Port);
            ldapConnection.Bind(_ldapSettings.ProtocolVersion,
                $"{_ldapSettings.DomainName}\\{username}", pwd);

            return ldapConnection;
        }
    }
}