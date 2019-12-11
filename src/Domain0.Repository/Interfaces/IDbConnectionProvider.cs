using System.Data;

namespace Domain0.Repository
{
    public interface IDbConnectionProvider
    {
        IDbConnection Connection { get; }
    }
}