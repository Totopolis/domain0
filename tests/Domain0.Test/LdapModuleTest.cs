using System;
using System.Threading.Tasks;
using Autofac;
using Domain0.Model;
using Domain0.Nancy;
using Domain0.Nancy.Model;
using Domain0.Nancy.Service.Ldap;
using Domain0.Repository;
using Domain0.Repository.Model;
using Domain0.Service;
using Moq;
using Nancy;
using Nancy.Testing;
using Xunit;

namespace Domain0.Test
{
    public class LdapModuleTest
    {
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

            var user = "user";
            var password = "password";
            var email = "user@domain.ru";

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByLogin(email)).ReturnsAsync(new Account {Id = 1, Login = email, Email = email, Password = password});

            var ldapMock = Mock.Get(container.Resolve<ILdapClient>());
            ldapMock.Setup(c => c.Authorize(user, password))
                .Returns(Task.FromResult(new LdapUser {Email = email}));

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

            var response = await browser.Post(LdapModule.LoginByDomainUserUrl, with =>
            {
                with.Accept(format);
                with.DataFormatBody(format, new ActiveDirectoryUserLoginRequest {UserName = user, Password = password});
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            tokenMock.Verify(t => t.Save(It.IsAny<TokenRegistration>()), Times.Once);
            accountMock.Verify(t=>t.FindByLogin(email));
            ldapMock.Verify(l=>l.Authorize(user, password));

            var result = response.Body.AsDataFormat<AccessTokenResponse>(format);
            Assert.Equal(1, result.Profile.Id);
            Assert.Equal(email, result.Profile.Email);
            Assert.Equal("1test1test2", result.AccessToken);
            Assert.Equal("0_1", result.RefreshToken);
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task LoginAndRegister_Success(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(builder =>
            {
                builder
                    .RegisterInstance(new Mock<ITokenGenerator>().Object)
                    .Keyed<ITokenGenerator>("HS256");

            });
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var user = "user";
            var password = "password";
            var email = "user@domain.ru";

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByLogin(email)).ReturnsAsync((Account)null);
            accountMock.Setup(a => a.Insert(It.IsAny<Account>())).ReturnsAsync(1);

            var ldapMock = Mock.Get(container.Resolve<ILdapClient>());
            ldapMock.Setup(c => c.Authorize(user, password))
                .Returns(Task.FromResult(new LdapUser {Email = email}));

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

            var response = await browser.Post(LdapModule.LoginByDomainUserUrl, with =>
            {
                with.Accept(format);
                with.DataFormatBody(format, new ActiveDirectoryUserLoginRequest {UserName = user, Password = password});
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            tokenMock.Verify(t => t.Save(It.IsAny<TokenRegistration>()), Times.Once);
            accountMock.Verify(t=>t.FindByLogin(email));
            accountMock.Verify(t=>t.Insert(It.IsAny<Account>()));
            ldapMock.Verify(l=>l.Authorize(user, password));

            var result = response.Body.AsDataFormat<AccessTokenResponse>(format);
            Assert.Equal(1, result.Profile.Id);
            Assert.Equal(email, result.Profile.Email);
            Assert.Equal("1test1test2", result.AccessToken);
            Assert.Equal("0_1", result.RefreshToken);
        }


        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task Login_Negative(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer();

            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var user = "user";
            var password = "password";


            var ldapMock = Mock.Get(container.Resolve<ILdapClient>());
            ldapMock.Setup(c => c.Authorize(user, password))
                .Returns(Task.FromResult((LdapUser) null));


            var response = await browser.Post(LdapModule.LoginByDomainUserUrl, with =>
            {
                with.Accept(format);
                with.DataFormatBody(format, new ActiveDirectoryUserLoginRequest {UserName = user, Password = password});
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            ldapMock.Verify(l=>l.Authorize(user, password));
        }
    }
}