using Xunit;

namespace Domain0.Persistence.Tests.Fixtures
{
    [CollectionDefinition("SqlServer")]
    public class SqlServerCollection : ICollectionFixture<SqlServerFixture>
    {
        
    }
}