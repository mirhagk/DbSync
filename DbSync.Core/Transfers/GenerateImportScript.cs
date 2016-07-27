using DbSync.Core.DataReaders;
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
    public class GenerateImportScript : Transfer
    {
        public static GenerateImportScript Instance = new GenerateImportScript();
        public string Filename { get; set; }
        public override void Run(JobSettings settings, string environment, IErrorHandler errorHandler)
        {
            using (var connection = new SqlConnection(settings.ConnectionString))
            using (var file = new StreamWriter(Filename))
            {
                connection.Open();

                foreach (var table in settings.Tables)
                    using (var cmd = connection.CreateCommand())
                    {
                        table.Initialize(connection, settings, errorHandler);
                        cmd.CommandText = $"SELECT * FROM {table.QualifiedName}";
                        var diffGenerator = new DiffGenerator();
                        using (var target = cmd.ExecuteReader())
                        using (var source = new XmlRecordDataReader(Path.Combine(settings.Path, table.Name + ".xml"), table))
                        using (var writer = new SqlSimpleDataWriter(file, table, settings))
                        {
                            diffGenerator.GenerateDifference(source, target, table, writer, settings);
                        }
                    }
            }
        }
    }
}
