﻿using System.Threading.Tasks;
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
using Newtonsoft.Json;
using System;
using System.Security.Claims;

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

            var smsRequestMock = Mock.Get(container.Resolve<ISmsRequestRepository>());
            smsRequestMock.Verify(a => a.Save(It.IsAny<SmsRequest>()), Times.Once());

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

        [Fact]
        public async Task Login_Success()
        {
            var container = TestModuleTests.GetContainer(b =>
            {
                b.RegisterInstance(new Mock<IAuthGenerator>().Object).As<IAuthGenerator>().SingleInstance();
            });
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var phone = 79000000000;
            var password = "password";

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByLogin(phone.ToString())).ReturnsAsync(new Account {Id = 1, Login = phone.ToString(), Phone = phone, Password = password});

            var permissionMock = Mock.Get(container.Resolve<IPermissionRepository>());
            permissionMock.Setup(a => a.GetByUserId(It.IsAny<int>())).ReturnsAsync(new[] {"test1", "test2"});

            var tokenMock = Mock.Get(container.Resolve<ITokenRegistrationRepository>());
            tokenMock.Setup(a => a.FindLastTokenByUserId(It.IsAny<int>())).ReturnsAsync((TokenRegistration) null);

            var authGenerator = Mock.Get(container.Resolve<IAuthGenerator>());
            authGenerator.Setup(a => a.CheckPassword(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns<string, string, string>((pasd, hash, salt) => pasd == hash);
            authGenerator.Setup(a => a.GenerateAccessToken(It.IsAny<int>(), It.IsAny<string[]>())).Returns<int, string[]>((userId, perms) => userId + string.Join("", perms));
            authGenerator.Setup(a => a.GenerateRefreshToken(It.IsAny<int>(), It.IsAny<int>())).Returns<int, int>((tid, userId) => $"{tid}_{userId}");

            var result = await browser.Post(SmsModule.LoginUrl, with =>
            {
                with.Accept("application/json");
                with.JsonBody(new SmsLoginRequest {Phone = phone.ToString(), Password = password});
            });

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            tokenMock.Verify(t => t.Save(It.IsAny<TokenRegistration>()), Times.Once);

            var response = JsonConvert.DeserializeObject<AccessTokenResponse>(result.Body.AsString());
            Assert.Equal(1, response.Profile.Id);
            Assert.Equal(phone, response.Profile.Phone);
            Assert.Equal("1test1test2", response.AccessToken);
            Assert.Equal("0_1", response.RefreshToken);
        }

        [Fact]
        public async Task Login_Register_NoRequest()
        {
            var container = TestModuleTests.GetContainer(b =>
            {
                b.RegisterInstance(new Mock<IAuthGenerator>().Object).As<IAuthGenerator>().SingleInstance();
            });
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var phone = 79000000000;
            var password = "password";

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByLogin(phone.ToString())).ReturnsAsync((Account) null);

            var smsRequestMock = Mock.Get(container.Resolve<ISmsRequestRepository>());
            smsRequestMock.Setup(a => a.Pick(phone)).ReturnsAsync((SmsRequest) null);

            var result = await browser.Post(SmsModule.LoginUrl, with =>
            {
                with.Accept("application/json");
                with.JsonBody(new SmsLoginRequest { Phone = phone.ToString(), Password = password });
            });

            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task Login_Register_ExpiredRequest()
        {
            var container = TestModuleTests.GetContainer(b =>
            {
                b.RegisterInstance(new Mock<IAuthGenerator>().Object).As<IAuthGenerator>().SingleInstance();
            });
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var phone = 79000000000;
            var password = "password";

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByLogin(phone.ToString())).ReturnsAsync((Account)null);

            var smsRequestMock = Mock.Get(container.Resolve<ISmsRequestRepository>());
            smsRequestMock.Setup(a => a.Pick(phone)).ReturnsAsync(new SmsRequest { Phone = phone, Password = password, ExpiredAt = DateTime.UtcNow.AddSeconds(-1)});

            var result = await browser.Post(SmsModule.LoginUrl, with =>
            {
                with.Accept("application/json");
                with.JsonBody(new SmsLoginRequest { Phone = phone.ToString(), Password = password });
            });

            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            smsRequestMock.Verify(r => r.Remove(phone), Times.Once);
        }

        [Fact]
        public async Task ChangePassword_Account()
        {
            var container = TestModuleTests.GetContainer(b =>
            {
                b.RegisterInstance(new Mock<IAuthGenerator>().Object).As<IAuthGenerator>().SingleInstance();
            });
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var userId = 1;
            var phone = 79000000000;
            var password = "password";
            var newpassword = "newpassword";
            var salt = "salt";

            var contextMock = Mock.Get(container.Resolve<IRequestContext>());
            contextMock.Setup(a => a.UserId).Returns(userId);

            var account = new Account {Id = userId, Login = phone.ToString(), Phone = phone, Password = password};
            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByUserId(1)).ReturnsAsync(account);

            var authGenerator = Mock.Get(container.Resolve<IAuthGenerator>());
            authGenerator.Setup(a => a.CheckPassword(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns<string, string, string>((p, h, s) => p == h);
            authGenerator.Setup(a => a.GenerateSalt()).Returns(() => salt);
            authGenerator.Setup(a => a.HashPassword(It.IsAny<string>(), It.IsAny<string>())).Returns<string, string>((p, s) => p + s);

            var result = await browser.Post(SmsModule.ChangePasswordUrl, with =>
            {
                with.Accept("application/json");
                with.JsonBody(new ChangePasswordRequest { OldPassword = password, NewPassword = newpassword });
            });

            Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);

            accountMock.Verify(a => a.Update(It.IsAny<Account>()), Times.Once);
            Assert.Equal(newpassword + salt, account.Password);
            Assert.Equal(salt, account.Salt);
        }

        [Fact]
        public async Task ResetPassword_Success()
        {
            var container = TestModuleTests.GetContainer(b =>
            {
                b.RegisterInstance(new Mock<IAuthGenerator>().Object).As<IAuthGenerator>().SingleInstance();
            });
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var phone = 79000000000;
            var password = "password";

            var account = new Account { Id = 1, Login = phone.ToString(), Phone = phone, Password = password };
            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByPhone(phone)).ReturnsAsync(account);

            var messageTemplateMock = Mock.Get(container.Resolve<IMessageTemplateRepository>());
            messageTemplateMock.Setup(a => a.GetRequestResetTemplate()).ReturnsAsync("{0}_test");

            var authGenerator = Mock.Get(container.Resolve<IAuthGenerator>());
            authGenerator.Setup(a => a.GeneratePassword()).Returns(() => password);

            var result = await browser.Post(SmsModule.RequestResetPasswordUrl, with =>
            {
                with.Accept("application/json");
                with.JsonBody(phone);
            });

            Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);

            var smsRequestMock = Mock.Get(container.Resolve<ISmsRequestRepository>());
            smsRequestMock.Verify(a => a.Save(It.IsAny<SmsRequest>()), Times.Once);

            var smsMock = Mock.Get(container.Resolve<ISmsClient>());
            smsMock.Verify(a => a.Send(phone, password + "_test"), Times.Once);
        }

        [Fact]
        public async Task ResetPassword_NotFound()
        {
            var container = TestModuleTests.GetContainer(b =>
            {
                b.RegisterInstance(new Mock<IAuthGenerator>().Object).As<IAuthGenerator>().SingleInstance();
            });
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var phone = 79000000000;
            var password = "password";

            var account = new Account { Id = 1, Login = phone.ToString(), Phone = phone, Password = password };
            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByPhone(phone)).ReturnsAsync((Account) null);

            var result = await browser.Post(SmsModule.RequestResetPasswordUrl, with =>
            {
                with.Accept("application/json");
                with.JsonBody(phone);
            });

            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        }

        [Fact]
        public async Task ForceChangePhone_Success()
        {
            var container = TestModuleTests.GetContainer(b =>
            {
                b.RegisterInstance(new Mock<IAuthGenerator>().Object).As<IAuthGenerator>().SingleInstance();
            });
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var userId = 1;
            var phone = 79000000000;
            var newphone = 79000000001;

            var account = new Account { Id = 1, Login = phone.ToString(), Phone = phone };
            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByUserId(userId)).ReturnsAsync(account);

            var result = await browser.Post(SmsModule.ForceChangePhoneUrl, with =>
            {
                with.Accept("application/json");
                with.JsonBody(new ChangePhoneRequest {UserId = userId, NewPhone = newphone});
            });

            Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
            Assert.Equal(newphone, account.Phone);
            Assert.Equal(newphone.ToString(), account.Login);
            accountMock.Verify(a => a.Update(It.IsAny<Account>()), Times.Once);
        }

        [Fact]
        public async Task ForceChangePhone_NotFound()
        {
            var container = TestModuleTests.GetContainer(b =>
            {
                b.RegisterInstance(new Mock<IAuthGenerator>().Object).As<IAuthGenerator>().SingleInstance();
            });
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var userId = 1;
            var newphone = 79000000001;

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByUserId(userId)).ReturnsAsync((Account) null);

            var result = await browser.Post(SmsModule.ForceChangePhoneUrl, with =>
            {
                with.Accept("application/json");
                with.JsonBody(new ChangePhoneRequest { UserId = userId, NewPhone = newphone });
            });

            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        }

        [Fact]
        public async Task DoesUserExists_IsTrue()
        {
            var container = TestModuleTests.GetContainer(b =>
            {
                b.RegisterInstance(new Mock<IAuthGenerator>().Object).As<IAuthGenerator>().SingleInstance();
            });
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var phone = 79000000000;

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByPhone(phone)).ReturnsAsync(new Account());

            var result = await browser.Get(SmsModule.DoesUserExistUrl, with =>
            {
                with.Accept("application/json");
                with.Query(nameof(phone), phone.ToString());
            });

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.True(bool.Parse(result.Body.AsString()));
        }

        [Fact]
        public async Task DoesUserExists_IsFalse()
        {
            var container = TestModuleTests.GetContainer(b =>
            {
                b.RegisterInstance(new Mock<IAuthGenerator>().Object).As<IAuthGenerator>().SingleInstance();
            });
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var phone = 79000000000;

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByPhone(phone)).ReturnsAsync((Account)null);

            var result = await browser.Get(SmsModule.DoesUserExistUrl, with =>
            {
                with.Accept("application/json");
                with.Query(nameof(phone), phone.ToString());
            });

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.False(bool.Parse(result.Body.AsString()));
        }

        [Fact]
        public async Task GetPhoneByUserId_Success()
        {
            var container = TestModuleTests.GetContainer(b =>
            {
                b.RegisterInstance(new Mock<IAuthGenerator>().Object).As<IAuthGenerator>().SingleInstance();
            });
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var phone = 79000000000;
            var id = 1;

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByUserId(id)).ReturnsAsync(new Account {Id = id, Phone = phone});

            var result = await browser.Get(SmsModule.PhoneByUserIdUrl, with =>
            {
                with.Accept("application/json");
                with.Query(nameof(id), id.ToString());
            });

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal(phone.ToString(), result.Body.AsString());
        }

        [Fact]
        public async Task GetPhoneByUserId_NotFound()
        {
            var container = TestModuleTests.GetContainer(b =>
            {
                b.RegisterInstance(new Mock<IAuthGenerator>().Object).As<IAuthGenerator>().SingleInstance();
            });
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var id = 1;

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByUserId(id)).ReturnsAsync((Account) null);

            var result = await browser.Get(SmsModule.PhoneByUserIdUrl, with =>
            {
                with.Accept("application/json");
                with.Query(nameof(id), id.ToString());
            });

            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        }

        [Fact]
        public async Task Refresh_Success()
        {
            var container = TestModuleTests.GetContainer(b =>
            {
                b.RegisterInstance(new Mock<IAuthGenerator>().Object).As<IAuthGenerator>().SingleInstance();
            });
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var userId = 101;
            var tid = 1001;
            var refreshToken = "refreshToken123123";
            var accessToken = "test1,test2,test3";

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByUserId(userId)).ReturnsAsync(new Account {Id = userId});

            var passwordMock = Mock.Get(container.Resolve<IAuthGenerator>());
            passwordMock.Setup(p => p.GetTid(refreshToken)).Returns(tid);

            var authGenerator = Mock.Get(container.Resolve<IAuthGenerator>());
            authGenerator.Setup(a => a.Parse(It.IsAny<string>())).Returns<string>(token =>
                new ClaimsPrincipal(new ClaimsIdentity(token.Split(',').Select(r => new Claim(ClaimTypes.Role, r)))));
            authGenerator.Setup(a => a.GenerateAccessToken(It.IsAny<int>(), It.IsAny<string[]>()))
                .Returns<int, string[]>((uid, roles) => $"{uid}_{string.Join("_", roles)}");

            var tokenMock = Mock.Get(container.Resolve<ITokenRegistrationRepository>());
            tokenMock.Setup(a => a.FindById(tid)).ReturnsAsync(new TokenRegistration
            {
                Id = tid,
                AccessToken = accessToken,
                UserId = userId
            });

            var result = await browser.Get(SmsModule.RefreshUrl.Replace("{refreshToken}", refreshToken), with =>
            {
                with.Accept("application/json");
            });

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var response = JsonConvert.DeserializeObject<AccessTokenResponse>(result.Body.AsString());
            Assert.Equal(userId, response.Profile.Id);
            Assert.Equal("101_test1_test2_test3", response.AccessToken);
            Assert.Equal(refreshToken, response.RefreshToken);
        }

        [Fact]
        public async Task Refresh_Account_NotFound()
        {
            var container = TestModuleTests.GetContainer(b =>
            {
                b.RegisterInstance(new Mock<IAuthGenerator>().Object).As<IAuthGenerator>().SingleInstance();
            });
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var userId = 101;
            var tid = 1001;
            var refreshToken = "refreshToken123123";
            var accessToken = "test1,test2,test3";

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByUserId(userId)).ReturnsAsync((Account) null);

            var authGenerator = Mock.Get(container.Resolve<IAuthGenerator>());
            authGenerator.Setup(p => p.GetTid(refreshToken)).Returns(tid);

            var tokenMock = Mock.Get(container.Resolve<ITokenRegistrationRepository>());
            tokenMock.Setup(a => a.FindById(tid)).ReturnsAsync(new TokenRegistration
            {
                Id = tid,
                AccessToken = accessToken,
                UserId = userId
            });

            var result = await browser.Get(SmsModule.RefreshUrl.Replace("{refreshToken}", refreshToken), with =>
            {
                with.Accept("application/json");
            });

            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        }

        [Fact]
        public async Task Refresh_TokenRegistry_NotFound()
        {
            var container = TestModuleTests.GetContainer(b =>
            {
                b.RegisterInstance(new Mock<IAuthGenerator>().Object).As<IAuthGenerator>().SingleInstance();
            });
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var userId = 101;
            var tid = 1001;
            var refreshToken = "refreshToken123123";

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByUserId(userId)).ReturnsAsync(new Account { Id = userId });

            var authGenerator = Mock.Get(container.Resolve<IAuthGenerator>());
            authGenerator.Setup(p => p.GetTid(refreshToken)).Returns(tid);

            var result = await browser.Get(SmsModule.RefreshUrl.Replace("{refreshToken}", refreshToken), with =>
            {
                with.Accept("application/json");
            });

            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        }

        [Fact]
        public async Task GetMyProfile_Success()
        {
            var container = TestModuleTests.GetContainer(b =>
            {
                b.RegisterInstance(new Mock<IAuthGenerator>().Object).As<IAuthGenerator>().SingleInstance();
            });
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var phone = 79000000000;
            var userId = 1;

            var requestMock = Mock.Get(container.Resolve<IRequestContext>());
            requestMock.Setup(a => a.UserId).Returns(userId);

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByUserId(userId)).ReturnsAsync(new Account {Id = userId, Phone = phone});

            var result = await browser.Get(SmsModule.GetMyProfileUrl, with =>
            {
                with.Accept("application/json");
            });

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var response = JsonConvert.DeserializeObject<UserProfile>(result.Body.AsString());
            Assert.Equal(userId, response.Id);
            Assert.Equal(phone, response.Phone);
        }

        [Fact]
        public async Task GetProfileByPhone_Success()
        {
            var container = TestModuleTests.GetContainer(b =>
            {
                b.RegisterInstance(new Mock<IAuthGenerator>().Object).As<IAuthGenerator>().SingleInstance();
            });
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var phone = 79000000000;
            var userId = 1;

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByPhone(phone)).ReturnsAsync(new Account { Id = userId, Phone = phone });

            var result = await browser.Get(SmsModule.GetUserByPhoneUrl.Replace("{phone}", phone.ToString()), with =>
            {
                with.Accept("application/json");
            });

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var response = JsonConvert.DeserializeObject<UserProfile>(result.Body.AsString());
            Assert.Equal(userId, response.Id);
            Assert.Equal(phone, response.Phone);
        }

        [Fact]
        public async Task GetProfileByPhone_NotFound()
        {
            var container = TestModuleTests.GetContainer(b =>
            {
                b.RegisterInstance(new Mock<IAuthGenerator>().Object).As<IAuthGenerator>().SingleInstance();
            });
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var phone = 79000000000;

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByPhone(phone)).ReturnsAsync((Account) null);

            var result = await browser.Get(SmsModule.GetUserByPhoneUrl.Replace("{phone}", phone.ToString()), with =>
            {
                with.Accept("application/json");
            });

            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        }
    }
}
