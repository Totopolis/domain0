using Gerakul.FastSql;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Domain0.FastSql
{
    public class DbManager
    {
        private readonly string _connectionString;

        public DbManager(string connectionString)
        {
            _connectionString = connectionString;
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
                        SimpleCommand.ExecuteNonQuery(_connectionString, script);
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
