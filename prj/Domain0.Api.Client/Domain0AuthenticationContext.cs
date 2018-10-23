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

        public async Task<bool> LoginByPhone(long phone, string password)
        {
            try
            {
                LoginInfo = await domain0Client.LoginAsync(new SmsLoginRequest(password, phone));
                Trace.TraceInformation($"Login: { LoginInfo.Profile.Id }");

                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Login: { phone }  {ex.ToString()}");
                return false;
            }
        }

        public async Task<bool> LoginByEmail(string email, string password)
        {
            try
            { 
                LoginInfo = await domain0Client.LoginByEmailAsync(new EmailLoginRequest(email, password));
                Trace.TraceInformation($"Login: { LoginInfo.Profile.Id }");

                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Login: { email }  {ex.ToString()}");
                return false;
            }
        }

        public void Logout()
        {
            Trace.TraceInformation($"Logout: { LoginInfo.Profile.Id }");
            LoginInfo = null;
        }

        public IDomain0Client Client { get { return clientProxyWithTokenRefresh; } }

        public bool ShouldRemember { get; set; }

        public string HostUrl
        {
            get
            {
                return domain0Client.BaseUrl;
            }
            set
            {
                domain0Client.BaseUrl = value;
                AdjustConnectionsLimit();
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

                    if (accessTokenValidTo?.AddMinutes(ReserveTimeToUpdateToken) > DateTime.UtcNow)
                        return true;

                    if (refreshTokenValidTo?.AddMinutes(ReserveTimeToUpdateToken) > DateTime.UtcNow)
                        return true;

                    return false;

                }
                finally
                {
                    tokenChangeLock.ExitReadLock();
                }
            }
        }

        private bool NeedAndAbleToRefresh()
        {
            try
            {
                tokenChangeLock.EnterReadLock();

                return
                    // need refresh
                    accessTokenValidTo < DateTime.UtcNow.AddMinutes(ReserveTimeToUpdateToken)
                    // and able to refresh
                    && refreshTokenValidTo?.AddMinutes(ReserveTimeToUpdateToken) > DateTime.UtcNow;
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
            using (defaultRequestHeadersSetupLock.WriterLock())
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
        private readonly AsyncReaderWriterLock defaultRequestHeadersSetupLock = new AsyncReaderWriterLock();

        private class RefreshTokenInterceptor : AsyncInterceptorBase
        {
            private Domain0AuthenticationContext context;

            public RefreshTokenInterceptor(Domain0AuthenticationContext domain0AuthenticationContext)
            {
                context = domain0AuthenticationContext;
            }

            protected override async Task InterceptAsync(IInvocation invocation, Func<IInvocation, Task> proceed)
            {
                if (context.NeedAndAbleToRefresh())
                    context.LoginInfo = await context.domain0Client.RefreshAsync(context.LoginInfo.RefreshToken);

                using (await context.defaultRequestHeadersSetupLock.ReaderLockAsync())
                {
                    await proceed(invocation).ConfigureAwait(false);
                }
            }

            protected override async Task<TResult> InterceptAsync<TResult>(IInvocation invocation, Func<IInvocation, Task<TResult>> proceed)
            {
                if (context.NeedAndAbleToRefresh())
                    context.LoginInfo = await context.domain0Client.RefreshAsync(context.LoginInfo.RefreshToken);

                using (await context.defaultRequestHeadersSetupLock.ReaderLockAsync())
                {
                    return await proceed(invocation).ConfigureAwait(false);
                }
            }
        }
    }
}
