using Moq;
using System.Threading.Tasks;
using Xunit;

namespace Domain0.Api.Client.Test
{
    public class Domain0ContextAttachedClientTest
    {
        public interface ITestClient
        {
            Task MethodA();

            void MethodB();
        }

        [Fact]
        public async void AttachedClientLogin()
        {
            var testContext = TestContext.MockUp();

            var domain0Context = new Domain0AuthenticationContext(
                domain0ClientEnvironment: testContext.ClientScopeMock.Object,
                externalStorage: testContext.LoginInfoStorageMock.Object);

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
            
            attachedEnvironmentMock
                .VerifySet(env => 
                    env.Token = It.Is<string>(s => string.IsNullOrWhiteSpace(s)), 
                    Times.Once);
        }

        [Fact]
        public async void ClientShouldLoginWhenAttached()
        {
            var testContext = TestContext.MockUp();

            var domain0Context = new Domain0AuthenticationContext(
                domain0ClientEnvironment: testContext.ClientScopeMock.Object,
                externalStorage: testContext.LoginInfoStorageMock.Object);

            var attachedClientMock = new Mock<ITestClient>();
            var attachedClient = attachedClientMock.Object;

            var attachedEnvironmentMock = new Mock<IClientScope<ITestClient>>();
            attachedEnvironmentMock
                .Setup(callTo => callTo.Client)
                .Returns(() => attachedClient);

            var profile = await domain0Context.LoginByPhone(123, "2");
            Assert.NotNull(profile);

            var proxy = domain0Context.AttachClientEnvironment(attachedEnvironmentMock.Object);
            
            Assert.NotNull(proxy);
            
            attachedEnvironmentMock
                .VerifySet(env => env.Token = It.IsAny<string>(), Times.Once);
        }

        [Fact]
        public async void AttachedClientRefresh()
        {
            var testContext = TestContext.MockUp(accessValidTime: 0.1);

            var domain0Context = new Domain0AuthenticationContext(
                domain0ClientEnvironment: testContext.ClientScopeMock.Object,
                externalStorage: testContext.LoginInfoStorageMock.Object);

            var attachedClientMock = new Mock<ITestClient>();
            var attachedClient = attachedClientMock.Object;

            var attachedEnvironmentMock = new Mock<ClientLockScope<ITestClient>>();
            attachedEnvironmentMock
                .Setup(callTo => callTo.Client)
                .Returns(() => attachedClient);

            var proxy = domain0Context.AttachClientEnvironment(attachedEnvironmentMock.Object);

            var profile = await domain0Context.LoginByPhone(123, "2");
            
            Assert.NotNull(profile);

            proxy.MethodB();
            attachedEnvironmentMock
                .VerifySet(env => 
                    env.Token = It.Is<string>(s => !string.IsNullOrWhiteSpace(s)), 
                    Times.Exactly(2));
            attachedClientMock
                .Verify(c => c.MethodB(), Times.Once);

            await proxy.MethodA();
            attachedEnvironmentMock
                .VerifySet(env =>
                    env.Token = It.Is<string>(s => !string.IsNullOrWhiteSpace(s)),
                    Times.Exactly(3));
            attachedClientMock
                .Verify(c => c.MethodA(), Times.Once);
        }

        [Fact]
        public async void AttachedClientLogout()
        {
            var testContext = TestContext.MockUp(accessValidTime: 0.1);

            var domain0Context = new Domain0AuthenticationContext(
                domain0ClientEnvironment: testContext.ClientScopeMock.Object,
                externalStorage: testContext.LoginInfoStorageMock.Object);

            var attachedClientMock = new Mock<ITestClient>();
            var attachedClient = attachedClientMock.Object;

            var attachedEnvironmentMock = new Mock<IClientScope<ITestClient>>();
            attachedEnvironmentMock
                .Setup(callTo => callTo.Client)
                .Returns(() => attachedClient);


            var profile = await domain0Context.LoginByPhone(123, "2");
            Assert.NotNull(profile);

            var proxy = domain0Context.AttachClientEnvironment(attachedEnvironmentMock.Object);
            Assert.NotNull(proxy);

            domain0Context.Logout();

            attachedEnvironmentMock
                .VerifySet(env => 
                    env.Token = It.Is<string>(x => string.IsNullOrWhiteSpace(x)),
                    Times.Once);
        }

        [Fact]
        public async void ClientDetach()
        {
            var testContext = TestContext.MockUp();

            var domain0Context = new Domain0AuthenticationContext(
                domain0ClientEnvironment: testContext.ClientScopeMock.Object,
                externalStorage: testContext.LoginInfoStorageMock.Object);

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

            attachedEnvironmentMock
                .VerifySet(env =>
                    env.Token = It.Is<string>(s => string.IsNullOrWhiteSpace(s)),
                    Times.Once);

            domain0Context.DetachClientEnvironment(attachedEnvironmentMock.Object);

            profile = await domain0Context.LoginByPhone(123, "2");
            Assert.NotNull(profile);

            attachedEnvironmentMock
                .VerifySet(env =>
                    env.Token = It.Is<string>(s => string.IsNullOrWhiteSpace(s)),
                    Times.Once);
        }
    }
}
