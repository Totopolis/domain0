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
    public class SqlServerRoleRepositoryTest : RoleRepositoryTest
    {
        public SqlServerRoleRepositoryTest(SqlServerFixture fixture) : base(fixture.Container)
        {
        }
    }

    [Collection("PostgreSql")]
    public class PostgreSqlRoleRepositoryTest : RoleRepositoryTest
    {
        public PostgreSqlRoleRepositoryTest(PostgreSqlFixture fixture) : base(fixture.Container)
        {
        }
    }

    public abstract class RoleRepositoryTest
    {
        private readonly IContainer _container;

        public RoleRepositoryTest(IContainer container)
        {
            _container = container;
        }

        [Fact]
        public async Task CrudOne()
        {
            var roles = _container.Resolve<IRoleRepository>();

            // CREATE
            var role = new Role
            {
                Name = "role",
                IsDefault = false,
            };

            role.Id = await roles.Insert(role);

            role.Id.Should().BeGreaterThan(0);

            // READ
            var result = await roles.FindByIds(new[] {role.Id});
            result.Should().BeEquivalentTo(role);
            result = await roles.GetByRoleNames(role.Name);
            result.Should().BeEquivalentTo(role);

            // UPDATE
            role.Name = "new role";

            await roles.Update(role);

            result = await roles.FindByIds(new[] {role.Id});
            result.Should().BeEquivalentTo(role);
            result = await roles.GetByRoleNames(role.Name);
            result.Should().BeEquivalentTo(role);

            // DELETE
            await roles.Delete(role.Id);

            result = await roles.FindByIds(new[] {role.Id});
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task UserRoles()
        {
            var accRepo = _container.Resolve<IAccountRepository>();
            var acc = new Account {Login = "user"};
            acc.Id = await accRepo.Insert(acc);
            var roles = _container.Resolve<IRoleRepository>();
            var role = new Role
            {
                Name = "role",
                IsDefault = false,
            };
            role.Id = await roles.Insert(role);
            var roleDefault = new Role
            {
                Name = "role-default",
                IsDefault = true,
            };
            roleDefault.Id = await roles.Insert(roleDefault);
            var expected = new UserRole
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                IsDefault = role.IsDefault,
                UserId = acc.Id,
            };
            var expectedDefault = new UserRole
            {
                Id = roleDefault.Id,
                Name = roleDefault.Name,
                Description = roleDefault.Description,
                IsDefault = roleDefault.IsDefault,
                UserId = acc.Id,
            };

            // AddUserRoles
            await roles.AddUserRoles(acc.Id, new[] {role.Id, roleDefault.Id});

            var result = await roles.FindByUserIds(new[] {acc.Id});
            result.Should().BeEquivalentTo(expected, expectedDefault);

            // RemoveUserRole - 1
            await roles.RemoveUserRole(acc.Id, new[] {role.Id});

            result = await roles.FindByUserIds(new[] {acc.Id});
            result.Should().BeEquivalentTo(expectedDefault);

            // RemoveUserRole - 2
            await roles.RemoveUserRole(acc.Id, new[] {roleDefault.Id});

            result = await roles.FindByUserIds(new[] {acc.Id});
            result.Should().BeEmpty();

            // AddUserToDefaultRoles
            await roles.AddUserToDefaultRoles(acc.Id);

            result = await roles.FindByUserIds(new[] {acc.Id});
            result.Should().BeEquivalentTo(expectedDefault);
            await roles.RemoveUserRole(acc.Id, new[] {roleDefault.Id});

            // AddUserToRoles
            await roles.AddUserToRoles(acc.Id, role.Name);

            result = await roles.FindByUserIds(new[] {acc.Id});
            result.Should().BeEquivalentTo(expected);

            await roles.RemoveUserRole(acc.Id, new[] {role.Id});
            await roles.Delete(role.Id);
        }
    }
}