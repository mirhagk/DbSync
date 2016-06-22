using DbSync.Core.Services;
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
        public string Filename { get; set; }
        string GetSQLLiteral(string value) => value == null ? "NULL" : $"'{value}'";
        string ImportScriptForFile(Table table, string file)
        {
            string result = "";

            XmlDocument doc = new XmlDocument();
            doc.Load(file);

            var jsonData = Newtonsoft.Json.JsonConvert.SerializeXmlNode(doc);
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonData) as JObject;

            var root = data["root"];

            if (!root.HasValues)
                return null;

            var rows = data["root"]["row"];

            var primaryKey = table.PrimaryKey;

            var columns = new string[] { table.PrimaryKey }.Concat(table.DataFields).ToList();
            

            result += "INSERT INTO ##" + table.BasicName + " (" + string.Join(",", columns) + ")\nVALUES\n";
            bool isFirst = true;
            foreach (var row in (rows as JArray)?.ToArray() ?? new JObject[] { rows as JObject })
            {
                Dictionary<string, string> rowValues = new Dictionary<string, string>();
                foreach(JProperty child in row.Children())
                {
                    rowValues.Add(child.Name.ToLowerInvariant(), child.Value.Value<string>());
                }
                if (isFirst)
                    isFirst = false;
                else
                    result += ",";
                result += "(" + string.Join(", ", columns.Select(f => GetSQLLiteral(rowValues.Keys.Contains("@" + f) ? rowValues["@" + f] : null))) + ")\n";
            }

            return result;
        }
        public override void Run(JobSettings settings, string environment, IErrorHandler errorHandler)
        {
            using (var conn = new SqlConnection(settings.ConnectionString))
            {
                conn.Open();
                string script = "";

                foreach (var table in settings.Tables)
                {
                    if (!table.Initialize(conn, settings, errorHandler))
                        continue;

                    Console.WriteLine($"Generating import script for {table.Name}");
                    var fields = table.Fields;

                    script += GetTempTableScript(table) + "\n\n\n";

                    script += ImportScriptForFile(table, Path.Combine(settings.Path, table.Name));


                    if (table.IsEnvironmentSpecific)
                        if (File.Exists(table.EnvironmentSpecificFileName))
                        {
                            var result = ImportScriptForFile(table, table.EnvironmentSpecificFileName);
                            if (result == null)
                            {
                                Console.WriteLine($"Table {table.Name} is empty, skipping it");
                                continue;
                            }
                            script += result;
                        }


                    script += Merge.GetSqlForMergeStrategy(settings, table);
                }
                File.WriteAllText(Filename, script);
            }
        }
    }
}
