using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbSync.Core.Utility
{
    class SqlClient
    {
        SqlConnection connection;
        public SqlConnection Connection => connection;
        public SqlClient(SqlConnection connection)
        {
            this.connection = connection;
        }
        public void ExecuteSql(string sql)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.CommandTimeout = 120;
                cmd.ExecuteNonQuery();
            }
        }
    }
}
