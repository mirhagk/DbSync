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
        List<string> GetFields(string table, SqlConnection connection)
        {
            using (var cmd = connection.CreateCommand())
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
                return fields;
            }

        }
        string GetTempTableScript(string table, List<string> fields) 
            => "CREATE TABLE ##" + Get1PartName(table) + "( " + string.Join(", ", fields.Select(f => f + " NVARCHAR(MAX) NULL")) + ")";
        string GetPrimaryKey(string table, List<string> fields) 
            => fields.SingleOrDefault(f => f.ToLowerInvariant() == "id" || f.ToLowerInvariant() == Get1PartName(table).ToLowerInvariant() + "id");
        List<string> GetNonPKOrAuditFields(string table, List<string> fields, JobSettings settings)
            => fields
                .Where(f => f != GetPrimaryKey(table, fields))
                .Where(r => !settings.AuditColumns.AuditColumnNames().Contains(r))
                .ToList();
        public void Import(JobSettings settings)
        {
            using (var conn = new SqlConnection(settings.ConnectionString))
            {
                conn.Open();
                foreach (var table in settings.Tables)
                {
                    var fields = GetFields(table, conn);

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = GetTempTableScript(table, fields);
                        cmd.ExecuteNonQuery();

                        var reader = new XmlRecordDataReader(Path.Combine(settings.Path, table), fields);

                        SqlBulkCopy bulkCopy = new SqlBulkCopy(conn);
                        bulkCopy.BulkCopyTimeout = 120;
                        bulkCopy.DestinationTableName = "##" + Get1PartName(table);
                        bulkCopy.EnableStreaming = true;

                        bulkCopy.WriteToServer(reader);

                        var primaryKey = GetPrimaryKey(table, fields);

                        var rest = GetNonPKOrAuditFields(table, fields, settings);

                        cmd.CommandText = Merge.GetSqlForMergeStrategy(settings, Get2PartName(table), "##" + Get1PartName(table), primaryKey, rest);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
