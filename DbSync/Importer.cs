using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbSync
{
    class Importer:Transfer
    {
        public static Importer Instance = new Importer();
        private Importer() { }
        public void Import(JobSettings settings)
        {
            using (var conn = new SqlConnection(settings.ConnectionString))
            {
                conn.Open();
                foreach (var table in settings.Tables)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
SELECT c.name 
FROM sys.all_objects o
LEFT JOIN sys.all_columns c ON o.object_id = c.object_id
WHERE o.name = '@table'
ORDER BY column_id
".FormatWith(new { table = Get1PartName(table) });

                        cmd.CommandType = CommandType.Text;
                        var sqlReader = cmd.ExecuteReader();

                        var fields = new List<string>();

                        while (sqlReader.Read())
                        {
                            fields.Add(sqlReader.GetString(0));
                        }
                        sqlReader.Close();

                        cmd.CommandText = "CREATE TABLE ##" + Get1PartName(table) + "( " + string.Join(", ", fields.Select(f => f + " NVARCHAR(MAX) NULL")) + ")";
                        cmd.ExecuteNonQuery();

                        var reader = new XmlRecordDataReader(Path.Combine(settings.Path, table), fields);

                        SqlBulkCopy bulkCopy = new SqlBulkCopy(conn);
                        bulkCopy.BulkCopyTimeout = 120;
                        bulkCopy.DestinationTableName = "##" + Get1PartName(table);
                        bulkCopy.EnableStreaming = true;

                        bulkCopy.WriteToServer(reader);

                        var primaryKey = fields.SingleOrDefault(f => f.ToLowerInvariant() == "id" || f.ToLowerInvariant() == Get1PartName(table).ToLowerInvariant() + "id");

                        var rest = fields
                            .Where(f => f != primaryKey)
                            .Where(r => !settings.AuditColumns.AuditColumnNames().Contains(r))
                            .ToList();

                        cmd.CommandText = Merge.GetSqlForMergeStrategy(settings, Get2PartName(table), "##" + Get1PartName(table), primaryKey, rest);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
