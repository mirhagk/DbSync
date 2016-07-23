using Dapper;
using DbSync.Core.Services;
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

namespace DbSync.Core.Transfers
{
    public class Importer : Transfer
    {
        public static Importer Instance = new Importer();
        private Importer() { }
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
                        using (var target = cmd.ExecuteReader())
                        using (var source = new XmlRecordDataReader(Path.Combine(settings.Path, table.Name + ".xml"), table))
                        using (var writer = new SqlSimpleDataWriter(settings.ConnectionString, table, settings))
                        {
                            diffGenerator.GenerateDifference(source, target, table, writer);
                        }
                    }
            }
        }
    }
}
