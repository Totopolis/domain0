using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace Domain0.Api.Client
{
    public class AuthenticationContext : IAuthenticationContext, IDisposable
    {
        public AuthenticationContext(
            IDomain0ClientScope domain0ClientEnvironment = null,
            ILoginInfoStorage externalStorage = null,
            uint reserveTimeToUpdateToken = 120,
            bool enableAutoRefreshTimer = false)
        {
            ReserveTimeToUpdate = reserveTimeToUpdateToken;
            AutoRefreshEnabled = enableAutoRefreshTimer;

            Domain0Scope = domain0ClientEnvironment ?? new Domain0ClientScope();

            var loginInfoStorage = externalStorage ?? new LoginInfoStorage();

            tokenChangeManager = new TokenChangeManager(this, loginInfoStorage);

            tokenChangeManager.RestoreLoginInfo();

            Client = new ProxyGenerator()
                .CreateInterfaceProxyWithTargetInterface(
                     Domain0Scope.Client,
                     new RefreshTokenInterceptor(this, Domain0Scope));
        }

        public TClient AttachClientEnvironment<TClient>(IClientScope<TClient> clientEnvironment)
            where TClient : class
        {
            return tokenChangeManager.AttachClientEnvironment(clientEnvironment);
        }

        public bool DetachClientEnvironment<TClient>(IClientScope<TClient> clientEnvironment)
            where TClient : class
        {
            return tokenChangeManager.DetachClientEnvironment(clientEnvironment);
        }

        public void AttachTokenStore(ITokenStore tokenStore)
        {
            tokenChangeManager.AttachTokenStore(tokenStore);
        }

        public bool DetachTokenStore(ITokenStore tokenStore)
        {
            return tokenChangeManager.DetachTokenStore(tokenStore);
        }

        public async Task<UserProfile> LoginByPhone(long phone, string password)
        {
            try
            {
                var li = await Domain0Scope.Client.LoginAsync(new SmsLoginRequest(password, phone))
                    .ConfigureAwait(false);
                
                tokenChangeManager.LoginInfo = li;
                Trace.TraceInformation($"Login: { li?.Profile?.Id } | { phone }");

                return li?.Profile;
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Login by: { phone } error: { ex }");
                throw new AuthenticationContextException("Login by phone error", ex);
            }
        }

        public async Task<UserProfile> LoginByEmail(string email, string password)
        {
            try
            { 
                var li = await Domain0Scope.Client.LoginByEmailAsync(new EmailLoginRequest(email, password))
                    .ConfigureAwait(false);

                tokenChangeManager.LoginInfo = li;
                Trace.TraceInformation($"Login: { li?.Profile?.Id } | { email }");

                return li?.Profile;
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Login by: { email } error: { ex }");
                throw new AuthenticationContextException("Login by email error", ex);
            }
        }

        public void Logout()
        {
            Trace.TraceInformation($"Logout: { tokenChangeManager.LoginInfo?.Profile?.Id }");
            tokenChangeManager.LoginInfo = null;
        }

        public bool HavePermission(string permissionToCheck)
        {
            return tokenChangeManager.HavePermission(permissionToCheck);
        }

        public bool HavePermissions(IEnumerable<string> permissionsToCheck)
        {
            return tokenChangeManager.HavePermissions(permissionsToCheck);
        }


        public IDomain0Client Client { get; }

        public string HostUrl
        {
            get => Domain0Scope.HostUrl;
            set => Domain0Scope.HostUrl = value;
        }

        public bool ShouldRemember
        {
            get => tokenChangeManager.ShouldRemember;
            set => tokenChangeManager.ShouldRemember = value;
        }

        public string Token
        {
            get => tokenChangeManager.LoginInfo?.AccessToken;
        }

        public event Action<AccessTokenResponse> AccessTokenChanged
        {
            add => tokenChangeManager.AccessTokenChanged += value;
            remove => tokenChangeManager.AccessTokenChanged -= value;
        }

        public bool IsLoggedIn
        {
            get => tokenChangeManager.IsLoggedIn;
        }

        internal async Task Refresh()
        {
            var loginInfo = tokenChangeManager.GetSuitableToUpdateLoginInfo();
            if (loginInfo != null)
            {
                try
                {
                    var request = new RefreshTokenRequest(loginInfo.RefreshToken);
                    tokenChangeManager.LoginInfo = await Domain0Scope.Client.RefreshTokenAsync(request)
                        .ConfigureAwait(false);
                }
                catch (Domain0ClientException e) when (e.StatusCode != 200)
                {
                    tokenChangeManager.LoginInfo = null;
                    throw new AuthenticationContextException("Refresh token failed", e);
                }
                catch (Exception e)
                {
                    throw new AuthenticationContextException("Refresh token error", e);
                }
            }
        }

        public void Dispose()
        {
            tokenChangeManager?.Dispose();
        }

        internal readonly bool AutoRefreshEnabled;
        internal readonly IDomain0ClientScope Domain0Scope;
        internal readonly uint ReserveTimeToUpdate;
        private readonly TokenChangeManager tokenChangeManager;
    }
}
