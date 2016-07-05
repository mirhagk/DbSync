using DbSync.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DbSync
{
    class XmlRecordDataReader : IDataReader
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
        List<Table.Field> fields;
        Table table;
        public XmlRecordDataReader(string path, Table table)
        {
            this.table = table;
            fields = table.Fields;
            xmlReader = XmlReader.Create(path, new XmlReaderSettings { Async = true });
        }
        public object this[string name]
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public object this[int i]
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int Depth
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int FieldCount
        {
            get
            {
                return fields.Count;
            }
        }

        public bool IsClosed
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int RecordsAffected
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public bool GetBoolean(int i)
        {
            throw new NotImplementedException();
        }

        public byte GetByte(int i)
        {
            throw new NotImplementedException();
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public char GetChar(int i)
        {
            throw new NotImplementedException();
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public IDataReader GetData(int i)
        {
            throw new NotImplementedException();
        }

        public string GetDataTypeName(int i)
        {
            throw new NotImplementedException();
        }

        public DateTime GetDateTime(int i)
        {
            throw new NotImplementedException();
        }

        public decimal GetDecimal(int i)
        {
            throw new NotImplementedException();
        }

        public double GetDouble(int i)
        {
            throw new NotImplementedException();
        }

        public Type GetFieldType(int i)
        {
            throw new NotImplementedException();
        }

        public float GetFloat(int i)
        {
            throw new NotImplementedException();
        }

        public Guid GetGuid(int i)
        {
            throw new NotImplementedException();
        }

        public short GetInt16(int i)
        {
            throw new NotImplementedException();
        }

        public int GetInt32(int i)
        {
            throw new NotImplementedException();
        }

        public long GetInt64(int i)
        {
            throw new NotImplementedException();
        }

        public string GetName(int i)
        {
            throw new NotImplementedException();
        }

        public int GetOrdinal(string name)
        {
            throw new NotImplementedException();
        }

        public DataTable GetSchemaTable()
        {
            throw new NotImplementedException();
        }

        public string GetString(int i)
        {
            throw new NotImplementedException();
        }
        string TrimBrackets(string value)
        {
            value = value.Trim();
            if (value.StartsWith("(") && value.EndsWith(")"))
                return TrimBrackets(value.Substring(1, value.Length - 2));
            return value;
        }
        public object GetValue(int i)
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

        public int GetValues(object[] values)
        {
            throw new NotImplementedException();
        }

        public bool IsDBNull(int i)
        {
            if (fields[i].IsNullable)
                return !currentRecord.ContainsKey(fields[i].CanonicalName);
            return false;
        }

        public bool NextResult()
        {
            throw new NotImplementedException();
        }

        public bool Read()
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

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).          
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources. 
        // ~XmlDataReader() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
