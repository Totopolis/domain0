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
using Domain0.Model;
using System.Collections.Generic;
using System.Linq;

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
                b.RegisterInstance(new Mock<IAuthGenerator>().Object).As<IAuthGenerator>().SingleInstance();
            });
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var phone = 79000000000;

            var accountRepository = container.Resolve<IAccountRepository>();
            var accountMock = Mock.Get(accountRepository);
            accountMock.Setup(a => a.FindByPhone(phone)).ReturnsAsync((Account)null);

            var messageTemplateRepository = container.Resolve<IMessageTemplateRepository>();
            var messageTemplate = Mock.Get(messageTemplateRepository);
            messageTemplate.Setup(r => r.GetRegisterTemplate()).ReturnsAsync("hello {0}!");

            var passwordGenerator = container.Resolve<IAuthGenerator>();
            var passwordMock = Mock.Get(passwordGenerator);
            passwordMock.Setup(p => p.GeneratePassword()).Returns("password");

            var result = await browser.Put(SmsModule.RegisterUrl, with =>
            {
                with.Accept("application/json");
                with.JsonBody(phone);
            });

            Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);

            var smsClient = container.Resolve<ISmsClient>();
            var smsMock = Mock.Get(smsClient);
            smsMock.Verify(s => s.Send(phone, "hello password!"));
        }

        [Fact]
        public async Task ForceCreateUser_SendSms_CustomTemplate()
        {
            var container = TestModuleTests.GetContainer(b =>
            {
                b.RegisterInstance(new Mock<IAuthGenerator>().Object).As<IAuthGenerator>().SingleInstance();
            });
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var phone = 79000000000;
            var roles = new List<string> {"role1", "role2"};

            var accountRepository = container.Resolve<IAccountRepository>();
            var accountMock = Mock.Get(accountRepository);
            var registers = new Dictionary<decimal, Account>();
            accountMock.Setup(a => a.Insert(It.IsAny<Account>())).Callback<Account>(a => registers[a.Phone] = a).Returns(Task.FromResult(1));
            accountMock.Setup(a => a.FindByPhone(phone)).Returns<decimal>(p => Task.FromResult(registers.TryGetValue(p, out var acc) ? acc : null));

            var roleRepository = container.Resolve<IRoleRepository>();
            var roleMock = Mock.Get(roleRepository);
            roleMock.Setup(r => r.GetByIds(It.IsAny<string[]>())).ReturnsAsync(roles.Select(role => new Role {Code = role }).ToArray());

            var passwordGenerator = container.Resolve<IAuthGenerator>();
            var passwordMock = Mock.Get(passwordGenerator);
            passwordMock.Setup(p => p.GeneratePassword()).Returns("password");

            var result = await browser.Put(SmsModule.ForceCreateUserUrl, with =>
            {
                with.Accept("application/json");
                with.JsonBody(new ForceCreateUserRequest
                {
                    BlockSmsSend = false,
                    Phone = phone,
                    Name = "test",
                    Roles = roles,
                    CustomSmsTemplate = "password {password} phone {phone}"
                });
            });

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);

            var smsClient = container.Resolve<ISmsClient>();
            var smsMock = Mock.Get(smsClient);
            smsMock.Verify(s => s.Send(phone, "password password phone " + phone));
        }

        [Fact]
        public async Task ForceCreateUser_SendSms_StandardTemplate()
        {
            var container = TestModuleTests.GetContainer(b =>
            {
                b.RegisterInstance(new Mock<IAuthGenerator>().Object).As<IAuthGenerator>().SingleInstance();
            });
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var phone = 79000000000;
            var roles = new List<string> { "role1", "role2" };

            var accountRepository = container.Resolve<IAccountRepository>();
            var accountMock = Mock.Get(accountRepository);
            var registers = new Dictionary<decimal, Account>();
            accountMock.Setup(a => a.Insert(It.IsAny<Account>())).Callback<Account>(a => registers[a.Phone] = a).Returns(Task.FromResult(1));
            accountMock.Setup(a => a.FindByPhone(phone)).Returns<decimal>(p => Task.FromResult(registers.TryGetValue(p, out var acc) ? acc : null));

            var roleRepository = container.Resolve<IRoleRepository>();
            var roleMock = Mock.Get(roleRepository);
            roleMock.Setup(r => r.GetByIds(It.IsAny<string[]>())).ReturnsAsync(roles.Select(role => new Role { Code = role }).ToArray());

            var passwordGenerator = container.Resolve<IAuthGenerator>();
            var passwordMock = Mock.Get(passwordGenerator);
            passwordMock.Setup(p => p.GeneratePassword()).Returns("password");

            var messageTemplateRepository = container.Resolve<IMessageTemplateRepository>();
            var messageTemplate = Mock.Get(messageTemplateRepository);
            messageTemplate.Setup(r => r.GetWelcomeTemplate()).ReturnsAsync("hello {1} {0}!");

            var result = await browser.Put(SmsModule.ForceCreateUserUrl, with =>
            {
                with.Accept("application/json");
                with.JsonBody(new ForceCreateUserRequest
                {
                    BlockSmsSend = false,
                    Phone = phone,
                    Name = "test",
                    Roles = roles,
                });
            });

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);

            var smsClient = container.Resolve<ISmsClient>();
            var smsMock = Mock.Get(smsClient);
            smsMock.Verify(s => s.Send(phone, "hello password " + phone + "!"));
        }

        [Fact]
        public async Task ForceCreateUser_NotSendSms()
        {
            var container = TestModuleTests.GetContainer(b =>
            {
                b.RegisterInstance(new Mock<IAuthGenerator>().Object).As<IAuthGenerator>().SingleInstance();
            });
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var phone = 79000000000;
            var roles = new List<string> { "role1", "role2" };

            var accountRepository = container.Resolve<IAccountRepository>();
            var accountMock = Mock.Get(accountRepository);
            var registers = new Dictionary<decimal, Account>();
            accountMock.Setup(a => a.Insert(It.IsAny<Account>())).Callback<Account>(a => registers[a.Phone] = a).Returns(Task.FromResult(1));
            accountMock.Setup(a => a.FindByPhone(phone)).Returns<decimal>(p => Task.FromResult(registers.TryGetValue(p, out var acc) ? acc : null));

            var roleRepository = container.Resolve<IRoleRepository>();
            var roleMock = Mock.Get(roleRepository);
            roleMock.Setup(r => r.GetByIds(It.IsAny<string[]>())).ReturnsAsync(roles.Select(role => new Role { Code = role }).ToArray());

            var passwordGenerator = container.Resolve<IAuthGenerator>();
            var passwordMock = Mock.Get(passwordGenerator);
            passwordMock.Setup(p => p.GeneratePassword()).Returns("password");

            var result = await browser.Put(SmsModule.ForceCreateUserUrl, with =>
            {
                with.Accept("application/json");
                with.JsonBody(new ForceCreateUserRequest
                {
                    BlockSmsSend = true,
                    Phone = phone,
                    Name = "test",
                    Roles = roles,
                    CustomSmsTemplate = "password {password} phone {phone}"
                });
            });

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);

            var smsClient = container.Resolve<ISmsClient>();
            var smsMock = Mock.Get(smsClient);
            smsMock.Verify(c => c.Send(It.IsAny<decimal>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ForceCreateUser_UserExists()
        {
            var container = TestModuleTests.GetContainer();
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var phone = 79000000000;
            var roles = new List<string> { "role1", "role2" };

            var accountRepository = container.Resolve<IAccountRepository>();
            var accountMock = Mock.Get(accountRepository);
            accountMock.Setup(a => a.FindByPhone(phone)).ReturnsAsync(new Account {Phone = phone});

            var result = await browser.Put(SmsModule.ForceCreateUserUrl, with =>
            {
                with.Accept("application/json");
                with.JsonBody(new ForceCreateUserRequest
                {
                    BlockSmsSend = true,
                    Phone = phone,
                    Name = "test",
                    Roles = roles,
                    CustomSmsTemplate = "password {password} phone {phone}"
                });
            });

            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task ForceCreateUser_Validation()
        {
            var container = TestModuleTests.GetContainer();
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var result = await browser.Put(SmsModule.ForceCreateUserUrl, with =>
            {
                with.Accept("application/json");
                with.Body("");
            });

            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }
    }
}
