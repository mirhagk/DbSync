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
    public class Importer : ImportTransfer
    {
        public static Importer Instance = new Importer();
        private Importer() { }
        void CopyFromFileToTable(SqlConnection connection, string file, string table, List<string> fields)
        {
            var reader = new XmlRecordDataReader(file, fields);

            SqlBulkCopy bulkCopy = new SqlBulkCopy(connection);
            bulkCopy.BulkCopyTimeout = 120;
            bulkCopy.DestinationTableName = table;
            bulkCopy.EnableStreaming = true;

            bulkCopy.WriteToServer(reader);
        }
        public void override Run(JobSettings settings, string environment)
        {
            using (var conn = new SqlConnection(settings.ConnectionString))
            {
                conn.Open();
                foreach (var table in settings.Tables)
                {
                    table.Initialize(conn, settings);
                    Console.WriteLine($"Importing table {table.Name}");
                    var fields = table.Fields;

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = GetTempTableScript(table);
                        cmd.ExecuteNonQuery();

                        CopyFromFileToTable(conn, Path.Combine(settings.Path, table.Name), "##" + table.BasicName, table.Fields);

                        if (table.IsEnvironmentSpecific)
                        {
                            var enviroFile = Path.Combine(settings.Path, table.Name) + "." + environment;
                            if (File.Exists(enviroFile))
                                CopyFromFileToTable(conn, enviroFile, "##" + table.BasicName, table.Fields);
                        }
                        

                        if (table.PrimaryKey == null)
                            throw new DbSyncException($"No primary key found for table {table}");

                        cmd.CommandText = Merge.GetSqlForMergeStrategy(settings, table.QualifiedName, "##" + table.BasicName, table.PrimaryKey, table.DataFields);

                        cmd.CommandTimeout = 120;

                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
