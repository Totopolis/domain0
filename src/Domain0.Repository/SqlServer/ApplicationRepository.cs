using System;
using Domain0.Repository.Model;
using Gerakul.FastSql.Common;

namespace Domain0.Repository.SqlServer
{
    public class ApplicationRepository : RepositoryBase<int, Application>, IApplicationRepository
    {
        public ApplicationRepository(Func<DbContext> getContextFunc)
            :base(getContextFunc)
        {
            TableName = "[dom].[Application]";
        }
    }
}
