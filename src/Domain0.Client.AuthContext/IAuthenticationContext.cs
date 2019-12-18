using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain0.Api.Client
{
    public interface IAuthenticationContext
    {
        IDomain0Client Client { get; }

        string HostUrl { get; set; }

        bool ShouldRemember { get; set; }

        string Token { get; }

        event Action<string> AccessTokenChanged;

        bool IsLoggedIn { get; }

        TClient AttachClientEnvironment<TClient>(IClientScope<TClient> clientEnvironment)
            where TClient : class;

        bool DetachClientEnvironment<TClient>(IClientScope<TClient> clientEnvironment)
            where TClient : class;

        void AttachTokenStore(ITokenStore tokenStore);

        bool DetachTokenStore(ITokenStore tokenStore);

        Task<UserProfile> LoginByPhone(long phone, string password);

        Task<UserProfile> LoginByEmail(string email, string password);

        void Logout();

        bool HavePermission(string permission);

        bool HavePermissions(IEnumerable<string> permissions);
    }
}