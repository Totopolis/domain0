using Domain0.Repository;
using Domain0.Repository.Model;

namespace Domain0.FastSql
{
    public class ApplicationRepository : RepositoryBase<int, Application>, IApplicationRepository
    {
        public ApplicationRepository(string connectionString) : base(connectionString)
        {
            TableName = "[dom].[Application]";
        }
    }
}
