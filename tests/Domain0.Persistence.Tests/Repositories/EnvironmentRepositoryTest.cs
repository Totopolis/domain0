using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Domain0.Persistence.Tests.Fixtures;
using Domain0.Repository;
using Domain0.Repository.Model;
using FluentAssertions;
using Xunit;

namespace Domain0.Persistence.Tests.Repositories
{
    [Collection("SqlServer")]
    public class SqlServerEnvironmentRepositoryTest : EnvironmentRepositoryTest
    {
        public SqlServerEnvironmentRepositoryTest(SqlServerFixture fixture) : base(fixture.Container)
        {
        }
    }

    [Collection("PostgreSql")]
    public class PostgreSqlEnvironmentRepositoryTest : EnvironmentRepositoryTest
    {
        public PostgreSqlEnvironmentRepositoryTest(PostgreSqlFixture fixture) : base(fixture.Container)
        {
        }
    }

    public abstract class EnvironmentRepositoryTest
    {
        private readonly IContainer _container;

        public EnvironmentRepositoryTest(IContainer container)
        {
            _container = container;
        }

        [Fact]
        public async Task CrudOne()
        {
            var envRepo = _container.Resolve<IEnvironmentRepository>();

            // CREATE
            var env = new Environment
            {
                Name = "name",
                Description = "d",
                Token = "token",
            };

            env.Id = await envRepo.Insert(env);

            env.Id.Should().NotBeNull();

            // READ
            var result = await envRepo.FindById(env.Id.Value);
            result.Should().BeEquivalentTo(env);

            // UPDATE
            env.Name = "new name";

            await envRepo.Update(env);

            result = await envRepo.FindById(env.Id.Value);
            result.Should().BeEquivalentTo(env);

            // DELETE
            await envRepo.Delete(env.Id.Value);

            result = await envRepo.FindById(env.Id.Value);
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetMany()
        {
            var envRepo = _container.Resolve<IEnvironmentRepository>();
            var envList = Enumerable.Range(1, 10)
                .Select(x => new Environment
                {
                    Name = x.ToString(),
                    Token = $"token-{x}",
                })
                .ToList();
            var ids = await Task.WhenAll(envList.Select(x => envRepo.Insert(x)));
            for (var i = 0; i < ids.Length; ++i)
            {
                envList[i].Id = ids[i];
            }

            var result = await envRepo.FindByIds(ids);

            result.Should().BeEquivalentTo(envList);
            await Task.WhenAll(ids.Select(x => envRepo.Delete(x)));
        }

        [Fact]
        public async Task GetByToken()
        {
            var envRepo = _container.Resolve<IEnvironmentRepository>();
            const string token = "token";
            var env = new Environment
            {
                Name = "name",
                Token = token,
            };
            env.Id = await envRepo.Insert(env);

            var result = await envRepo.GetByToken(token);

            result.Should().BeEquivalentTo(env);
            await envRepo.Delete(env.Id.Value);
        }

        [Fact]
        public async Task GetDefault_Success()
        {
            var envRepo = _container.Resolve<IEnvironmentRepository>();
            var env = new Environment
            {
                Name = "name",
                Token = "token",
                IsDefault = true,
            };
            env.Id = await envRepo.Insert(env);

            var result = await envRepo.GetDefault();

            result.Should().BeEquivalentTo(env);
            await envRepo.Delete(env.Id.Value);
        }

        [Fact]
        public async Task GetDefault_Failed()
        {
            var envRepo = _container.Resolve<IEnvironmentRepository>();
            var env = new Environment
            {
                Name = "name",
                Token = "token",
                IsDefault = false,
            };
            env.Id = await envRepo.Insert(env);

            var result = await envRepo.GetDefault();

            result.Should().BeNull();
            await envRepo.Delete(env.Id.Value);
        }

        [Fact]
        public async Task SetEnvironmentForUser()
        {
            var accRepo = _container.Resolve<IAccountRepository>();
            var envRepo = _container.Resolve<IEnvironmentRepository>();
            var env = new Environment
            {
                Name = "name",
                Token = "token",
                IsDefault = false,
            };
            env.Id = await envRepo.Insert(env);
            var acc = new Account {Login = "user"};
            acc.Id = await accRepo.Insert(acc);

            await envRepo.SetUserEnvironment(acc.Id, env.Id.Value);

            var result = await envRepo.GetByUser(acc.Id);
            result.Should().BeEquivalentTo(env);
            await accRepo.Delete(acc.Id);
            await envRepo.Delete(env.Id.Value);
        }
    }
}