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
    class Exporter:Transfer
    {
        public static Exporter Instance = new Exporter();
        private Exporter() { }
        public async Task Export(JobSettings settings)
        {
            if (!Directory.Exists(settings.Path))
                Directory.CreateDirectory(settings.Path);
            using (var conn = new SqlConnection(settings.ConnectionString))
            {
                conn.Open();
                foreach (var table in settings.Tables.Select(t=>t.Name))
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
                                    if (reader.IsDBNull(i))//for null values just don't output the attribute at all
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
    }
}
