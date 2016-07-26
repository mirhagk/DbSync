using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbSync.Core.DataReaders
{
    abstract class SimplifiedDataReader : IDataReader
    {
        protected List<Table.Field> fields;

        public SimplifiedDataReader(List<Table.Field> fields)
        {
            this.fields = fields;
        }

        public object this[string name] => this[fields.FindIndex(f => f.CanonicalName == name)];
        public object this[int i] => GetValue(i);

        public int Depth
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int FieldCount => fields.Count;

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

        protected virtual void Dispose(bool disposing)
        {
            //No cleanup work to do, but child classes might have some
        }
        public void Dispose()
        {
            Dispose(true);
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

        public abstract object GetValue(int i);

        public int GetValues(object[] values)
        {
            throw new NotImplementedException();
        }

        public abstract bool IsDBNull(int i);

        public bool NextResult()
        {
            throw new NotImplementedException();
        }

        public abstract bool Read();
    }
}
