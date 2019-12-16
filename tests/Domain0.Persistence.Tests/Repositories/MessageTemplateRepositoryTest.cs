using System.Globalization;
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
    public class SqlServerMessageTemplateRepositoryTest : MessageTemplateRepositoryTest
    {
        public SqlServerMessageTemplateRepositoryTest(SqlServerFixture fixture) : base(fixture.Container)
        {
        }
    }

    [Collection("PostgreSql")]
    public class PostgreSqlMessageTemplateRepositoryTest : MessageTemplateRepositoryTest
    {
        public PostgreSqlMessageTemplateRepositoryTest(PostgreSqlFixture fixture) : base(fixture.Container)
        {
        }
    }

    public abstract class MessageTemplateRepositoryTest
    {
        private readonly IContainer _container;

        public MessageTemplateRepositoryTest(IContainer container)
        {
            _container = container;
        }

        [Fact]
        public async Task CrudOne()
        {
            var templates = _container.Resolve<IMessageTemplateRepository>();

            // CREATE
            var template = new MessageTemplate
            {
                Locale = "en",
                Type = "email",
                Template = "To enjoy online services...",
                Name = "WelcomeTemplate",
                Description = "cool welcome message",
                EnvironmentId = 1,
            };

            template.Id = await templates.Insert(template);

            template.Id.Should().BeGreaterThan(0);

            // READ
            var results = await templates.FindByIds(new[] { template.Id });
            results.Should().BeEquivalentTo(template);

            // UPDATE
            template.Name = "new name";

            await templates.Update(template);

            results = await templates.FindByIds(new[] { template.Id });
            results.Should().BeEquivalentTo(template);

            // DELETE
            await templates.Delete(template.Id);

            results = await templates.FindByIds(new[] { template.Id });
            results.Should().BeEmpty();
        }

        [Fact]
        public async Task GetMany()
        {
            var templates = _container.Resolve<IMessageTemplateRepository>();
            var templatesList = Enumerable.Range(1, 10)
                .Select(x => new MessageTemplate
                {
                    Name = x.ToString(),
                    Template = "111",
                    EnvironmentId = 1,
                })
                .ToList();
            var ids = await Task.WhenAll(templatesList.Select(x => templates.Insert(x)));
            for (var i = 0; i < ids.Length; ++i)
            {
                templatesList[i].Id = ids[i];
            }

            var result = await templates.FindByIds(ids);

            result.Should().BeEquivalentTo(templatesList);
            await Task.WhenAll(ids.Select(x => templates.Delete(x)));
        }

        [Fact]
        public async Task GetTemplate()
        {
            var templates = _container.Resolve<IMessageTemplateRepository>();
            var template = new MessageTemplate
            {
                Locale = CultureInfo.CurrentCulture.Name,
                Type = MessageTemplateType.email.ToString(),
                Template = "welcome template",
                Name = MessageTemplateName.WelcomeTemplate.ToString(),
                EnvironmentId = 1,
            };
            template.Id = await templates.Insert(template);

            var result = await templates.GetTemplate(
                MessageTemplateName.WelcomeTemplate,
                CultureInfo.CurrentCulture,
                MessageTemplateType.email,
                1);

            result.Should().BeEquivalentTo(template.Template);
        }
    }
}