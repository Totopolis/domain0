using System;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Net;
using Domain0.Nancy.Model;
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

        public LdapUser Authorize(string username, string pwd)
        {
            var crd = new NetworkCredential(username, pwd);
            try
            {
                using (var ldapConnection = GetldapConnection(crd))
                {
                    var distinguishedName = string.Join(", ",
                        _ldapSettings.DomainControllerName.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).Select(dc => $"DC={dc}"));

                    var request = new SearchRequest(
                        distinguishedName,
                        $"(&(objectCategory=person)(objectClass=user)(SAMAccountName={username}))",
                        SearchScope.Subtree, "*");
                    var response = (SearchResponse)ldapConnection.SendRequest(request);

                    if (response == null || response.Entries.Count == 0)
                        return null;

                    var entry = response.Entries[0];
                    var attr = entry.Attributes[_ldapSettings.EmailAttributeName];

                    if (attr != null)
                        return new LdapUser
                        {
                            Email = (string)attr.GetValues(typeof(string)).FirstOrDefault()
                        };
                    return null;

                }
            }
            catch (LdapException e)
            {
                _logger.Warn($"User { username} wrong login or password!");
                _logger.Error(e.Message);
                return null;
            }

        }

        private LdapConnection GetldapConnection(NetworkCredential cred = null)
        {
            var ldi = new LdapDirectoryIdentifier(_ldapSettings.DomainControllerName, _ldapSettings.LdapPort);
            var ldapConnection = new LdapConnection(ldi)
            {
                AuthType = _ldapSettings.LdapAuthType,
            };

            ldapConnection.SessionOptions.ProtocolVersion = _ldapSettings.LdapProtocolVersion;
            if (_ldapSettings.UseSecureSocketLayer)
            {
                ldapConnection.SessionOptions.SecureSocketLayer = _ldapSettings.UseSecureSocketLayer;
                ldapConnection.SessionOptions.VerifyServerCertificate = (con, cer) => true;
            }

            ldapConnection.Credential = cred;
            ldapConnection.Bind();
            return ldapConnection;
        }
    }
}
