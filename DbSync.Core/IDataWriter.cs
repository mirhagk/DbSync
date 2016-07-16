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
        void Connect(Table table);
        void Add(Dictionary<string,object> entry);
        void Update(Dictionary<string, object> entry);
        void Delete(object key);
    }
    class SqlSimpleDataWriter : IDataWriter, IDisposable
    {
        Table table;
        SqlConnection connection;
        public SqlSimpleDataWriter(string connectionString, Table table)
        {
            connection = new SqlConnection(connectionString);
            connection.Open();
            this.table = table;
        }
        void RunSql(string sql)
        {
            using (var cmd = new SqlCommand(sql, connection))
                cmd.ExecuteNonQuery();
        }
        string Escape(object value)
        {
            return $"'{value}'";
        }
        public void Add(Dictionary<string, object> entry)
        {
            RunSql($"INSERT INTO {table.QualifiedName} ({string.Join(",", table.Fields)}) VALUES ({string.Join(",", table.Fields.Select(f => Escape(entry[f.CanonicalName])))})");
            throw new NotImplementedException();
        }

        public void Update(Dictionary<string, object> entry)
        {
            RunSql($"UPDATE {table.QualifiedName} SET {string.Join(", ", table.Fields.Where(f => !f.IsPrimaryKey).Select(f => $"{f.Name} = {entry[f.CanonicalName]}"))}");
        }
        public void Delete(object key)
        {
            RunSql($"DELETE FROM {table.QualifiedName} WHERE {table.PrimaryKey} = {Escape(key)}");
        }
        public void Dispose()
        {
            connection.Close();
            connection.Dispose();
        }
    }
}
