using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain0.Repository;
using Domain0.Repository.Model;
using Gerakul.FastSql;

namespace Domain0.FastSql
{
    public class AccountRepository : IAccountRepository
    {
        public const string TableName = "[dom].[Account]";

        private readonly string _connectionString;

        public AccountRepository(string connectionString)
            => _connectionString = connectionString;

        public Task<Account> FindByLogin(string login)
            => SimpleCommand.ExecuteQueryAsync<Account>(_connectionString,
                $"select * from {TableName} where {nameof(Account.Login)}=@p0", login).FirstOrDefault();

        public Task<Account> FindByPhone(decimal phone)
            => SimpleCommand.ExecuteQueryAsync<Account>(_connectionString,
                $"select * from {TableName} where {nameof(Account.Phone)}=@p0", phone).FirstOrDefault();

        public Task<Account> FindByUserId(int userId)
            => SimpleCommand.ExecuteQueryAsync<Account>(_connectionString,
                $"select * from {TableName} where id=@p0", userId).FirstOrDefault();

        public Task<Account[]> FindByUserIds(IEnumerable<int> userIds)
            => userIds.Any()
                ? SimpleCommand.ExecuteQueryAsync<Account>(_connectionString,
                    $"select * from {TableName} where id in ({string.Join(",", userIds)})").ToArray()
                : Task.FromResult(new Account[0]);

        public async Task<int> Insert(Account account)
            => (int) await MappedCommand.InsertAndGetIdAsync(_connectionString, TableName, account, nameof(Account.Id));

        public Task Update(Account account)
            => MappedCommand.UpdateAsync(_connectionString, TableName, account, nameof(Account.Id));
    }
}
