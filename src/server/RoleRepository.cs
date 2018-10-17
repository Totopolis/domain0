using Domain0.Repository;
using Domain0.Repository.Model;
using Gerakul.FastSql.Common;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Domain0.FastSql
{
    public class RoleRepository : RepositoryBase<int, Role>, IRoleRepository
    {
        public const string UserRoleTableName = "[dom].[RoleUser]";

        public RoleRepository(Func<DbContext> getContextFunc)
            : base(getContextFunc)
        {
            TableName = "[dom].[Role]";
        }

        public Task AddRolePermissions(int roleId, int[] ids)
            => getContext()
                .CreateSimple(
                    $"insert into { PermissionRepository.PermissionRoleTableName } " +
                    $"(PermissionId, RoleId) " +
                    $"select Id as PermissionId, @p0 as RoleId " +
                    $"from { PermissionRepository.PermissionTableName } p " +
                    $"where p.Id in ({string.Join(", ", ids)})",
                    roleId)
                .ExecuteNonQueryAsync();

        public Task AddUserRoles(int userId, int[] ids)
            => getContext()
                .CreateSimple(
                    $"insert into { UserRoleTableName } " +
                    $"(RoleId, UserId) " +
                    $"select Id as RoleId, @p0 as UserId " +
                    $"from { TableName } r " +
                    $"where r.Id in ({string.Join(", ", ids)})",
                    userId)
                .ExecuteNonQueryAsync();

        public Task AddUserToDefaultRoles(int userId)
            => getContext()
                .CreateSimple(
                    $@"insert into {UserRoleTableName}(roleId, userId)
select r.id, @p0 from {TableName} r where {nameof(Role.IsDefault)}=1 
    and not exists (select top 1 1 from {UserRoleTableName} ru where ru.roleId=r.id and ru.userid=@p0)", userId)
                .ExecuteNonQueryAsync();

        public Task AddUserToRoles(int userId, params string[] roles)
        {
            var commandParams = new object[] {userId}.Concat(roles).ToArray();

            return getContext()
                .CreateSimple(
                    $"insert into {UserRoleTableName}(roleId, userId) " +
                    $"select r.id, @p0 from {TableName} r " +
                    $"where r.{nameof(Role.Name)} in " +
                    $"(" +
                    $"  {string.Join(",", roles.Select((r, i) => $"@P{i + 1}"))}" +
                    $")" +
                    $"and not exists " +
                    $"(" +
                    $"  select top 1 1 from {UserRoleTableName} ru " +
                    $"  where ru.roleId=r.id and ru.userid=@p0" +
                    $")", commandParams)
                .ExecuteNonQueryAsync();
        }

        public Task<Role[]> GetByRoleNames(params string[] roleNames)
            => getContext()
                .CreateSimple(
                    $"select * from {TableName} " +
                    $"where {nameof(Role.Name)} in " +
                    $"(" +
                    $"  {string.Join(",", roleNames.Select((r, i) => $"@P{i}"))}" +
                    $")",
                    roleNames)
                .ExecuteQueryAsync<Role>()
                .ToArray();

        public Task RemoveRolePermissions(int roleId, int[] ids)
            => getContext()
                .CreateSimple(
                    $"delete from { PermissionRepository.PermissionRoleTableName } " +
                    $"where " +
                    $"  RoleId = @p0 " +
                    $"  and PermissionId in ({string.Join(", ", ids)}) ",
                    roleId)
                .ExecuteNonQueryAsync();

        public Task RemoveUserRole(int userId, int[] ids)
            => getContext()
                .CreateSimple(
                    $"delete from { UserRoleTableName } " +
                    $"where " +
                    $"  UserId = @p0 " +
                    $"  and RoleId in ({string.Join(", ", ids)})",
                    userId)
                .ExecuteNonQueryAsync();
    }
}
