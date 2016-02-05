using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbSync.Core.Transfers
{
    public class ImportDiffGenerator : ImportTransfer
    {
        public static ImportDiffGenerator Instance = new ImportDiffGenerator();
        void ImportTable(SqlConnection connection, Table table, JobSettings settings)
        {
            Console.WriteLine($"Generating diff for table {table.Name}");

            connection.Execute(GetTempTableScript(table));

            CopyFromFileToTable(connection, Path.Combine(settings.Path, table.Name), "##" + table.BasicName, table.Fields);

            if (table.IsEnvironmentSpecific)
                if (File.Exists(table.EnvironmentSpecificFileName))
                    CopyFromFileToTable(connection, table.EnvironmentSpecificFileName, "##" + table.BasicName, table.Fields);

            var data = connection.Query($"SELECT * FROM ##{table.BasicName} EXCEPT SELECT * FROM {table.Name}");

            var id = data.First().OrganizationID;
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
