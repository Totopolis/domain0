using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Domain0.Nancy;
using Xunit;
using Autofac;
using Domain0.Repository;
using Moq;
using Domain0.Repository.Model;
using Nancy.Hosting.Self;
using Domain0.Api.Client;
using Domain0.Service;
using Domain0.Service.Tokens;
using Application = Domain0.Repository.Model.Application;

namespace Domain0.Test
{
    public class Domain0ClientTests : IDisposable
    {
        private readonly NancyHost host;
        private readonly IContainer container;
        private const string TestUrl =  "http://localhost:51234";

        public Domain0ClientTests()
        {
            container = TestContainerBuilder.GetContainer(
                builder =>
                {
                    builder.RegisterType<SymmetricKeyTokenGenerator>().As<ITokenGenerator>().SingleInstance();
                });

            var bootstrapper = new Domain0Bootstrapper(container);
            host = BuildTestNancyHost(bootstrapper);
            host.Start();
        }

        [Fact]
        public async Task Sms_ClientRegistrationTest()
        {
            var phone = 79000000000;

            var smsRequestRepository = container.Resolve<ISmsRequestRepository>();
            var smsRequestRepositoryMock = Mock.Get(smsRequestRepository);
            smsRequestRepositoryMock.Setup(a => a.Pick(phone))
                .ReturnsAsync(() => new SmsRequest()
                {
                    ExpiredAt = DateTime.UtcNow.AddHours(1)
                });

            var messageTemplateRepository = container.Resolve<IMessageTemplateRepository>();
            var messageTemplateMock = Mock.Get(messageTemplateRepository);
            messageTemplateMock
                .Setup(r => r.GetTemplate(
                    It.IsAny<MessageTemplateName>(),
                    It.IsAny<CultureInfo>(),
                    It.IsAny<MessageTemplateType>()))
                .ReturnsAsync("Your password is: {0} will valid for {1} min");

            using (var http = new HttpClient())
            {
                var client = new Domain0Client(TestUrl, http);
                await client.RegisterAsync(phone);
            }

            smsRequestRepositoryMock.Verify(t => t.Pick(It.IsAny<decimal>()), Times.Once);
        }

        [Fact]
        public async Task Email_ClientRegistrationTest()
        {
            var testEmail = "email";
            var emailRequestRepository = container.Resolve<IEmailRequestRepository>();
            var emailRequestRepositoryMock = Mock.Get(emailRequestRepository);
            emailRequestRepositoryMock.Setup(a => a.Pick(It.IsAny<string>()))
                .ReturnsAsync(() => new EmailRequest()
                {
                    ExpiredAt = DateTime.UtcNow.AddHours(1)
                });

            var messageTemplateRepository = container.Resolve<IMessageTemplateRepository>();
            var messageTemplateMock = Mock.Get(messageTemplateRepository);
            messageTemplateMock
                .Setup(r => r.GetTemplate(
                    It.IsAny<MessageTemplateName>(),
                    It.IsAny<CultureInfo>(),
                    It.IsAny<MessageTemplateType>()))
                .ReturnsAsync("Your password is: {0} will valid for {1} min");

            using (var http = new HttpClient())
            {
                var client = new Domain0Client(TestUrl, http);
                await client.RegisterByEmailAsync(new RegisterRequest(testEmail));
            }

            emailRequestRepositoryMock.Verify(t => t.Pick(It.Is<string>(e => e == testEmail)), Times.Once);
        }

