using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autofac;
using Domain0.Repository.Settings;
using Domain0.Repository.SqlServer;
using Microsoft.Data.SqlClient;
using NLog;

namespace Domain0.Persistence.Tests.Fixtures
{
    public class SqlServerFixture
    {
        public SqlServerFixture()
        {
            var conn = new SqlConnection(ConnectionStrings.SqlServer);

            var isConnected = false;
            while (!isConnected)
            {
                System.Threading.Thread.Sleep(1000);
                try
                {
                    conn.Open();
                    isConnected = true;
                }
                catch
                {
                    // ignore
                }
            }

            var script = File.ReadAllText(@"./db/mssql/db.sql");
            var statements = SplitSqlStatements(script);
            foreach (var commandString in statements)
            {
                using (var command = new SqlCommand(commandString, conn))
                {
                    command.ExecuteNonQuery();
                }
            }

            conn.Close();

            var builder = new ContainerBuilder();

            builder.Register(c => LogManager.GetCurrentClassLogger()).As<ILogger>().InstancePerDependency();
            builder.RegisterModule(new DatabaseModule(new DbSettings
            {
                ConnectionString = ConnectionStrings.SqlServer,
                Provider = DbProvider.SqlServer,
            }));

            Container = builder.Build();
        }

        private static IEnumerable<string> SplitSqlStatements(string sqlScript)
        {
            return sqlScript.Split(new[] {"GO", "Go", "gO", "go"}, StringSplitOptions.RemoveEmptyEntries)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim(' ', '\r', '\n'));
        }

        public IContainer Container { get; set; }
    }
}