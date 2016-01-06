using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbSync.Core.Transfers
{
    public abstract class ImportTransfer : Transfer
    {
        protected string GetTempTableScript(Table table)
        {
            return $@"IF OBJECT_ID('tempdb..##{table.BasicName}') IS NOT NULL
	DROP TABLE ##{table.BasicName}

CREATE TABLE ##{table.BasicName}( " + string.Join(", ", table.Fields.Select(f => $"[{f}] NVARCHAR(MAX) NULL")) + ")";
        }
    }
}
