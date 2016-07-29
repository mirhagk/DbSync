using DbSync.Core.DataReaders;
using DbSync.Core.DataWriter;
using DbSync.Core.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbSync.Core.Transfers
{
    public abstract class Transfer
    {
        public abstract void Run(JobSettings settings, string environment, IErrorHandler errorHandler);
        public static List<T> ImportFromFileToMemory<T>(string path, IErrorHandler errorHandler = null)
        {
            JobSettings settings = new JobSettings
            {
                Tables = new List<Table>(),
                AuditColumns = new JobSettings.AuditSettings(),
                IgnoreAuditColumnsOnExport = true,
                UseAuditColumnsOnImport = false,
                Path = Path.GetDirectoryName(path)
            };
            errorHandler = errorHandler ?? new DefaultErrorHandler();

            Table table = new Table();
            table.Name = typeof(T).Name;
            table.Initialize<T>(settings, errorHandler);

            var diffGenerator = new DiffGenerator();
            using (var target = new EmptyDataReader(table))
            using (var source = new XmlRecordDataReader(path, table))
            using (var writer = new InMemoryDataWriter<T>(table))
            {
                diffGenerator.GenerateDifference(source, target, table, writer, settings);
                return writer.Data;
            }
        }
    }
}
