using Domain0.Repository;
using Domain0.Repository.Model;
using Gerakul.FastSql.Common;
using System.Linq;
using System.Threading.Tasks;

namespace Domain0.FastSql
{
    public class EnvironmentRepository : RepositoryBase<int, Environment>, IEnvironmentRepository
    {
        public EnvironmentRepository(
            System.Func<DbContext> getContextFunc)
            : base(getContextFunc)
        { 
            TableName = "[dom].[Environment]";
            KeyName = "Id";
        }

        public async Task<Environment> GetByToken(string token)
        {
            var env = await getContext()
                .CreateSimple(
                    $"select * from {TableName} where " +
                    $"{nameof(Environment.Token)}=@p0", 
                    token)
                .ExecuteQueryAsync<Environment>()
                .FirstOrDefault();

            return env;
        }

        public async Task<Environment> GetDefault()
        {
            var env = await getContext()
                .CreateSimple(
                    $"select * from {TableName} where " +
                    $"{nameof(Environment.IsDefault)}=@p0",
                    1)
                .ExecuteQueryAsync<Environment>()
                .FirstOrDefault();

            return env;
        }
    }
}