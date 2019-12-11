using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Domain0.Repository.Model;

namespace Domain0.Repository.SqlServer
{
    public class RoleRepository : IRoleRepository
    {
        private readonly IDbConnectionProvider _connectionProvider;

        public RoleRepository(IDbConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        public async Task<int> Insert(Role entity)
        {
            const string query = @"
INSERT INTO [dom].[Role]
           ([Name]
           ,[Description]
           ,[IsDefault])
     VALUES
           (@Name
           ,@Description
           ,@IsDefault)
;select SCOPE_IDENTITY() id
";
            using (var con = _connectionProvider.Connection)
            {
                return await con.ExecuteScalarAsync<int>(query, entity);
            }
        }

        public async Task Update(Role entity)
        {
            const string query = @"
UPDATE [dom].[Role]
   SET [Name] = @Name
      ,[Description] = @Description
      ,[IsDefault] = @IsDefault
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
                    @"delete from [dom].[Role] where [Id] = @Id",
                    new {Id = id});
            }
        }

        public async Task<Role[]> FindByIds(IEnumerable<int> ids)
        {
            var listIds = ids.ToList();
            if (!listIds.Any())
            {
                const string query = @"
SELECT [Id]
      ,[Name]
      ,[Description]
      ,[IsDefault]
  FROM [dom].[Role]
";
                using (var con = _connectionProvider.Connection)
                {
                    var result = await con.QueryAsync<Role>(query);
                    return result.ToArray();
                }
            }

            const string queryIn = @"
SELECT [Id]
      ,[Name]
      ,[Description]
      ,[IsDefault]
  FROM [dom].[Role]
where [Id] in @Ids
";
            using (var con = _connectionProvider.Connection)
            {
                var result = await con.QueryAsync<Role>(queryIn, new {Ids = listIds});
                return result.ToArray();
            }
        }

        public async Task AddRolePermissions(int roleId, int[] ids)
        {
            const string query = @"
insert into [dom].[PermissionRole] ([PermissionId], [RoleId])
select [Id] as [PermissionId], @RoleId as [RoleId]
from [dom].[Permission] p
where p.[Id] in @Ids
";
            using (var con = _connectionProvider.Connection)
            {
                await con.ExecuteAsync(query, new {RoleId = roleId, Ids = ids});
            }
        }

        public async Task AddUserRoles(int userId, int[] ids)
        {
            const string query = @"
insert into [dom].[RoleUser] ([RoleId], [UserId])
select [Id] as [RoleId], @UserId as [UserId]
from [dom].[Role] r
where r.[Id] in @Ids
";
            using (var con = _connectionProvider.Connection)
            {
                await con.ExecuteAsync(query, new {UserId = userId, Ids = ids});
            }
        }

        public async Task AddUserToDefaultRoles(int userId)
        {
            const string query = @"
insert into [dom].[RoleUser] ([RoleId], [UserId])
select [Id] as [RoleId], @UserId as [UserId]
from [dom].[Role] r
where r.[IsDefault] = 1
  and not exists (
    select top 1 1
    from [dom].[RoleUser] ru
    where ru.[RoleId] = r.[Id]
      and ru.[UserId] = @UserId
  )
";
            using (var con = _connectionProvider.Connection)
            {
                await con.ExecuteAsync(query, new {UserId = userId});
            }
        }

        public async Task AddUserToRoles(int userId, params string[] roles)
        {
            const string query = @"
insert into [dom].[RoleUser] ([RoleId], [UserId])
select [Id] as [RoleId], @UserId as [UserId]
from [dom].[Role] r
where r.[Name] in @Roles
  and not exists (
    select top 1 1
    from [dom].[RoleUser] ru
    where ru.[RoleId] = r.[Id]
      and ru.[UserId] = @UserId
  )
";
            using (var con = _connectionProvider.Connection)
            {
                await con.ExecuteAsync(query, new {UserId = userId, Roles = roles});
            }
        }

        public async Task<Role[]> GetByRoleNames(params string[] roleNames)
        {
            const string query = @"
SELECT [Id]
      ,[Name]
      ,[Description]
      ,[IsDefault]
  FROM [dom].[Role]
where [Name] in @Roles
";
            using (var con = _connectionProvider.Connection)
            {
                var result = await con.QueryAsync<Role>(query, new {Roles = roleNames});
                return result.ToArray();
            }
        }

        public async Task RemoveRolePermissions(int roleId, int[] ids)
        {
            const string query = @"
delete from [dom].[PermissionRole]
where [RoleId] = @RoleId
  and [PermissionId] in @Ids
";
            using (var con = _connectionProvider.Connection)
            {
                await con.ExecuteAsync(query, new {RoleId = roleId, Ids = ids});
            }
        }

        public async Task RemoveUserRole(int userId, int[] ids)
        {
            const string query = @"
delete from [dom].[RoleUser]
where [UserId] = @UserId
  and [RoleId] in @Ids
";
            using (var con = _connectionProvider.Connection)
            {
                await con.ExecuteAsync(query, new {UserId = userId, Ids = ids});
            }
        }

        public async Task<UserRole[]> FindByUserIds(IEnumerable<int> userIds)
        {
            var listIds = userIds.ToList();
            if (!listIds.Any())
            {
                const string query = @"
select r.[Id]
      ,r.[Name]
      ,r.[Description]
      ,r.[IsDefault]
      ,ru.[UserId]
from [dom].[Role] r
join [dom].[RoleUser] ru on r.[Id] = ru.[RoleId]
";
                using (var con = _connectionProvider.Connection)
                {
                    var result = await con.QueryAsync<UserRole>(query);
                    return result.ToArray();
                }
            }

            const string queryIn = @"
select r.[Id]
      ,r.[Name]
      ,r.[Description]
      ,r.[IsDefault]
      ,ru.[UserId]
from [dom].[Role] r
join [dom].[RoleUser] ru on r.[Id] = ru.[RoleId]
where ru.[UserId] in @Ids
";
            using (var con = _connectionProvider.Connection)
            {
                var result = await con.QueryAsync<UserRole>(queryIn, new {Ids = listIds});
                return result.ToArray();
            }
        }
    }
}