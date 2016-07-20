﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DbSync.Core;
using DbSync.Tests.Helpers;
using System.Xml.Serialization;

namespace DbSync.Tests
{
    [TestClass]
    public class MergeTests
    {
        public struct Values
        {
            [XmlAttribute]
            public int id { get; set; }
            [XmlAttribute]
            public string value { get; set; }
            public override bool Equals(object obj)
            {
                if (!(obj is Values))
                    return false;
                var other = (Values)obj;
                return other.id == id && other.value == value;
            }
            public override int GetHashCode()
            {
                return id.GetHashCode() ^ value.GetHashCode();
            }
        }
        [TestMethod]
        public void TestSimpleImport()
        {
            using (var test = new DatabaseTest<Values>())
            {
                test.Create();

                test.Initialize();
                test.Load(new System.Collections.Generic.List<Values> { new Values { id = 1, value = "test" }, new Values { id = 2, value = "test2" } });
                test.RoundTripCheck();
            }
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
