using Dapper;
using DbSync.Core.Services;
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
        public string Filename { get; set; }
        void ImportTable(SqlConnection connection, Table table, JobSettings settings, StringBuilder generatedSql)
        {
            Console.WriteLine($"Generating diff for table {table.Name}");

            connection.Execute(GetTempTableScript(table));

            CopyFromFileToTempTable(connection, Path.Combine(settings.Path, table.Name), table);

            if (table.IsEnvironmentSpecific)
                if (File.Exists(table.EnvironmentSpecificFileName))
                    CopyFromFileToTempTable(connection, table.EnvironmentSpecificFileName, table);

            var sql = $@"
;WITH Differences AS
(
	SELECT * FROM ##{table.BasicName}
	EXCEPT
	SELECT * FROM {table.Name}
)
SELECT d.*,
CASE WHEN t.{table.PrimaryKey} IS NULL THEN 1 ELSE 0 END AS IsNew
FROM Differences d
LEFT JOIN {table.Name} t on d.{table.PrimaryKey} = t.{table.PrimaryKey}";

            var data = connection.Query(sql);

            var recordsToUpdate = data.Where(x => x.IsNew == 0);
            var recordsToInsert = data.Where(x => x.IsNew == 1);
            

            foreach(var record in recordsToUpdate)
            {
                generatedSql.Append($"UPDATE {table.Name} SET ")
                    .Append(string.Join(" ", table.DataFields.Select(df => $"{df} = {record[df]}")))
                    .Append($" WHERE {table.PrimaryKey} = {record[table.PrimaryKey]}");
            }
        }
        public override void Run(JobSettings settings, string environment, IErrorHandler errorHandler)
        {
            using (var conn = new SqlConnection(settings.ConnectionString))
            {
                var generatedSql = new StringBuilder();
                conn.Open();
                foreach (var table in settings.Tables)
                {
                    if (table.Initialize(conn, settings, errorHandler))
                        ImportTable(conn, table, settings, generatedSql);
                }
                File.WriteAllText(Filename, generatedSql.ToString());
            }
        }
    }
}
