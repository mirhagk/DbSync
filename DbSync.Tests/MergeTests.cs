using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DbSync.Core;
using DbSync.Tests.Helpers;

namespace DbSync.Tests
{
    [TestClass]
    public class MergeTests
    {
        class Values
        {
            public int ID { get; set; }
            public string value { get; set; }
        }
        [TestMethod]
        public void TestSimpleImport()
        {
            var test = new DatabaseTest();
            test.Create(@"CREATE TABLE Values(ID int NOT NULL IDENTITY(1,1), value NVARCHAR(50) NOT NULL)");

            test.Initialize("<root></root>");
            test.Load("<root><row ID='1' value='2'></root>");
        }
        [TestMethod]
        public void DoesGetSqlForMergeStrategyRun()
        {
            const string connectionString = @"Data Source=.;Database=tempdb;Integrated Security=True";

            var settings = new JobSettings();
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
            Table table = new Table();
            table.PrimaryKey = "testID";
            table.Name = "testTable";
            //table.DataFields = new[] { "testColumn1", "testColumn2", "testColumn3" }
            table.MergeStrategy = null;

            var sql = Merge.GetSqlForMergeStrategy(settings, table);
            Assert.IsTrue(sql.Contains("testTable"));
            Assert.IsTrue(sql.Contains("##testTable"));
            Assert.IsTrue(sql.Contains("testID"));
            Assert.IsTrue(sql.Contains("testColumn1"));
            Assert.IsTrue(sql.Contains("testColumn1"));
            Assert.IsTrue(sql.Contains("DELETE"));
        }
    }
}
