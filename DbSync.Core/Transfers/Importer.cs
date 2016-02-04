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
		void ImportTable(SqlConnection connection, Table table, JobSettings settings, string environment)
		{
			Console.WriteLine($"Importing table {table.Name}");

			using (var cmd = connection.CreateCommand())
			{
				cmd.CommandText = GetTempTableScript(table);
				cmd.ExecuteNonQuery();

				CopyFromFileToTable(connection, Path.Combine(settings.Path, table.Name), "##" + table.BasicName, table.Fields);

				if (table.IsEnvironmentSpecific)
				{
					var enviroFile = Path.Combine(settings.Path, table.Name) + "." + environment;
					if (File.Exists(enviroFile))
						CopyFromFileToTable(connection, enviroFile, "##" + table.BasicName, table.Fields);
				}
				


				cmd.CommandText = Merge.GetSqlForMergeStrategy(settings, table.QualifiedName, "##" + table.BasicName, table.PrimaryKey, table.DataFields);

				cmd.CommandTimeout = 120;

				cmd.ExecuteNonQuery();
			}
		}
        public override void Run(JobSettings settings, string environment)
        {
            using (var conn = new SqlConnection(settings.ConnectionString))
            {
                conn.Open();
                foreach (var table in settings.Tables)
                {
                    table.Initialize(conn, settings);
					
					if (table.PrimaryKey == null)
						throw new DbSyncException($"No primary key found for table {table}");
				
                    ImportTable(conn,table,settings,environment);
					
					
                }
            }
        }
    }
}
