using System;
using System.Linq;
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
        private readonly IContainer _container;

        public AccountRepositoryTest(IContainer container)
        {
            _container = container;
        }

        [Fact]
        public async Task CrudOne()
        {
            EquivalencyAssertionOptions<Account> AccountCheckOptions(EquivalencyAssertionOptions<Account> options) =>
                options.Using<DateTime?>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation.Value))
                    .When(info => info.SelectedMemberPath.EndsWith("Date"));

            var accounts = _container.Resolve<IAccountRepository>();

            // CREATE
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

            // READ
            var result = await accounts.FindByUserId(account.Id);
            result.Should().BeEquivalentTo(account, AccountCheckOptions);

            result = await accounts.FindByLogin(account.Login);
            result.Should().BeEquivalentTo(account, AccountCheckOptions);

            result = await accounts.FindByPhone(account.Phone.Value);
            result.Should().BeEquivalentTo(account, AccountCheckOptions);

            var results = await accounts.FindByUserIds(new[] {account.Id});
            results.Should().BeEquivalentTo(new[] {account}, AccountCheckOptions);

            // UPDATE
            account.Name = "new name";

            await accounts.Update(account);

            result = await accounts.FindByUserId(account.Id);
            result.Should().BeEquivalentTo(account, AccountCheckOptions);

            // DELETE
            await accounts.Delete(account.Id);

            result = await accounts.FindByUserId(account.Id);
            result.Should().BeNull();
        }

        [Fact]
        public async Task Locking()
        {
            var accounts = _container.Resolve<IAccountRepository>();
            var acc = new Account {IsLocked = false};
            var id = await accounts.Insert(acc);

            await accounts.Lock(id);

            var result = await accounts.FindByUserId(id);
            result.IsLocked.Should().BeTrue();

            await accounts.Unlock(id);

            result = await accounts.FindByUserId(id);
            result.IsLocked.Should().BeFalse();
        }

        [Fact]
        public async Task GetMany()
        {
            var accounts = _container.Resolve<IAccountRepository>();
            var accList = Enumerable.Range(1, 10)
                .Select(x => new Account {Login = x.ToString()})
                .ToList();
            var ids = await Task.WhenAll(accList.Select(x => accounts.Insert(x)));
            for (var i = 0; i < ids.Length; ++i)
            {
                accList[i].Id = ids[i];
            }

            var result = await accounts.FindByUserIds(ids);

            result.Should().BeEquivalentTo(accList);
            await Task.WhenAll(ids.Select(x => accounts.Delete(x)));
        }
    }
}