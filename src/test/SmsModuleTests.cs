using System.Threading.Tasks;
using Domain0.Nancy;
using Nancy;
using Nancy.Testing;
using Xunit;
using Autofac;
using Domain0.Repository;
using Moq;
using Domain0.Repository.Model;
using Domain0.Service;

namespace Domain0.Test
{
    public class SmsModuleTests
    {
        [Fact]
        public async Task Registration_Validation_UserExists()
        {
            var container = TestModuleTests.GetContainer();
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var phone = 79000000000;

            var accountRepository = container.Resolve<IAccountRepository>();
            var accountMock = Mock.Get(accountRepository);
            accountMock.Setup(a => a.FindByPhone(phone)).ReturnsAsync(new Account());

            var result = await browser.Put(SmsModule.RegisterUrl, with =>
            {
                with.Accept("application/json");
                with.JsonBody(phone);
            });

            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task Registration_Success()
        {
            var container = TestModuleTests.GetContainer(b =>
            {
                b.RegisterInstance(new Mock<IPasswordGenerator>().Object).As<IPasswordGenerator>().SingleInstance();
            });
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var phone = 79000000000;

            var accountRepository = container.Resolve<IAccountRepository>();
            var messageTemplateRepository = container.Resolve<IMessageTemplateRepository>();
            var passwordGenerator = container.Resolve<IPasswordGenerator>();
            var smsClient = container.Resolve<ISmsClient>();
            var accountMock = Mock.Get(accountRepository);
            var messageTemplate = Mock.Get(messageTemplateRepository);
            var passwordMock = Mock.Get(passwordGenerator);
            var smsMock = Mock.Get(smsClient);
            accountMock.Setup(a => a.FindByPhone(phone)).ReturnsAsync((Account) null);
            messageTemplate.Setup(r => r.GetRegisterTemplate()).ReturnsAsync("hello {0}!");
            passwordMock.Setup(p => p.Generate()).Returns("password");

            var result = await browser.Put(SmsModule.RegisterUrl, with =>
            {
                with.Accept("application/json");
                with.JsonBody(phone);
            });

            Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
            smsMock.Verify(s => s.Send(phone, "hello password!"));
        }
    }
}