        [Fact]
        public async Task Email_ClientLoginTest()
        {
            var testEmail = "email";
            var testPassword = "password";

            var passwordGenerator = container.Resolve<IPasswordGenerator>();
            var passwordGeneratorMock = Mock.Get(passwordGenerator);
            passwordGeneratorMock.Setup(p => p.HashPassword(It.IsAny<string>())).Returns(testPassword);
            passwordGeneratorMock.Setup(p => p.CheckPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);


            var permissionRepository = container.Resolve<IPermissionRepository>();
            var permissionRepositoryMock = Mock.Get(permissionRepository);

            permissionRepositoryMock
                .Setup(p => p.GetByUserId(It.IsAny<int>()))
                .ReturnsAsync(new[] { new Repository.Model.Permission() });

            var accountRepository = container.Resolve<IAccountRepository>();
            var accountRepositoryMock = Mock.Get(accountRepository);
            accountRepositoryMock
                .Setup(x => x.FindByLogin(It.IsAny<string>()))
                .Returns<string>(x =>
                    Task.FromResult(new Account { Login = x }));


            using (var http = new HttpClient())
            {
                var client = new Domain0Client(TestUrl, http);
                await client.LoginByEmailAsync(new Api.Client.EmailLoginRequest(testEmail, testPassword));
            }

            accountRepositoryMock.Verify(t => t.FindByLogin(It.Is<string>(e => e == testEmail)), Times.Once);
            passwordGeneratorMock.Verify(t => t.HashPassword(It.Is<string>(p => p == testPassword)), Times.Once);
        }


        [Fact]
        public async Task Sms_ClientLoginTest()
        {
            var testPhone = "3579";
            var testPassword = "password";

            var passwordGenerator = container.Resolve<IPasswordGenerator>();
            var passwordGeneratorMock = Mock.Get(passwordGenerator);
            passwordGeneratorMock.Setup(p => p.HashPassword(It.IsAny<string>())).Returns(testPassword);
            passwordGeneratorMock.Setup(p => p.CheckPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            var accountRepository = container.Resolve<IAccountRepository>();
            var accountRepositoryMock = Mock.Get(accountRepository);
            accountRepositoryMock
                .Setup(x => x.FindByLogin(It.IsAny<string>()))
                .Returns<string>(x =>
                    Task.FromResult(new Account { Login = x }));


            var permissionRepository = container.Resolve<IPermissionRepository>();
            var permissionRepositoryMock = Mock.Get(permissionRepository);

            permissionRepositoryMock
                .Setup(p => p.GetByUserId(It.IsAny<int>()))
                .ReturnsAsync(new[] { new Repository.Model.Permission() });

            using (var http = new HttpClient())
            {
                var client = new Domain0Client(TestUrl, http);
                await client.LoginAsync(new SmsLoginRequest(testPassword, testPhone));
            }

            accountRepositoryMock.Verify(t => t.FindByLogin(It.Is<string>(e => e == testPhone.ToString())), Times.Once);
            passwordGeneratorMock.Verify(t => 
                t.CheckPassword(testPassword, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Sms_ClientForceCreateUserTest()
        {
            var testRequest = new ForceCreateUserRequest(
                true, "customTemplate", "userName", 123456, new List<string> { "1", "2", "3" });

            var accountRepository = container.Resolve<IAccountRepository>();
            var accountRepositoryMock = Mock.Get(accountRepository);
            accountRepositoryMock
                .Setup(s => s.FindByPhone(It.IsAny<decimal>()))
                .ReturnsAsync((Account)null);


            var roleRepository = container.Resolve<IRoleRepository>();
            var roleRepositoryMock = Mock.Get(roleRepository);
            roleRepositoryMock
                .Setup(s => s.GetByRoleNames(It.IsAny<string[]>()))
                .ReturnsAsync(testRequest.Roles.Select(x => new Repository.Model.Role(){ Name = x}).ToArray());


            using (var http = new HttpClient())
            {
                var client = new Domain0Client(TestUrl, http);
                http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer",
                        TestContainerBuilder.BuildToken(container, 1, TokenClaims.CLAIM_PERMISSIONS_FORCE_CREATE_USER));

                await client.ForceCreateUserAsync(testRequest);

                accountRepositoryMock.Verify(ar =>
                    ar.Insert(It.Is<Account>(a =>
                        a.Name == testRequest.Name
                        && a.Phone == testRequest.Phone)),
                    Times.Once());

                roleRepositoryMock.Verify(rr => 
                    rr.GetByRoleNames(It.Is<string[]>(s => testRequest.Roles.SequenceEqual(s))),
                    Times.Once);
            }
        }

        [Fact]
        public async Task Sms_ClientForceChangePhoneTest()
        {
            var testRequest = new ChangePhoneRequest(3579, 1);

            var accountRepository = container.Resolve<IAccountRepository>();
            var accountRepositoryMock = Mock.Get(accountRepository);
            accountRepositoryMock
                .Setup(s => s.FindByUserId(It.IsAny<int>()))
                .Returns<int>(x => Task.FromResult(new Account
                {
                    Id = x
                }));

            using (var http = new HttpClient())
            {
                var client = new Domain0Client(TestUrl, http);
                http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer",
                        TestContainerBuilder.BuildToken(container, 1, TokenClaims.CLAIM_PERMISSIONS_FORCE_CHANGE_PHONE));

                await client.ForceChangePhoneAsync(testRequest);


                accountRepositoryMock.Verify(ar =>
                        ar.Update(It.Is<Account>(a =>
                            a.Id == testRequest.UserId
                            && a.Phone == testRequest.NewPhone)),
                    Times.Once());
            }
        }

        [Fact]
        public async Task ClientGetPhoneByUserIdTest()
        {
            var testId = 1;
            var testPhone = 321;

            var accountRepository = container.Resolve<IAccountRepository>();
            var accountRepositoryMock = Mock.Get(accountRepository);
            accountRepositoryMock
                .Setup(s => s.FindByUserId(It.IsAny<int>()))
                .Returns<int>(x => Task.FromResult(new Account
                {
                    Id = x,
                    Phone = testPhone
                }));

            using (var http = new HttpClient())
            {
                var client = new Domain0Client(TestUrl, http);
                http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer",
                        TestContainerBuilder.BuildToken(container, 1, TokenClaims.CLAIM_PERMISSIONS_VIEW_USERS));

                var phone = await client.PhoneByUserIdAsync(testId);
                Assert.Equal(testPhone, phone);
                accountRepositoryMock.Verify(ar =>
                        ar.FindByUserId(It.Is<int>(id => id == testId)),
                    Times.Once());
            }
        }

        [Fact]
        public async Task ClientGetMyProfileTest()
        {
            var testId = 1;
            var testPhone = 321;

            var accountRepository = container.Resolve<IAccountRepository>();
            var accountRepositoryMock = Mock.Get(accountRepository);
            accountRepositoryMock
                .Setup(s => s.FindByUserId(It.IsAny<int>()))
                .Returns<int>(x => Task.FromResult(new Account
                {
                    Id = x,
                    Phone = testPhone
                }));

            using (var http = new HttpClient())
            {
                var client = new Domain0Client(TestUrl, http);
                http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer",
                        TestContainerBuilder.BuildToken(container, testId, "user"));

                var profile = await client.GetMyProfileAsync();
                Assert.Equal(testPhone, profile.Phone);
                Assert.Equal(testId, profile.Id);
                accountRepositoryMock.Verify(ar =>
                        ar.FindByUserId(It.Is<int>(id => id == testId)),
                    Times.Once());
            }
        }

