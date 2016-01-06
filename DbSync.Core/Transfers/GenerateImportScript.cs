using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DbSync.Core.Transfers
{
    public class GenerateImportScript : ImportTransfer
    {
        public static GenerateImportScript Instance = new GenerateImportScript();
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
        public string Run(JobSettings settings, string environment)
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
