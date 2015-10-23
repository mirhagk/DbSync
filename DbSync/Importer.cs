using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DbSync
{
    class Importer : Transfer
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
        {
            return $@"IF OBJECT_ID('tempdb..##{Get1PartName(table)}') IS NOT NULL
	DROP TABLE ##{Get1PartName(table)}

CREATE TABLE ##{Get1PartName(table)}( " + string.Join(", ", fields.Select(f => $"[{f}] NVARCHAR(MAX) NULL")) + ")";
        }
        string GetPrimaryKey(string table, List<string> fields)
            => fields.SingleOrDefault(f => f.ToLowerInvariant() == "id" || f.ToLowerInvariant() == Get1PartName(table).ToLowerInvariant() + "id");
        List<string> GetNonPKOrAuditFields(List<string> fields, string primaryKey, JobSettings settings)
            => fields
                .Where(f => f != primaryKey)
                .Where(r => !settings.AuditColumns.AuditColumnNames().Contains(r))
                .ToList();
        string LoadPrimaryKey(string table, SqlConnection conn)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $@"SELECT column_name
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
WHERE OBJECTPROPERTY(OBJECT_ID(constraint_name), 'IsPrimaryKey') = 1
AND table_name = '{Get1PartName(table)}'";
                return cmd.ExecuteScalar() as string;
            }
        }
        private void CopyFromFileToTable(SqlConnection connection, string file, string table, List<string> fields)
        {
            var reader = new XmlRecordDataReader(file, fields);

            SqlBulkCopy bulkCopy = new SqlBulkCopy(connection);
            bulkCopy.BulkCopyTimeout = 120;
            bulkCopy.DestinationTableName = table;
            bulkCopy.EnableStreaming = true;

            bulkCopy.WriteToServer(reader);
        }
        public void Import(JobSettings settings, string environment)
        {
            using (var conn = new SqlConnection(settings.ConnectionString))
            {
                conn.Open();
                foreach (var table in settings.Tables)
                {
                    table.Initialize(conn, settings);
                    Console.WriteLine($"Importing table {table.Name}");
                    var fields = table.Fields;

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = GetTempTableScript(table.Name, fields);
                        cmd.ExecuteNonQuery();

                        CopyFromFileToTable(conn, Path.Combine(settings.Path, table.Name), "##" + table.BasicName, table.Fields);

                        if (table.IsEnvironmentSpecific)
                        {
                            var enviroFile = Path.Combine(settings.Path, table.Name) + "." + environment;
                            if (File.Exists(enviroFile))
                                CopyFromFileToTable(conn, enviroFile, "##" + table.BasicName, table.Fields);
                        }
                        

                        if (table.PrimaryKey == null)
                            throw new DbSyncException($"No primary key found for table {table}");

                        cmd.CommandText = Merge.GetSqlForMergeStrategy(settings, table.QualifiedName, "##" + table.BasicName, table.PrimaryKey, table.DataFields);

                        cmd.CommandTimeout = 120;

                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
        string GetSQLLiteral(string value) => value == null ? "NULL" : $"'{value}'";
        public string GenerateImportScript(JobSettings settings)
        {
            using (var conn = new SqlConnection(settings.ConnectionString))
            {
                conn.Open();
                string script = "";

                foreach (var table in settings.Tables.Select(t=>t.Name))
                {
                    Console.WriteLine($"Generating import script for {table}");
                    var fields = GetFields(table, conn);

                    script += GetTempTableScript(table, fields) + "\n\n\n";

                    var reader = new XmlRecordDataReader(Path.Combine(settings.Path, table), fields);
                    XmlDocument doc = new XmlDocument();
                    doc.Load(Path.Combine(settings.Path, table));

                    var jsonData = Newtonsoft.Json.JsonConvert.SerializeXmlNode(doc);
                    var data = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonData) as JObject;

                    var rows = data["root"]["row"];

                    var primaryKey = LoadPrimaryKey(table, conn);

                    var columns = new string[] { primaryKey }.Concat(GetNonPKOrAuditFields(fields, primaryKey, settings)).ToList();


                    script += "INSERT INTO ##" + Get1PartName(table) + " (" + string.Join(",", columns) + ")\nVALUES\n";
                    bool isFirst = true;
                    foreach (var row in (rows as JArray)?.ToArray() ?? new JObject[] { rows as JObject })
                    {
                        if (isFirst)
                            isFirst = false;
                        else
                            script += ",";
                        script += "(" + string.Join(", ", columns.Select(f => GetSQLLiteral(row["@" + f]?.Value<string>()))) + ")\n";
                    }

                    var rest = GetNonPKOrAuditFields(fields, primaryKey, settings);

                    script += Merge.GetSqlForMergeStrategy(settings, Get2PartName(table), "##" + Get1PartName(table), primaryKey, rest);
                }
                return script;
            }
        }
    }
}
