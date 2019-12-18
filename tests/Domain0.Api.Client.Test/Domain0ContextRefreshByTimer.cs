using Moq;
using System.Threading.Tasks;
using Xunit;
using static Domain0.Api.Client.Test.Domain0ContextAttachedClientTest;

namespace Domain0.Api.Client.Test
{
    public class Domain0ContextRefreshByTimer
    {
        [Fact]
        public async void Refresh()
        {
            var testContext = TestContext.MockUp(accessValidTime: 0.1);

            var domain0Context = new AuthenticationContext(
                domain0ClientEnvironment: testContext.ClientScopeMock.Object,
                externalStorage: testContext.LoginInfoStorageMock.Object,
                enableAutoRefreshTimer: true,
                reserveTimeToUpdateToken: 0);

            Assert.False(domain0Context.IsLoggedIn);

            var profile = await domain0Context.LoginByPhone(123, "2");
            Assert.NotNull(profile);

            await Task.Delay(1000);

            testContext.ClientScopeMock
                .VerifySet(callTo => callTo.Token =
                    It.Is<string>(s => !string.IsNullOrWhiteSpace(s)),
                    Times.AtLeast(5));

            Assert.True(domain0Context.IsLoggedIn);

            domain0Context.Dispose();
        }

        [Fact]
        public async void AttachedClientRefresh()
        {
            var testContext = TestContext.MockUp(accessValidTime: 0.1);

            var domain0Context = new AuthenticationContext(
                domain0ClientEnvironment: testContext.ClientScopeMock.Object,
                externalStorage: testContext.LoginInfoStorageMock.Object,
                enableAutoRefreshTimer: true,
                reserveTimeToUpdateToken: 0);

            var attachedClientMock = new Mock<ITestClient>();
            var attachedClient = attachedClientMock.Object;

            var attachedEnvironmentMock = new Mock<IClientScope<ITestClient>>();
            attachedEnvironmentMock
                .Setup(callTo => callTo.Client)
                .Returns(() => attachedClient);

            var proxy = domain0Context.AttachClientEnvironment(attachedEnvironmentMock.Object);
            Assert.NotNull(proxy);

            var profile = await domain0Context.LoginByPhone(123, "2");
            Assert.NotNull(profile);
            
            await domain0Context.LoginByPhone(123, "2");
            await domain0Context.LoginByPhone(123, "2");
            await domain0Context.LoginByPhone(123, "2");
            await domain0Context.LoginByPhone(123, "2");

            await Task.Delay(1000);


            attachedEnvironmentMock
                .VerifySet(env =>
                    env.Token = It.Is<string>(s => !string.IsNullOrWhiteSpace(s)),
                    Times.AtLeast(5));
        }
    }
}
