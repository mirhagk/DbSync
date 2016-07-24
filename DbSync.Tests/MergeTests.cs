using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DbSync.Core;
using DbSync.Tests.Helpers;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;

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
        List<Values> GetValueList(params string[] values)
        {
            int id = 1;
            return values.Select(v => new Tests.MergeTests.Values { id = id++, value = v }).ToList();
        }
        [TestMethod]
        public void TestSimpleImport()
        {
            using (var test = new DatabaseTest<Values>())
            {
                test.Create();

                test.Initialize();
                test.Load(GetValueList("test","test2"));
                test.RoundTripCheck();
            }
        }
        [TestMethod]
        public void DoesEscaping()
        {
            using (var test = new DatabaseTest<Values>())
            {
                test.Create();

                test.Initialize();
                test.Load(GetValueList("dawdaw\\dwadawd", "dwadaw'dwadaw", "\"\"dawdawda\"", "[]|_13155616378899!@#%$^^^(!#@!)@#)!@#!()~?/>;--\\"));
                test.RoundTripCheck();
            }
        }
    }
}
