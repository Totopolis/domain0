using System.Threading.Tasks;
using Domain0.Nancy.Model;

namespace Domain0.Nancy.Service.Ldap
{
    public interface ILdapClient
    {
        Task<LdapUser> Authorize(string username, string pwd);
    }
}