using Domain0.Repository;
using Gerakul.FastSql;
using System.Linq;
using System.Threading.Tasks;

namespace Domain0.FastSql
{
    public class PermissionRepository : IPermissionRepository
    {
        private readonly string _connectionString;

        public const string TableName = "[dom].[Permission]";

        public const string PermissionRoleTableName = "[dom].[PermissionRole]";

        public PermissionRepository(string connectionString)
            => _connectionString = connectionString;

        public Task<string[]> GetByUserId(int userId)
            => SimpleCommand.ExecuteQueryFirstColumnAsync<string>(_connectionString,
                $"select p.Name from {RoleRepository.UserRoleTableName} ru join {PermissionRoleTableName} pr on pr.roleId=ru.roleid join {TableName} p on pr.PermissionId=p.Id where ru.UserId=@p0",
                userId).ToArray();
    }
}
