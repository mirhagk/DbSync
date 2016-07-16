using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DbSync.Core.Services;
using System.Data.SqlClient;

namespace DbSync.Core.Transfers
{
    public class SmartTransfer : Transfer
    {
        public static SmartTransfer Instance { get; } = new SmartTransfer();
        private SmartTransfer() { }
        public override void Run(JobSettings settings, string environment, IErrorHandler errorHandler)
        {
            using (var connection = new SqlConnection(settings.ConnectionString))
            {
                connection.Open();

                foreach (var table in settings.Tables)
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = $"SELECT * FROM {table.QualifiedName}";
                        var diffGenerator = new DiffGenerator();
                        using (var target = cmd.ExecuteReader())
                        using (var source = new XmlRecordDataReader(settings.Path, table))
                        using (var writer = new SqlSimpleDataWriter(settings.ConnectionString, table))
                        {
                            diffGenerator.GenerateDifference(source, target, table, writer);
                        }
                    }
            }
        }
    }
}
