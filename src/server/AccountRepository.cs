using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain0.Repository;
using Domain0.Repository.Model;
using Gerakul.FastSql;

namespace Domain0.FastSql
{
    public class AccountRepository : RepositoryBase<int, Account>, IAccountRepository
    {
        public AccountRepository(string connectionString)
            : base(connectionString)
        {
            TableName = "[dom].[Account]";
        }

        public Task<Account> FindByLogin(string login)
            => SimpleCommand.ExecuteQueryAsync<Account>(connectionString,
                $"select * from {TableName} where {nameof(Account.Login)}=@p0", login).FirstOrDefault();

        public Task<Account> FindByPhone(decimal phone)
            => SimpleCommand.ExecuteQueryAsync<Account>(connectionString,
                $"select * from {TableName} where {nameof(Account.Phone)}=@p0", phone).FirstOrDefault();

        public Task<Account> FindByUserId(int userId)
            => SimpleCommand.ExecuteQueryAsync<Account>(connectionString,
                $"select * from {TableName} where id=@p0", userId).FirstOrDefault();

        public Task<Account[]> FindByUserIds(IEnumerable<int> userIds)
            => userIds.Any()
                ? SimpleCommand.ExecuteQueryAsync<Account>(connectionString,
                    $"select * from {TableName} where id in ({string.Join(",", userIds)})").ToArray()
                : SimpleCommand.ExecuteQueryAsync<Account>(connectionString,
                    $"select * from {TableName}").ToArray();

        public new async Task<int> Insert(Account account)
            => (int) await MappedCommand.InsertAndGetIdAsync(connectionString, TableName, account, nameof(Account.Id));

        public Task Lock(int userId)
            => SimpleCommand.ExecuteNonQueryAsync(
                connectionString,
                $"update {TableName} set {nameof(Account.IsLocked)} = 1 where {nameof(Account.Id)} = @p0",
                userId);

        public Task Unlock(int userId)
            => SimpleCommand.ExecuteNonQueryAsync(
                connectionString,
                $"update {TableName} set {nameof(Account.IsLocked)} = 0 where {nameof(Account.Id)} = @p0",
                userId);
    }
}
