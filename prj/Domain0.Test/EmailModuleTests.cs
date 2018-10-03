using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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
                builder.RegisterType<TokenGenerator>().As<ITokenGenerator>().SingleInstance());

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
                    It.IsAny<MessageTemplateType>()))
                .Returns<MessageTemplateName, CultureInfo, MessageTemplateType>((n, l, t) =>
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
                    It.Is<MessageTemplateType>(mtt => mtt == MessageTemplateType.email)),
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
    }
}
