using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Autofac;
using Domain0.Model;
using Domain0.Nancy;
using Domain0.Nancy.Infrastructure;
using Domain0.Repository;
using Domain0.Repository.Model;
using Domain0.Service;
using Domain0.Service.Tokens;
using Moq;
using Nancy;
using Nancy.Testing;
using Xunit;

namespace Domain0.Test
{

    public class SmsModuleTests
    {
        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task Registration_Validation_UserExists(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer();
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var phone = 79000000000;

            var accountRepository = container.Resolve<IAccountRepository>();
            var accountMock = Mock.Get(accountRepository);
            accountMock.Setup(a => a.FindByPhone(phone)).ReturnsAsync(new Account());

            var response = await browser.Put(SmsModule.RegisterUrl, with =>
            {
                with.Accept(format);
                with.DataFormatBody(format, phone);
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task Registration_Success(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer();
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var phone = 79000000000;

            var accountRepository = container.Resolve<IAccountRepository>();
            var accountMock = Mock.Get(accountRepository);
            accountMock.Setup(a => a.FindByPhone(phone)).ReturnsAsync((Account)null);

            var permissionRepository = container.Resolve<IPermissionRepository>();
            var permissionRepositoryMock = Mock.Get(permissionRepository);

            permissionRepositoryMock
                .Setup(p => p.GetByUserId(It.IsAny<int>()))
                .ReturnsAsync(new[] {new Repository.Model.Permission()});

            var messageTemplateRepository = container.Resolve<IMessageTemplateRepository>();
            var messageTemplate = Mock.Get(messageTemplateRepository);
            messageTemplate
                .Setup(r => r.GetTemplate(
                    It.IsAny<MessageTemplateName>(),
                    It.IsAny<CultureInfo>(),
                    It.IsAny<MessageTemplateType>(),
                    It.IsAny<int>()))
                .Returns<MessageTemplateName, CultureInfo, MessageTemplateType, int>((n, l, t, e) =>
                {
                    if (n == MessageTemplateName.RegisterTemplate)
                        return Task.FromResult("Your password is: {0} will valid for {1} min");

                    if (n == MessageTemplateName.WelcomeTemplate)
                        return Task.FromResult("Hello {0}!");

                    throw new NotImplementedException();
                });

            var passwordGenerator = container.Resolve<IPasswordGenerator>();
            var passwordMock = Mock.Get(passwordGenerator);
            passwordMock.Setup(p => p.GeneratePassword()).Returns("password");

            var environmentRepositoryMock = Mock.Get(container.Resolve<IEnvironmentRepository>());
            var env = new Repository.Model.Environment
            {
                Name = "default envToken",
                Id = 123,
                Token = "default token",
                IsDefault = true
            };
            environmentRepositoryMock
                .Setup(callTo => callTo.GetDefault())
                .ReturnsAsync(env);

            environmentRepositoryMock
                .Setup(callTo => callTo.FindById(It.IsAny<int>()))
                .ReturnsAsync(env);

            var registerResponse = await browser.Put(SmsModule.RegisterUrl, with =>
            {
                with.Accept(format);
                with.DataFormatBody(format, phone);
            });

            Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

            var smsRequestMock = Mock.Get(container.Resolve<ISmsRequestRepository>());
            smsRequestMock
                .Setup(x => x.ConfirmRegister(It.IsAny<decimal>(), It.IsAny<string>()))
                .ReturnsAsync(new SmsRequest
                {
                    EnvironmentId = env.Id
                });

            smsRequestMock.Verify(a => a.Save(It.IsAny<SmsRequest>()), Times.Once());

            var smsClient = container.Resolve<ISmsClient>();
            var smsMock = Mock.Get(smsClient);
            smsMock.Verify(s => s.Send(phone, "Your password is: password will valid for 15 min", It.IsAny<string>()));

            var firstLoginResponse = await browser.Post(SmsModule.LoginUrl, 
                with =>
                {
                    with.Accept(format);
                    with.DataFormatBody(format,
                        new SmsLoginRequest { Phone = phone, Password = "password" });
                });

            Assert.Equal(HttpStatusCode.OK, firstLoginResponse.StatusCode);

            environmentRepositoryMock
                .Verify(
                    callTo => callTo.SetUserEnvironment(It.IsAny<int>(), 123),
                    Times.Once);
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task Registration_With_Environment_Success(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer();
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var phone = 79000000000;
            var envToken = "EnvironmentToken";

            var accountRepository = container.Resolve<IAccountRepository>();
            var accountMock = Mock.Get(accountRepository);
            accountMock.Setup(a => a.FindByPhone(phone)).ReturnsAsync((Account)null);

            var permissionRepository = container.Resolve<IPermissionRepository>();
            var permissionRepositoryMock = Mock.Get(permissionRepository);

            permissionRepositoryMock
                .Setup(p => p.GetByUserId(It.IsAny<int>()))
                .ReturnsAsync(new[] { new Repository.Model.Permission() });

            var messageTemplateRepository = container.Resolve<IMessageTemplateRepository>();
            var messageTemplate = Mock.Get(messageTemplateRepository);
            messageTemplate
                .Setup(r => r.GetTemplate(
                    It.IsAny<MessageTemplateName>(),
                    It.IsAny<CultureInfo>(),
                    It.IsAny<MessageTemplateType>(),
                    It.IsAny<int>()))
                .Returns<MessageTemplateName, CultureInfo, MessageTemplateType, int>((n, l, t, e) =>
                {
                    if (n == MessageTemplateName.RegisterTemplate)
                        return Task.FromResult("Your password is: {0} will valid for {1} min");

                    if (n == MessageTemplateName.WelcomeTemplate)
                        return Task.FromResult("Hello {0}!");

                    throw new NotImplementedException();
                });

            var passwordGenerator = container.Resolve<IPasswordGenerator>();
            var passwordMock = Mock.Get(passwordGenerator);
            passwordMock.Setup(p => p.GeneratePassword()).Returns("password");

            var environmentRepositoryMock = Mock.Get(container.Resolve<IEnvironmentRepository>());
            var env = new Repository.Model.Environment
            {
                Name = "test envToken",
                Id = 765,
                Token = envToken
            };
            environmentRepositoryMock
                .Setup(callTo => callTo.GetByToken(It.Is<string>(s => s.Equals(envToken))))
                .ReturnsAsync(env);

            environmentRepositoryMock
                .Setup(callTo => callTo.FindById(It.Is<int>(id => id == env.Id)))
                .ReturnsAsync(env);


            var smsRequestMock = Mock.Get(container.Resolve<ISmsRequestRepository>());
            smsRequestMock
                .Setup(callTo => callTo.ConfirmRegister(It.IsAny<decimal>(), It.IsAny<string>()))
                .ReturnsAsync(new SmsRequest
                {
                    EnvironmentId = env.Id
                });

            var registerResponse = await browser.Put(
                SmsModule.RegisterWithEnvironmentUrl.Replace("{EnvironmentToken}", envToken), 
                with =>
                {
                    with.Accept(format);
                    with.DataFormatBody(format, phone);
                });

            Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
            environmentRepositoryMock
                .Verify(callTo =>
                    callTo.GetByToken(It.Is<string>(t => t.Equals(envToken))),
                    Times.Once);
            smsRequestMock.Verify(a => a.Save(It.Is<SmsRequest>(r => r.EnvironmentId == env.Id)), Times.Once());

            smsRequestMock
                .Setup(x => x.ConfirmRegister(It.IsAny<decimal>(), It.IsAny<string>()))
                .ReturnsAsync(new SmsRequest
                {
                    EnvironmentId = env.Id
                });


            var smsClient = container.Resolve<ISmsClient>();
            var smsMock = Mock.Get(smsClient);
            smsMock.Verify(s => s.Send(phone, "Your password is: password will valid for 15 min", It.IsAny<string>()));

            var firstLoginResponse = await browser.Post(SmsModule.LoginUrl,
                with =>
                {
                    with.Accept(format);
                    with.DataFormatBody(format,
                        new SmsLoginRequest { Phone = phone, Password = "password" });
                });

            Assert.Equal(HttpStatusCode.OK, firstLoginResponse.StatusCode);
            environmentRepositoryMock
                .Verify(
                    callTo => callTo.SetUserEnvironment(It.IsAny<int>(), env.Id.Value),
                    Times.Once);
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task ForceCreateUser_SendSms_CustomTemplate(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(builder =>
                builder.RegisterType<SymmetricKeyTokenGenerator>().As<ITokenGenerator>().SingleInstance());
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var userId = 1;
            var phone = 79000000000;
            var roles = new List<string> {"role1", "role2"};
            var accessToken = TestContainerBuilder.BuildToken(container, userId, TokenClaims.CLAIM_PERMISSIONS_FORCE_CREATE_USER);

            var accountRepository = container.Resolve<IAccountRepository>();
            var accountMock = Mock.Get(accountRepository);
            var registers = new Dictionary<decimal, Account>();
            accountMock.Setup(a => a.Insert(It.IsAny<Account>())).Callback<Account>(a => registers[a.Phone.GetValueOrDefault()] = a).Returns(Task.FromResult(1));
            accountMock.Setup(a => a.FindByPhone(phone)).Returns<decimal>(p => Task.FromResult(registers.TryGetValue(p, out var acc) ? acc : null));

            var roleRepository = container.Resolve<IRoleRepository>();
            var roleMock = Mock.Get(roleRepository);
            roleMock.Setup(r => r.GetByRoleNames(It.IsAny<string[]>())).ReturnsAsync(roles.Select(role => new Repository.Model.Role { Name = role }).ToArray());

            var passwordGenerator = container.Resolve<IPasswordGenerator>();
            var passwordMock = Mock.Get(passwordGenerator);
            passwordMock.Setup(p => p.GeneratePassword()).Returns("password");

            var response = await browser.Put(SmsModule.ForceCreateUserUrl, with =>
            {
                with.Accept(format);
                with.Header("Authorization", $"Bearer {accessToken}");
                with.DataFormatBody(format, new ForceCreateUserRequest
                {
                    BlockSmsSend = false,
                    Phone = phone,
                    Name = "test",
                    Roles = roles,
                    CustomSmsTemplate = "password {password} phone {phone}"
                });
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var environmentRepositoryMock = Mock.Get(container.Resolve<IEnvironmentRepository>());
            environmentRepositoryMock
                .Verify(
                    callTo => callTo.SetUserEnvironment(It.IsAny<int>(), 123),
                    Times.Once);

            var smsClient = container.Resolve<ISmsClient>();
            var smsMock = Mock.Get(smsClient);
            smsMock.Verify(s => s.Send(phone, "password password phone " + phone, It.IsAny<string>()));
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task ForceCreateUser_SendSms_StandardTemplate(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(builder =>
                builder.RegisterType<SymmetricKeyTokenGenerator>().As<ITokenGenerator>().SingleInstance());
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var userId = 1;
            var phone = 79000000000;
            var roles = new List<string> { "role1", "role2" };
            var accessToken = TestContainerBuilder.BuildToken(container, userId, TokenClaims.CLAIM_PERMISSIONS_FORCE_CREATE_USER);

            var accountRepository = container.Resolve<IAccountRepository>();
            var accountMock = Mock.Get(accountRepository);
            var registers = new Dictionary<decimal, Account>();
            accountMock.Setup(a => a.Insert(It.IsAny<Account>())).Callback<Account>(a => registers[a.Phone.GetValueOrDefault()] = a).Returns(Task.FromResult(1));
            accountMock.Setup(a => a.FindByPhone(phone)).Returns<decimal>(p => Task.FromResult(registers.TryGetValue(p, out var acc) ? acc : null));

            var roleRepository = container.Resolve<IRoleRepository>();
            var roleMock = Mock.Get(roleRepository);
            roleMock.Setup(r => r.GetByRoleNames(It.IsAny<string[]>())).ReturnsAsync(roles.Select(role => new Repository.Model.Role { Name = role }).ToArray());

            var passwordGenerator = container.Resolve<IPasswordGenerator>();
            var passwordMock = Mock.Get(passwordGenerator);
            passwordMock.Setup(p => p.GeneratePassword()).Returns("password");

            var messageTemplateRepository = container.Resolve<IMessageTemplateRepository>();
            var messageTemplate = Mock.Get(messageTemplateRepository);
            messageTemplate.Setup(r => 
                r.GetTemplate(
                    It.IsAny<MessageTemplateName>(),
                    It.IsAny<CultureInfo>(),
                    It.IsAny<MessageTemplateType>(),
                    It.IsAny<int>())
                )
                .ReturnsAsync("hello {1} {0}!");

            var response = await browser.Put(SmsModule.ForceCreateUserUrl, with =>
            {
                with.Accept(format);
                with.Header("Authorization", $"Bearer {accessToken}");
                with.DataFormatBody(format, new ForceCreateUserRequest
                {
                    BlockSmsSend = false,
                    Phone = phone,
                    Name = "test",
                    Roles = roles,
                });
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var environmentRepositoryMock = Mock.Get(container.Resolve<IEnvironmentRepository>());
            environmentRepositoryMock
                .Verify(
                    callTo => callTo.SetUserEnvironment(It.IsAny<int>(), 123),
                    Times.Once);

            var smsClient = container.Resolve<ISmsClient>();
            var smsMock = Mock.Get(smsClient);
            smsMock.Verify(s => s.Send(phone, "hello password " + phone + "!", It.IsAny<string>()));
        }

        
        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task ForceResetPassword_Success(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(builder =>
                builder.RegisterType<SymmetricKeyTokenGenerator>().As<ITokenGenerator>().SingleInstance());
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var userId = 1;
            var phone = 79000000000;
            var password = "password";
            var accessToken = TestContainerBuilder.BuildToken(container, userId, TokenClaims.CLAIM_PERMISSIONS_FORCE_PASSWORD_RESET);

            var accountRepository = container.Resolve<IAccountRepository>();
            var accountMock = Mock.Get(accountRepository);
            accountMock
                .Setup(a => a.FindByPhone(phone))
                .ReturnsAsync(new Account
                {
                    Id = userId,
                    Phone = phone,
                    Password = password
                });

            var passwordGenerator = container.Resolve<IPasswordGenerator>();
            var passwordMock = Mock.Get(passwordGenerator);
            passwordMock.Setup(p => p.GeneratePassword()).Returns(password);
            passwordMock.Setup(a => a.HashPassword(It.IsAny<string>())).Returns<string>(p => p);

            var messageTemplateRepository = container.Resolve<IMessageTemplateRepository>();
            var messageTemplate = Mock.Get(messageTemplateRepository);
            messageTemplate.Setup(r =>
                r.GetTemplate(
                    It.IsAny<MessageTemplateName>(),
                    It.IsAny<CultureInfo>(),
                    It.IsAny<MessageTemplateType>(),
                    It.IsAny<int>())
                )
                .ReturnsAsync("hello, your new password is {0}!");

            var response = await browser.Post(
                SmsModule.ForceResetPasswordUrl, 
                with =>
                {
                    with.Accept(format);
                    with.Header("Authorization", $"Bearer {accessToken}");
                    with.DataFormatBody(format, phone);
                });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            accountMock.Verify(am => am.FindByPhone(phone));
            accountMock.Verify(am => am.Update(
                It.Is<Account>(a => a.Password == password)));
            messageTemplate.Verify(mt => mt.GetTemplate(
                MessageTemplateName.ForcePasswordResetTemplate,
                It.IsAny<CultureInfo>(),
                MessageTemplateType.sms,
                It.IsAny<int>()));

            var smsClient = container.Resolve<ISmsClient>();
            var smsMock = Mock.Get(smsClient);
            smsMock.Verify(s => 
                s.Send(phone, "hello, your new password is password!", It.IsAny<string>()));
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task ForceCreateUser_NotSendSms(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(builder =>
                builder.RegisterType<SymmetricKeyTokenGenerator>().As<ITokenGenerator>().SingleInstance());
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var userId = 1;
            var phone = 79000000000;
            var roles = new List<string> {"role1", "role2"};
            var accessToken = TestContainerBuilder.BuildToken(container, userId, TokenClaims.CLAIM_PERMISSIONS_FORCE_CREATE_USER);

            var accountRepository = container.Resolve<IAccountRepository>();
            var accountMock = Mock.Get(accountRepository);
            var registers = new Dictionary<decimal, Account>();
            accountMock.Setup(a => a.Insert(It.IsAny<Account>())).Callback<Account>(a => registers[a.Phone.GetValueOrDefault()] = a).Returns(Task.FromResult(1));
            accountMock.Setup(a => a.FindByPhone(phone)).Returns<decimal>(p => Task.FromResult(registers.TryGetValue(p, out var acc) ? acc : null));

            var roleRepository = container.Resolve<IRoleRepository>();
            var roleMock = Mock.Get(roleRepository);
            roleMock.Setup(r => r.GetByRoleNames(It.IsAny<string[]>())).ReturnsAsync(roles.Select(role => new Repository.Model.Role { Name = role }).ToArray());

            var passwordGenerator = container.Resolve<IPasswordGenerator>();
            var passwordMock = Mock.Get(passwordGenerator);
            passwordMock.Setup(p => p.GeneratePassword()).Returns("password");

            var response = await browser.Put(SmsModule.ForceCreateUserUrl, with =>
            {
                with.Accept(format);
                with.Header("Authorization", $"Bearer {accessToken}");
                with.DataFormatBody(format, new ForceCreateUserRequest
                {
                    BlockSmsSend = true,
                    Phone = phone,
                    Name = "test",
                    Roles = roles,
                    CustomSmsTemplate = "password {password} phone {phone}"
                });
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var environmentRepositoryMock = Mock.Get(container.Resolve<IEnvironmentRepository>());
            environmentRepositoryMock
                .Verify(
                    callTo => callTo.SetUserEnvironment(It.IsAny<int>(), 123),
                    Times.Once);

            var smsClient = container.Resolve<ISmsClient>();
            var smsMock = Mock.Get(smsClient);
            smsMock.Verify(c => c.Send(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task ForceCreateUser_UserExists(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(builder =>
                builder.RegisterType<SymmetricKeyTokenGenerator>().As<ITokenGenerator>().SingleInstance());
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var userId = 1;
            var phone = 79000000000;
            var roles = new List<string> { "role1", "role2" };
            var accessToken = TestContainerBuilder.BuildToken(container, userId, TokenClaims.CLAIM_PERMISSIONS_FORCE_CREATE_USER);

            var accountRepository = container.Resolve<IAccountRepository>();
            var accountMock = Mock.Get(accountRepository);
            accountMock.Setup(a => a.FindByPhone(phone)).ReturnsAsync(new Account {Phone = phone});

            var response = await browser.Put(SmsModule.ForceCreateUserUrl, with =>
            {
                with.Accept(format);
                with.Header("Authorization", $"Bearer {accessToken}");
                with.DataFormatBody(format, new ForceCreateUserRequest
                {
                    BlockSmsSend = true,
                    Phone = phone,
                    Name = "test",
                    Roles = roles,
                    CustomSmsTemplate = "password {password} phone {phone}"
                });
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task ForceCreateUser_Validation(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(builder =>
                builder.RegisterType<SymmetricKeyTokenGenerator>().As<ITokenGenerator>().SingleInstance());
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var userId = 1;
            var accessToken = TestContainerBuilder.BuildToken(container, userId, TokenClaims.CLAIM_PERMISSIONS_FORCE_CREATE_USER);
            var response = await browser.Put(SmsModule.ForceCreateUserUrl, with =>
            {
                with.Accept(format);
                with.Header("Authorization", $"Bearer {accessToken}");

            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task Login_Success(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(builder =>
            {
                builder
                    .RegisterInstance(new Mock<ITokenGenerator>().Object)
                    .Keyed<ITokenGenerator>("HS256");

            });
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var phone = 79000000000;
            var password = "password";

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByLogin(phone.ToString())).ReturnsAsync(new Account {Id = 1, Login = phone.ToString(), Phone = phone, Password = password});

            var permissionMock = Mock.Get(container.Resolve<IPermissionRepository>());
            permissionMock.Setup(a => a.GetByUserId(It.IsAny<int>())).ReturnsAsync(new[] 
            {
                new Repository.Model.Permission { Name = "test1" },
                new Repository.Model.Permission { Name = "test2" },
            });

            var tokenMock = Mock.Get(container.Resolve<ITokenRegistrationRepository>());
            tokenMock.Setup(a => a.FindLastTokenByUserId(It.IsAny<int>())).ReturnsAsync((TokenRegistration) null);

            var authGenerator = Mock.Get(container.Resolve<IPasswordGenerator>());
            authGenerator.Setup(a => a.CheckPassword(It.IsAny<string>(), It.IsAny<string>())).Returns<string, string>((pasd, hash) => pasd == hash);
            var tokenGenerator = Mock.Get(container.ResolveKeyed<ITokenGenerator>("HS256"));
            tokenGenerator.Setup(a => a.GenerateAccessToken(
                It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<string[]>()))
                .Returns<int, DateTime, string[]>((userId, dt, perms) => userId + string.Join("", perms));
            tokenGenerator.Setup(a => a.GenerateRefreshToken(It.IsAny<int>(), It.IsAny<int>())).Returns<int, int>((tid, userId) => $"{tid}_{userId}");

            var response = await browser.Post(SmsModule.LoginUrl, with =>
            {
                with.Accept(format);
                with.DataFormatBody(format, new SmsLoginRequest {Phone = phone, Password = password});
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            tokenMock.Verify(t => t.Save(It.IsAny<TokenRegistration>()), Times.Once);

            var result = response.Body.AsDataFormat<AccessTokenResponse>(format);
            Assert.Equal(1, result.Profile.Id);
            Assert.Equal(phone.ToString(), result.Profile.Phone);
            Assert.Equal("1test1test2", result.AccessToken);
            Assert.Equal("0_1", result.RefreshToken);
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task Login_Register_NoRequest(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer();
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var phone = 79000000000;
            var password = "password";

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByLogin(phone.ToString())).ReturnsAsync((Account) null);

            var smsRequestMock = Mock.Get(container.Resolve<ISmsRequestRepository>());
            smsRequestMock.Setup(a => a.Pick(phone)).ReturnsAsync((SmsRequest) null);

            var response = await browser.Post(SmsModule.LoginUrl, with =>
            {
                with.Accept(format);
                with.DataFormatBody(format, new SmsLoginRequest { Phone = phone, Password = password });
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task Login_Register_ExpiredRequest(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer();
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var phone = 79000000000;
            var password = "password";

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByLogin(phone.ToString())).ReturnsAsync((Account)null);

            var smsRequestMock = Mock.Get(container.Resolve<ISmsRequestRepository>());
            smsRequestMock.Setup(a => a.Pick(phone)).ReturnsAsync(new SmsRequest { Phone = phone, Password = password, ExpiredAt = DateTime.UtcNow.AddSeconds(-1)});

            var response = await browser.Post(SmsModule.LoginUrl, with =>
            {
                with.Accept(format);
                with.DataFormatBody(format, new SmsLoginRequest { Phone = phone, Password = password });
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task ChangePassword_Account(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(
                builder => builder.RegisterType<SymmetricKeyTokenGenerator>().As<ITokenGenerator>().SingleInstance());

            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var userId = 1;
            var phone = 79000000000;
            var password = "password";
            var newpassword = "newpassword";

            string accessToken = TestContainerBuilder.BuildToken(container, userId, TokenClaims.CLAIM_PERMISSIONS_BASIC);

            var contextMock = Mock.Get(container.Resolve<IRequestContext>());
            contextMock.Setup(a => a.UserId).Returns(userId);

            var account = new Account { Id = userId, Login = phone.ToString(), Phone = phone, Password = password };
            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByUserId(1)).ReturnsAsync(account);

            var authGenerator = Mock.Get(container.Resolve<IPasswordGenerator>());
            authGenerator.Setup(a => a.CheckPassword(It.IsAny<string>(), It.IsAny<string>())).Returns<string, string>((p, h) => p == h);
            authGenerator.Setup(a => a.HashPassword(It.IsAny<string>())).Returns<string>((p) => p);

            var response = await browser.Post(SmsModule.ChangePasswordUrl, with =>
            {
                with.Header("Authorization", $"Bearer {accessToken}");
                with.Accept(format);
                with.DataFormatBody(format, new ChangePasswordRequest { OldPassword = password, NewPassword = newpassword });
            });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            accountMock.Verify(a => a.Update(It.IsAny<Account>()), Times.Once);
            Assert.Equal(newpassword, account.Password);
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task ResetPassword_Success(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer();
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var phone = 79000000000;
            var password = "password";

            var account = new Account { Id = 1, Login = phone.ToString(), Phone = phone, Password = password };
            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByPhone(phone)).ReturnsAsync(account);

            var messageTemplateMock = Mock.Get(container.Resolve<IMessageTemplateRepository>());
            messageTemplateMock
                .Setup(a => a.GetTemplate(
                    It.IsAny<MessageTemplateName>(),
                    It.IsAny<CultureInfo>(),
                    It.IsAny<MessageTemplateType>(),
                    It.IsAny<int>()))
                .ReturnsAsync("{0}_test");

            var authGenerator = Mock.Get(container.Resolve<IPasswordGenerator>());
            authGenerator.Setup(a => a.GeneratePassword()).Returns(() => password);

            var response = await browser.Post(SmsModule.RequestResetPasswordUrl, with =>
            {
                with.Accept(format);
                with.DataFormatBody(format, phone);
            });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var smsRequestMock = Mock.Get(container.Resolve<ISmsRequestRepository>());
            smsRequestMock.Verify(a => a.Save(It.IsAny<SmsRequest>()), Times.Once);

            var smsMock = Mock.Get(container.Resolve<ISmsClient>());
            smsMock.Verify(a => a.Send(phone, password + "_test", It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void Generate()
        {
            var passwordGenerator = new PasswordGenerator();
            var password = passwordGenerator.GeneratePassword();
            var hash = passwordGenerator.HashPassword(password);
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task ResetPassword_NotFound(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer();
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var phone = 79000000000;

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByPhone(phone)).ReturnsAsync((Account) null);

            var response = await browser.Post(SmsModule.RequestResetPasswordUrl, with =>
            {
                with.Accept(format);
                with.DataFormatBody(format, phone);
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task ForceChangePhone_Success(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(builder => 
                builder.RegisterType<SymmetricKeyTokenGenerator>().As<ITokenGenerator>().SingleInstance());

            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var userId = 1;
            var phone = 79000000000;
            var newphone = 79000000001;
            var accessToken = TestContainerBuilder.BuildToken(container, userId, TokenClaims.CLAIM_PERMISSIONS_FORCE_CHANGE_PHONE);

            var account = new Account { Id = 1, Login = phone.ToString(), Phone = phone };
            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByUserId(userId)).ReturnsAsync(account);

            var response = await browser.Post(SmsModule.ForceChangePhoneUrl, with =>
            {
                with.Accept(format);
                with.Header("Authorization", $"Bearer {accessToken}");
                with.DataFormatBody(format, new ChangePhoneRequest {UserId = userId, NewPhone = newphone});
            });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(newphone, account.Phone);
            Assert.Equal(newphone.ToString(), account.Login);
            accountMock.Verify(a => a.Update(It.IsAny<Account>()), Times.Once);
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task ForceChangePhone_NotFound(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(builder => 
                builder.RegisterType<SymmetricKeyTokenGenerator>().As<ITokenGenerator>().SingleInstance());

            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var userId = 1;
            var newphone = 79000000001;
            var accessToken = TestContainerBuilder.BuildToken(container, userId, TokenClaims.CLAIM_PERMISSIONS_FORCE_CHANGE_PHONE);

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByUserId(userId)).ReturnsAsync((Account) null);

            var response = await browser.Post(SmsModule.ForceChangePhoneUrl, with =>
            {
                with.Accept(format);
                with.Header("Authorization", $"Bearer {accessToken}");
                with.DataFormatBody(format, new ChangePhoneRequest { UserId = userId, NewPhone = newphone });
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }


        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task RequestChangePhone_Success(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(builder =>
                builder.RegisterType<SymmetricKeyTokenGenerator>().As<ITokenGenerator>().SingleInstance());

            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var accountId = 321;
            var userId = 1;
            var phone = 79000000000;
            var newPhone = 79000000001;
            var password = "123";
            var pin = "333";
            var accessToken = TestContainerBuilder.BuildToken(container, userId, TokenClaims.CLAIM_PERMISSIONS_BASIC);

            var account = new Account { Id = accountId, Login = phone.ToString(), Phone = phone, Password = password};
            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByUserId(userId)).ReturnsAsync(account);

            var smsRequestRepository = Mock.Get(container.Resolve<ISmsRequestRepository>());
            smsRequestRepository
                .Setup(sr => sr.PickByUserId(
                    It.Is<int>(x => x == userId)))
                .ReturnsAsync(new SmsRequest
                {
                    Phone = newPhone,
                    UserId = userId,
                    Password = pin
                });

            var authGenerator = Mock.Get(container.Resolve<IPasswordGenerator>());
            authGenerator.Setup(a => 
                a.CheckPassword(It.IsAny<string>(), It.IsAny<string>())).Returns<string, string>((pasd, hash) => pasd == hash);
            authGenerator.Setup(a => a.GeneratePassword())
                .Returns(pin);

            var messageTemplate = Mock.Get(container.Resolve<IMessageTemplateRepository>());
            messageTemplate
                .Setup(r => r.GetTemplate(
                    It.IsAny<MessageTemplateName>(),
                    It.IsAny<CultureInfo>(),
                    It.IsAny<MessageTemplateType>(),
                    It.IsAny<int>()))
                .Returns<MessageTemplateName, CultureInfo, MessageTemplateType, int>((n, l, t, e) =>
                {
                    if (n == MessageTemplateName.RequestPhoneChangeTemplate)
                        return Task.FromResult("Your password is: {0} will valid for {1} min");

                    throw new NotImplementedException();
                });

            var smsClient = Mock.Get(container.Resolve<ISmsClient>());

            var responseToChangeRequest = await browser.Post(
                SmsModule.RequestChangePhoneUrl, 
                with =>
                {
                    with.Accept(format);
                    with.Header("Authorization", $"Bearer {accessToken}");
                    with.DataFormatBody(format, 
                        new ChangePhoneUserRequest
                        {
                            Password = password,
                            Phone = newPhone
                        });
                });
            Assert.Equal(HttpStatusCode.NoContent, responseToChangeRequest.StatusCode);

            authGenerator.Verify(ag => 
                ag.CheckPassword(
                    It.Is<string>(x => x == password),
                    It.Is<string>(x => x == password)),
                Times.Once);

            smsRequestRepository.Verify(
                srr => srr.Save(
                    It.Is<SmsRequest>(r =>
                        r.UserId == userId
                        && r.Phone == newPhone
                        && r.Password == pin)), 
                Times.Once);

            messageTemplate.Verify(mtr => 
                mtr.GetTemplate(
                    It.Is<MessageTemplateName>(mt => mt == MessageTemplateName.RequestPhoneChangeTemplate),
                    It.IsAny<CultureInfo>(),
                    It.Is<MessageTemplateType>(mtt => mtt == MessageTemplateType.sms),
                    It.IsAny<int>()),
                Times.Once);

            smsClient.Verify(sc => 
                sc.Send(
                    It.Is<decimal>(x => x == newPhone),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Once);
            

            var responseToCommitRequest = await browser.Post(
                SmsModule.CommitChangePhoneUrl, 
                with =>
                {
                    with.Accept(format);
                    with.Header("Authorization", $"Bearer {accessToken}");
                    with.Query("code", pin);
                });
            Assert.Equal(HttpStatusCode.NoContent, responseToCommitRequest.StatusCode);

            accountMock.Verify(ar => 
                ar.Update(
                    It.Is<Account>(a => 
                        a.Id == accountId
                        && a.Login == newPhone.ToString()
                        && a.Phone == newPhone)), 
                    Times.Once);
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task DoesUserExists_IsTrue(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer();
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var phone = 79000000000;

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByPhone(phone)).ReturnsAsync(new Account());

            var response = await browser.Get(SmsModule.DoesUserExistUrl, with =>
            {
                with.Accept(format);
                with.Query(nameof(phone), phone.ToString());
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = response.Body.AsDataFormat<bool>(format);
            Assert.True(result);
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task DoesUserExists_IsFalse(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer();
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var phone = 79000000000;

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByPhone(phone)).ReturnsAsync((Account)null);

            var response = await browser.Get(SmsModule.DoesUserExistUrl, with =>
            {
                with.Accept(format);
                with.Query(nameof(phone), phone.ToString());
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = response.Body.AsDataFormat<bool>(format);
            Assert.False(result);
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task GetPhoneByUserId_Success(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(builder =>
                builder.RegisterType<SymmetricKeyTokenGenerator>().As<ITokenGenerator>().SingleInstance());
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var phone = 79000000000;
            var id = 1;
            var userId = 1;
            var accessToken = TestContainerBuilder.BuildToken(container, userId, TokenClaims.CLAIM_PERMISSIONS_VIEW_USERS);

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByUserId(id)).ReturnsAsync(new Account {Id = id, Phone = phone});

            var response = await browser.Get(SmsModule.PhoneByUserIdUrl, with =>
            {
                with.Accept(format);
                with.Header("Authorization", $"Bearer {accessToken}");
                with.Query(nameof(id), id.ToString());
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = response.Body.AsDataFormat<long>(format);
            Assert.Equal(phone, result);
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task GetPhoneByUserId_NotFound(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(builder =>
                builder.RegisterType<SymmetricKeyTokenGenerator>().As<ITokenGenerator>().SingleInstance());
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var id = 1;
            var userId = 1;
            var accessToken = TestContainerBuilder.BuildToken(container, userId, TokenClaims.CLAIM_PERMISSIONS_VIEW_USERS);

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByUserId(id)).ReturnsAsync((Account) null);

            var response = await browser.Get(SmsModule.PhoneByUserIdUrl, with =>
            {
                with.Accept(format);
                with.Header("Authorization", $"Bearer {accessToken}");
                with.Query(nameof(id), id.ToString());
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task Refresh_Success(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(builder =>
            {
                builder
                    .RegisterInstance(new Mock<ITokenGenerator>().Object)
                    .Keyed<ITokenGenerator>("HS256");
                builder
                    .RegisterInstance(new Mock<IAccessLogRepository>().Object);
            });
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var accessLogRepository = Mock.Get(container.Resolve<IAccessLogRepository>());

            var userId = 101;
            var tid = 1001;
            var refreshToken = "refreshToken123123";
            var accessToken = "test1,test2,test3";

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByUserId(userId)).ReturnsAsync(new Account {Id = userId});

            var tokenGeneratorMock = Mock.Get(container.ResolveKeyed<ITokenGenerator>("HS256"));
            tokenGeneratorMock.Setup(p => p.GetTid(refreshToken)).Returns(tid);
            tokenGeneratorMock
                .Setup(a => a.Parse(It.IsAny<string>(),It.IsAny<bool>()))
                .Returns<string,bool>((token,x) =>
                    new ClaimsPrincipal(new ClaimsIdentity(token.Split(',').Select(r => new Claim(ClaimTypes.Role, r)))));
            tokenGeneratorMock.Setup(a => a.GenerateAccessToken(It.IsAny<int>(), It.IsAny<string[]>()))
                .Returns<int, string[]>((uid, roles) => $"{uid}_{string.Join("_", roles)}");

            var tokenMock = Mock.Get(container.Resolve<ITokenRegistrationRepository>());
            tokenMock.Setup(a => a.FindById(tid)).ReturnsAsync(new TokenRegistration
            {
                Id = tid,
                AccessToken = accessToken,
                UserId = userId
            });

            var response = await browser.Get(SmsModule.RefreshUrl.Replace("{refreshToken}", refreshToken), with =>
            {
                with.Accept(format);
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = response.Body.AsDataFormat<AccessTokenResponse>(format);
            Assert.Equal(userId, result.Profile.Id);
            Assert.Equal("101_test1_test2_test3", result.AccessToken);
            Assert.Equal(refreshToken, result.RefreshToken);

            accessLogRepository.Verify(l =>
                l.Insert(It.Is<AccessLogEntry>(x => x.Action.Equals(
                    SmsModule.RefreshUrl.Replace("{refreshToken}",
                        NancyExceptionHandling.SensitiveInfoReplacement))
                )), Times.Once);
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task Refresh_Account_NotFound(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(builder =>
            {
                builder
                    .RegisterInstance(new Mock<ITokenGenerator>().Object)
                    .Keyed<ITokenGenerator>("HS256");

            });
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var userId = 101;
            var tid = 1001;
            var refreshToken = "refreshToken123123";
            var accessToken = "test1,test2,test3";

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByUserId(userId)).ReturnsAsync((Account) null);

            var tokenGenerator = Mock.Get(container.ResolveKeyed<ITokenGenerator>("HS256"));
            tokenGenerator.Setup(p => p.GetTid(refreshToken)).Returns(tid);

            var tokenMock = Mock.Get(container.Resolve<ITokenRegistrationRepository>());
            tokenMock.Setup(a => a.FindById(tid)).ReturnsAsync(new TokenRegistration
            {
                Id = tid,
                AccessToken = accessToken,
                UserId = userId
            });

            var response = await browser.Get(SmsModule.RefreshUrl.Replace("{refreshToken}", refreshToken), with =>
            {
                with.Accept(format);
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task Refresh_TokenRegistry_NotFound(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(builder =>
            {
                builder
                    .RegisterInstance(new Mock<ITokenGenerator>().Object)
                    .Keyed<ITokenGenerator>("HS256");

            });
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var userId = 101;
            var tid = 1001;
            var refreshToken = "refreshToken123123";

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByUserId(userId)).ReturnsAsync(new Account { Id = userId });

            var tokenGenerator = Mock.Get(container.ResolveKeyed<ITokenGenerator>("HS256"));
            tokenGenerator.Setup(p => p.GetTid(refreshToken)).Returns(tid);

            var response = await browser.Get(SmsModule.RefreshUrl.Replace("{refreshToken}", refreshToken), with =>
            {
                with.Accept(format);
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
