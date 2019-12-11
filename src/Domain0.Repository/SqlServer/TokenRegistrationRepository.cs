using System.Threading.Tasks;
using Dapper;
using Domain0.Repository.Model;

namespace Domain0.Repository.SqlServer
{
    public class TokenRegistrationRepository : ITokenRegistrationRepository
    {
        private readonly IDbConnectionProvider _connectionProvider;

        public TokenRegistrationRepository(IDbConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        public async Task<TokenRegistration> FindById(int id)
        {
            const string query = @"
select [Id]
      ,[UserId]
      ,[AccessToken]
      ,[IssuedAt]
      ,[ExpiredAt]
from [dom].[TokenRegistration]
where [Id] = @Id
";
            using (var con = _connectionProvider.Connection)
            {
                return await con.QueryFirstOrDefaultAsync<TokenRegistration>(query, new {Id = id});
            }
        }

        public async Task<TokenRegistration> FindLastTokenByUserId(int userId)
        {
            const string query = @"
select [Id]
      ,[UserId]
      ,[AccessToken]
      ,[IssuedAt]
      ,[ExpiredAt]
from [dom].[TokenRegistration]
where [UserId] = @UserId
order by [Id] desc
";
            using (var con = _connectionProvider.Connection)
            {
                return await con.QueryFirstOrDefaultAsync<TokenRegistration>(query, new {UserId = userId});
            }
        }

        public async Task Save(TokenRegistration registration)
        {
            if (registration.Id > 0)
            {
                const string query = @"
UPDATE [dom].[TokenRegistration]
   SET [UserId] = @UserId
      ,[AccessToken] = @AccessToken
      ,[IssuedAt] = @IssuedAt
      ,[ExpiredAt] = @ExpiredAt
 WHERE [Id] = @Id
";
                using (var con = _connectionProvider.Connection)
                {
                    await con.ExecuteAsync(query, registration);
                }
            }
            else
            {
                const string query = @"
INSERT INTO [dom].[TokenRegistration]
           ([UserId]
           ,[AccessToken]
           ,[IssuedAt]
           ,[ExpiredAt])
     VALUES
           (@UserId
           ,@AccessToken
           ,@IssuedAt
           ,@ExpiredAt)
;select SCOPE_IDENTITY() id
";
                using (var con = _connectionProvider.Connection)
                {
                    registration.Id = await con.ExecuteScalarAsync<int>(query, registration);
                }
            }
        }

        public async Task RevokeByUserId(int userId)
        {
            using (var con = _connectionProvider.Connection)
            {
                await con.ExecuteAsync(
                    @"delete from [dom].[TokenRegistration] where [UserId] = @UserId",
                    new {UserId = userId});
            }
        }
    }
}