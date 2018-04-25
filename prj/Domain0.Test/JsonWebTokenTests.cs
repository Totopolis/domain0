using Autofac;
using Domain0.Service;
using System;
using Xunit;
using Sdl.Domain0.Shared;
using Newtonsoft.Json;

namespace Domain0.Test
{
    public class JsonWebTokenTests
    {
        [Fact]
        public void AccessToken_Basic()
        {
            var container = TestModuleTests.GetContainer();
            var tokenGenerator = container.Resolve<ITokenGenerator>();
            var userId = 165;
            var secret = Convert.FromBase64String("kiHLSfGebYvXGTDx0vWb53JhyUpnw6HvgRwOJ6h/hUs=");
            var permissions = new [] {"test1","test2"};

            var issueTime = DateTime.UtcNow;
            var accessToken = new JsonWebToken().Encode(new
            {
                typ = "access_token",
                sub = $"{userId}",
                permissions = JsonConvert.SerializeObject(permissions),
                exp = new DateTimeOffset(issueTime.AddMinutes(15)).ToUnixTimeSeconds(),
                iat = new DateTimeOffset(issueTime).ToUnixTimeSeconds(),
                iss = "sdl",
                aud = "*",
            }, secret, JwtHashAlgorithm.HS256);

            var accessToken2 = tokenGenerator.GenerateAccessToken(userId, issueTime, permissions);
            Assert.Equal(accessToken, accessToken2);

            var principal = tokenGenerator.Parse(accessToken);
        }

        [Fact]
        public void RefreshToken_Basic()
        {
            var container = TestModuleTests.GetContainer();
            var tokenGenerator = container.Resolve<ITokenGenerator>();

            var issueTime = DateTime.UtcNow;
            var userId = 165;
            var tid = 111;
            var secret = Convert.FromBase64String("kiHLSfGebYvXGTDx0vWb53JhyUpnw6HvgRwOJ6h/hUs=");
            var refreshToken = new JsonWebToken().Encode(new
            {
                typ = "refresh_token",
                sub = $"{userId}",
                exp = new DateTimeOffset(issueTime.AddMinutes(15)).ToUnixTimeSeconds(),
                iat = new DateTimeOffset(issueTime).ToUnixTimeSeconds(),
                iss = "sdl",
                aud = "*",
            }, secret, JwtHashAlgorithm.HS256);

            var refreshToken2 = tokenGenerator.GenerateRefreshToken(tid, issueTime, userId);
            Assert.Equal(refreshToken, refreshToken2);
       }
    }
}
