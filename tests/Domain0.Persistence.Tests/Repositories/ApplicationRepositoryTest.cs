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
    public class SqlServerApplicationRepositoryTest : ApplicationRepositoryTest
    {
        public SqlServerApplicationRepositoryTest(SqlServerFixture fixture) : base(fixture.Container)
        {
        }
    }

    [Collection("PostgreSql")]
    public class PostgreSqlApplicationRepositoryTest : ApplicationRepositoryTest
    {
        public PostgreSqlApplicationRepositoryTest(PostgreSqlFixture fixture) : base(fixture.Container)
        {
        }
    }

    public abstract class ApplicationRepositoryTest
    {
        private readonly IContainer _container;

        public ApplicationRepositoryTest(IContainer container)
        {
            _container = container;
        }

        [Fact]
        public async Task CrudOne()
        {
            var apps = _container.Resolve<IApplicationRepository>();

            // CREATE
            var app = new Application
            {
                Name = "name",
                Description = "d",
            };

            app.Id = await apps.Insert(app);

            app.Id.Should().BeGreaterThan(0);

            // READ
            var results = await apps.FindByIds(new[] {app.Id});
            results.Should().BeEquivalentTo(app);

            // UPDATE
            app.Name = "new name";

            await apps.Update(app);

            results = await apps.FindByIds(new[] {app.Id});
            results.Should().BeEquivalentTo(app);

            // DELETE
            await apps.Delete(app.Id);

            results = await apps.FindByIds(new[] {app.Id});
            results.Should().BeEmpty();
        }

        [Fact]
        public async Task GetMany()
        {
            var apps = _container.Resolve<IApplicationRepository>();
            var appList = Enumerable.Range(1, 10)
                .Select(x => new Application {Name = x.ToString()})
                .ToList();
            var ids = await Task.WhenAll(appList.Select(x => apps.Insert(x)));
            for (var i = 0; i < ids.Length; ++i)
            {
                appList[i].Id = ids[i];
            }

            var result = await apps.FindByIds(ids);

            result.Should().BeEquivalentTo(appList);
            await Task.WhenAll(ids.Select(x => apps.Delete(x)));
        }
    }
}