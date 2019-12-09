using System;
using System.Linq;
using System.Threading.Tasks;
using Domain0.Repository.Model;
using Gerakul.FastSql.Common;
using Gerakul.FastSql.SqlServer;

namespace Domain0.Repository.SqlServer
{
    public class TokenRegistrationRepository : ITokenRegistrationRepository
    {
        public const string TableName = "[dom].[TokenRegistration]";

        public TokenRegistrationRepository(Func<DbContext> getContextFunc)
        {
            getContext = getContextFunc;
        }

        public Task<TokenRegistration> FindById(int id)
            => getContext()
                .CreateSimple(
                    $"select * from {TableName} where {nameof(TokenRegistration.Id)} = @p0",
                    id)
                .ExecuteQueryAsync<TokenRegistration>()
                .FirstOrDefault();

        public Task<TokenRegistration> FindLastTokenByUserId(int userId)
            => getContext()
                .CreateSimple(
                    $"select top 1 * from {TableName} " +
                    $"where {nameof(TokenRegistration.UserId)} = @p0 " +
                    $"order by Id desc", 
                    userId)
                .ExecuteQueryAsync<TokenRegistration>()
                .FirstOrDefault();

        public async Task Save(TokenRegistration registration)
        {
            if (registration.Id > 0)
            {
                await getContext().UpdateAsync(TableName, registration, nameof(TokenRegistration.Id));
            }
            else
            {
                // TODO use crosbase CreateInsertWithOutput
                registration.Id = (int)await (getContext() as ISqlCommandCreator)
                    .CreateInsertAndGetID(TableName, registration, ignoreFields: nameof(TokenRegistration.Id))
                    //.CreateInsertWithOutput(TableName, entity, ignoreFields: new [] { KeyName }, outputFields: KeyName)
                    .ExecuteQueryFirstColumnAsync<decimal>()
                    .First();
            }
        }

        public Task RevokeByUserId(int userId)
            => getContext()
                .CreateSimple(
                    $"delete from {TableName} where {nameof(TokenRegistration.UserId)} = @p0",
                    userId)
                .ExecuteNonQueryAsync();

        private readonly Func<DbContext> getContext;
    }
}
