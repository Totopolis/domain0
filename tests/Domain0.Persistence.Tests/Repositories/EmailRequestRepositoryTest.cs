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
    public class SqlServerEmailRequestRepositoryTest : EmailRequestRepositoryTest
    {
        public SqlServerEmailRequestRepositoryTest(SqlServerFixture fixture) : base(fixture.Container)
        {
        }
    }

    [Collection("PostgreSql")]
    public class PostgreSqlEmailRequestRepositoryTest : EmailRequestRepositoryTest
    {
        public PostgreSqlEmailRequestRepositoryTest(PostgreSqlFixture fixture) : base(fixture.Container)
        {
        }
    }

    public abstract class EmailRequestRepositoryTest
    {
        private static EquivalencyAssertionOptions<EmailRequest> CompareOptions(
            EquivalencyAssertionOptions<EmailRequest> options) =>
            options
                .Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation))
                .WhenTypeIs<DateTime>()
                .Excluding(x => x.Id);

        private readonly IContainer _container;

        public EmailRequestRepositoryTest(IContainer container)
        {
            _container = container;
        }

        [Fact]
        public async Task RequestTest()
        {
            var userId = 1;
            var requests = _container.Resolve<IEmailRequestRepository>();
            var request = new EmailRequest
            {
                UserId = userId,
                Email = "example@domain.local",
                Password = "password",
                ExpiredAt = DateTime.UtcNow.AddDays(+1),
            };
            await requests.Save(request);

            var result = await requests.Pick(request.Email);
            result.Should().BeEquivalentTo(request, CompareOptions);
            result = await requests.PickByUserId(userId);
            result.Should().BeEquivalentTo(request, CompareOptions);

            result = await requests.ConfirmRegister(request.Email, request.Password);
            result.Should().BeEquivalentTo(request, CompareOptions);
            result = await requests.Pick(request.Email);
            result.Should().BeNull("Requests are removed after confirmation");

            request.ExpiredAt = DateTime.UtcNow.AddDays(-1); // expired
            await requests.Save(request);
            result = await requests.Pick(request.Email);
            result.Should().BeNull("Expired requests are not returned");
            result = await requests.PickByUserId(userId);
            result.Should().BeNull("Expired requests are not returned");

            result = await requests.ConfirmRegister(request.Email, request.Password);
            result.Should().BeNull("Expired request cannot be confirmed");
        }
    }
}