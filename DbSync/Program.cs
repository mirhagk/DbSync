using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace DbSync
{
    public class Program
    {
        static string Get2PartName(string tableName)
        {
            if (!tableName.Contains("."))
                return "dbo." + tableName;
            return tableName;
        }
        static string Get1PartName(string tableName)
        {
            return tableName.Split('.').Last();
        }
        static async Task Export(JobSettings settings)
        {
            using (var conn = new SqlConnection(settings.ConnectionString))
            {
                conn.Open();
                foreach(var table in settings.Tables)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT * FROM " + Get2PartName(table);
                        cmd.CommandType = CommandType.Text;

                        using (var reader = cmd.ExecuteReader())
                        {

                            var xmlSettings = new XmlWriterSettings
                            {
                                NewLineOnAttributes = true,
                                Indent = true,
                                IndentChars = "  ",
                                NewLineChars = Environment.NewLine,
                                OmitXmlDeclaration = true,
                            };
                            var writer = XmlWriter.Create(Path.Combine(settings.Path, table), xmlSettings);
                            writer.WriteStartElement("root");

                            while (await reader.ReadAsync())
                            {
                                writer.WriteStartElement("row");
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    var fieldName = reader.GetName(i);
                                    if (settings.IgnoreAuditColumnsOnExport && settings.IsAuditColumn(fieldName))
                                        continue;

                                    writer.WriteAttributeString(reader.GetName(i), reader.GetValue(i).ToString());
                                }
                                writer.WriteEndElement();
                            }
                            writer.WriteEndElement();
                            writer.Flush();
                            writer.Close();
                        }
                    }
                }
            }
        }
        static void Import(JobSettings settings)
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

                        var skipByAmount = 1000;

                        cmd.CommandText = "CREATE TABLE ##" + Get1PartName(table) + "( " + string.Join(", ", fields.Select(f => f + " NVARCHAR(MAX) NULL"))+ ")";
                        cmd.ExecuteNonQuery();

                        var reader = new XmlRecordDataReader(Path.Combine(settings.Path, table), fields);

                        SqlBulkCopy bulkCopy = new SqlBulkCopy(conn);
                        bulkCopy.BulkCopyTimeout = 120;
                        bulkCopy.DestinationTableName = "##" + Get1PartName(table);
                        bulkCopy.EnableStreaming = true;

                        bulkCopy.WriteToServer(reader);

                        var primaryKey = fields.SingleOrDefault(f => f.ToLowerInvariant() == "id");

                        var rest = fields.Where(f => f != primaryKey).ToList();

                        cmd.CommandText = Merge.GetSqlForMergeStrategy(settings, Get2PartName(table), "##" + Get1PartName(table), primaryKey, rest);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
        class CommandLineArguments
        {
            public bool Import { get; set; }
            public bool Export { get; set; }
            public string Config { get; set; }
            public string Job { get; set; }
        }
        public class Settings
        {
            [XmlElement("Job")]
            public List<JobSettings> Jobs { get; set; }
        }
        static void RunJob(JobSettings job, CommandLineArguments cmdArgs)
        {
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            if (cmdArgs.Export)
                Export(job).Wait();
            if (cmdArgs.Import)
                Import(job);

            watch.Stop();
            Console.WriteLine($"Executed job {job.Name}, Elapsed {watch.ElapsedMilliseconds}ms");
        }
        static void Main(string[] args)
        {
            var cmdArgs = PowerCommandParser.Parser.ParseArguments<CommandLineArguments>(args);
            var serializer = new XmlSerializer(typeof(Settings));
            StreamReader configFileStream = new StreamReader(cmdArgs.Config);

            var settings = (Settings)serializer.Deserialize(configFileStream);

            if (settings.Jobs.Count == 0)
            {
                Console.Error.WriteLine("Must specify at least one job in the config file");
                return;
            }

            if (string.IsNullOrWhiteSpace(cmdArgs.Job))
            {
                foreach (var job in settings.Jobs)
                {
                    RunJob(job, cmdArgs);
                }
            }
            else
            {
                var selectedJob = settings.Jobs.SingleOrDefault(j => j.Name.Equals(cmdArgs.Job, StringComparison.InvariantCultureIgnoreCase));
                if (selectedJob == null)
                {
                    Console.Error.WriteLine($"No job found that matches {cmdArgs.Job}");
                    return;
                }
            }
        }
    }
}
