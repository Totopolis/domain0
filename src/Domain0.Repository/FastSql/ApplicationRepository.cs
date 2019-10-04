using Domain0.Repository;
using Domain0.Repository.Model;
using Gerakul.FastSql.Common;
using System;

namespace Domain0.FastSql
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
