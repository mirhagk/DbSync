using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DbSync.Core.Transfers;

namespace DbSync.Core.DataReaders
{
    class OverrideDataReader : SimplifiedDataReader
    {
        Table Table { get; }
        IDataReader BaseReader { get; }
        IDataReader OverrideReader { get; }
        bool BaseLoaded{get;set;} = true;
        bool BaseEmpty{get;set;} = false;
        bool OverrideLoaded{get;set;} = true;
        bool OverrideEmpty{get;set;} = false;
        public OverrideDataReader(Table table, IDataReader baseReader, IDataReader overrideReader) : base(table.Fields)
        {
            Table = table;
            BaseReader = baseReader;
            OverrideReader = overrideReader;
        }

        public override object GetValue(int i)
        {
            if (OverrideLoaded && !OverrideReader.IsDBNull(i))
                return OverrideReader.GetValue(i);
            return BaseReader.GetValue(i);
        }

        public override bool IsDBNull(int i)
        {
            if (OverrideLoaded)
                if (!OverrideReader.IsDBNull(i))
                    return false;
            if (BaseLoaded)
                if (!BaseReader.IsDBNull(i))
                    return false;
            return true;
        }

        public override bool Read()
        {
            if (BaseLoaded && !BaseEmpty)
                BaseEmpty = !BaseReader.Read();
            if (OverrideLoaded && !OverrideEmpty)
                OverrideEmpty = !OverrideReader.Read();
            if (BaseEmpty)
                BaseLoaded = false;
            else if (OverrideEmpty)
                OverrideLoaded = false;
            else
            {
                var comparisonResult = DiffGenerator.CompareObjects(BaseReader[Table.PrimaryKey],OverrideReader[Table.PrimaryKey]);
                if (comparisonResult == 0)
                {
                    OverrideLoaded = true;
                    BaseLoaded = true;
                }
                else if (comparisonResult == 1)
                    OverrideLoaded = true;
                else
                    BaseLoaded = true;
            }
            return !BaseEmpty || !OverrideEmpty;
        }
    }
}
