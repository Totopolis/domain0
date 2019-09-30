using Domain0.Repository.Model;
using Domain0.Repository;
using System.Linq;
using System.Threading.Tasks;
using Gerakul.FastSql.Common;
using System;
using System.Collections.Generic;

namespace Domain0.FastSql
{
    public class PermissionRepository : RepositoryBase<int, Permission>, IPermissionRepository
    {
        public const string PermissionRoleTableName = "[dom].[PermissionRole]";
        public const string PermissionUserTableName = "[dom].[PermissionUser]";
        public const string PermissionTableName = "[dom].[Permission]";

        public PermissionRepository(Func<DbContext> getContextFunc)
            : base(getContextFunc)
        {
            TableName = PermissionTableName;
        }

        public async Task AddUserPermission(int userId, int[] ids)
        {
            if (!ids.Any())
                return;

            await getContext()
                .CreateSimple(
                    $"insert into { PermissionUserTableName } " +
                    $"(PermissionId, UserId) " +
                    $"select Id as PermissionId, @p0 as UserId " +
                    $"from { PermissionTableName } p " +
                    $"where p.Id in ({string.Join(", ", ids)})",
                    userId)
                .ExecuteNonQueryAsync();
        }

        public async Task<RolePermission[]> FindRolePermissionsByRoleIds(List<int> ids)
        {
            if (!ids.Any())
                return new RolePermission[0];

            var roleIds = string.Join(",", ids);

            var rolePermissions = await getContext()
                .CreateSimple(
                    $"select p.*, pr.RoleId from {PermissionTableName} p " +
                    $"join {PermissionRoleTableName} pr on " +
                    $"  p.Id = pr.PermissionId " +
                    $"where pr.RoleId in ({roleIds}) ")
                .ExecuteQueryAsync<RolePermission>()
                .ToArray(); 

            return rolePermissions;
        }

        public async Task<UserPermission[]> FindUserPermissionsByUserIds(List<int> ids)
        {
            if (!ids.Any())
                return new UserPermission[0];

            var userIds = string.Join(",", ids);

            var rolePermissions = await getContext()
                .CreateSimple(
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
                .ExecuteQueryAsync<UserPermission>()
                .ToArray();

            return rolePermissions;
        }

        public Task<Permission[]> GetByRoleId(int roleId)
            => getContext()
                .CreateSimple(
                    $"select * from { TableName } p" +
                    $"join { PermissionRoleTableName } pr on" +
                    $"      p.Id = pr.PermissionId" +
                    $"where pr.RoleId = @p0",
                    roleId)
                .ExecuteQueryAsync<Permission>()
                .ToArray();

        public Task<Permission[]> GetByUserId(int userId)
            => getContext()
                .CreateSimple(
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
                .ExecuteQueryAsync<Permission>()
                .ToArray();

        public async Task RemoveUserPermissions(int userId, int[] ids)
        {
            if (!ids.Any())
                return;

            await getContext()
                .CreateSimple(
                    $"delete from { PermissionUserTableName } " +
                    $"where " +
                    $"  UserId = @p0 " +
                    $"  and PermissionId in ({string.Join(", ", ids)})",
                    userId)
                .ExecuteNonQueryAsync();
        }
    }
}
