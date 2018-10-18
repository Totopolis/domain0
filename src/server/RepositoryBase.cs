using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gerakul.FastSql.Common;
using Gerakul.FastSql.SqlServer;

namespace Domain0.Repository
{
    public class RepositoryBase<TKey, TEntity> : IRepository<TKey, TEntity>
        where TEntity : new()
    {
        public RepositoryBase(Func<DbContext> getContextFunc)
        {
            getContext = getContextFunc;
            KeyName = "Id";
        }

        public Task<TEntity[]> FindByIds(IEnumerable<TKey> ids)
        {
            if (!ids.Any())
            {
                return getContext()
                    .CreateSimple($"select * from {TableName}")
                    .ExecuteQueryAsync<TEntity>()
                    .ToArray();
            }

            var idsStr = string.Join(",", ids);

            return getContext()
                .CreateSimple(
                    $"select * from {TableName} " +
                    $"where { KeyName } in ({idsStr})")
                .ExecuteQueryAsync<TEntity>()
                .ToArray();
        }

        public async Task<decimal> Insert(TEntity entity)
        {
            // TODO use crosbase CreateInsertWithOutput
            return await (getContext() as ISqlCommandCreator)
                .CreateInsertAndGetID(TableName, entity, ignoreFields: KeyName)
                //.CreateInsertWithOutput(TableName, entity, ignoreFields: new [] { KeyName }, outputFields: KeyName)
                .ExecuteQueryFirstColumnAsync<decimal>()
                .First();
        }

        public Task Update(TEntity entity)
            => getContext()
                .UpdateAsync(TableName, entity, KeyName);

        public Task Delete(TKey id)
            //=> getContext().DeleteAsync(TableName, id, KeyName);
            => getContext()
                .CreateSimple(
                    $"delete from { TableName } " +
                    $"where " +
                    $"  { KeyName } = @p0 ",
                    id)
                .ExecuteNonQueryAsync();

        protected readonly Func<DbContext> getContext;

        protected string TableName { get; set; }

        protected string KeyName { get; set; }
    }
}
