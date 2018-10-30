using System.Collections.Generic;
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
    public class UsersModuleTests
    {
        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task GetMyProfile_Success(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(
                builder => builder.RegisterType<SymmetricKeyTokenGenerator>().As<ITokenGenerator>().SingleInstance());

            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var phone = 79000000000;
            var userId = 1;

            var accessToken = TestContainerBuilder.BuildToken(container, 1);

            var requestMock = Mock.Get(container.Resolve<IRequestContext>());
            requestMock.Setup(a => a.UserId).Returns(userId);

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByUserId(userId)).ReturnsAsync(new Account { Id = userId, Phone = phone });

            var response = await browser.Get(UsersModule.GetMyProfileUrl, with =>
            {
                with.Header("Authorization", $"Bearer {accessToken}");
                with.Accept(format);
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = response.Body.AsDataFormat<UserProfile>(format);
            Assert.Equal(userId, result.Id);
            Assert.Equal(phone.ToString(), result.Phone);
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task GetProfileByPhone_Success(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(builder =>
                builder.RegisterType<SymmetricKeyTokenGenerator>().As<ITokenGenerator>().SingleInstance());
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
            Assert.Equal(phone.ToString(), result.Phone);
        }

        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task GetProfileByPhone_NotFound(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(builder =>
                builder.RegisterType<SymmetricKeyTokenGenerator>().As<ITokenGenerator>().SingleInstance());
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var userId = 1;
            var phone = 79000000000;
            var accessToken = TestContainerBuilder.BuildToken(container, userId, TokenClaims.CLAIM_PERMISSIONS_VIEW_USERS);

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByPhone(phone)).ReturnsAsync((Account)null);

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
                builder.RegisterType<SymmetricKeyTokenGenerator>().As<ITokenGenerator>().SingleInstance());
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var userId = 1;
            var accessToken = TestContainerBuilder.BuildToken(container, userId, TokenClaims.CLAIM_PERMISSIONS_VIEW_USERS);

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByUserId(userId)).ReturnsAsync(new Account { Id = userId });

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
        public async Task UpdateUser_Success(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(builder =>
                builder.RegisterType<SymmetricKeyTokenGenerator>().As<ITokenGenerator>().SingleInstance());
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var userUpdate = new UserProfile
            {
                Id = 343,
                Description = "newDescription",
                Email = "email",
                Phone = "412312",
                Name = "name"
            };

            var accessToken = TestContainerBuilder.BuildToken(
                container, 
                userUpdate.Id, 
                TokenClaims.CLAIM_PERMISSIONS_EDIT_USERS);

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByUserId(userUpdate.Id)).ReturnsAsync(
                new Account
                {
                    Id = userUpdate.Id,
                    Email = userUpdate.Email,
                    Phone = decimal.Parse(userUpdate.Phone),
                    Name = userUpdate.Name,
                    Description = userUpdate.Description,
                });

            var response = await browser.Post(UsersModule.PostUserUrl.Replace("{id}", userUpdate.Id.ToString()), with =>
            {
                with.Accept(format);
                with.Header("Authorization", $"Bearer {accessToken}");
                with.DataFormatBody(format, userUpdate);
            });
            accountMock.Verify(ar => ar.FindByUserId(userUpdate.Id), Times.Once);
            accountMock.Verify(ar => ar.Update(
                It.Is<Account>(a => a.Id == userUpdate.Id
                    && a.Description == userUpdate.Description
                    && a.Email == userUpdate.Email
                    && a.Name == userUpdate.Name
                    && a.Phone.ToString() == userUpdate.Phone)), 
                Times.Once);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = response.Body.AsDataFormat<UserProfile>(format);
            Assert.Equal(userUpdate.Id, result.Id);
            Assert.Equal(userUpdate.Description, result.Description);
            Assert.Equal(userUpdate.Email, result.Email);
            Assert.Equal(userUpdate.Phone, result.Phone);
            Assert.Equal(userUpdate.Name, result.Name);
        }


        [Theory]
        [InlineData(DataFormat.Json)]
        [InlineData(DataFormat.Proto)]
        public async Task GetProfileByUserId_NotFound(DataFormat format)
        {
            var container = TestContainerBuilder.GetContainer(builder =>
                builder.RegisterType<SymmetricKeyTokenGenerator>().As<ITokenGenerator>().SingleInstance());
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
                builder.RegisterType<SymmetricKeyTokenGenerator>().As<ITokenGenerator>().SingleInstance());
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var userId = 1;
            var accessToken = TestContainerBuilder.BuildToken(container, userId, TokenClaims.CLAIM_PERMISSIONS_VIEW_PROFILE);

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByUserIds(It.IsAny<IEnumerable<int>>())).Returns<IEnumerable<int>>(ids => Task.FromResult(ids.Select(id => new Account { Id = id }).ToArray()));

            var response = await browser.Post(UsersModule.GetUsersByFilterUrl, with =>
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
                builder.RegisterType<SymmetricKeyTokenGenerator>().As<ITokenGenerator>().SingleInstance());
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var userId = 1;
            var accessToken = TestContainerBuilder.BuildToken(container, userId, TokenClaims.CLAIM_PERMISSIONS_VIEW_PROFILE);

            var accountMock = Mock.Get(container.Resolve<IAccountRepository>());
            accountMock.Setup(a => a.FindByUserIds(It.IsAny<IEnumerable<int>>())).Returns<IEnumerable<int>>(ids => Task.FromResult(ids.Select(id => new Account { Id = id }).ToArray()));

            var response = await browser.Post(UsersModule.GetUsersByFilterUrl, with =>
            {
                with.Accept(format);
                with.Header("Authorization", $"Bearer {accessToken}");
                with.JsonBody("{userIds:['qwe','rty']}");
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

    }
}
