using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Domain0.Model;
using Domain0.Nancy;
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
    public class EmailModuleTests
    {
        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task RequestChangeEmail_Success(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(builder =>
                builder.RegisterType<SymmetricKeyTokenGenerator>().As<ITokenGenerator>().SingleInstance());

            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var accountId = 321;
            var userId = 1;
            var email = "email";
            var newEmail = "newEmail";
            var password = "123";
            var pin = "333";
            var accessToken = TestContainerBuilder.BuildToken(container, userId, TokenClaims.CLAIM_PERMISSIONS_BASIC);

            var account = new Account { Id = accountId, Login = email, Email = email, Password = password };
            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByUserId(userId)).ReturnsAsync(account);

            var emailRequestRepository = Mock.Get(container.Resolve<IEmailRequestRepository>());
            emailRequestRepository
                .Setup(sr => sr.PickByUserId(
                    It.Is<int>(x => x == userId)))
                .ReturnsAsync(new EmailRequest
                {
                    Email = newEmail,
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
                    if (n == MessageTemplateName.RequestEmailChangeTemplate)
                        return Task.FromResult("Your password is: {0} will valid for {1} min");

                    if (n == MessageTemplateName.RequestEmailChangeSubjectTemplate)
                        return Task.FromResult("Subject");

                    throw new NotImplementedException();
                });

            var emailClient = Mock.Get(container.Resolve<IEmailClient>());

            var responseToChangeRequest = await browser.Post(
                EmailModule.RequestChangeEmailUrl,
                with =>
                {
                    with.Accept(format);
                    with.Header("Authorization", $"Bearer {accessToken}");
                    with.DataFormatBody(format,
                        new ChangeEmailUserRequest
                        {
                            Password = password,
                            Email = newEmail
                        });
                });
            Assert.Equal(HttpStatusCode.NoContent, responseToChangeRequest.StatusCode);

            authGenerator.Verify(ag =>
                ag.CheckPassword(
                    It.Is<string>(x => x == password),
                    It.Is<string>(x => x == password)),
                Times.Once);

            emailRequestRepository.Verify(
                srr => srr.Save(
                    It.Is<EmailRequest>(r =>
                        r.UserId == userId
                        && r.Email == newEmail
                        && r.Password == pin)),
                Times.Once);

            messageTemplate.Verify(mtr =>
                mtr.GetTemplate(
                    It.Is<MessageTemplateName>(mt => mt == MessageTemplateName.RequestEmailChangeTemplate),
                    It.IsAny<CultureInfo>(),
                    It.Is<MessageTemplateType>(mtt => mtt == MessageTemplateType.email),
                    It.IsAny<int>()),
                Times.Once);

            emailClient.Verify(sc =>
                sc.Send(
                    It.IsAny<string>(),
                    It.Is<string>(x => x == newEmail),
                    It.IsAny<string>()),
                Times.Once);


            var responseToCommitRequest = await browser.Post(
                EmailModule.CommitChangeEmailUrl,
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
                        && a.Login == newEmail
                        && a.Email == newEmail)),
                    Times.Once);
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
            var email = "email";
            var password = "password";
            var accessToken = TestContainerBuilder.BuildToken(container, userId, TokenClaims.CLAIM_PERMISSIONS_FORCE_PASSWORD_RESET);

            var tokenRegistrationRepository = container.Resolve<ITokenRegistrationRepository>();
            var tokenRegistrationMock = Mock.Get(tokenRegistrationRepository);

            var accountRepository = container.Resolve<IAccountRepository>();
            var accountMock = Mock.Get(accountRepository);
            accountMock
                .Setup(a => a.FindByLogin(email))
                .ReturnsAsync(new Account
                {
                    Id = userId,
                    Email = email,
                    Login = email,
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
                    MessageTemplateName.ForcePasswordResetTemplate,
                    It.IsAny<CultureInfo>(),
                    It.IsAny<MessageTemplateType>(),
                    It.IsAny<int>())
                )
                .ReturnsAsync("hello, your new password is {0}!");
            messageTemplate.Setup(r =>
                    r.GetTemplate(
                        MessageTemplateName.ForcePasswordResetSubjectTemplate,
                        It.IsAny<CultureInfo>(),
                        It.IsAny<MessageTemplateType>(),
                        It.IsAny<int>()))
                .ReturnsAsync("subject");

            var response = await browser.Post(
                UsersModule.ForceResetUserPasswordUrl,
                with =>
                {
                    with.Accept(format);
                    with.Header("Authorization", $"Bearer {accessToken}");
                    with.DataFormatBody(
                        format,
                        new ForceResetPasswordRequest
                        {
                            Email = email
                        });
                });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            accountMock.Verify(am => am.FindByLogin(email));
            accountMock.Verify(am => am.Update(
                It.Is<Account>(a => a.Password == password)));
            messageTemplate.Verify(mt => mt.GetTemplate(
                MessageTemplateName.ForcePasswordResetTemplate,
                It.IsAny<CultureInfo>(),
                MessageTemplateType.email,
                It.IsAny<int>()));
            messageTemplate.Verify(mt => mt.GetTemplate(
                MessageTemplateName.ForcePasswordResetSubjectTemplate,
                It.IsAny<CultureInfo>(),
                MessageTemplateType.email,
                It.IsAny<int>()));

            var emaClient = container.Resolve<IEmailClient>();
            var smsMock = Mock.Get(emaClient);
            smsMock.Verify(s =>
                s.Send("subject", email, "hello, your new password is password!"));

            tokenRegistrationMock.Verify(x => x.RevokeByUserId(userId), Times.Once);
        }


        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task ForceCreateUser_SendEmail_StandardTemplate(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(builder =>
                builder.RegisterType<SymmetricKeyTokenGenerator>().As<ITokenGenerator>().SingleInstance());
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var userId = 1;
            var email = "email";
            var roles = new List<string> { "role1", "role2" };
            var accessToken = TestContainerBuilder.BuildToken(container, userId, TokenClaims.CLAIM_PERMISSIONS_FORCE_CREATE_USER);

            var accountRepository = container.Resolve<IAccountRepository>();
            var accountMock = Mock.Get(accountRepository);
            var registers = new Dictionary<string, Account>();
            accountMock
                .Setup(a => a.Insert(It.IsAny<Account>()))
                .Callback<Account>(a => registers[a.Email] = a)
                .Returns(Task.FromResult(1));
            accountMock
                .Setup(a => a.FindByLogin(email))
                .Returns<string>(p => 
                    Task.FromResult(registers.TryGetValue(p, out var acc) 
                        ? acc 
                        : null));

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
                    MessageTemplateName.WelcomeTemplate,
                    It.IsAny<CultureInfo>(),
                    It.IsAny<MessageTemplateType>(),
                    It.IsAny<int>()))
                .ReturnsAsync("hello {1} {0}!");
            messageTemplate.Setup(r =>
                    r.GetTemplate(
                        MessageTemplateName.WelcomeSubjectTemplate,
                        It.IsAny<CultureInfo>(),
                        It.IsAny<MessageTemplateType>(),
                        It.IsAny<int>()))
                .ReturnsAsync("Subject!");

            var response = await browser.Put(
                EmailModule.ForceCreateUserUrl, 
                with =>
                {
                    with.Accept(format);
                    with.Header("Authorization", $"Bearer {accessToken}");
                    with.DataFormatBody(
                        format, 
                        new ForceCreateEmailUserRequest
                        {
                            BlockEmailSend = false,
                            Email = email,
                            Name = "test",
                            Roles = roles,
                        });
                });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var emailClient = container.Resolve<IEmailClient>();
            var emailMock = Mock.Get(emailClient);
            emailMock.Verify(s => s.Send("Subject!", email, "hello password " + email + "!"));

            var environmentRepositoryMock = Mock.Get(container.Resolve<IEnvironmentRepository>());
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

            var email = "test@email.com";
            var envToken = "EnvironmentToken";

            var accountRepository = container.Resolve<IAccountRepository>();
            var accountMock = Mock.Get(accountRepository);
            accountMock.Setup(a => a.FindByLogin(email)).ReturnsAsync((Account)null);

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

                    if (n == MessageTemplateName.RegisterSubjectTemplate)
                        return Task.FromResult("subject");

                    if (n == MessageTemplateName.WelcomeSubjectTemplate)
                        return Task.FromResult("subject");

                    if (n == MessageTemplateName.WelcomeTemplate)
                        return Task.FromResult("Hello {0}!");

                    throw new NotImplementedException();
                });

            var passwordGenerator = container.Resolve<IPasswordGenerator>();
            var passwordMock = Mock.Get(passwordGenerator);
            passwordMock.Setup(p => p.GeneratePassword()).Returns("password");

            var emailRequestMock = Mock.Get(container.Resolve<IEmailRequestRepository>());

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

            var registerResponse = await browser.Put(
                EmailModule.RegisterByEmailWithEnvironmentUrl.Replace("{EnvironmentToken}", envToken),
                with =>
                {
                    with.Accept(format);
                    with.DataFormatBody(format, new RegisterRequest
                    {
                        Email = email
                    });
                });

            Assert.Equal(HttpStatusCode.NoContent, registerResponse.StatusCode);
            environmentRepositoryMock
                .Verify(callTo => 
                    callTo.GetByToken(It.Is<string>(t => t.Equals(envToken))), 
                    Times.Once);
            emailRequestMock.Verify(a => a.Save(It.Is<EmailRequest>(r => r.EnvironmentId == env.Id)), Times.Once());

            emailRequestMock
                .Setup(x => x.ConfirmRegister(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new EmailRequest
                {
                    EnvironmentId = env.Id
                });
            var smsClient = container.Resolve<IEmailClient>();
            var emailMock = Mock.Get(smsClient);
            emailMock.Verify(s => s.Send("subject", email, "Your password is: password will valid for 120 min"));

            var firstLoginResponse = await browser.Post(EmailModule.LoginByEmailUrl,
                with =>
                {
                    with.Accept(format);
                    with.DataFormatBody(format,
                        new EmailLoginRequest { Email = email, Password = "password" });
                });

            Assert.Equal(HttpStatusCode.OK, firstLoginResponse.StatusCode);

            environmentRepositoryMock
                .Verify(
                    callTo => callTo.SetUserEnvironment(It.IsAny<int>(), env.Id.Value), 
                    Times.Once);
            accountMock.Verify(x => x.Insert(It.IsAny<Account>()), Times.Once);
        }
    }
}
