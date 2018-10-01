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
using System;
using System.Security.Claims;
using Sdl.Domain0.Shared;
using Newtonsoft.Json;
using Domain0.Service.Tokens;
using System.Globalization;

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

            var messageTemplateRepository = container.Resolve<IMessageTemplateRepository>();
            var messageTemplate = Mock.Get(messageTemplateRepository);
            messageTemplate
                .Setup(r => r.GetTemplate(
                    It.IsAny<MessageTemplateName>(),
                    It.IsAny<CultureInfo>(),
                    It.IsAny<MessageTemplateType>()))
                .Returns<MessageTemplateName, CultureInfo, MessageTemplateType>((n, l, t) =>
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

            var registerResponse = await browser.Put(SmsModule.RegisterUrl, with =>
            {
                with.Accept(format);
                with.DataFormatBody(format, phone);
            });

            Assert.Equal(HttpStatusCode.NoContent, registerResponse.StatusCode);

            var smsRequestMock = Mock.Get(container.Resolve<ISmsRequestRepository>());
            smsRequestMock
                .Setup(x => x.ConfirmRegister(It.IsAny<decimal>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            smsRequestMock.Verify(a => a.Save(It.IsAny<SmsRequest>()), Times.Once());

            var smsClient = container.Resolve<ISmsClient>();
            var smsMock = Mock.Get(smsClient);
            smsMock.Verify(s => s.Send(phone, "Your password is: password will valid for 1,5 min"));

            var firstLoginResponse = await browser.Post(SmsModule.LoginUrl, 
                with =>
                {
                    with.Accept(format);
                    with.DataFormatBody(format,
                        new SmsLoginRequest { Phone = phone.ToString(), Password = "password" });
                });

            Assert.Equal(HttpStatusCode.OK, firstLoginResponse.StatusCode);
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task ForceCreateUser_SendSms_CustomTemplate(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(builder =>
                builder.RegisterType<TokenGenerator>().As<ITokenGenerator>().SingleInstance());
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

            var smsClient = container.Resolve<ISmsClient>();
            var smsMock = Mock.Get(smsClient);
            smsMock.Verify(s => s.Send(phone, "password password phone " + phone));
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task ForceCreateUser_SendSms_StandardTemplate(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(builder =>
                builder.RegisterType<TokenGenerator>().As<ITokenGenerator>().SingleInstance());
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
                    It.IsAny<MessageTemplateType>())
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

            var smsClient = container.Resolve<ISmsClient>();
            var smsMock = Mock.Get(smsClient);
            smsMock.Verify(s => s.Send(phone, "hello password " + phone + "!"));
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task ForceCreateUser_NotSendSms(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(builder =>
                builder.RegisterType<TokenGenerator>().As<ITokenGenerator>().SingleInstance());
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

            var smsClient = container.Resolve<ISmsClient>();
            var smsMock = Mock.Get(smsClient);
            smsMock.Verify(c => c.Send(It.IsAny<decimal>(), It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task ForceCreateUser_UserExists(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(builder =>
                builder.RegisterType<TokenGenerator>().As<ITokenGenerator>().SingleInstance());
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
                builder.RegisterType<TokenGenerator>().As<ITokenGenerator>().SingleInstance());
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
            var container = TestContainerBuilder.GetContainer();
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
            var tokenGenerator = Mock.Get(container.Resolve<ITokenGenerator>());
            tokenGenerator.Setup(a => a.GenerateAccessToken(It.IsAny<int>(), It.IsAny<string[]>())).Returns<int, string[]>((userId, perms) => userId + string.Join("", perms));
            tokenGenerator.Setup(a => a.GenerateRefreshToken(It.IsAny<int>(), It.IsAny<int>())).Returns<int, int>((tid, userId) => $"{tid}_{userId}");

            var response = await browser.Post(SmsModule.LoginUrl, with =>
            {
                with.Accept(format);
                with.DataFormatBody(format, new SmsLoginRequest {Phone = phone.ToString(), Password = password});
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            tokenMock.Verify(t => t.Save(It.IsAny<TokenRegistration>()), Times.Once);

            var result = response.Body.AsDataFormat<AccessTokenResponse>(format);
            Assert.Equal(1, result.Profile.Id);
            Assert.Equal(phone, result.Profile.Phone);
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
                with.DataFormatBody(format, new SmsLoginRequest { Phone = phone.ToString(), Password = password });
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
                with.DataFormatBody(format, new SmsLoginRequest { Phone = phone.ToString(), Password = password });
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task ChangePassword_Account(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(
                builder => builder.RegisterType<TokenGenerator>().As<ITokenGenerator>().SingleInstance());

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
                    It.IsAny<MessageTemplateType>()))
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
            smsMock.Verify(a => a.Send(phone, password + "_test"), Times.Once);
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
                builder.RegisterType<TokenGenerator>().As<ITokenGenerator>().SingleInstance());

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
                builder.RegisterType<TokenGenerator>().As<ITokenGenerator>().SingleInstance());

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
                builder.RegisterType<TokenGenerator>().As<ITokenGenerator>().SingleInstance());
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
                builder.RegisterType<TokenGenerator>().As<ITokenGenerator>().SingleInstance());
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
            var container = TestContainerBuilder.GetContainer();
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var userId = 101;
            var tid = 1001;
            var refreshToken = "refreshToken123123";
            var accessToken = "test1,test2,test3";

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByUserId(userId)).ReturnsAsync(new Account {Id = userId});

            var tokenGeneratorMock = Mock.Get(container.Resolve<ITokenGenerator>());
            tokenGeneratorMock.Setup(p => p.GetTid(refreshToken)).Returns(tid);
            tokenGeneratorMock.Setup(a => a.Parse(It.IsAny<string>())).Returns<string>(token =>
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
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task Refresh_Account_NotFound(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer();
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var userId = 101;
            var tid = 1001;
            var refreshToken = "refreshToken123123";
            var accessToken = "test1,test2,test3";

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByUserId(userId)).ReturnsAsync((Account) null);

            var tokenGenerator = Mock.Get(container.Resolve<ITokenGenerator>());
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
            var container = TestContainerBuilder.GetContainer();
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var userId = 101;
            var tid = 1001;
            var refreshToken = "refreshToken123123";

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByUserId(userId)).ReturnsAsync(new Account { Id = userId });

            var tokenGenerator = Mock.Get(container.Resolve<ITokenGenerator>());
            tokenGenerator.Setup(p => p.GetTid(refreshToken)).Returns(tid);

            var response = await browser.Get(SmsModule.RefreshUrl.Replace("{refreshToken}", refreshToken), with =>
            {
                with.Accept(format);
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task GetMyProfile_Success(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(
                builder => builder.RegisterType<TokenGenerator>().As<ITokenGenerator>().SingleInstance());

            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var phone = 79000000000;
            var userId = 1;

            var accessToken = TestContainerBuilder.BuildToken(container, 1);

            var requestMock = Mock.Get(container.Resolve<IRequestContext>());
            requestMock.Setup(a => a.UserId).Returns(userId);

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByUserId(userId)).ReturnsAsync(new Account {Id = userId, Phone = phone});

            var response = await browser.Get(UsersModule.GetMyProfileUrl, with =>
            {
                with.Header("Authorization", $"Bearer {accessToken}");
                with.Accept(format);
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = response.Body.AsDataFormat<UserProfile>(format);
            Assert.Equal(userId, result.Id);
            Assert.Equal(phone, result.Phone);
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task GetProfileByPhone_Success(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(builder =>
                builder.RegisterType<TokenGenerator>().As<ITokenGenerator>().SingleInstance());
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var phone = 79000000000;
            var userId = 1;
            var accessToken = TestContainerBuilder.BuildToken(container, userId, TokenClaims.CLAIM_PERMISSIONS_VIEW_USERS);

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByPhone(phone)).ReturnsAsync(new Account { Id = userId, Phone = phone });

            var response = await browser.Get(UsersModule.GetUserByPhoneUrl.Replace("{phone}", phone.ToString()), with =>
            {
                with.Accept(format);
                with.Header("Authorization", $"Bearer {accessToken}");

            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = response.Body.AsDataFormat<UserProfile>(format);
            Assert.Equal(userId, result.Id);
            Assert.Equal(phone, result.Phone);
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task GetProfileByPhone_NotFound(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(builder =>
                builder.RegisterType<TokenGenerator>().As<ITokenGenerator>().SingleInstance());
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var userId = 1;
            var phone = 79000000000;
            var accessToken = TestContainerBuilder.BuildToken(container, userId, TokenClaims.CLAIM_PERMISSIONS_VIEW_USERS);

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByPhone(phone)).ReturnsAsync((Account) null);

            var response = await browser.Get(UsersModule.GetUserByPhoneUrl.Replace("{phone}", phone.ToString()), with =>
            {
                with.Accept(format);
                with.Header("Authorization", $"Bearer {accessToken}");
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task GetProfileByUserId_Success(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(builder =>
                builder.RegisterType<TokenGenerator>().As<ITokenGenerator>().SingleInstance());
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var userId = 1;
            var accessToken = TestContainerBuilder.BuildToken(container, userId, TokenClaims.CLAIM_PERMISSIONS_VIEW_USERS);

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByUserId(userId)).ReturnsAsync(new Account {Id = userId});

            var response = await browser.Get(UsersModule.GetUserByIdUrl.Replace("{id}", userId.ToString()), with =>
            {
                with.Accept(format);
                with.Header("Authorization", $"Bearer {accessToken}");
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = response.Body.AsDataFormat<UserProfile>(format);
            Assert.Equal(userId, result.Id);
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task GetProfileByUserId_NotFound(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(builder =>
                builder.RegisterType<TokenGenerator>().As<ITokenGenerator>().SingleInstance());
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var userId = 1;
            var accessToken = TestContainerBuilder.BuildToken(container, userId, TokenClaims.CLAIM_PERMISSIONS_VIEW_USERS);

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByUserId(userId)).ReturnsAsync((Account)null);

            var response = await browser.Get(UsersModule.GetUserByIdUrl.Replace("{id}", userId.ToString()), with =>
            {
                with.Accept(format);
                with.Header("Authorization", $"Bearer {accessToken}");
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task GetProfilesByFilter_Success(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(builder =>
                builder.RegisterType<TokenGenerator>().As<ITokenGenerator>().SingleInstance());
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var userId = 1;
            var accessToken = TestContainerBuilder.BuildToken(container, userId, TokenClaims.CLAIM_PERMISSIONS_VIEW_PROFILE);

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByUserIds(It.IsAny<IEnumerable<int>>())).Returns<IEnumerable<int>>(ids => Task.FromResult(ids.Select(id => new Account {Id=id}).ToArray()));

            var response = await browser.Post(SmsModule.GetUsersByFilterUrl, with =>
            {
                with.Accept(format);
                with.Header("Authorization", $"Bearer {accessToken}");

                with.DataFormatBody(format, new UserProfileFilter
                {
                    UserIds = Enumerable.Range(1, 10).ToList()
                });
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = response.Body.AsArrayDataFormat<UserProfile>(format);
            Assert.Equal(10, result.Length);
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        public async Task GetProfilesByFilter_BadRequest(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(builder =>
                builder.RegisterType<TokenGenerator>().As<ITokenGenerator>().SingleInstance());
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var userId = 1;
            var accessToken = TestContainerBuilder.BuildToken(container, userId, TokenClaims.CLAIM_PERMISSIONS_VIEW_PROFILE);

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByUserIds(It.IsAny<IEnumerable<int>>())).Returns<IEnumerable<int>>(ids => Task.FromResult(ids.Select(id => new Account { Id = id }).ToArray()));

            var response = await browser.Post(SmsModule.GetUsersByFilterUrl, with =>
            {
                with.Accept(format);
                with.Header("Authorization", $"Bearer {accessToken}");
                with.JsonBody("{userIds:['qwe','rty']}");
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
