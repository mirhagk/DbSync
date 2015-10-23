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
        string GetTempTableScript(Table table)
        {
            return $@"IF OBJECT_ID('tempdb..##{table.BasicName}') IS NOT NULL
	DROP TABLE ##{table.BasicName}

CREATE TABLE ##{table.BasicName}( " + string.Join(", ", table.Fields.Select(f => $"[{f}] NVARCHAR(MAX) NULL")) + ")";
        }
        void CopyFromFileToTable(SqlConnection connection, string file, string table, List<string> fields)
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
                        cmd.CommandText = GetTempTableScript(table);
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
        string ImportScriptForFile(Table table, string file)
        {
            string result = "";

            XmlDocument doc = new XmlDocument();
            doc.Load(file);

            var jsonData = Newtonsoft.Json.JsonConvert.SerializeXmlNode(doc);
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonData) as JObject;

            var rows = data["root"]["row"];

            var primaryKey = table.PrimaryKey;

            var columns = new string[] { table.PrimaryKey }.Concat(table.DataFields).ToList();


            result += "INSERT INTO ##" + table.BasicName + " (" + string.Join(",", columns) + ")\nVALUES\n";
            bool isFirst = true;
            foreach (var row in (rows as JArray)?.ToArray() ?? new JObject[] { rows as JObject })
            {
                if (isFirst)
                    isFirst = false;
                else
                    result += ",";
                result += "(" + string.Join(", ", columns.Select(f => GetSQLLiteral(row["@" + f]?.Value<string>()))) + ")\n";
            }

            return result;
        }
        public string GenerateImportScript(JobSettings settings, string environment)
        {
            using (var conn = new SqlConnection(settings.ConnectionString))
            {
                conn.Open();
                string script = "";

                foreach (var table in settings.Tables)
                {
                    table.Initialize(conn, settings);

                    Console.WriteLine($"Generating import script for {table}");
                    var fields = table.Fields;

                    script += GetTempTableScript(table) + "\n\n\n";
                    
                    script += ImportScriptForFile(table, Path.Combine(settings.Path, table.Name));


                    if (table.IsEnvironmentSpecific)
                    {
                        var enviroFile = Path.Combine(settings.Path, table.Name) + "." + environment;
                        if (File.Exists(enviroFile))
                            script += ImportScriptForFile(table, enviroFile);
                    }


                    script += Merge.GetSqlForMergeStrategy(settings, table.QualifiedName, "##" + table.BasicName, table.PrimaryKey, table.DataFields);
                }
                return script;
            }
        }
    }
}
