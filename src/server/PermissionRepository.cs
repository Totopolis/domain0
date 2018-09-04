using Domain0.Repository.Model;
using Domain0.Repository;
using Gerakul.FastSql;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Domain0.FastSql
{
    public class PermissionRepository : RepositoryBase<int, Permission>, IPermissionRepository
    {
        public const string PermissionRoleTableName = "[dom].[PermissionRole]";
        public const string PermissionUserTableName = "[dom].[PermissionUser]";
        public const string PermissionTableName = "[dom].[Permission]";

        public PermissionRepository(string connectionString): base(connectionString)
        {
            TableName = PermissionTableName;
        }

        public Task AddUserPermission(int userId, int[] ids)
            => SimpleCommand.ExecuteNonQueryAsync(connectionString,
                $"insert into { PermissionUserTableName } " +
                $"(PermissionId, UserId) " +
                $"select Id as PermissionId, @p0 as UserId " +
                $"from { PermissionTableName } p " +
                $"where p.Id in ({string.Join(", ", ids)})",
                userId);

        public async Task<Permission[]> FindByFilter(Model.PermissionFilter filter)
        {
            var permissionIds = new HashSet<int>(filter.PermissionIds);

            var anyCondition = filter.RoleId.HasValue
                || filter.PermissionIds.Any();

            var anyConditionAdded = false;

            var query =
                $"select * from { TableName } p " +
                $"join { PermissionRoleTableName } pr on " +
                $"p.Id = pr.PermissionId ";

            if (anyCondition)
                query += "where ";

            if (filter.RoleId.HasValue)
            {
                anyConditionAdded = true;
                query += $"pr.RoleId = { filter.RoleId.Value } ";
            }

            if (filter.PermissionIds.Any())
            {
                if (anyConditionAdded)
                    query += " and ";

                anyConditionAdded = true;
                query += $"p.Id in ({ string.Join(",", filter.PermissionIds) })";
            }

            return await SimpleCommand.ExecuteQueryAsync<Permission>(
                    connectionString,
                    query)
                .ToArray();
        }

        public Task<Permission[]> GetByRoleId(int roleId)
            => SimpleCommand.ExecuteQueryAsync<Permission>(connectionString,
                    $"select * from { TableName } p" +
                    $"join { PermissionRoleTableName } pr on" +
                    $"      p.Id = pr.PermissionId" +
                    $"where pr.RoleId = @p0",
                    roleId)
                .ToArray();

        public Task<Permission[]> GetByUserId(int userId)
            => SimpleCommand.ExecuteQueryAsync<Permission>(connectionString,
                    $"select * from {TableName} " +
                    $"where {KeyName} in " +
                    $"(" +
                        // user permissions
                        $"  select PermissionId from {PermissionUserTableName} pu where pu.UserId = @p0" +
                        $"  union" +
                        // users roles permissions
                        $"  select PermissionId" +
                        $"  from {PermissionRoleTableName} pr " +
                        $"  join {RoleRepository.UserRoleTableName } ru on " +
                        $"      pr.RoleId = ru.RoleId" +
                        $"  where" +
                        $"      ru.UserId = @p0 " +
                    $")",
                    userId)
                .ToArray();

        public Task RemoveUserPermissions(int userId, int[] ids)
            => SimpleCommand.ExecuteNonQueryAsync(connectionString,
                $"delete from { PermissionUserTableName } " +
                $"where " +
                $"  UserId = @p0 " +
                $"  and PermissionId in ({string.Join(", ", ids)})",
                userId);
    }
}
