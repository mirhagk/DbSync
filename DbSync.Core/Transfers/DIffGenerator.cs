using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DbSync.Core.Services;
using System.Data;

namespace DbSync.Core.Transfers
{
    class DiffGenerator
    {
        public class MismatchedSchemaException:DbSyncException
        {
            public MismatchedSchemaException(string message) : base(message) { }
        }
        public class NotSupportedKeyException : DbSyncException
        {
            public NotSupportedKeyException(string message) : base(message) { }
        }

        public List<object> ReadRecord(IDataReader reader)
        {
            if (!reader.Read())
                return null;
            var size = reader.FieldCount;
            List<object> result = new List<object>(size);
            for (int i = 0; i < size; i++)
                result.Add(reader.GetValue(i));
            return result;
        }
        public int? CompareObjects(object key1, object key2)
        {
            {
                long key1Data;
                long key2Data;
                if (long.TryParse(key1?.ToString(), out key1Data) && long.TryParse(key2?.ToString(), out key2Data))
                    return key1Data.CompareTo(key2Data);
            }
            return key1?.ToString()?.CompareTo(key2?.ToString());
        }
        Dictionary<string, object> SerializeRecordAsDictionary(List<object> record, Table table)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            for (int i = 0; i < record.Count; i++)
                result.Add(table.Fields[i].CanonicalName, record[i]);
            return result;
        }
        public void GenerateDifference(IDataReader source, IDataReader target, Table table, IDataWriter dataWriter)
        {
            if (source.FieldCount != target.FieldCount)
            {
                throw new MismatchedSchemaException($"Schema difference detected while importing {table.BasicName}. Please ensure the schemas match before syncing");
            }
            List<object> sourceRecord = ReadRecord(source);
            List<object> targetRecord = ReadRecord(target);
            List<object> targetsToDelete = new List<object>();
            List<List<object>> recordsToInsert = new List<List<object>>();
            List<List<object>> recordsToUpdate = new List<List<object>>();

            while (true)
            {
                int? comparison;
                if (sourceRecord == null && targetRecord == null)
                    break;
                else if (sourceRecord == null)
                    comparison = 1;
                else if (targetRecord == null)
                    comparison = -1;
                else
                    comparison = CompareObjects(source[table.PrimaryKey], target[table.PrimaryKey]);

                if (sourceRecord != null)
                    dataWriter.Entry(SerializeRecordAsDictionary(sourceRecord, table));

                if (comparison == null)
                {
                    throw new NotSupportedKeyException($"Could not compare key {table.PrimaryKey} of {table.Name}. DbSync does not support comparison of keys of it's type");
                }
                bool consumeTarget = false;
                bool consumeSource = false;
                //record exists in both
                if (comparison == 0)
                {
                    var identical = true;
                    for (int i = 0; i < sourceRecord.Count; i++)
                        if (!table.Fields[i].IsAuditingColumn)
                            if (CompareObjects(sourceRecord[i], targetRecord[i]) != 0)
                                identical = false;
                    if (!identical)
                        dataWriter.Update(SerializeRecordAsDictionary(sourceRecord, table));
                    consumeSource = true;
                    consumeTarget = true;
                }
                //target contains a record not in source
                else if (comparison == 1)
                {
                    dataWriter.Delete(target[table.PrimaryKey]);
                    consumeTarget = true;
                }
                //source contains a record not in target
                else if (comparison == -1)
                {
                    dataWriter.Add(SerializeRecordAsDictionary(sourceRecord, table));
                    consumeSource = true;
                }
                if (consumeSource)
                    sourceRecord = ReadRecord(source);
                if (consumeTarget)
                    targetRecord = ReadRecord(target);
            }
        }
    }
}
