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
        static string Get1PartName(string tableName)
        {
            return tableName.Split('.').Last();
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
                        cmd.CommandText = @"
SELECT c.name 
FROM sys.all_objects o
LEFT JOIN sys.all_columns c ON o.object_id = c.object_id
WHERE o.name = '@table'
ORDER BY column_id
".FormatWith(new { table = Get1PartName(table) });

                            //"SELECT * INTO ##" + Get1PartName(table) + " FROM " + Get2PartName(table) + " WHERE 1=0";
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

                        XmlReader xmlReader = XmlReader.Create(Path.Combine(settings.Path, table), new XmlReaderSettings { Async = true });
                        
                        

                        //xmlReader.Read();


                        var done = false;
                        while (!done)
                        {
                            var records = new List<Dictionary<string, object>>(skipByAmount);
                            for (int i = 0; i < skipByAmount; i++)
                            {
                                if (!xmlReader.ReadToFollowing("row"))
                                {
                                    done = true;
                                    break;
                                }
                                xmlReader.MoveToFirstAttribute();

                                var row = new Dictionary<string,object>();
                                for (int p = 0; p < xmlReader.AttributeCount; p++)
                                {
                                    if (!fields.Contains(xmlReader.Name))
                                        fields.Add(xmlReader.Name);
                                    row[xmlReader.Name] = xmlReader.GetValueAsync().Result;
                                    xmlReader.MoveToNextAttribute();
                                }

                                records.Add(row);
                            }

                            SqlBulkCopy bulkCopy = new SqlBulkCopy(conn);
                            bulkCopy.BulkCopyTimeout = 120;
                            bulkCopy.DestinationTableName = "##" + Get1PartName(table);
                            //DataTable dataTable = new DataTable();
                            //using (var reader = ObjectReader.Create(records, fields.ToArray()))
                            using (var reader = new DictionaryDataReader(records,fields))
                            {
                                bulkCopy.WriteToServer(reader);
                                //dataTable.Load(reader);
                            }
                            //bulkCopy.WriteToServer(dataTable);
                        }

                        var primaryKey = fields.SingleOrDefault(f => f.ToLowerInvariant() == "id");

                        var rest = fields.Where(f => f != primaryKey).ToList();

                        switch (settings.MergeStrategy)
                        {
                            case Settings.Strategy.MergeWithoutDelete:
                                cmd.CommandText =
@"
SET IDENTITY_INSERT @target ON

INSERT INTO @target (@id, @columns)
SELECT @id, @columns
FROM @source s
WHERE s.@id NOT IN (SELECT @id FROM @target t)

SET IDENTITY_INSERT @target OFF

".FormatWith(new { target = Get2PartName(table), id = primaryKey, columns = string.Join(",", rest), source = "##" + Get1PartName(table) });
                                break;
                        }

                        cmd.ExecuteNonQuery();
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
