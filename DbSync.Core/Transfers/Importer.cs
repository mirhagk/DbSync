using Dapper;
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
		void ImportTable(SqlConnection connection, Table table, JobSettings settings)
		{
			Console.WriteLine($"Importing table {table.Name}");

            connection.Execute(GetTempTableScript(table));

            if (table.ByEnvironment)
            {
                if (File.Exists(table.EnvironmentSpecificFileName))
                    CopyFromFileToTable(connection, table.EnvironmentSpecificFileName, "##" + table.BasicName, table.Fields);
            }
            else
                CopyFromFileToTable(connection, Path.Combine(settings.Path, table.Name), "##" + table.BasicName, table.Fields);
            
			if (table.IsEnvironmentSpecific)
				if (File.Exists(table.EnvironmentSpecificFileName))
					CopyFromFileToTable(connection, table.EnvironmentSpecificFileName, "##" + table.BasicName, table.Fields);

            try
            {
                connection.Execute(Merge.GetSqlForMergeStrategy(settings, table));
            }
            catch (SqlException ex)
            {
                throw new DbSyncException($"Error while importing {table.Name}: {ex.Message}");
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
                    
                    ImportTable(conn, table, settings);
                }
            }
        }
    }
}
