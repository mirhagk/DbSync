using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbSync.Core.DataReaders
{
    class EmptyDataReader : SimplifiedDataReader
    {
        public EmptyDataReader(Table table) : base(table.Fields)
        {
        }

        public override object GetValue(int i) => null;

        public override bool IsDBNull(int i) => false;

        public override bool Read() => false;
    }
}
