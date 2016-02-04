using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DbSync.Core
{
    public class Table
    {
        #region XmlValues
        [XmlText]
        public string Name { get; set; }
        [XmlAttribute]
        public bool IsEnvironmentSpecific { get; set; }
        #endregion

        SqlConnection connection;
        JobSettings settings;
        public void Initialize(SqlConnection connection, JobSettings settings)
        {
            this.connection = connection;
            this.settings = settings;
			
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

				Fields = new List<string>();

				while (sqlReader.Read())
				{
					var field = sqlReader.GetString(0);

					Fields.Add(field);
				}
				sqlReader.Close();
			}
        }

        [XmlIgnore]
        public string BasicName => Name.Split('.').Last();
		[XmlIgnore]
		public string SchemaName => Name.Contains(".") ? Name.Split('.')[0]:"[dbo]";
        [XmlIgnore]
        public string QualifiedName => $"{SchemaName}.[{BasicName}]";
        List<string> Fields;
        [XmlIgnore]
        public List<string> Fields { get; }
        List<string> dataFields;
        [XmlIgnore]
        public List<string> DataFields
        {
            get
            {
                if (dataFields != null)
                    return dataFields;

                var data = Fields
                    .Where(f => f != PrimaryKey)
                    .Where(f => !settings.AuditColumns.AuditColumnNames().Contains(f));

                if (IsEnvironmentSpecific)
                    data = data.Where(f => f.ToLowerInvariant() != "isenvironmentspecific");

                dataFields = data.ToList();

                return dataFields;
            }
        }
        string primaryKey;
        [XmlAttribute]
        public string PrimaryKey
        {
            get
            {
                if (primaryKey != null)
                    return primaryKey;

                primaryKey = Fields.SingleOrDefault(f => f.ToLowerInvariant() == "id" || f.ToLowerInvariant() == BasicName.ToLowerInvariant() + "id");
                return primaryKey;
            }
            set
            {
                primaryKey = value;
            }
        }
    }
}
