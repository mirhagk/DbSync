using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbSync.Core
{
    interface IDataWriter
    {
        void Add(Dictionary<string,object> entry);
        void Update(Dictionary<string, object> entry);
        void Delete(object key);
    }
    class SqlSimpleDataWriter : IDataWriter, IDisposable
    {
        Table table;
        SqlConnection connection;
        bool hasAdded = false;
        JobSettings settings;
        public SqlSimpleDataWriter(string connectionString, Table table, JobSettings settings)
        {
            connection = new SqlConnection(connectionString);
            connection.Open();
            this.table = table;
            this.settings = settings;
        }
        void RunSql(string sql)
        {
            using (var cmd = new SqlCommand(sql, connection))
                cmd.ExecuteNonQuery();
        }
        string Escape(object value)
        {
            if (value == null)
                return "NULL";
            return $"'{value}'";
        }
        public void Add(Dictionary<string, object> entry)
        {
            if (!hasAdded)
            {
                hasAdded = true;
                RunSql($"IF OBJECTPROPERTY(OBJECT_ID('{table.QualifiedName}'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT {table.QualifiedName} ON");
            }
            var nonAuditFields = table.Fields.Where(f => !f.IsAuditingColumn);
            var fieldNames = string.Join(",", nonAuditFields.Select(f => f.Name));
            var fieldValues = string.Join(",", nonAuditFields.Select(f => Escape(entry[f.CanonicalName])));
            if (settings.UseAuditColumnsOnImport ?? false)
            {
                var c = settings.AuditColumns;
                fieldNames += $", {c.CreatedDate}, {c.CreatedUser}, {c.ModifiedDate}, {c.ModifiedUser}";
                fieldValues += $", GETDATE(), SUSER_NAME(), GETDATE(), SUSER_NAME()";
            }

            RunSql($"INSERT INTO {table.QualifiedName} ({fieldNames}) VALUES ({fieldValues})");
        }

        public void Update(Dictionary<string, object> entry)
        {
            RunSql($"UPDATE {table.QualifiedName} SET {string.Join(", ", table.Fields.Where(f => !f.IsPrimaryKey && !f.IsAuditingColumn).Select(f => $"{f.Name} = {Escape(entry[f.CanonicalName])}"))} WHERE {table.PrimaryKey} = {Escape(entry[table.PrimaryKey])}");
        }
        public void Delete(object key)
        {
            RunSql($"DELETE FROM {table.QualifiedName} WHERE {table.PrimaryKey} = {Escape(key)}");
        }
        public void Dispose()
        {
            if (hasAdded)
            {
                RunSql($"IF OBJECTPROPERTY(OBJECT_ID('{table.QualifiedName}'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT {table.QualifiedName} OFF");
            }
            connection.Close();
            connection.Dispose();
        }
    }
}
