using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Domain0.Repository.Model;

namespace Domain0.Repository.SqlServer
{
    public class ApplicationRepository : IApplicationRepository
    {
        private readonly IDbConnectionProvider _connectionProvider;

        public ApplicationRepository(IDbConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        public async Task<int> Insert(Application entity)
        {
            const string query = @"
INSERT INTO [dom].[Application]
           ([Name]
           ,[Description])
     VALUES
           (@Name
           ,@Description)
;select SCOPE_IDENTITY() id
";
            using (var con = _connectionProvider.Connection)
            {
                return await con.ExecuteScalarAsync<int>(query, entity);
            }
        }

        public async Task<Application[]> FindByIds(IEnumerable<int> ids)
        {
            var listIds = ids.ToList();
            if (!listIds.Any())
            {
                const string query = @"
SELECT [Id]
      ,[Name]
      ,[Description]
  FROM [dom].[Application]
";
                using (var con = _connectionProvider.Connection)
                {
                    var result = await con.QueryAsync<Application>(query);
                    return result.ToArray();
                }
            }

            const string queryIn = @"
SELECT [Id]
      ,[Name]
      ,[Description]
  FROM [dom].[Application]
where [Id] in @Ids
";
            using (var con = _connectionProvider.Connection)
            {
                var result = await con.QueryAsync<Application>(queryIn, new {Ids = listIds});
                return result.ToArray();
            }
        }

        public async Task Update(Application entity)
        {
            const string query = @"
UPDATE [dom].[Application]
   SET [Name] = @Name
      ,[Description] = @Description
 WHERE [Id] = @Id
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
                    @"delete from [dom].[Application] where [Id] = @Id ",
                    new {Id = id});
            }
        }
    }
}