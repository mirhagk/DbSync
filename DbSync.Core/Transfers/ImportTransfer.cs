using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbSync.Core.Transfers
{
    public abstract class ImportTransfer : Transfer
    {
        protected void CopyFromFileToTable(SqlConnection connection, string file, string table, List<string> fields)
        {
            var reader = new XmlRecordDataReader(file, fields);

            SqlBulkCopy bulkCopy = new SqlBulkCopy(connection);
            bulkCopy.BulkCopyTimeout = 120;
            bulkCopy.DestinationTableName = table;
            bulkCopy.EnableStreaming = true;

            bulkCopy.WriteToServer(reader);
        }
        protected string GetTempTableScript(Table table)
        {
            return $@"IF OBJECT_ID('tempdb..##{table.BasicName}') IS NOT NULL
	DROP TABLE ##{table.BasicName}

CREATE TABLE ##{table.BasicName}( " + string.Join(", ", table.Fields.Select(f => $"[{f}] NVARCHAR(MAX) NULL")) + ")";
        }
    }
}
