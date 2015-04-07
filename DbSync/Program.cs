using FastMember;
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

namespace DbSync
{
    class Program
    {
        public class Settings
        {
            public string ConnectionString { get; set; }
            public List<string> Tables { get; set; } = new List<string>();
            public string Path { get; set; } = System.IO.Path.GetFullPath(".");
            public enum Strategy
            {
                MergeWithoutDelete, MergeWithDelete, Add, Overwrite
            }
            public Strategy MergeStrategy { get; set; } = Strategy.MergeWithoutDelete;

        }
        static string Get2PartName(string tableName)
        {
            if (!tableName.Contains("."))
                return "dbo." + tableName;
            return tableName;
        }
        static async Task Export(Settings settings)
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
        static void Import(Settings settings)
        {
            using (var conn = new SqlConnection(settings.ConnectionString))
            {
                conn.Open();
                foreach (var table in settings.Tables)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "CREATE TABLE ##" + Get2PartName(table);
                        cmd.CommandType = CommandType.Text;

                        var skipByAmount = 1000;

                        XmlReader xmlReader = XmlReader.Create(Path.Combine(settings.Path, table), new XmlReaderSettings { Async = true });

                        //xmlReader.Read();

                        var done = false;
                        while (!done)
                        {
                            var fields = new List<string>();
                            var records = new List<object>(skipByAmount);
                            for (int i = 0; i < skipByAmount; i++)
                            {
                                if (!xmlReader.ReadToFollowing("row"))
                                {
                                    done = true;
                                    break;
                                }
                                xmlReader.MoveToFirstAttribute();

                                var row = new ExpandoObject();
                                for (int p = 0; p < xmlReader.AttributeCount; p++)
                                {
                                    if (!fields.Contains(xmlReader.Name))
                                        fields.Add(xmlReader.Name);
                                    (row as IDictionary<string, object>).Add(xmlReader.Name, xmlReader.GetValueAsync().Result);
                                    xmlReader.MoveToNextAttribute();
                                }

                                records.Add(row);
                            }

                            SqlBulkCopy bulkCopy = new SqlBulkCopy(conn);
                            DataTable dataTable = new DataTable();
                            using (var reader = ObjectReader.Create(new List<object>(), fields.ToArray()))
                            {
                                dataTable.Load(reader);
                            }
                            bulkCopy.BulkCopyTimeout = 120;
                            bulkCopy.DestinationTableName = "StopTimes2";
                            bulkCopy.WriteToServer(dataTable);
                        }
                    }
                }
            }
        }
        static void Main(string[] args)
        {
            var settings = new Settings
            {
                ConnectionString = "server=.;database=BusTap;Integrated Security=True;",
                Tables = new List<string> { "Calendars", "Stops" }
            };

            Import(settings);
            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
        }
    }
}