        [Fact]
        public async Task Sms_ClientGetUserByPhoneTest()
        {
            var testId = 1;
            var testPhone = 321;

            var accountRepository = container.Resolve<IAccountRepository>();
            var accountRepositoryMock = Mock.Get(accountRepository);
            accountRepositoryMock
                .Setup(s => s.FindByPhone(It.IsAny<decimal>()))
                .Returns<decimal>(x => Task.FromResult(new Account
                {
                    Id = testId,
                    Phone = x
                }));

            using (var http = new HttpClient())
            {
                var client = new Domain0Client(TestUrl, http);
                http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer",
                        TestContainerBuilder.BuildToken(container, testId, TokenClaims.CLAIM_PERMISSIONS_VIEW_USERS));

                var profile = await client.GetUserByPhoneAsync(testPhone);
                Assert.Equal(testPhone, profile.Phone);
                Assert.Equal(testId, profile.Id);
                accountRepositoryMock.Verify(ar =>
                        ar.FindByPhone(It.Is<decimal>(phone => phone == testPhone)),
                    Times.Once());
            }
        }


        [Fact]
        public async Task Sms_ClientGetUserByIdTest()
        {
            var testId = 1;
            var testPhone = 321;

            var accountRepository = container.Resolve<IAccountRepository>();
            var accountRepositoryMock = Mock.Get(accountRepository);
            accountRepositoryMock
                .Setup(s => s.FindByUserId(It.IsAny<int>()))
                .Returns<int>(x => Task.FromResult(new Account
                {
                    Id = x,
                    Phone = testPhone
                }));

            using (var http = new HttpClient())
            {
                var client = new Domain0Client(TestUrl, http);
                http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer",
                        TestContainerBuilder.BuildToken(container, testId, TokenClaims.CLAIM_PERMISSIONS_VIEW_USERS));

                var profile = await client.GetUserByIdAsync(testId);
                Assert.Equal(testPhone, profile.Phone);
                Assert.Equal(testId, profile.Id);
                accountRepositoryMock.Verify(ar =>
                        ar.FindByUserId(It.Is<int>(id => id == testId)),
                    Times.Once());
            }
        }

        [Fact]
        public async Task Sms_ClientGetUserByFilterTest()
        {
            var filter = new UserProfileFilter(new List<int> {1, 2, 3});

            var accountRepository = container.Resolve<IAccountRepository>();
            var accountRepositoryMock = Mock.Get(accountRepository);
            accountRepositoryMock
                .Setup(s => s.FindByUserIds(It.IsAny<IEnumerable<int>>()))
                .Returns<IEnumerable<int>>(x => Task.FromResult(
                    x.Select(id => 
                        new Account
                        {
                            Id = id,
                            Phone = id
                        })
                    .ToArray()));

            using (var http = new HttpClient())
            {
                var client = new Domain0Client(TestUrl, http);
                http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer",
                        TestContainerBuilder.BuildToken(container, 1, TokenClaims.CLAIM_PERMISSIONS_VIEW_PROFILE));

                var profiles = await client.GetUserByFilterAsync(filter);
                Assert.True(profiles.Select(p => p.Id).SequenceEqual(filter.UserIds));

                accountRepositoryMock.Verify(ar =>
                        ar.FindByUserIds(It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(filter.UserIds))),
                    Times.Once());
            }
        }

        [Fact]
        public async Task ClientRefreshTest()
        {
            var userId = 101;
            var tid = 1001;

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByUserId(userId)).ReturnsAsync(new Account { Id = userId });

            var tokenGenerator = container.Resolve<ITokenGenerator>();
            var refreshToken = tokenGenerator.GenerateRefreshToken(tid, userId);
            var accessToken = tokenGenerator.GenerateAccessToken(userId, new [] {"1","2","3"});


            var tokenMock = Mock.Get(container.Resolve<ITokenRegistrationRepository>());
            tokenMock.Setup(a => a.FindById(tid)).ReturnsAsync(new TokenRegistration
            {
                Id = tid,
                AccessToken = accessToken,
                UserId = userId
            });

            using (var http = new HttpClient())
            {
                var client = new Domain0Client(TestUrl, http);

                var newToken = await client.RefreshAsync(refreshToken);

                var claims = tokenGenerator.Parse(newToken.AccessToken);

                Assert.NotNull(claims);

                tokenMock.Verify(ar =>
                        ar.FindById(It.Is<int>(t => t == tid)),
                    Times.Once());
            }
        }

        [Fact]
        public async Task Sms_ClientDoesUserExistTest()
        {
            var testPhone = 1123123;

            var accountRepository = container.Resolve<IAccountRepository>();
            var accountRepositoryMock = Mock.Get(accountRepository);
            accountRepositoryMock
                .Setup(x => x.FindByPhone(It.IsAny<decimal>()))
                .Returns<decimal>(x => x == testPhone 
                    ? Task.FromResult(new Account {Phone = x}) 
                    : Task.FromResult((Account) null));

            using (var http = new HttpClient())
            {
                var client = new Domain0Client(TestUrl, http);

                var isExists = await client.DoesUserExistAsync(testPhone);
                Assert.True(isExists);
                accountRepositoryMock.Verify(ar =>
                        ar.FindByPhone(It.Is<decimal>(p => p == testPhone)),
                    Times.Once);

                var isNotExists = await client.DoesUserExistAsync(12312);
                Assert.False(isNotExists);
                accountRepositoryMock.Verify(ar =>
                        ar.FindByPhone(It.Is<decimal>(p => p == 12312)),
                    Times.Once);
            }
        }

        [Fact]
        public async Task Email_ClientDoesUserExistTest()
        {
            var testEmail = new RegisterRequest("email");
            var notExistedEmail = new RegisterRequest("asdfa");

            var accountRepository = container.Resolve<IAccountRepository>();
            var accountRepositoryMock = Mock.Get(accountRepository);
            accountRepositoryMock
                .Setup(x => x.FindByLogin(It.IsAny<string>()))
                .Returns<string>(x => x == testEmail.Email
                    ? Task.FromResult(new Account { Login = x })
                    : Task.FromResult((Account)null));

            using (var http = new HttpClient())
            {
                var client = new Domain0Client(TestUrl, http);

                var isExists = await client.DoesUserExistByEmailAsync(testEmail);
                Assert.True(isExists);
                accountRepositoryMock.Verify(ar =>
                        ar.FindByLogin(It.Is<string>(p => p == testEmail.Email)),
                    Times.Once);

                var isNotExists = await client.DoesUserExistByEmailAsync(notExistedEmail);
                Assert.False(isNotExists);
                accountRepositoryMock.Verify(ar =>
                        ar.FindByLogin(It.Is<string>(p => p == notExistedEmail.Email)),
                    Times.Once);

            }
        }

        [Fact]
        public async Task Email_ClientForceChangeEmailTest()
        {
            var testRequest = new ChangeEmailRequest("email", 1);

            var accountRepository = container.Resolve<IAccountRepository>();
            var accountRepositoryMock = Mock.Get(accountRepository);
            accountRepositoryMock
                .Setup(s => s.FindByUserId(It.IsAny<int>()))
                .Returns<int>(x => Task.FromResult(new Account
                {
                    Id = x
                }));

            using (var http = new HttpClient())
            {
                var client = new Domain0Client(TestUrl, http);
                http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer",
                        TestContainerBuilder.BuildToken(container, 1, TokenClaims.CLAIM_PERMISSIONS_FORCE_CHANGE_EMAIL));

                await client.ForceChangeEmailAsync(testRequest);

                accountRepositoryMock.Verify(ar =>
                        ar.Update(It.Is<Account>(a =>
                            a.Id == testRequest.UserId
                            && a.Email == testRequest.NewEmail)),
                    Times.Once());
            }
        }


        [Fact]
        public async Task Admin_ClientRolePermissionOperationsTest()
        {
            var roleId = 333;
            var ids = new IdArrayRequest(new List<int> {1, 2, 3});

            var roleRepository = container.Resolve<IRoleRepository>();
            var roleRepositoryMock = Mock.Get(roleRepository);

            var permissionRepository = container.Resolve<IPermissionRepository>();
            var permissionRepositoryMock = Mock.Get(permissionRepository);

            permissionRepositoryMock
                .Setup(x => x.FindByFilter(It.IsAny<Model.RolePermissionFilter>()))
                .ReturnsAsync(
                    ids.Ids.Select(x => 
                        new Repository.Model.RolePermission
                        {
                            Id = x
                        })
                    .ToArray());
                
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer",
                        TestContainerBuilder.BuildToken(container, 1, TokenClaims.CLAIM_PERMISSIONS_ADMIN));
                var client = new Domain0Client(TestUrl, http);

                await client.AddRolePermissionsAsync(roleId, ids);
                roleRepositoryMock.Verify(t => 
                    t.AddRolePermissions(
                        roleId, 
                        It.Is<int[]>(rids => rids.SequenceEqual(ids.Ids))), 
                    Times.Once);

                var rolePermissions = await client.LoadRolePermissionsAsync(roleId);
                Assert.True(ids.Ids.SequenceEqual(rolePermissions.Select(x=> x.Id.GetValueOrDefault())));
                permissionRepositoryMock.Verify(t => 
                    t.FindByFilter(It.Is<Model.RolePermissionFilter>(f => 
                        f.RoleIds.Contains(roleId))), 
                    Times.Once);

                await client.RemoveRolePermissionsAsync(roleId, ids);
                roleRepositoryMock.Verify(t => t.RemoveRolePermissions(
                    roleId, 
                    It.Is<int[]>(rids => rids.SequenceEqual(ids.Ids))), 
                    Times.Once);
            }
        }

        [Fact]
        public async Task Admin_ClientUserPermissionOperationsTest()
        {
            var permissionId = 888;
            var userId = 777;
            var ids = new IdArrayRequest(new List<int> { 1, 2, 3 });

            var permissionRepository = container.Resolve<IPermissionRepository>();
            var permissionRepositoryMock = Mock.Get(permissionRepository);

            permissionRepositoryMock
                .Setup(x => x.FindByFilter(It.IsAny<Model.UserPermissionFilter>()))
                .ReturnsAsync(new[] { new Repository.Model.UserPermission() { Id = permissionId } });

            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer",
                        TestContainerBuilder.BuildToken(container, 1, TokenClaims.CLAIM_PERMISSIONS_ADMIN));
                var client = new Domain0Client(TestUrl, http);

                await client.AddUserPermissionsAsync(userId, ids);
                permissionRepositoryMock.Verify(t => t.AddUserPermission(userId, It.IsAny<int[]>()), Times.Once);

                var permissions = await client.LoadPermissionsByUserFilterAsync(new UserPermissionFilter(new List<int> { userId }));
                permissionRepositoryMock.Verify(t => t.FindByFilter(It.Is<Model.UserPermissionFilter>(pf => pf.UserIds.Contains(userId))), Times.Once);
                Assert.Equal(permissionId, permissions.FirstOrDefault()?.Id);

                await client.RemoveUserPermissionsAsync(userId, ids);
                permissionRepositoryMock.Verify(t => t.RemoveUserPermissions(userId, It.IsAny<int[]>()), Times.Once);
            }
        }

        [Fact]
        public async Task Admin_ClientApplicationsOperationsTest()
        {
            var applicationId = 11;
            var testApplication = new Api.Client.Application("description", applicationId, "name");
            var updatedApplication = new Api.Client.Application("update description", applicationId, "name");

            var applicationRepository = container.Resolve<IApplicationRepository>();
            var applicationRepositoryMock = Mock.Get(applicationRepository);

            applicationRepositoryMock
                .Setup(x => x.Insert(It.IsAny<Application>()))
                .ReturnsAsync(applicationId);

            applicationRepositoryMock
                .Setup(x => x.FindByIds(It.IsAny<IEnumerable<int>>()))
                .Returns<IEnumerable<int>>(x => 
                    Task.FromResult(x.Select(a => new Application { Id = a}).ToArray()));

            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer",
                        TestContainerBuilder.BuildToken(container, 1, TokenClaims.CLAIM_PERMISSIONS_ADMIN));

                var client = new Domain0Client(TestUrl, http);

                var id = await client.CreateApplicationAsync(testApplication);
                Assert.Equal(id, applicationId);
                applicationRepositoryMock.Verify(x => x.Insert(It.IsAny<Application>()), Times.Once());

                var loadedApplications = await client.LoadApplicationAsync(applicationId);
                Assert.Contains(loadedApplications, a => a.Id == applicationId);
                applicationRepositoryMock.Verify(x => x.FindByIds(It.IsAny<IEnumerable<int>>()), Times.Once());

                await client.UpdateApplicationAsync(updatedApplication);
                applicationRepositoryMock.Verify(x => 
                    x.Update(It.Is<Application>(a => 
                        a.Id == updatedApplication.Id
                        && a.Description == updatedApplication.Description)), 
                    Times.Once());

                var filteredApplications = await client.LoadApplicationsByFilterAsync(new ApplicationFilter(new List<int>{3}));
                Assert.Contains(filteredApplications, a => a.Id == 3);
                applicationRepositoryMock.Verify(x => 
                    x.FindByIds(
                        It.Is<IEnumerable<int>>(e => e.Contains(3))), 
                    Times.Once());

                await client.RemoveApplicationAsync(applicationId);

                applicationRepositoryMock.Verify(x =>
                    x.Delete(It.Is<int>(deletedId => deletedId == applicationId)),
                    Times.Once());
            }
        }

        [Fact]
        public async Task Admin_ClientPasswordOperationsTest()
        {
            var userId = 85;

            var accountRepository = container.Resolve<IAccountRepository>();
            var accountRepositoryMock = Mock.Get(accountRepository);

            var passwordGenerator = container.Resolve<IPasswordGenerator>();
            var passwordMock = Mock.Get(passwordGenerator);
            passwordMock.Setup(p => p.CheckPassword(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(true);

            var messageTemplateRepository = container.Resolve<IMessageTemplateRepository>();
            var messageTemplateMock = Mock.Get(messageTemplateRepository);
            messageTemplateMock
                .Setup(r => r.GetTemplate(
                    It.IsAny<MessageTemplateName>(),
                    It.IsAny<CultureInfo>(),
                    It.IsAny<MessageTemplateType>()))
                .Returns<MessageTemplateName, CultureInfo, MessageTemplateType>((n, l, t) => Task.FromResult("{0} {1}"));


            accountRepositoryMock
                .Setup(x => x.FindByUserId(It.IsAny<int>()))
                .ReturnsAsync(new Account { Id = userId });

            accountRepositoryMock
                .Setup(x => x.FindByPhone(It.IsAny<decimal>()))
                .Returns<decimal>(x => 
                    Task.FromResult(new Account { Phone = x }));

            accountRepositoryMock
                .Setup(x => x.FindByLogin(It.IsAny<string>()))
                .Returns<string>(x =>
                    Task.FromResult(new Account { Login = x }));


            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer",
                        TestContainerBuilder.BuildToken(container, userId, TokenClaims.CLAIM_PERMISSIONS_BASIC ));

                var client = new Domain0Client(TestUrl, http);

                await client.ChangePasswordAsync(new ChangePasswordRequest("new", "old"));
                accountRepositoryMock
                    .Verify(x => 
                        x.FindByUserId(It.Is<int>(id => id == userId)),
                        Times.Once);

                await client.ChangeMyPasswordAsync(new ChangePasswordRequest("new", "old"));
                accountRepositoryMock
                    .Verify(x =>
                            x.FindByUserId(It.Is<int>(id => id == userId)),
                        Times.Exactly(2));

                var resetPhone = 123;
                await client.RequestResetPasswordAsync(resetPhone);
                accountRepositoryMock
                    .Verify(x => x.FindByPhone(It.Is<decimal>(phone => phone == resetPhone)), Times.Once);

                var resetEmail = "email";
                await client.RequestResetPasswordByEmailAsync(new RegisterRequest("email"));
                accountRepositoryMock
                    .Verify(x => x.FindByLogin(It.Is<string>(email => email == resetEmail)), Times.Once);
            }
        }

        [Fact]
        public async Task Admin_ClientMessageTemplateOperationsTest()
        {
            var testMessageTemplate = new Api.Client.MessageTemplate(
                "description",
                321,
                "en",
                "template",
                "{0}{1}",
                "sms");


            var messageTemplateRepository = container.Resolve<IMessageTemplateRepository>();
            var messageTemplateRepositoryMock = Mock.Get(messageTemplateRepository);

            messageTemplateRepositoryMock
                .Setup(x => x.Insert(It.IsAny<Repository.Model.MessageTemplate>()))
                .ReturnsAsync(testMessageTemplate.Id.GetValueOrDefault());

            messageTemplateRepositoryMock
                .Setup(x => x.FindByIds(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new []
                {
                    new Repository.Model.MessageTemplate
                    {
                        Id = testMessageTemplate.Id.GetValueOrDefault(),
                        Description = testMessageTemplate.Description,
                        Locale = testMessageTemplate.Locale,
                        Name = testMessageTemplate.Name,
                        Template = testMessageTemplate.Template,
                        Type = testMessageTemplate.Type
                    }
                });

            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer",
                        TestContainerBuilder.BuildToken(container, 1, TokenClaims.CLAIM_PERMISSIONS_ADMIN));
                var client = new Domain0Client(TestUrl, http);

                var id = await client.CreateMessageTemplateAsync(testMessageTemplate);
                Assert.Equal(testMessageTemplate.Id, id);

                messageTemplateRepositoryMock.Verify(t => 
                    t.Insert(It.Is<Repository.Model.MessageTemplate>(x => 
                        x.Id == testMessageTemplate.Id
                        && x.Description == testMessageTemplate.Description
                        && x.Locale == testMessageTemplate.Locale
                        && x.Name == testMessageTemplate.Name
                        && x.Template == testMessageTemplate.Template
                        && x.Type == testMessageTemplate.Type)), 
                    Times.Once);

                var templates = await client.LoadMessageTemplateAsync(testMessageTemplate.Id.GetValueOrDefault());
                Assert.Single(templates);
                Assert.Equal(templates[0].Id, testMessageTemplate.Id);
                Assert.Equal(templates[0].Description, testMessageTemplate.Description);
                Assert.Equal(templates[0].Locale, testMessageTemplate.Locale);
                Assert.Equal(templates[0].Name, testMessageTemplate.Name);
                Assert.Equal(templates[0].Template, testMessageTemplate.Template);
                Assert.Equal(templates[0].Type, testMessageTemplate.Type);
                messageTemplateRepositoryMock.Verify(mt =>
                        mt.FindByIds(It.Is<IEnumerable<int>>(x => x.Contains(testMessageTemplate.Id.GetValueOrDefault()))),
                    Times.Once);

                templates = await client.LoadMessageTemplatesByFilterAsync(
                    new MessageTemplateFilter(new List<int>(testMessageTemplate.Id.GetValueOrDefault())));
                messageTemplateRepositoryMock.Verify(mt =>
                        mt.FindByIds(It.Is<IEnumerable<int>>(x => x.Contains(testMessageTemplate.Id.GetValueOrDefault()))),
                    Times.Once);
                Assert.Single(templates);
                Assert.Equal(templates[0].Id, testMessageTemplate.Id);
                Assert.Equal(templates[0].Description, testMessageTemplate.Description);
                Assert.Equal(templates[0].Locale, testMessageTemplate.Locale);
                Assert.Equal(templates[0].Name, testMessageTemplate.Name);
                Assert.Equal(templates[0].Template, testMessageTemplate.Template);
                Assert.Equal(templates[0].Type, testMessageTemplate.Type);

                await client.RemoveMessageTemplateAsync(testMessageTemplate.Id.GetValueOrDefault());
                messageTemplateRepositoryMock.Verify(mt =>
                        mt.Delete(It.Is<int>(x => x == testMessageTemplate.Id.GetValueOrDefault())),
                    Times.Once);

                var changedMessageTemplate = new Api.Client.MessageTemplate(
                    "changed",
                    321,
                    "en",
                    "template",
                    "{0}{1}",
                    "sms");


                await client.UpdateMessageTemplateAsync(changedMessageTemplate);
                messageTemplateRepositoryMock.Verify(t =>
                        t.Update(It.Is<Repository.Model.MessageTemplate>(x =>
                            x.Id == changedMessageTemplate.Id
                            && x.Description == changedMessageTemplate.Description
                            && x.Locale == changedMessageTemplate.Locale
                            && x.Name == changedMessageTemplate.Name
                            && x.Template == changedMessageTemplate.Template
                            && x.Type == changedMessageTemplate.Type)),
                    Times.Once);
            }
        }

        private static NancyHost BuildTestNancyHost(Domain0Bootstrapper bootstrapper)
        {
            var host = new NancyHost(
                bootstrapper,
                new HostConfiguration
                {
                    UrlReservations = new UrlReservations
                    {
                        CreateAutomatically = true
                    }
                },
                new Uri(TestUrl));
            return host;
        }

        public void Dispose()
        {
            host.Dispose();
        }
    }
}
