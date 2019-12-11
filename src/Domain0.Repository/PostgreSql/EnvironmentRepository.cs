using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Domain0.Repository.Model;

namespace Domain0.Repository.PostgreSql
{
    public class EnvironmentRepository : IEnvironmentRepository
    {
        private readonly IDbConnectionProvider _connectionProvider;

        public EnvironmentRepository(IDbConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        public async Task<int> Insert(Environment entity)
        {
            const string query = @"
INSERT INTO dom.""Environment""
           (""Name""
           ,""Description""
           ,""Token""
           ,""IsDefault"")
     VALUES
           (@Name
           ,@Description
           ,@Token
           ,@IsDefault)
returning ""Id""
";
            using (var con = _connectionProvider.Connection)
            {
                return await con.ExecuteScalarAsync<int>(query, entity);
            }
        }

        public async Task Update(Environment entity)
        {
            const string query = @"
UPDATE dom.""Environment""
   SET ""Name"" = @Name
      ,""Description"" = @Description
      ,""Token"" = @Token
      ,""IsDefault"" = @IsDefault
 WHERE ""Id"" = @Id
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
                    @"delete from dom.""Environment"" where ""Id"" = @Id",
                    new {Id = id});
            }
        }

        public async Task<Environment> FindById(int id)
        {
            const string query = @"
SELECT ""Id""
      ,""Name""
      ,""Description""
      ,""Token""
      ,""IsDefault""
  FROM dom.""Environment""
where ""Id"" = @Id
";
            using (var con = _connectionProvider.Connection)
            {
                return await con.QueryFirstOrDefaultAsync<Environment>(query, new {Id = id});
            }
        }

        public async Task<Environment[]> FindByIds(IEnumerable<int> ids)
        {
            var listIds = ids.ToList();
            if (!listIds.Any())
            {
                const string query = @"
SELECT ""Id""
      ,""Name""
      ,""Description""
      ,""Token""
      ,""IsDefault""
  FROM dom.""Environment""
";
                using (var con = _connectionProvider.Connection)
                {
                    var result = await con.QueryAsync<Environment>(query);
                    return result.ToArray();
                }
            }

            const string queryIn = @"
SELECT ""Id""
      ,""Name""
      ,""Description""
      ,""Token""
      ,""IsDefault""
  FROM dom.""Environment""
where ""Id"" in @Ids
";
            using (var con = _connectionProvider.Connection)
            {
                var result = await con.QueryAsync<Environment>(queryIn, new {Ids = listIds});
                return result.ToArray();
            }
        }

        public async Task<Environment> GetByToken(string token)
        {
            const string query = @"
SELECT ""Id""
      ,""Name""
      ,""Description""
      ,""Token""
      ,""IsDefault""
  FROM dom.""Environment""
where ""Token"" = @Token
";
            using (var con = _connectionProvider.Connection)
            {
                return await con.QueryFirstOrDefaultAsync<Environment>(query, new {Token = token});
            }
        }

        public async Task<Environment> GetDefault()
        {
            const string query = @"
SELECT ""Id""
      ,""Name""
      ,""Description""
      ,""Token""
      ,""IsDefault""
  FROM dom.""Environment""
where ""IsDefault"" = TRUE
";
            using (var con = _connectionProvider.Connection)
            {
                return await con.QueryFirstOrDefaultAsync<Environment>(query);
            }
        }

        public async Task<Environment> GetByUser(int userId)
        {
            const string query = @"
SELECT e.""Id""
      ,e.""Name""
      ,e.""Description""
      ,e.""Token""
      ,e.""IsDefault""
  FROM dom.""Environment"" e
join dom.""AccountEnvironment"" ae on e.""Id"" = ae.""EnvironmentId""
where ae.""UserId"" = @UserId
";
            using (var con = _connectionProvider.Connection)
            {
                return await con.QueryFirstOrDefaultAsync<Environment>(query, new {UserId = userId});
            }
        }

        public async Task SetUserEnvironment(int userId, int environmentId)
        {
            const string query = @"
INSERT INTO dom.""AccountEnvironment""
           (""EnvironmentId""
           ,""UserId"")
     VALUES
           (@EnvironmentId
           ,@UserId)
";
            using (var con = _connectionProvider.Connection)
            {
                await con.ExecuteAsync(query, new
                {
                    EnvironmentId = environmentId,
                    UserId = userId,
                });
            }
        }
    }
}