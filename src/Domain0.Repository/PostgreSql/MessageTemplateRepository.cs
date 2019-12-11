using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Domain0.Repository.Extensions;
using Domain0.Repository.Model;

namespace Domain0.Repository.PostgreSql
{
    public class MessageTemplateRepository : IMessageTemplateRepository
    {
        private readonly IDbConnectionProvider _connectionProvider;

        public MessageTemplateRepository(IDbConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        public async Task<int> Insert(MessageTemplate entity)
        {
            const string query = @"
insert into dom.""Message""
(""Description"", ""Type"", ""Locale"", ""Name"", ""Template"", ""EnvironmentId"")
values
(@Description, @Type, @Locale, @Name, @Template, @EnvironmentId)
returning ""Id""
";
            using (var con = _connectionProvider.Connection)
            {
                return await con.ExecuteScalarAsync<int>(query, entity);
            }
        }

        public async Task<MessageTemplate[]> FindByIds(IEnumerable<int> ids)
        {
            var listIds = ids.ToList();
            if (!listIds.Any())
            {
                const string query = @"
select ""Id"", ""Description"", ""Type"", ""Locale"", ""Name"", ""Template"", ""EnvironmentId""
from dom.""Message""
";
                using (var con = _connectionProvider.Connection)
                {
                    var result = await con.QueryAsync<MessageTemplate>(query);
                    return result.ToArray();
                }
            }

            const string queryIn = @"
select ""Id"", ""Description"", ""Type"", ""Locale"", ""Name"", ""Template"", ""EnvironmentId""
from dom.""Message""
where ""Id"" in @Ids
";
            using (var con = _connectionProvider.Connection)
            {
                var result = await con.QueryAsync<MessageTemplate>(queryIn, new {Ids = listIds});
                return result.ToArray();
            }
        }

        public async Task Update(MessageTemplate entity)
        {
            const string query = @"
update dom.""Message""
set ""Description"" = @Description
  , ""Type"" = @Type
  , ""Locale"" = @Locale
  , ""Name"" = @Name
  , ""Template"" = @Template
  , ""EnvironmentId"" = @EnvironmentId
where ""Id"" = @Id
";
            using (var con = _connectionProvider.Connection)
            {
                await con.ExecuteAsync(query, entity);
            }
        }

        public async Task Delete(int id)
        {
            using (var con = _connectionProvider.Connection)
            {
                await con.ExecuteAsync(
                    @"delete from dom.""Message"" where ""Id"" = @Id",
                    new {Id = id});
            }
        }

        public async Task<string> GetTemplate(
            MessageTemplateName name,
            CultureInfo culture,
            MessageTemplateType type,
            int environmentId)
        {
            const string query = @"
select ""Id"", ""Description"", ""Type"", ""Locale"", ""Name"", ""Template"", ""EnvironmentId""
from dom.""Message""
where ""Name"" = @Name
  and ""Type"" = @Type
  and ""EnvironmentId"" = @EnvironmentId
order by ""Locale""
";
            using (var con = _connectionProvider.Connection)
            {
                var templates = await con.QueryAsync<MessageTemplate>(query,
                    new
                    {
                        Name = name.ToString(),
                        Type = type.ToString(),
                        EnvironmentId = environmentId,
                    });
                return culture.GetMatchedTemplate(templates.ToArray());
            }
        }
    }
}