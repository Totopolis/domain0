using System;
using System.Threading.Tasks;
using Autofac;
using Domain0.Persistence.Tests.Fixtures;
using Domain0.Repository;
using Domain0.Repository.Model;
using FluentAssertions;
using FluentAssertions.Equivalency;
using Xunit;

namespace Domain0.Persistence.Tests.Repositories
{
    [Collection("SqlServer")]
    public class SqlServerTokenRegistrationRepositoryTest : TokenRegistrationRepositoryTest
    {
        public SqlServerTokenRegistrationRepositoryTest(SqlServerFixture fixture) : base(fixture.Container)
        {
        }
    }

    [Collection("PostgreSql")]
    public class PostgreSqlTokenRegistrationRepositoryTest : TokenRegistrationRepositoryTest
    {
        public PostgreSqlTokenRegistrationRepositoryTest(PostgreSqlFixture fixture) : base(fixture.Container)
        {
        }
    }

    public abstract class TokenRegistrationRepositoryTest
    {
        private static EquivalencyAssertionOptions<TokenRegistration> CompareOptions(
            EquivalencyAssertionOptions<TokenRegistration> options) =>
            options.Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation))
                .When(info => info.SelectedMemberPath.EndsWith("At"));

        private readonly IContainer _container;

        public TokenRegistrationRepositoryTest(IContainer container)
        {
            _container = container;
        }

        [Fact]
        public async Task CrudOne()
        {
            var userId = 1;
            var tokens = _container.Resolve<ITokenRegistrationRepository>();
            var registration = new TokenRegistration
            {
                UserId = userId,
                IssuedAt = DateTime.UtcNow.AddDays(-1),
                AccessToken = "token",
                ExpiredAt = DateTime.UtcNow.AddDays(+1)
            };

            // CREATE
            await tokens.Save(registration);

            // READ
            var result = await tokens.FindById(registration.Id);
            result.Should().BeEquivalentTo(registration, CompareOptions);

            // UPDATE
            registration.AccessToken = "new token";
            await tokens.Save(registration);
            result = await tokens.FindById(registration.Id);
            result.Should().BeEquivalentTo(registration, CompareOptions);

            // DELETE
            await tokens.RevokeByUserId(userId);
            result = await tokens.FindById(registration.Id);
            result.Should().BeNull();
        }

        [Fact]
        public async Task FindLastTokenRegistration()
        {
            var userId = 1;
            var tokens = _container.Resolve<ITokenRegistrationRepository>();
            var registrationFirst = new TokenRegistration
            {
                UserId = userId,
                IssuedAt = DateTime.UtcNow.AddDays(0),
                AccessToken = "token",
                ExpiredAt = DateTime.UtcNow.AddDays(+1)
            };
            await tokens.Save(registrationFirst);
            var registrationSecond = new TokenRegistration
            {
                UserId = userId,
                IssuedAt = DateTime.UtcNow.AddDays(-1),
                AccessToken = "token",
                ExpiredAt = DateTime.UtcNow.AddDays(0)
            };
            await tokens.Save(registrationSecond);

            var result = await tokens.FindLastTokenByUserId(userId);
            result.Should().BeEquivalentTo(registrationSecond, CompareOptions,
                "tokens are sorted by Id");

            await tokens.RevokeByUserId(userId);
        }
    }
}