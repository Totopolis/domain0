using Moq;
using System.Threading.Tasks;
using Xunit;

namespace Domain0.Api.Client.Test
{
    public class Domain0ContextAttachedTokenStoreTest
    {
        public interface ITestStore : ITokenStore
        {
        }

        [Fact]
        public async void AttachedTokenStoreLogin()
        {
            var testContext = TestContext.MockUp();

            var domain0Context = new Domain0AuthenticationContext(
                domain0ClientEnvironment: testContext.ClientScopeMock.Object,
                externalStorage: testContext.LoginInfoStorageMock.Object);

            var attachedTokenStoreMock = new Mock<ITestStore>();


            domain0Context.AttachTokenStore(attachedTokenStoreMock.Object);
            
            var profile = await domain0Context.LoginByPhone(123, "2");
            Assert.NotNull(profile);

            attachedTokenStoreMock
                .VerifySet(env => 
                    env.Token = It.Is<string>(s => string.IsNullOrWhiteSpace(s)), 
                    Times.Once);
        }

        [Fact]
        public async void TokenStoreShouldLoginWhenAttached()
        {
            var testContext = TestContext.MockUp();

            var domain0Context = new Domain0AuthenticationContext(
                domain0ClientEnvironment: testContext.ClientScopeMock.Object,
                externalStorage: testContext.LoginInfoStorageMock.Object);

            var attachedTokenStoreMock = new Mock<ITestStore>();


            var profile = await domain0Context.LoginByPhone(123, "2");
            Assert.NotNull(profile);

            domain0Context.AttachTokenStore(attachedTokenStoreMock.Object);

            attachedTokenStoreMock
                .VerifySet(env => env.Token = It.IsAny<string>(), Times.Once);
        }

        [Fact]
        public async void AttachedTokenStoreLogout()
        {
            var testContext = TestContext.MockUp(accessValidTime: 0.1);

            var domain0Context = new Domain0AuthenticationContext(
                domain0ClientEnvironment: testContext.ClientScopeMock.Object,
                externalStorage: testContext.LoginInfoStorageMock.Object);

            var attachedTokenStoreMock = new Mock<ITestStore>();

            var profile = await domain0Context.LoginByPhone(123, "2");
            Assert.NotNull(profile);

            domain0Context.AttachTokenStore(attachedTokenStoreMock.Object);
            
            domain0Context.Logout();

            attachedTokenStoreMock
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

            var attachedTokenStoreMock = new Mock<ITestStore>();

            domain0Context.AttachTokenStore(attachedTokenStoreMock.Object);
            
            var profile = await domain0Context.LoginByPhone(123, "2");
            Assert.NotNull(profile);

            attachedTokenStoreMock
                .VerifySet(env =>
                    env.Token = It.Is<string>(s => string.IsNullOrWhiteSpace(s)),
                    Times.Once);

            domain0Context.DetachTokenStore(attachedTokenStoreMock.Object);

            profile = await domain0Context.LoginByPhone(123, "2");
            Assert.NotNull(profile);

            attachedTokenStoreMock
                .VerifySet(env =>
                    env.Token = It.Is<string>(s => string.IsNullOrWhiteSpace(s)),
                    Times.Once);
        }
    }
}
