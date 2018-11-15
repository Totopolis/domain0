using Microsoft.IdentityModel.Tokens;
using Moq;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Nito.AsyncEx;
using Newtonsoft.Json;

namespace Domain0.Api.Client.Test
{
    internal class TestContext
    {
        internal Mock<IDomain0Client> ClientMock;
        internal Mock<IDomain0ClientScope> ClientScopeMock;
        internal Mock<ILoginInfoStorage> LoginInfoStorageMock;

        public static TestContext MockUp(
        double accessValidTime = 300,
        double refreshValidTime = 600)
        {
            var clientMock = new Mock<IDomain0Client>();
            clientMock
                .Setup(callFor => callFor.LoginAsync(It.IsAny<SmsLoginRequest>()))
                .ReturnsAsync(MakeTokenResponse(DefaultSmsUser, accessValidTime, refreshValidTime));

            clientMock
                .Setup(callFor => callFor.LoginByEmailAsync(It.IsAny<EmailLoginRequest>()))
                .ReturnsAsync(MakeTokenResponse(DefaultEmailUser, accessValidTime, refreshValidTime));


            clientMock
                .Setup(callFor => callFor.RefreshAsync(It.IsAny<string>()))
                .ReturnsAsync(MakeTokenResponse(DefaultSmsUser, accessValidTime, refreshValidTime));

            var clientScopeMock = new Mock<IDomain0ClientScope>();
            clientScopeMock
                .Setup(callFor => callFor.Client)
                .Returns(clientMock.Object);

            var lockScope = new AsyncReaderWriterLock();
            clientScopeMock
                .Setup(callFor => callFor.RequestSetupLock)
                .Returns(() => lockScope);

            var loginInfoStorageMock = new Mock<ILoginInfoStorage>();


            return new TestContext
            {
                ClientMock = clientMock,
                ClientScopeMock = clientScopeMock,
                LoginInfoStorageMock = loginInfoStorageMock
            };
        }

        private static AccessTokenResponse MakeTokenResponse(
            UserProfile profile,
            double accessValidTime,
            double refreshValidTime)
        {
            var defPermissions = new Claim[]
            {
                new Claim("permissions",
                    JsonConvert.SerializeObject(new[] {"claimsA", "claimsB"}))
            };

            var access = Handler.CreateToken(
                new SecurityTokenDescriptor
                {
                    Expires = DateTime.UtcNow.AddSeconds(accessValidTime),
                    Subject = new ClaimsIdentity(defPermissions)
                });

            var refresh = Handler.CreateToken(
                new SecurityTokenDescriptor
                {
                    Expires = DateTime.UtcNow.AddSeconds(refreshValidTime)
                });

            return new AccessTokenResponse(
                Handler.WriteToken(access),
                profile,
                Handler.WriteToken(refresh));
        }

        private static readonly UserProfile DefaultSmsUser = new UserProfile("description", null, 1, false, "name", "123");
        private static readonly UserProfile DefaultEmailUser = new UserProfile("description", "email", 1, false, "name", null);
        private static readonly JwtSecurityTokenHandler Handler = new JwtSecurityTokenHandler();
    }
}
