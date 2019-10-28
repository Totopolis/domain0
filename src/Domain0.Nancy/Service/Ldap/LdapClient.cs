using System;
using System.Linq;
using System.Threading.Tasks;
using Domain0.Nancy.Model;
using LdapForNet;
using LdapForNet.Native;
using NLog;

namespace Domain0.Nancy.Service.Ldap
{
    public class LdapClient : ILdapClient
    {
        private readonly ILogger _logger;
        private readonly LdapSettings _ldapSettings;

        public LdapClient(ILogger logger, LdapSettings ldapSettings)
        {
            _logger = logger;
            _ldapSettings = ldapSettings;
        }

        public async Task<LdapUser> Authorize(string username, string pwd)
        {
            try
            {
                using (var ldapConnection = GetldapConnection(username, pwd))
                {
                    var distinguishedName = string.Join(",",
                        _ldapSettings.DomainControllerName.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).Select(dc => $"DC={dc}"));
                    var response = await ldapConnection.SearchAsync(distinguishedName,
                        $"(&(objectCategory=person)(objectClass=user)(SAMAccountName={username}))",
                        Native.LdapSearchScope.LDAP_SCOPE_SUBTREE);

                    if (response == null || response.Count == 0)
                        return null;

                    var entry = response.First();
                    var attr = entry.Attributes[_ldapSettings.EmailAttributeName];

                    return attr != null
                        ? new LdapUser { Email = attr.FirstOrDefault() }
                        : null;
                }
            }
            catch (LdapException e)
            {
                _logger.Warn($"User {username} wrong login or password!");
                _logger.Error(e.Message);
                return null;
            }

        }

        private LdapConnection GetldapConnection(string username, string pwd)
        {
            var ldapConnection = new LdapConnection();
            
            ldapConnection.Connect(_ldapSettings.DomainControllerName, _ldapSettings.LdapPort,
                (Native.LdapVersion) _ldapSettings.LdapProtocolVersion);

            ldapConnection.Bind(Native.LdapAuthMechanism.GSSAPI, username, pwd);

            return ldapConnection;
        }
    }
}
