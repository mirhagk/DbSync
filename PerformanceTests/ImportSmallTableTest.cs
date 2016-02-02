using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBench.Util;
using NBench;
using System.Data.SqlClient;
using DbSync.Core;

namespace PerformanceTests
{
    public class ImportSmallTableTest
    {
        Counter counter;
        SqlConnection connection;
        //const string connectionString = @"Data Source=(LocalDB)\v11.0;AttachDbFilename=|DataDirectory|\ImportSmallTableTest.mdf;Integrated Security=True";
        const string connectionString = @"Data Source=.;Database=tempdb;Integrated Security=True";
        JobSettings settings;
        [PerfSetup]
        public void Setup(BenchmarkContext context)
        {
            connection = new SqlConnection(connectionString);
            connection.Open();
            counter = context.GetCounter(nameof(ImportSmallTableTest));
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "CREATE TABLE SmallTable( SmallTableID int NOT NULL, [Key] NVARCHAR(MAX) NOT NULL, Value NVARCHAR(MAX))";
                cmd.ExecuteNonQuery();
            }

            settings = new JobSettings();
            settings.ConnectionString = connectionString;
            settings.Path = @"Data";
            settings.UseAuditColumnsOnImport = false;
            settings.IgnoreAuditColumnsOnExport = false;
            settings.MergeStrategy = Merge.Strategy.MergeWithDelete;
            settings.AuditColumns = new JobSettings.AuditSettings();
            settings.Tables.Add(new Table()
            {
                Name = "dbo.SmallTable",
            });
        }
        [PerfBenchmark(NumberOfIterations =3,RunMode =RunMode.Throughput,RunTimeMilliseconds =2000)]
        [MemoryMeasurement(MemoryMetric.TotalBytesAllocated)]
        [GcMeasurement(GcMetric.TotalCollections,GcGeneration.AllGc)]
        [CounterThroughputAssertion(nameof(ImportSmallTableTest),MustBe.GreaterThan,10000)]
        public void Run()
        {
            DbSync.Core.Transfers.Importer.Instance.Run(settings, "test");
            counter.Increment();
        }
        [PerfCleanup]
        public void Cleanup()
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "DROP TABLE SmallTable";
                cmd.ExecuteNonQuery();
            }
        }
    }
}
