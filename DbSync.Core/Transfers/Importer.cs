using DbSync.Core.Utility;
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
        void CopyFromFileToTable(SqlClient client, string file, string table, List<string> fields)
        {
            var reader = new XmlRecordDataReader(file, fields);
            
            client.BulkImportToTable(table, reader);
        }
		void ImportTable(SqlClient client, Table table, JobSettings settings)
		{
			Console.WriteLine($"Importing table {table.Name}");

            client.ExecuteSql(GetTempTableScript(table));

			CopyFromFileToTable(client, Path.Combine(settings.Path, table.Name), "##" + table.BasicName, table.Fields);

			if (table.IsEnvironmentSpecific)
				if (File.Exists(table.EnvironmentSpecificFileName))
					CopyFromFileToTable(client, table.EnvironmentSpecificFileName, "##" + table.BasicName, table.Fields);

            client.ExecuteSql(Merge.GetSqlForMergeStrategy(settings, table.QualifiedName, "##" + table.BasicName, table.PrimaryKey, table.DataFields));
		}
        public override void Run(JobSettings settings, string environment)
        {
            using (var conn = new SqlConnection(settings.ConnectionString))
            {
                conn.Open();
                foreach (var table in settings.Tables)
                {
                    table.Initialize(conn, settings);
                    
                    var client = new SqlClient(conn);
                    ImportTable(client, table, settings);		
                }
            }
        }
    }
}
