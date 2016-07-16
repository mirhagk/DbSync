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
    }
    class SqlSimpleDataWriter : IDataWriter, IDisposable
    {
        Table table;
        SqlConnection connection;
        public SqlSimpleDataWriter(string connectionString)
        {
            connection = new SqlConnection(connectionString);
            connection.Open();
        }
        public void Connect(Table table)
        {
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
            RunSql($"INSERT INTO {table.QualifiedName} ({string.Join(",", table.Fields)}) VALUES ({string.Join(",", table.Fields.Select(f => Escape(entry[f.CanonicalName])))})";
            throw new NotImplementedException();
        }

        public void Update(Dictionary<string, object> entry)
        {
            //todo: implement
        }
        public void Dispose()
        {
            connection.Close();
            connection.Dispose();
        }
    }
}
