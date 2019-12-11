using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Domain0.Repository.Model;

namespace Domain0.Repository.PostgreSql
{
    public class AccountRepository : IAccountRepository
    {
        private readonly IDbConnectionProvider _connectionProvider;

        public AccountRepository(IDbConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        public async Task<int> Insert(Account account)
        {
            const string query = @"
insert into dom.""Account""
(""Email"", ""Phone"", ""Login"", ""Password"", ""Name"", ""Description"", ""FirstDate"", ""LastDate"", ""IsLocked"")
values
(@Email, @Phone, @Login, @Password, @Name, @Description, @FirstDate, @LastDate, @IsLocked)
returning ""Id""
";
            using (var con = _connectionProvider.Connection)
            {
                return await con.ExecuteScalarAsync<int>(query, account);
            }
        }

        public async Task<Account> FindByLogin(string login)
        {
            const string query = @"
SELECT ""Id""
      ,""Email""
      ,""Phone""
      ,""Login""
      ,""Password""
      ,""Name""
      ,""Description""
      ,""FirstDate""
      ,""LastDate""
      ,""IsLocked""
  FROM dom.""Account""
where ""Login"" = @Login
";
            using (var con = _connectionProvider.Connection)
            {
                return await con.QueryFirstOrDefaultAsync<Account>(query, new {Login = login});
            }
        }

        public async Task<Account> FindByPhone(decimal phone)
        {
            const string query = @"
SELECT ""Id""
      ,""Email""
      ,""Phone""
      ,""Login""
      ,""Password""
      ,""Name""
      ,""Description""
      ,""FirstDate""
      ,""LastDate""
      ,""IsLocked""
  FROM dom.""Account""
where ""Phone"" = @Phone
";
            using (var con = _connectionProvider.Connection)
            {
                return await con.QueryFirstOrDefaultAsync<Account>(query, new {Phone = phone});
            }
        }

        public async Task<Account> FindByUserId(int userId)
        {
            const string query = @"
SELECT ""Id""
      ,""Email""
      ,""Phone""
      ,""Login""
      ,""Password""
      ,""Name""
      ,""Description""
      ,""FirstDate""
      ,""LastDate""
      ,""IsLocked""
  FROM dom.""Account""
where ""Id"" = @Id
";
            using (var con = _connectionProvider.Connection)
            {
                return await con.QueryFirstOrDefaultAsync<Account>(query, new {Id = userId});
            }
        }

        public async Task<Account[]> FindByUserIds(IEnumerable<int> userIds)
        {
            var listIds = userIds.ToList();
            if (listIds.Any())
            {
                const string query = @"
SELECT ""Id""
      ,""Email""
      ,""Phone""
      ,""Login""
      ,""Password""
      ,""Name""
      ,""Description""
      ,""FirstDate""
      ,""LastDate""
      ,""IsLocked""
  FROM dom.""Account""
where ""Id"" = any (@Ids)
";
                using (var con = _connectionProvider.Connection)
                {
                    var result = await con.QueryAsync<Account>(query, new {Ids = listIds});
                    return result.ToArray();
                }
            }
            else
            {
                const string query = @"
SELECT ""Id""
      ,""Email""
      ,""Phone""
      ,""Login""
      ,""Password""
      ,""Name""
      ,""Description""
      ,""FirstDate""
      ,""LastDate""
      ,""IsLocked""
  FROM dom.""Account""
";
                using (var con = _connectionProvider.Connection)
                {
                    var result = await con.QueryAsync<Account>(query);
                    return result.ToArray();
                }
            }
        }

        public async Task Update(Account entity)
        {
            const string query = @"
UPDATE dom.""Account""
   SET ""Email"" = @Email
      ,""Phone"" = @Phone
      ,""Login"" = @Login
      ,""Password"" = @Password
      ,""Name"" = @Name
      ,""Description"" = @Description
      ,""FirstDate"" = @FirstDate
      ,""LastDate"" = @LastDate
      ,""IsLocked"" = @IsLocked
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
                    @"delete from dom.""Account"" where ""Id"" = @Id ",
                    new {Id = id});
            }
        }

        public async Task Lock(int userId)
        {
            using (var con = _connectionProvider.Connection)
            {
                await con.ExecuteAsync(
                    @"update dom.""Account"" set ""IsLocked"" = TRUE where ""Id"" = @Id",
                    new {Id = userId});
            }
        }

        public async Task Unlock(int userId)
        {
            using (var con = _connectionProvider.Connection)
            {
                await con.ExecuteAsync(
                    @"update dom.""Account"" set ""IsLocked"" = FALSE where ""Id"" = @Id",
                    new {Id = userId});
            }
        }
    }
}