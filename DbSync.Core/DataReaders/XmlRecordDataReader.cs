using DbSync.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DbSync.Core.DataReaders
{
    class XmlRecordDataReader : SimplifiedDataReader
    {
        public class XmlRecordDataReaderException : Exception
        {
            public string Field { get; set; }
        }
        public class NoDefaultException: Exception
        {
            public string Field { get; set; }
        }
        Dictionary<string, object> currentRecord;
        XmlReader xmlReader;
        Table table;
        public XmlRecordDataReader(string path, Table table):base(table.Fields)
        {
            this.table = table;
            xmlReader = XmlReader.Create(path, new XmlReaderSettings { Async = true });
        }
        string TrimBrackets(string value)
        {
            value = value.Trim();
            if (value.StartsWith("(") && value.EndsWith(")"))
                return TrimBrackets(value.Substring(1, value.Length - 2));
            return value;
        }
        public override object GetValue(int i)
        {
            if (fields[i].IsAuditingColumn)//Ignore auditing columns
                return null;
            if (currentRecord.ContainsKey(fields[i].CanonicalName))
                return currentRecord[fields[i].CanonicalName];
            if (fields[i].IsNullable || !table.UseDefaults)
                return null;
            if (fields[i].DefaultValue != null)
                return TrimBrackets(fields[i].DefaultValue);
            throw new NoDefaultException { Field = fields[i].Name };
        }

        public override bool IsDBNull(int i)
        {
            if (fields[i].IsNullable)
                return !currentRecord.ContainsKey(fields[i].CanonicalName);
            return false;
        }

        public override bool Read()
        {
            currentRecord = new Dictionary<string, object>();
            if (!xmlReader.ReadToFollowing("row"))
            {
                return false;
            }
            xmlReader.MoveToFirstAttribute();
            
            for (int p = 0; p < xmlReader.AttributeCount; p++)
            {
                if (!fields.Any(f=>f.CanonicalName == xmlReader.Name.ToLowerInvariant()))
                    throw new XmlRecordDataReaderException { Field = xmlReader.Name };
                currentRecord[xmlReader.Name.ToLowerInvariant()] = xmlReader.GetValueAsync().Result;
                xmlReader.MoveToNextAttribute();
            }
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            xmlReader.Dispose();
        }
    }
}
