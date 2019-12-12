using System;
using System.Threading.Tasks;
using Autofac;
using Domain0.Persistence.Tests.Fixtures;
using Domain0.Repository;
using Domain0.Repository.Model;
using Xunit;

namespace Domain0.Persistence.Tests
{
    [Collection("SqlServer")]
    public class SqlServerAccessLogRepositoryTest : AccessLogRepositoryTest
    {
        public SqlServerAccessLogRepositoryTest(SqlServerFixture fixture) : base(fixture.Container)
        {
        }
    }

    [Collection("PostgreSql")]
    public class PostgreSqlAccessLogRepositoryTest : AccessLogRepositoryTest
    {
        public PostgreSqlAccessLogRepositoryTest(PostgreSqlFixture fixture) : base(fixture.Container)
        {
        }
    }

    public abstract class AccessLogRepositoryTest
    {
        private readonly IContainer _container;

        public AccessLogRepositoryTest(IContainer container)
        {
            _container = container;
        }

        [Fact]
        public async Task WriteToAccessLog_ShouldntThrow()
        {
            var logs = _container.Resolve<IAccessLogRepository>();
            var log = new AccessLogEntry
            {
                Action = "drop_database",
                ClientIp = "1.1.1.1",
                Method = "GET",
                ProcessedAt = DateTime.UtcNow,
                ProcessingTime = 999,
                Referer = "boss",
                StatusCode = 200,
                UserAgent = "me",
                UserId = "1",
            };

            await logs.Insert(log);
        }
    }
}