using System.Threading.Tasks;
using Dapper;
using Domain0.Repository.Model;

namespace Domain0.Repository.PostgreSql
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
select ""Id"", ""UserId"", ""AccessToken"", ""IssuedAt"", ""ExpiredAt""
from dom.""TokenRegistration""
where ""Id"" = @Id
";
            using (var con = _connectionProvider.Connection)
            {
                return await con.QueryFirstOrDefaultAsync<TokenRegistration>(query, new {Id = id});
            }
        }

        public async Task Save(TokenRegistration registration)
        {
            if (registration.Id > 0)
            {
                const string query = @"
update dom.""TokenRegistration""
set ""UserId"" = @UserId, ""AccessToken"" = @AccessToken, ""IssuedAt"" = @IssuedAt, ""ExpiredAt"" = @ExpiredAt
where ""Id"" = @Id
";
                using (var con = _connectionProvider.Connection)
                {
                    await con.ExecuteAsync(query, registration);
                }
            }
            else
            {
                const string query = @"
insert into dom.""TokenRegistration""
(""UserId"", ""AccessToken"", ""IssuedAt"", ""ExpiredAt"")
values
(@UserId, @AccessToken, @IssuedAt, @ExpiredAt)
returning ""Id""
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
                    @"delete from dom.""TokenRegistration"" where ""UserId"" = @UserId",
                    new {UserId = userId});
            }
        }
    }
}