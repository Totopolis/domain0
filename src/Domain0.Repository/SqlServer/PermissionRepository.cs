using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Domain0.Repository.Model;

namespace Domain0.Repository.SqlServer
{
    public class PermissionRepository : IPermissionRepository
    {
        private readonly IDbConnectionProvider _connectionProvider;

        public PermissionRepository(IDbConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        public async Task<int> Insert(Permission entity)
        {
            const string query = @"
INSERT INTO [dom].[Permission]
           ([ApplicationId]
           ,[Name]
           ,[Description])
     VALUES
           (@ApplicationId
           ,@Name
           ,@Description)
;select SCOPE_IDENTITY() id
";
            using (var con = _connectionProvider.Connection)
            {
                return await con.ExecuteScalarAsync<int>(query, entity);
            }
        }

        public async Task Update(Permission entity)
        {
            const string query = @"
UPDATE [dom].[Permission]
   SET [ApplicationId] = @ApplicationId
      ,[Name] = @Name
      ,[Description] = @Description
 WHERE [Id] = @Id
";
            using (var con = _connectionProvider.Connection)
            {
                await con.ExecuteAsync(query, entity);
            }
        }

        public async Task Delete(int id)
        {
            using (var con = _connectionProvider.Connection)
            {
                await con.ExecuteAsync(
                    @"delete from [dom].[Permission] where [Id] = @Id",
                    new { Id = id });
            }
        }

        public async Task<Permission[]> FindByIds(IEnumerable<int> ids)
        {
            var listIds = ids.ToList();
            if (!listIds.Any())
            {
                const string query = @"
SELECT [Id]
      ,[ApplicationId]
      ,[Name]
      ,[Description]
  FROM [dom].[Permission]
";
                using (var con = _connectionProvider.Connection)
                {
                    var result = await con.QueryAsync<Permission>(query);
                    return result.ToArray();
                }
            }

            const string queryIn = @"
SELECT [Id]
      ,[ApplicationId]
      ,[Name]
      ,[Description]
  FROM [dom].[Permission]
where [Id] in @Ids
";
            using (var con = _connectionProvider.Connection)
            {
                var result = await con.QueryAsync<Permission>(queryIn, new { Ids = listIds });
                return result.ToArray();
            }
        }

        public async Task AddUserPermission(int userId, int[] ids)
        {
            if (!ids.Any())
                return;

            const string query = @"
insert into [dom].[PermissionUser] ([PermissionId], [UserId])
select [Id] as [PermissionId], @UserId as [UserId]
from [dom].[Permission] p
where p.[Id] in @Ids
";
            using (var con = _connectionProvider.Connection)
            {
                await con.ExecuteAsync(query, new {UserId = userId, Ids = ids});
            }
        }

        public async Task<RolePermission[]> FindRolePermissionsByRoleIds(List<int> ids)
        {
            if (!ids.Any())
                return new RolePermission[0];

            const string query = @"
select p.[Id]
      ,pr.[RoleId]
      ,p.[ApplicationId]
      ,p.[Name]
      ,p.[Description]
from [dom].[Permission] p
join [dom].[PermissionRole] pr on p.[Id] = pr.[PermissionId]
where pr.[RoleId] in @Ids
";
            using (var con = _connectionProvider.Connection)
            {
                var result = await con.QueryAsync<RolePermission>(query, new {Ids = ids});
                return result.ToArray();
            }
        }

        public async Task<UserPermission[]> FindUserPermissionsByUserIds(List<int> ids)
        {
            if (!ids.Any())
                return new UserPermission[0];

            var idsStr = string.Join(",", ids);
            var query = $@"
select p.[Id]
      ,ru.[UserId]
      ,pr.[RoleId]
      ,p.[ApplicationId]
      ,p.[Name]
      ,p.[Description]
from [dom].[Permission] p
join [dom].[PermissionRole] pr on p.[Id] = pr.[PermissionId]
join [dom].[RoleUser] ru on pr.[RoleId] = ru.[RoleId]
where ru.[UserId] in ({idsStr})
union all
select p.[Id]
      ,pu.[UserId]
      ,null as [RoleId]
      ,p.[ApplicationId]
      ,p.[Name]
      ,p.[Description]
from [dom].[Permission] p
join [dom].[PermissionUser] pu on p.[Id] = pu.[PermissionId]
where pu.[UserId] in ({idsStr})
";
            using (var con = _connectionProvider.Connection)
            {
                var result = await con.QueryAsync<UserPermission>(query);
                return result.ToArray();
            }
        }

        public async Task<Permission[]> GetByRoleId(int roleId)
        {
            const string query = @"
select p.[Id]
      ,p.[ApplicationId]
      ,p.[Name]
      ,p.[Description]
from [dom].[Permission] p
join [dom].[PermissionRole] pr on p.[Id] = pr.[PermissionId]
where pr.[RoleId] = @RoleId
";
            using (var con = _connectionProvider.Connection)
            {
                var result = await con.QueryAsync<Permission>(query, new { RoleId = roleId });
                return result.ToArray();
            }
        }


        public async Task<Permission[]> GetByUserId(int userId)
        {
            const string query = @"
select p.[Id]
      ,p.[ApplicationId]
      ,p.[Name]
      ,p.[Description]
from [dom].[Permission] p
where [Id] in (
    select [PermissionId]
    from [dom].[PermissionUser] pu
    where pu.[UserId] = @UserId
    union
    select [PermissionId]
    from [dom].[PermissionRole] pr
    join [dom].[RoleUser] ru on pr.[RoleId] = ru.[RoleId]
    where ru.[UserId] = @UserId
)
";
            using (var con = _connectionProvider.Connection)
            {
                var result = await con.QueryAsync<Permission>(query, new { UserId = userId });
                return result.ToArray();
            }
        }

        public async Task RemoveUserPermissions(int userId, int[] ids)
        {
            if (!ids.Any())
                return;

            const string query = @"
delete from [dom].[PermissionUser]
where [UserId] = @UserId
  and [PermissionId] in @Ids
";
            using (var con = _connectionProvider.Connection)
            {
                await con.ExecuteAsync(query, new {UserId = userId, Ids = ids});
            }
        }
    }
}
