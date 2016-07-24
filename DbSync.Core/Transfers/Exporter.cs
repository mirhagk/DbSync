using DbSync.Core.DataWriter;
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
        public override void Run(JobSettings settings, string environment, IErrorHandler errorHandler)
        {

            using (var connection = new SqlConnection(settings.ConnectionString))
            {
                connection.Open();

                foreach (var table in settings.Tables)
                    using (var cmd = connection.CreateCommand())
                    {
                        table.Initialize(connection, settings, errorHandler);
                        cmd.CommandText = $"SELECT * FROM {table.QualifiedName}";
                        var diffGenerator = new DiffGenerator();
                        var file = Path.Combine(settings.Path, table.Name + ".xml");
                        File.Move(file, file + ".old");
                        using (var source = cmd.ExecuteReader())
                        using (var target = new XmlRecordDataReader(file+".old", table))
                        using (var writer = new XmlDataWriter(table, settings))
                        {
                            diffGenerator.GenerateDifference(source, target, table, writer, settings);
                        }
                        File.Delete(file + ".old");
                    }
            }
        }
    }
}
