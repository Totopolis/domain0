using System.IO;
using Autofac;
using Domain0.Repository.PostgreSql;
using Domain0.Repository.Settings;
using NLog;
using Npgsql;

namespace Domain0.Persistence.Tests.Fixtures
{
    public class PostgreSqlFixture
    {
        public PostgreSqlFixture()
        {
            var conn = new NpgsqlConnection(ConnectionStrings.PostgreSql);

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

            var files = new[]
            {
                @"./db/pgsql/db-before.sql",
                @"./db/pgsql/db-triggers.sql",
                @"./db/pgsql/db-after.sql",
            };

            foreach (var file in files)
            {
                var script = File.ReadAllText(file);
                using (var cmd = new NpgsqlCommand(script, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            conn.Close();

            var builder = new ContainerBuilder();

            builder.Register(c => LogManager.GetCurrentClassLogger()).As<ILogger>().InstancePerDependency();
            builder.RegisterModule(new DatabaseModule(new DbSettings
            {
                ConnectionString = ConnectionStrings.PostgreSql,
                Provider = DbProvider.PostgreSql,
            }));

            Container = builder.Build();
        }

        public IContainer Container { get; set; }
    }
}