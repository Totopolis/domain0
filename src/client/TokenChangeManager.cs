using Castle.DynamicProxy;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace Domain0.Api.Client
{
    internal class TokenChangeManager : IDisposable
    {
        public TokenChangeManager(Domain0AuthenticationContext authContext, ILoginInfoStorage storage)
        {
            domain0AuthenticationContext = authContext;
            loginInfoStorage = storage;
            if (domain0AuthenticationContext.AutoRefreshEnabled)
                refreshTokenTimer = new RefreshTokenTimer(authContext);
        }

        internal TClient AttachClientEnvironment<TClient>(IClientScope<TClient> scope)
            where TClient : class
        {
            using (tokenChangeLock.WriterLock())
            {
                attachedClients.Add(
                    new WeakReference<ITokenStore>(scope));


                scope.Token = loginInfo?.AccessToken;

                var proxyClient = new ProxyGenerator()
                    .CreateInterfaceProxyWithTargetInterface(
                         scope.Client,
                         new RefreshTokenInterceptor(domain0AuthenticationContext, scope));

                return proxyClient;
            }
        }

        internal bool DetachClientEnvironment<TClient>(IClientScope<TClient> clientEnvironment)
            where TClient : class
        {
            return RemoveTokenStore(clientEnvironment);
        }

        internal void AttachTokenStore(ITokenStore tokenStore)
        {
            using (tokenChangeLock.WriterLock())
            {
                attachedClients.Add(
                    new WeakReference<ITokenStore>(tokenStore));

                tokenStore.Token = loginInfo?.AccessToken;
            }
        }

        internal bool DetachTokenStore(ITokenStore tokenStore)
        {
            return RemoveTokenStore(tokenStore);
        }

        private bool shouldRemember;
        public bool ShouldRemember
        {
            get
            {
                using (tokenChangeLock.ReaderLock())
                {
                    return shouldRemember;
                }
            }
            set
            {
                using (tokenChangeLock.WriterLock())
                {
                    shouldRemember = value;
                    loginInfoStorage.Delete();
                }
            }
        }

        private DateTime? accessTokenValidTo;
        private DateTime? refreshTokenValidTo;
        public bool IsLoggedIn
        {
            get
            {
                using (tokenChangeLock.ReaderLock())
                {

                    if (loginInfo == null)
                        return false;

                    if (accessTokenValidTo > DateTime.UtcNow.AddSeconds(domain0AuthenticationContext.ReserveTimeToUpdate))
                        return true;

                    if (refreshTokenValidTo > DateTime.UtcNow.AddSeconds(domain0AuthenticationContext.ReserveTimeToUpdate))
                        return true;

                    return false;
                }
            }
        }

        internal AccessTokenResponse GetSuitableToUpdateLoginInfo()
        {
            using (tokenChangeLock.ReaderLock())
            {
                if (loginInfo?.RefreshToken == null)
                    return null;

                // no need refresh                
                if (accessTokenValidTo >= DateTime.UtcNow.AddSeconds(domain0AuthenticationContext.ReserveTimeToUpdate))
                    return null;

                // can't refresh
                if (refreshTokenValidTo?.AddSeconds(domain0AuthenticationContext.ReserveTimeToUpdate) <= DateTime.UtcNow)
                    return null;

                return loginInfo;
            }
        }

        private AccessTokenResponse loginInfo;
        internal AccessTokenResponse LoginInfo
        {
            get
            {
                using (tokenChangeLock.ReaderLock())
                {
                    return loginInfo;
                }
            }
            set
            {
                using (tokenChangeLock.WriterLock())
                {
                    loginInfo = value;
                    if (loginInfo != null)
                    {
                        ReadExpireDates();
                        SetToken();
                        if (shouldRemember)
                            loginInfoStorage.Save(loginInfo);
                    }
                    else
                    {
                        ReadExpireDates();
                        SetToken();
                        loginInfoStorage.Delete();
                    }

                    if (domain0AuthenticationContext.AutoRefreshEnabled)
                    {
                        refreshTokenTimer.NextRefreshTime =
                            accessTokenValidTo?.AddSeconds(-domain0AuthenticationContext.ReserveTimeToUpdate);
                    }
                }
            }
        }

        internal void RestoreLoginInfo()
        {
            using (tokenChangeLock.WriterLock())
            {
                loginInfo = loginInfoStorage.Load();
                ReadExpireDates();
                SetToken();
            }
        }

        private void SetToken()
        {
            var token = loginInfo?.AccessToken;

            domain0AuthenticationContext.Domain0Scope.Token = token;

            var aliveClients = GetAliveAttachedClients();

            foreach (var client in aliveClients)
            {
                try
                {
                    client.Token = token;
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Could not setToken for { client.GetType() } error: { ex }");
                }
            }
        }

        private ITokenStore[] GetAliveAttachedClients()
        {
            if (!attachedClients.Any())
                return new ITokenStore[0];

            var aliveClients = new List<ITokenStore>();
            var toRemoveClients = new List<WeakReference<ITokenStore>>();

            foreach (var c in attachedClients)
            {
                if (c.TryGetTarget(out ITokenStore client))
                {
                    aliveClients.Add(client);
                }
                else
                {
                    toRemoveClients.Add(c);
                }
            }

            foreach (var r in toRemoveClients)
            {
                attachedClients.Remove(r);
            }

            return aliveClients.ToArray();
        }

        private bool RemoveTokenStore(ITokenStore tokenStore)
        {
            using (tokenChangeLock.WriterLock())
            {
                var toDelete = attachedClients.FirstOrDefault(wc =>
                {
                    if (wc.TryGetTarget(out ITokenStore client))
                    {
                        if (client == tokenStore)
                            return true;
                    }

                    return false;
                });

                if (toDelete == null)
                    return false;

                return attachedClients.Remove(toDelete);
            }
        }

        private void ReadExpireDates()
        {
            if (loginInfo != null)
            {
                var accessTokenInfo = handler.ReadJwtToken(loginInfo.AccessToken);
                var refreshTokenInfo = handler.ReadJwtToken(loginInfo.RefreshToken);

                accessTokenValidTo = accessTokenInfo.ValidTo;
                refreshTokenValidTo = refreshTokenInfo.ValidTo;
            }
            else
            {
                accessTokenValidTo = null;
                refreshTokenValidTo = null;
            }
        }

        public void Dispose()
        {
            refreshTokenTimer?.Dispose();
        }

        private readonly List<WeakReference<ITokenStore>> attachedClients = new List<WeakReference<ITokenStore>>();
        private readonly Domain0AuthenticationContext domain0AuthenticationContext;
        private readonly JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
        private readonly ILoginInfoStorage loginInfoStorage;
        private readonly RefreshTokenTimer refreshTokenTimer;
        private readonly AsyncReaderWriterLock tokenChangeLock = new AsyncReaderWriterLock();
    }
}