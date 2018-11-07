using Moq;
using System.Threading.Tasks;
using Xunit;

namespace Domain0.Api.Client.Test
{
    public class Domain0ContextTest
    {
        [Fact]
        public async void LoginBySms()
        {
            var testContext = TestContext.MockUp();

            var domain0Context = new Domain0AuthenticationContext(
                domain0ClientEnvironment: testContext.ClientScopeMock.Object,
                externalStorage: testContext.LoginInfoStorageMock.Object);

            Assert.False(domain0Context.IsLoggedIn);

            var profile = await domain0Context.LoginByPhone(123, "2");
            Assert.NotNull(profile);

            testContext.ClientScopeMock
                .VerifySet(callTo => callTo.Token = 
                    It.Is<string>(s => !string.IsNullOrWhiteSpace(s)),
                    Times.Once);

            Assert.True(domain0Context.IsLoggedIn);
        }

        [Fact]
        public async void LoginByEmail()
        {
            var testContext = TestContext.MockUp();

            var domain0Context = new Domain0AuthenticationContext(
                domain0ClientEnvironment: testContext.ClientScopeMock.Object,
                externalStorage: testContext.LoginInfoStorageMock.Object);

            Assert.False(domain0Context.IsLoggedIn);

            var profile = await domain0Context.LoginByEmail("email", "2");
            Assert.NotNull(profile);

            testContext.ClientScopeMock
                .VerifySet(callTo => callTo.Token = 
                    It.Is<string>(s => !string.IsNullOrWhiteSpace(s)),
                    Times.Once);

            Assert.True(domain0Context.IsLoggedIn);
        }

        [Fact]
        public async void Logout()
        {
            var testContext = TestContext.MockUp();

            var domain0Context = new Domain0AuthenticationContext(
                domain0ClientEnvironment: testContext.ClientScopeMock.Object,
                externalStorage: testContext.LoginInfoStorageMock.Object);

            Assert.False(domain0Context.IsLoggedIn);

            var profile = await domain0Context.LoginByEmail("email", "2");
            Assert.NotNull(profile);

            testContext.ClientScopeMock
                .VerifySet(callTo => callTo.Token = 
                    It.Is<string>(s => !string.IsNullOrWhiteSpace(s)),
                    Times.Once);

            Assert.True(domain0Context.IsLoggedIn);

            domain0Context.Logout();

            Assert.False(domain0Context.IsLoggedIn);

            testContext.ClientScopeMock
                .VerifySet(callTo => callTo.Token = 
                    It.Is<string>(s => string.IsNullOrWhiteSpace(s)),
                    Times.Exactly(2));

            testContext.LoginInfoStorageMock
                .Verify(callTo => callTo.Delete(), Times.Once);

        }


        [Fact]
        public async void AutoRefresh()
        {
            var testContext = TestContext.MockUp(accessValidTime: 0.1);

            var domain0Context = new Domain0AuthenticationContext(
                domain0ClientEnvironment: testContext.ClientScopeMock.Object,
                reserveTimeToUpdateToken: 0,
                externalStorage: testContext.LoginInfoStorageMock.Object);

            Assert.False(domain0Context.IsLoggedIn);

            var profile = await domain0Context.LoginByPhone(123, "2");
            Assert.NotNull(profile);

            testContext.ClientScopeMock
                .VerifySet(callTo => callTo.Token = 
                    It.Is<string>(s => !string.IsNullOrWhiteSpace(s)),
                    Times.Once);

            Assert.True(domain0Context.IsLoggedIn);

            await Task.Delay(100);

            var phone = await domain0Context.Client.PhoneByUserIdAsync(1);
            Assert.Equal(0, phone);

            testContext.ClientScopeMock
                .VerifySet(callTo => callTo.Token = 
                    It.Is<string>(s => !string.IsNullOrWhiteSpace(s)),
                    Times.Exactly(2));

            Assert.True(domain0Context.IsLoggedIn);
        }

        [Fact]
        public async void ShouldRemember()
        {
            AccessTokenResponse savedLoginInfo = null;

            var testContext = TestContext.MockUp(accessValidTime: 0.1);
            testContext.LoginInfoStorageMock
                .Setup(callTo => callTo
                    .Save(It.IsAny<AccessTokenResponse>()))
                .Callback<AccessTokenResponse>(x =>
                {
                    savedLoginInfo = x;
                });
            testContext.LoginInfoStorageMock
                .Setup(callTo => callTo.Load())
                .Returns(() => savedLoginInfo);

            var domain0Context = new Domain0AuthenticationContext(
                domain0ClientEnvironment: testContext.ClientScopeMock.Object,
                externalStorage: testContext.LoginInfoStorageMock.Object);

            Assert.False(domain0Context.IsLoggedIn);

            domain0Context.ShouldRemember = true;

            var profile = await domain0Context.LoginByPhone(123, "2");
            Assert.NotNull(profile);

            Assert.True(domain0Context.IsLoggedIn);

            var newDomain0Context = new Domain0AuthenticationContext(
                domain0ClientEnvironment: testContext.ClientScopeMock.Object,
                externalStorage: testContext.LoginInfoStorageMock.Object);

            Assert.True(newDomain0Context.IsLoggedIn);
        }

        [Fact]
        public void UseCustomHost()
        {
            var testContext = TestContext.MockUp();

            var domain0Context = new Domain0AuthenticationContext(
                domain0ClientEnvironment: testContext.ClientScopeMock.Object,
                externalStorage: testContext.LoginInfoStorageMock.Object);

            domain0Context.HostUrl = "https://custom.com";

            testContext.ClientScopeMock
                .VerifySet(x => x.HostUrl = "https://custom.com");
        }
    }
}
