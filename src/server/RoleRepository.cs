using Domain0.Repository;
using Domain0.Repository.Model;
using Gerakul.FastSql;
using System.Linq;
using System.Threading.Tasks;

namespace Domain0.FastSql
{
    public class RoleRepository : IRoleRepository
    {
        private readonly string _connectionString;

        public const string TableName = "[dom].[Role]";

        public const string UserRoleTableName = "[dom].[RoleUser]";

        public RoleRepository(string connectionString)
            => _connectionString = connectionString;

        public Task AddUserToDefaultRoles(int userId)
            => SimpleCommand.ExecuteNonQueryAsync(_connectionString,
                $@"insert into {UserRoleTableName}(roleId, userId)
select r.id, @p0 from {TableName} r where {nameof(Role.IsDefault)}=1 
    and not exists (select top 1 1 from {UserRoleTableName} ru where ru.roleId=r.id and ru.userid=@p0)", userId);

        public Task AddUserToRoles(int userId, params string[] roles)
            => SimpleCommand.ExecuteNonQueryAsync(_connectionString,
                $@"insert into {UserRoleTableName}(roleId, userId)
select r.id, @p0 from {TableName} r where r.{nameof(Role.Name)} in ({string.Join(",", roles.Select(role => $"'{role}'"))})
    and not exists (select top 1 1 from {UserRoleTableName} ru where ru.roleId=r.id and ru.userid=@p0)", userId);

        public Task<Role> GetById(string id)
            => SimpleCommand.ExecuteQueryAsync<Role>(_connectionString,
                    $"select * from {TableName} where {nameof(Role.Id)}=@p0", id)
                .FirstOrDefault();

        public Task<Role[]> GetByIds(params string[] ids)
            => SimpleCommand.ExecuteQueryAsync<Role>(_connectionString,
                    $"select * from {TableName} where {nameof(Role.Id)} in ({string.Join(",", ids)})")
                .ToArray();
    }
}
