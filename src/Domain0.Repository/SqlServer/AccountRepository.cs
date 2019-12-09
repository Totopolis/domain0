using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain0.Repository.Model;
using Gerakul.FastSql.Common;
using Gerakul.FastSql.SqlServer;

namespace Domain0.Repository.SqlServer
{
    public class AccountRepository : RepositoryBase<int, Account>, IAccountRepository
    {
        public AccountRepository(Func<DbContext> getContextFunc)
            : base(getContextFunc)
        {
            TableName = "[dom].[Account]";
        }

        public Task<Account> FindByLogin(string login)
            => getContext()
                .CreateSimple($"select * from {TableName} where {nameof(Account.Login)}=@p0", login)
                .ExecuteQueryAsync<Account>()
                .FirstOrDefault();

        public Task<Account> FindByPhone(decimal phone)
            => getContext()
                .CreateSimple(
                    $"select * from {TableName} where {nameof(Account.Phone)}=@p0", phone)
                .ExecuteQueryAsync<Account>()
                .FirstOrDefault();

        public Task<Account> FindByUserId(int userId)
            => getContext()
                .CreateSimple(
                    $"select * from {TableName} where id=@p0", userId)
                .ExecuteQueryAsync<Account>()
                .FirstOrDefault();

        public Task<Account[]> FindByUserIds(IEnumerable<int> userIds)
            => userIds.Any()
                ? getContext()
                    .CreateSimple($"select * from {TableName} where id in ({string.Join(",", userIds)})")
                    .ExecuteQueryAsync<Account>()
                    .ToArray()
                : getContext()
                    .CreateSimple($"select * from {TableName}")
                    .ExecuteQueryAsync<Account>()
                    .ToArray();

        public Task Lock(int userId)
            => getContext()
                .CreateSimple(
                    $"update {TableName} set {nameof(Account.IsLocked)} = 1 where {nameof(Account.Id)} = @p0",
                    userId)
                .ExecuteNonQueryAsync();

        public Task Unlock(int userId)
            => getContext()
                .CreateSimple(
                    $"update {TableName} set {nameof(Account.IsLocked)} = 0 where {nameof(Account.Id)} = @p0",
                    userId)
                .ExecuteNonQueryAsync();

        public new async Task<int> Insert(Account account)
        {
            // TODO use crosbase CreateInsertWithOutput
            return (int)await (getContext() as ISqlCommandCreator)
                .CreateInsertAndGetID(TableName, account, ignoreFields: KeyName)
                //.CreateInsertWithOutput(TableName, entity, ignoreFields: new [] { KeyName }, outputFields: KeyName)
                .ExecuteQueryFirstColumnAsync<decimal>()
                .First();
        }
    }
}
