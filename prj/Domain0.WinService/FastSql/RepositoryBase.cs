using Domain0.Repository;
using Gerakul.FastSql;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain0.Repository
{
    public class RepositoryBase<TKey, TEntity> : IRepository<TKey, TEntity>
        where TEntity : new()
    {
        public RepositoryBase(string connectionStringArg)
        {
            connectionString = connectionStringArg;
            KeyName = "Id";
        }

        public Task<TEntity[]> FindByIds(IEnumerable<TKey> ids)
        {
            if (!ids.Any())
            {
                return SimpleCommand.ExecuteQueryAsync<TEntity>(
                        connectionString,
                        $"select * from {TableName}")
                    .ToArray();
            }

            var idsStr = string.Join(",", ids);

            return SimpleCommand.ExecuteQueryAsync<TEntity>(
                    connectionString,
                    $"select * from {TableName} " +
                    $"where { KeyName } in ({idsStr})")
                .ToArray();
        }

        public async Task<decimal> Insert(TEntity entity)
            => await MappedCommand.InsertAndGetIdAsync(connectionString, TableName, entity, KeyName);


        public Task Update(TEntity entity)
            => MappedCommand.UpdateAsync(connectionString, TableName, entity, KeyName);

        public Task Delete(TKey id)
            => SimpleCommand.ExecuteNonQueryAsync(connectionString,
                $"delete from {TableName} " +
                $"where { KeyName } = @p0",
                id);

        protected readonly string connectionString;

        protected string TableName { get; set; }

        protected string KeyName { get; set; }
    }
}
