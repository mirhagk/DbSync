using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DbSync
{
    public class Table
    {
        #region XmlValues
        [XmlText]
        public string Name { get; set; }
        [XmlAttribute]
        public string Key { get; set; }
        [XmlAttribute]
        public bool IsEnvironmentSpecific { get; set; }
        #endregion

        SqlConnection connection;
        public void Initialize(SqlConnection connection)
        {
            this.connection = connection;
        }

        public string BasicName
        {
            get
            {
                return Name.Split('.').Last();
            }
        }
        public string QualifiedName
        {
            get
            {
                if (!Name.Contains("."))
                    return $"[dbo].[{Name}]";
                if (!Name.Contains("["))
                    return $"[{Name.Split('.')[0]}].[{Name.Split('.')[1]}]";
                return Name;

            }
        }
        List<string> fields;
        public IEnumerable<string> Fields
        {
            get
            {
                if (fields != null)
                    return fields;
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT c.name 
FROM sys.all_objects o
LEFT JOIN sys.all_columns c ON o.object_id = c.object_id
WHERE o.name = '@table'
ORDER BY column_id
".FormatWith(new { table = BasicName });

                    cmd.CommandType = CommandType.Text;
                    var sqlReader = cmd.ExecuteReader();

                    var fields = new List<string>();

                    while (sqlReader.Read())
                    {
                        fields.Add(sqlReader.GetString(0));
                    }
                    sqlReader.Close();
                    return fields;
                }
            }
        }
    }
}
