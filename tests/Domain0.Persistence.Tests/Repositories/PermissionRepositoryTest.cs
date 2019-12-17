using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Domain0.Persistence.Tests.Fixtures;
using Domain0.Repository;
using Domain0.Repository.Model;
using FluentAssertions;
using Xunit;

namespace Domain0.Persistence.Tests.Repositories
{
    [Collection("SqlServer")]
    public class SqlServerPermissionRepositoryTest : PermissionRepositoryTest
    {
        public SqlServerPermissionRepositoryTest(SqlServerFixture fixture) : base(fixture.Container)
        {
        }
    }

    [Collection("PostgreSql")]
    public class PostgreSqlPermissionRepositoryTest : PermissionRepositoryTest
    {
        public PostgreSqlPermissionRepositoryTest(PostgreSqlFixture fixture) : base(fixture.Container)
        {
        }
    }

    public abstract class PermissionRepositoryTest
    {
        private readonly IContainer _container;

        public PermissionRepositoryTest(IContainer container)
        {
            _container = container;
        }

        [Fact]
        public async Task CrudOne()
        {
            var apps = _container.Resolve<IApplicationRepository>();
            var app = new Application {Name = "app"};
            app.Id = await apps.Insert(app);

            var permissions = _container.Resolve<IPermissionRepository>();

            // CREATE
            var permission = new Permission
            {
                Name = "permission",
                ApplicationId = app.Id,
            };

            permission.Id = await permissions.Insert(permission);

            permission.Id.Should().BeGreaterThan(0);

            // READ
            var result = await permissions.FindByIds(new[] {permission.Id});
            result.Should().BeEquivalentTo(permission);

            // UPDATE
            permission.Name = "new permission";

            await permissions.Update(permission);

            result = await permissions.FindByIds(new[] {permission.Id});
            result.Should().BeEquivalentTo(permission);

            // DELETE
            await permissions.Delete(permission.Id);

            result = await permissions.FindByIds(new[] {permission.Id});
            result.Should().BeEmpty();

            await apps.Delete(app.Id);
        }

        [Fact]
        public async Task UserPermissions()
        {
            var accRepo = _container.Resolve<IAccountRepository>();
            var acc = new Account {Login = "user"};
            acc.Id = await accRepo.Insert(acc);
            var apps = _container.Resolve<IApplicationRepository>();
            var app = new Application {Name = "app"};
            app.Id = await apps.Insert(app);
            var permissions = _container.Resolve<IPermissionRepository>();
            var permission = new Permission
            {
                Name = "permission",
                ApplicationId = app.Id,
            };
            permission.Id = await permissions.Insert(permission);


            // AddUserPermission
            await permissions.AddUserPermission(acc.Id, new[] {permission.Id});

            var result = await permissions.GetByUserId(acc.Id);
            result.Should().BeEquivalentTo(permission);

            // RemoveUserPermissions
            await permissions.RemoveUserPermissions(acc.Id, new[] {permission.Id});

            result = await permissions.GetByUserId(acc.Id);
            result.Should().BeEmpty();

            // FindUserPermissionsByUserIds
            await permissions.AddUserPermission(acc.Id, new[] {permission.Id});

            var userPermissions = await permissions.FindUserPermissionsByUserIds(new List<int> {acc.Id});

            userPermissions.Should().BeEquivalentTo(new UserPermission
            {
                Id = permission.Id,
                Name = permission.Name,
                Description = permission.Description,
                ApplicationId = permission.ApplicationId,
                RoleId = null,
                UserId = acc.Id,
            });
            await permissions.RemoveUserPermissions(acc.Id, new[] {permission.Id});

            // FindUserPermissionsByUserIds with Role
            var roles = _container.Resolve<IRoleRepository>();
            var role = new Role {Name = "role"};
            role.Id = await roles.Insert(role);
            await roles.AddUserRoles(acc.Id, new[] {role.Id});
            await permissions.AddRolePermissions(role.Id, new[] {permission.Id});
            userPermissions = await permissions.FindUserPermissionsByUserIds(new List<int> {acc.Id});
            userPermissions.Should().BeEquivalentTo(new UserPermission
            {
                Id = permission.Id,
                Name = permission.Name,
                Description = permission.Description,
                ApplicationId = permission.ApplicationId,
                RoleId = role.Id,
                UserId = acc.Id,
            });

            // Cleanup
            await permissions.RemoveRolePermissions(role.Id, new[] {permission.Id});
            await roles.RemoveUserRole(acc.Id, new[] {role.Id});
            await roles.Delete(role.Id);
            await permissions.Delete(permission.Id);
            await apps.Delete(app.Id);
            await accRepo.Delete(acc.Id);
        }

        [Fact]
        public async Task RolePermissions()
        {
            var accRepo = _container.Resolve<IAccountRepository>();
            var acc = new Account {Login = "user"};
            acc.Id = await accRepo.Insert(acc);

            var roles = _container.Resolve<IRoleRepository>();
            var role = new Role {Name = "role"};
            role.Id = await roles.Insert(role);

            var apps = _container.Resolve<IApplicationRepository>();
            var app = new Application {Name = "app"};
            app.Id = await apps.Insert(app);
            var permissions = _container.Resolve<IPermissionRepository>();
            var permission = new Permission
            {
                Name = "permission",
                ApplicationId = app.Id,
            };
            permission.Id = await permissions.Insert(permission);

            // AddRolePermissions
            await permissions.AddRolePermissions(role.Id, new[] {permission.Id});

            var result = await permissions.GetByRoleId(role.Id);
            result.Should().BeEquivalentTo(permission);

            // FindRolePermissionsByRoleIds
            var rolePermissions = await permissions.FindRolePermissionsByRoleIds(new List<int> {role.Id});

            rolePermissions.Should().BeEquivalentTo(new RolePermission
            {
                Id = permission.Id,
                Name = permission.Name,
                Description = permission.Description,
                ApplicationId = permission.ApplicationId,
                RoleId = role.Id,
            });

            // RemoveRolePermissions
            await permissions.RemoveRolePermissions(role.Id, new[] {permission.Id});

            result = await permissions.GetByRoleId(role.Id);
            result.Should().BeEmpty();

            // Cleanup
            await roles.Delete(role.Id);
            await permissions.Delete(permission.Id);
            await apps.Delete(app.Id);
        }
    }
}