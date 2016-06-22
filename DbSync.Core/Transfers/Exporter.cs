using DbSync.Core.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DbSync.Core.Transfers
{
    public class Exporter : Transfer
    {
        public static Exporter Instance = new Exporter();
        private Exporter() { }
        void WriteQueryToXmlFile(SqlCommand cmd, string path, JobSettings settings)
        {
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
                var writer = XmlWriter.Create(path, xmlSettings);
                writer.WriteStartElement("root");

                while (reader.Read())
                {
                    writer.WriteStartElement("row");
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var fieldName = reader.GetName(i);
                        if (settings.IgnoreAuditColumnsOnExport.Value && settings.IsAuditColumn(fieldName))
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
        public override void Run(JobSettings settings, string environment, IErrorHandler errorHandler)
        {
            if (!Directory.Exists(settings.Path))
                Directory.CreateDirectory(settings.Path);
            using (var conn = new SqlConnection(settings.ConnectionString))
            {
                conn.Open();
                foreach (var table in settings.Tables)
                {
                    if (!table.Initialize(conn, settings, errorHandler))
                        continue;

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT * FROM " + table.QualifiedName;
                        if (table.IsEnvironmentSpecific)
                        {
                            cmd.CommandText = $"SELECT {table.PrimaryKey}, {string.Join(", ", table.DataFields)} FROM {table.QualifiedName} WHERE IsEnvironmentSpecific = 0";
                        }
                        cmd.CommandType = CommandType.Text;

                        WriteQueryToXmlFile(cmd, Path.Combine(settings.Path, table.Name), settings);

                        if (table.IsEnvironmentSpecific)
                        {
                            //Switch IsEnvironmentSpecific = 0 to IsEnvironmentSpecific = 1
                            cmd.CommandText = cmd.CommandText.Substring(0, cmd.CommandText.Length - 1) + "1";
                            WriteQueryToXmlFile(cmd, Path.Combine(settings.Path, table.Name) + "." + environment, settings);
                        }
                    }
                }
            }
        }
    }
}
