using Gerakul.FastSql;
using Gerakul.FastSql.Common;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Domain0.FastSql
{
    public class DbManager
    {
        private readonly Func<DbContext> getContext;

        public DbManager(Func<DbContext> getContextFunc)
        {
            getContext = getContextFunc;
        }

        public void Initialize()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream("Domain0.WinService.Scripts.database.sql"))
            using (var reader = new StreamReader(stream))
            {
                var sql = reader.ReadToEnd();
                var scripts = sql.Split(new[] {"GO", "go", "Go", "gO"}, short.MaxValue, StringSplitOptions.RemoveEmptyEntries);
                foreach (var script in scripts)
                {
                    try
                    {
                        getContext()
                            .CreateSimple(script)
                            .ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError(ex.Message);
                        throw ex;
                    }
                }
            }

        }
    }
}
