using System;
using System.Threading.Tasks;
using Autofac;
using Domain0.Persistence.Tests.Fixtures;
using Domain0.Repository;
using Domain0.Repository.Model;
using FluentAssertions;
using FluentAssertions.Equivalency;
using Xunit;

namespace Domain0.Persistence.Tests
{
    [Collection("SqlServer")]
    public class SqlServerAccountRepositoryTest : AccountRepositoryTest
    {
        public SqlServerAccountRepositoryTest(SqlServerFixture fixture) : base(fixture.Container)
        {
        }
    }

    [Collection("PostgreSql")]
    public class PostgreSqlAccountRepositoryTest : AccountRepositoryTest
    {
        public PostgreSqlAccountRepositoryTest(PostgreSqlFixture fixture) : base(fixture.Container)
        {
        }
    }

    public abstract class AccountRepositoryTest
    {
        private static readonly Func<EquivalencyAssertionOptions<Account>, EquivalencyAssertionOptions<Account>>
            AccountCheckOptions = options => options
                .Using<DateTime?>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation.Value))
                .When(info => info.SelectedMemberPath.EndsWith("Date"));

        private readonly IContainer _container;

        public AccountRepositoryTest(IContainer container)
        {
            _container = container;
        }

        [Fact]
        public async Task CrudOne()
        {
            var accounts = _container.Resolve<IAccountRepository>();
            var account = new Account
            {
                Email = "example@domain.local",
                Phone = 1,
                Login = "1",
                Password = "111",
                Name = "name",
                Description = "d",
                FirstDate = DateTime.UtcNow,
                LastDate = DateTime.UtcNow,
                IsLocked = false,
            };

            account.Id = await accounts.Insert(account);
            account.Id.Should().BeGreaterThan(0);

            var result = await accounts.FindByUserId(account.Id);
            result.Should().BeEquivalentTo(account, AccountCheckOptions);

            result = await accounts.FindByLogin(account.Login);
            result.Should().BeEquivalentTo(account, AccountCheckOptions);

            result = await accounts.FindByPhone(account.Phone.Value);
            result.Should().BeEquivalentTo(account, AccountCheckOptions);

            var results = await accounts.FindByUserIds(new[] {account.Id});
            results.Should().BeEquivalentTo(new[] {account}, AccountCheckOptions);

            account.Name = "new name";

            await accounts.Update(account);

            result = await accounts.FindByUserId(account.Id);
            result.Should().BeEquivalentTo(account, AccountCheckOptions);

            await accounts.Delete(account.Id);

            result = await accounts.FindByUserId(account.Id);
            result.Should().BeNull();
        }
    }
}