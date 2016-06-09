using DbSync.Core.Services;
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
        protected void CopyFromFileToTempTable(SqlConnection connection, string file, Table table, IErrorHandler errorHandler)
        {
            var reader = new XmlRecordDataReader(file, table.Fields.Select(x=>x.Name).ToList());

            SqlBulkCopy bulkCopy = new SqlBulkCopy(connection);
            bulkCopy.BulkCopyTimeout = 120;
            bulkCopy.DestinationTableName = "##" + table.BasicName;
            bulkCopy.EnableStreaming = true;
            try
            {
                bulkCopy.WriteToServer(reader);
            }
            catch(XmlRecordDataReader.XmlRecordDataReaderException ex)
            {
                errorHandler.Error($"Xml file contains the field {ex.Field} but the table does not contain it");
            }
        }
        protected string GetTempTableScript(Table table)
        {
            return $@"IF OBJECT_ID('tempdb..##{table.BasicName}') IS NOT NULL
	DROP TABLE ##{table.BasicName}

CREATE TABLE ##{table.BasicName}( " + string.Join(", ", table.Fields.Select(f => $"[{f.Name}] NVARCHAR(MAX) NULL")) + ")";
        }
    }
}
