using Castle.DynamicProxy;
using Nito.AsyncEx;
using System;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Domain0.Api.Client
{
    public class Domain0AuthenticationContext : IDomain0AuthenticationContext
    {
        private const int ReserveTimeToUpdateToken = 3;

        public Domain0AuthenticationContext()
        {
            httpClient = new HttpClient();
            domain0Client = new Domain0Client(null, httpClient);
            clientProxyWithTokenRefresh = new ProxyGenerator()
                .CreateInterfaceProxyWithTargetInterface<IDomain0Client>(
                     domain0Client,
                     new RefreshTokenInterceptor(this));

            loginInfoStorage = new LoginInfoStorage();

            handler = new JwtSecurityTokenHandler();

            RestoreLoginInfo();
        }

        public async Task<UserProfile> LoginByPhone(long phone, string password)
        {
            try
            {
                var li = await domain0Client.LoginAsync(new SmsLoginRequest(password, phone));
                
                LoginInfo = li;
                Trace.TraceInformation($"Login: { li.Profile.Id }");

                return li.Profile;
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Login by: { phone } error: { ex }");
                throw new Domain0AuthenticationContextException("Login by phone error", ex);
            }
        }

        public async Task<UserProfile> LoginByEmail(string email, string password)
        {
            try
            { 
                var li = await domain0Client.LoginByEmailAsync(new EmailLoginRequest(email, password));

                LoginInfo = li;
                Trace.TraceInformation($"Login: { li.Profile.Id }");

                return li.Profile;
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Login by: { email } error: { ex }");
                throw new Domain0AuthenticationContextException("Login by email error", ex);
            }
        }

        public void Logout()
        {
            Trace.TraceInformation($"Logout: { LoginInfo.Profile.Id }");
            LoginInfo = null;
        }

        public IDomain0Client Client { get { return clientProxyWithTokenRefresh; } }

        private bool shouldRemember;
        public bool ShouldRemember
        {
            get
            {
                try
                {
                    tokenChangeLock.EnterReadLock();
                    return shouldRemember;
                }
                finally
                {
                    tokenChangeLock.ExitReadLock();
                }
            }
            set
            {
                try
                {
                    tokenChangeLock.EnterWriteLock();
                    shouldRemember = value;
                    loginInfoStorage.Delete();
                }
                finally
                {
                    tokenChangeLock.ExitWriteLock();
                }
            }
        }

        public string HostUrl
        {
            get
            {
                using (requestSetupLock.ReaderLock())
                {
                    return domain0Client.BaseUrl;
                }
            }
            set
            {
                using (requestSetupLock.WriterLock())
                {
                    domain0Client.BaseUrl = value;
                    AdjustConnectionsLimit();
                }
            }
        }

        private DateTime? accessTokenValidTo;
        private DateTime? refreshTokenValidTo;
        public bool IsLoggedIn
        {
            get
            {
                try
                {
                    tokenChangeLock.EnterReadLock();

                    if (loginInfo == null)
                        return false;

                    if (accessTokenValidTo > DateTime.UtcNow.AddMinutes(ReserveTimeToUpdateToken))
                        return true;

                    if (refreshTokenValidTo > DateTime.UtcNow.AddMinutes(ReserveTimeToUpdateToken))
                        return true;

                    return false;

                }
                finally
                {
                    tokenChangeLock.ExitReadLock();
                }
            }
        }

        private AccessTokenResponse GetSuitableToUpdateLoginInfo()
        {
            try
            {
                tokenChangeLock.EnterReadLock();

                if (loginInfo?.RefreshToken == null)
                    return null;
                    
                // no need refresh                
                if (accessTokenValidTo >= DateTime.UtcNow.AddMinutes(ReserveTimeToUpdateToken))
                    return null;
                    
                // can't refresh
                if (refreshTokenValidTo?.AddMinutes(ReserveTimeToUpdateToken) <= DateTime.UtcNow)
                    return null;

                return loginInfo;
            }
            finally
            {
                tokenChangeLock.ExitReadLock();
            }
        }

        private AccessTokenResponse loginInfo;
        private AccessTokenResponse LoginInfo
        {
            get
            {
                try
                {
                    tokenChangeLock.EnterReadLock();
                    return loginInfo;
                }
                finally
                {
                    tokenChangeLock.ExitReadLock();
                } 
            }
            set
            {
                try
                {
                    tokenChangeLock.EnterWriteLock();

                    loginInfo = value;
                    if (loginInfo != null)
                    {
                        ReadExpireDates();
                        SetAuthorizationHeader();
                        if (shouldRemember)
                            loginInfoStorage.Save(loginInfo);
                    }
                    else
                    {
                        ReadExpireDates();
                        SetAuthorizationHeader();
                        loginInfoStorage.Delete();
                    }
                }
                finally
                {
                    tokenChangeLock.ExitWriteLock();
                }
            }
        }

        private void RestoreLoginInfo()
        {
            try
            {
                tokenChangeLock.EnterWriteLock();

                loginInfo = loginInfoStorage.Load();
                ReadExpireDates();
                SetAuthorizationHeader();
            }
            finally
            {
                tokenChangeLock.ExitWriteLock();
            }
        }

        private void AdjustConnectionsLimit()
        {
            var delayServicePoint = ServicePointManager.FindServicePoint(
                new Uri(domain0Client.BaseUrl));
            delayServicePoint.ConnectionLimit = 15;
        }

        private void SetAuthorizationHeader()
        {
            using (requestSetupLock.WriterLock())
            { 
                if (string.IsNullOrWhiteSpace(loginInfo?.AccessToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization = null;
                }
                else
                {
                    httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", loginInfo.AccessToken);
                }
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

        private readonly Domain0Client domain0Client;
        private readonly IDomain0Client clientProxyWithTokenRefresh;
        private readonly JwtSecurityTokenHandler handler;
        private readonly HttpClient httpClient;
        private readonly LoginInfoStorage loginInfoStorage;
        private readonly ReaderWriterLockSlim tokenChangeLock = new ReaderWriterLockSlim();
        private readonly AsyncReaderWriterLock requestSetupLock = new AsyncReaderWriterLock();

        private class RefreshTokenInterceptor : AsyncInterceptorBase
        {
            private readonly Domain0AuthenticationContext context;

            public RefreshTokenInterceptor(Domain0AuthenticationContext domain0AuthenticationContext)
            {
                context = domain0AuthenticationContext;
            }

            protected override async Task InterceptAsync(IInvocation invocation, Func<IInvocation, Task> proceed)
            {
                var loginInfo = context.GetSuitableToUpdateLoginInfo();
                if (loginInfo != null)
                {
                    Trace.TraceInformation($"Refreshing token for { loginInfo.Profile.Id } ...");
                    try
                    {
                        context.LoginInfo = await context.domain0Client.RefreshAsync(loginInfo.RefreshToken);
                    }
                    catch (Exception e)
                    {
                        throw new Domain0AuthenticationContextException("Refresh token error", e);
                    }
                }

                using (await context.requestSetupLock.ReaderLockAsync())
                {
                    await proceed(invocation).ConfigureAwait(false);
                }
            }

            protected override async Task<TResult> InterceptAsync<TResult>(IInvocation invocation, Func<IInvocation, Task<TResult>> proceed)
            {
                var loginInfo = context.GetSuitableToUpdateLoginInfo();
                if (loginInfo != null)
                {
                    Trace.TraceInformation($"Refreshing token for { loginInfo.Profile.Id } ...");
                    try
                    {
                        context.LoginInfo = await context.domain0Client.RefreshAsync(loginInfo.RefreshToken);
                    }
                    catch (Exception e)
                    {
                        throw new Domain0AuthenticationContextException("Refresh token error", e);
                    }
                }

                using (await context.requestSetupLock.ReaderLockAsync())
                {
                    return await proceed(invocation).ConfigureAwait(false);
                }
            }
        }
    }
}
