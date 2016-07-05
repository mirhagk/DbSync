using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DbSync.Core.Services;
using System.Data;

namespace DbSync.Core.Transfers
{
    class DiffGenerator : Transfer
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
            var size = reader.FieldCount;
            List<object> result = new List<object>(size);
            for (int i = 0; i < size; i++)
                result.Add(reader.GetValue(i));
            return result;
        }
        public int? CompareKeys(object key1, object key2)
        {
            var comparison = (key1 as int?)?.CompareTo(key2);
            if (comparison != null)
                return comparison.Value;
            return null;
        }
        public void GenerateDifference(IDataReader source, IDataReader target, Table table)
        {
            source.Read();
            target.Read();
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
                var comparison = CompareKeys(source[table.PrimaryKey], target[table.PrimaryKey]);
                if (comparison == null)
                {
                    throw new NotSupportedKeyException($"Could not compare key {table.PrimaryKey} of {table.Name}. DbSync does not support comparison of keys of it's type");
                }
                //record exists in both
                if (comparison == 0)
                {

                }
                //target contains a record not in source
                else if (comparison == 1)
                {
                    targetsToDelete.Add(target[table.PrimaryKey]);
                    target.Read();
                    targetRecord = ReadRecord(target);
                }
                //source contains a record not in target
                else if (comparison == -1)
                {

                }
            }
        }

        public override void Run(JobSettings settings, string environment, IErrorHandler errorHandler)
        {
            throw new NotImplementedException();
        }
    }
}
