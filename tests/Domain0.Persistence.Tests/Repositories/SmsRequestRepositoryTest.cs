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
    public class SqlServerSmsRequestRepositoryTest : SmsRequestRepositoryTest
    {
        public SqlServerSmsRequestRepositoryTest(SqlServerFixture fixture) : base(fixture.Container)
        {
        }
    }

    [Collection("PostgreSql")]
    public class PostgreSqlSmsRequestRepositoryTest : SmsRequestRepositoryTest
    {
        public PostgreSqlSmsRequestRepositoryTest(PostgreSqlFixture fixture) : base(fixture.Container)
        {
        }
    }

    public abstract class SmsRequestRepositoryTest
    {
        private static EquivalencyAssertionOptions<SmsRequest> CompareOptions(
            EquivalencyAssertionOptions<SmsRequest> options) =>
            options
                .Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation))
                .WhenTypeIs<DateTime>()
                .Excluding(x => x.Id);

        private readonly IContainer _container;

        public SmsRequestRepositoryTest(IContainer container)
        {
            _container = container;
        }

        [Fact]
        public async Task RequestTest()
        {
            var userId = 1;
            var requests = _container.Resolve<ISmsRequestRepository>();
            var request = new SmsRequest
            {
                UserId = userId,
                Phone = 79998887766,
                Password = "password",
                ExpiredAt = DateTime.UtcNow.AddDays(+1),
            };
            await requests.Save(request);

            var result = await requests.Pick(request.Phone);
            result.Should().BeEquivalentTo(request, CompareOptions);
            result = await requests.PickByUserId(userId);
            result.Should().BeEquivalentTo(request, CompareOptions);

            result = await requests.ConfirmRegister(request.Phone, request.Password);
            result.Should().BeEquivalentTo(request, CompareOptions);
            result = await requests.Pick(request.Phone);
            result.Should().BeNull("Requests are removed after confirmation");

            request.ExpiredAt = DateTime.UtcNow.AddDays(-1); // expired
            await requests.Save(request);
            result = await requests.Pick(request.Phone);
            result.Should().BeNull("Expired requests are not returned");
            result = await requests.PickByUserId(userId);
            result.Should().BeNull("Expired requests are not returned");

            result = await requests.ConfirmRegister(request.Phone, request.Password);
            result.Should().BeNull("Expired request cannot be confirmed");
        }
    }
}