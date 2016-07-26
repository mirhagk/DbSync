using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DbSync.Core.DataReaders
{
    class InMemoryDataReader<T> : SimplifiedDataReader
    {
        IEnumerator<T> DataSource { get; }
        T currentRecord { get; set; }
        List<PropertyInfo> Properties { get; }
        public InMemoryDataReader(Table table, IEnumerable<T> dataSource) : base(table.Fields)
        {
            DataSource = dataSource.GetEnumerator();
            Properties = table.Fields.Select(f => f.CanonicalName).Join(typeof(T).GetProperties(), f => f, p => p.Name, (f, p) => p).ToList();
        }

        public override object GetValue(int i) => Properties[i].GetValue(currentRecord);

        public override bool IsDBNull(int i) => GetValue(i) == null;

        public override bool Read()
        {
            if (DataSource.MoveNext())
            {
                currentRecord = DataSource.Current;
                return true;
            }
            return false;
        }
    }
}
