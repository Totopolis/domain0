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
            return await FindByIds(filter.PermissionIds);
        }

        public async Task<RolePermission[]> FindByFilter(Model.RolePermissionFilter filter)
        {
            var roleIds = string.Join(",", filter.RoleIds);

            var rolePermissions = await SimpleCommand.ExecuteQueryAsync<RolePermission>(
                    connectionString,
                    $"select p.*, pr.RoleId from {PermissionTableName} p " +
                    $"join {PermissionRoleTableName} pr on " +
                    $"  p.Id = pr.PermissionId " +
                    $"where pr.RoleId in ({roleIds}) ")
                .ToArray(); 
            return rolePermissions;
        }

        public async Task<UserPermission[]> FindByFilter(Model.UserPermissionFilter filter)
        {
            var userIds = string.Join(",", filter.UserIds);

            var rolePermissions = await SimpleCommand.ExecuteQueryAsync<UserPermission>(
                    connectionString,
                    $"select p.*, ru.UserId, pr.RoleId from {PermissionTableName} p " +
                    $"join {PermissionRoleTableName} pr on " +
                    $"       p.Id = pr.PermissionId " +
                    $"join {RoleRepository.UserRoleTableName} ru on " +
                    $"       ru.RoleId = pr.RoleId " +
                    $"where ru.UserId in ({userIds}) " +
                    $"union all " +
                    $"select p.*, pu.UserId, null as RoleId from dom.Permission p " +
                    $"join {PermissionUserTableName} pu on " +
                    $"   pu.PermissionId = p.Id " +
                    $"where pu.UserId in ({userIds})")
                .ToArray();

            return rolePermissions;
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
