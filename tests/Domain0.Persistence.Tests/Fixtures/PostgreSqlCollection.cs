using Xunit;

namespace Domain0.Persistence.Tests.Fixtures
{
    [CollectionDefinition("PostgreSql")]
    public class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
    {
        
    }
}