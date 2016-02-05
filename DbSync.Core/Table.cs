using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
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

				while (sqlReader.Read())
				{
					var field = sqlReader.GetString(0);

					Fields.Add(field);
				}
				sqlReader.Close();
			}
            var data = Fields.Select(f => f.ToLowerInvariant());

            PrimaryKey = data.SingleOrDefault(f => f == "id" || f == BasicName.ToLowerInvariant() + "id");
            
            if (PrimaryKey == null)
                throw new DbSyncException($"No primary key found for table {Name}");

            data = data
                .Where(f => f != PrimaryKey)
                .Where(f => !settings.AuditColumns.AuditColumnNames().Select(a=>a.ToLowerInvariant()).Contains(f));

            if (IsEnvironmentSpecific)
                data = data.Where(f => f != "isenvironmentspecific");

            DataFields = data.ToList();
        }

        [XmlIgnore]
        public string BasicName => Name.Split('.').Last();
		[XmlIgnore]
		public string SchemaName => Name.Contains(".") ? Name.Split('.')[0]:"[dbo]";
        [XmlIgnore]
        public string QualifiedName => $"{SchemaName}.[{BasicName}]";
        [XmlIgnore]
        public string EnvironmentSpecificFileName => Path.Combine(settings.Path, Name) + "." + settings.CurrentEnvironment;
        [XmlIgnore]
        public List<string> Fields { get; } = new List<string>();
        [XmlIgnore]
        public List<string> DataFields { get; private set; }
        [XmlAttribute]
        public string PrimaryKey { get; set; }
    }
}
