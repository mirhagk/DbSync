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
    public class Importer : ImportTransfer
    {
        public static Importer Instance = new Importer();
        private Importer() { }
		void ImportTable(SqlConnection connection, Table table, JobSettings settings, IErrorHandler errorHandler)
		{
			Console.WriteLine($"Importing table {table.Name}");

            connection.Execute(GetTempTableScript(table));

            if (table.ByEnvironment)
            {
                if (File.Exists(table.EnvironmentSpecificFileName))
                    CopyFromFileToTempTable(connection, table.EnvironmentSpecificFileName, table, errorHandler);
            }
            else
                CopyFromFileToTempTable(connection, Path.Combine(settings.Path, table.Name), table, errorHandler);
            
			if (table.IsEnvironmentSpecific)
				if (File.Exists(table.EnvironmentSpecificFileName))
					CopyFromFileToTempTable(connection, table.EnvironmentSpecificFileName, table, errorHandler);

            try
            {
                connection.Execute(Merge.GetSqlForMergeStrategy(settings, table));
            }
            catch (SqlException ex)
            {
                errorHandler.Error($"Error while importing {table.Name}: {ex.Message}");
            }
		}
        public override void Run(JobSettings settings, string environment, IErrorHandler errorHandler)
        {
            using (var conn = new SqlConnection(settings.ConnectionString))
            {
                conn.Open();
                foreach(var table in settings.Tables)
                    conn.Execute($"ALTER TABLE {table.QualifiedName} NOCHECK CONSTRAINT ALL");

                foreach (var table in settings.Tables)
                {
                    if (table.Initialize(conn, settings, errorHandler))
                        ImportTable(conn, table, settings, errorHandler);
                }

                foreach (var table in settings.Tables)
                    conn.Execute($"ALTER TABLE {table.QualifiedName} CHECK CONSTRAINT ALL");
            }
        }
    }
}
