using Dapper;
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
        [XmlIgnore]
        public Merge.Strategy? MergeStrategy { get; set; }
        [XmlAttribute]
        public bool ByEnvironment { get; set; }

        [XmlAttribute(nameof(MergeStrategy))]
        private Merge.Strategy MergeStrategySerialized
        {
            get
            {
                return MergeStrategy.Value;
            }
            set
            {
                MergeStrategy = value;
            }
        }
        private bool ShouldSerializeMergeStrategySerialized() => MergeStrategy.HasValue;
        #endregion

        class Schema
        {
            public string Name { get; set; }
        }

        SqlConnection connection;
        JobSettings settings;
        public void Initialize(SqlConnection connection, JobSettings settings)
        {
            this.connection = connection;
            this.settings = settings;

            Fields.AddRange(connection.Query<Schema>(@"
SELECT c.Name 
FROM sys.all_objects o
LEFT JOIN sys.all_columns c ON o.object_id = c.object_id
LEFT JOIN sys.schemas s ON o.schema_id = s.schema_id
WHERE o.name = @table AND s.name = @schema
ORDER BY column_id
", new { table = BasicName, schema = SchemaName }).Select(x => x.Name));

            if (!Fields.Any())
                throw new DbSyncException($"Could not find any information for table {Name}. Make sure it exists in the target database");

            var data = Fields.Select(f => f.ToLowerInvariant());

            if (PrimaryKey == null)
            {
                PrimaryKey = data.SingleOrDefault(f => f == "id" || f == BasicName.ToLowerInvariant() + "id");

                if (PrimaryKey == null)
                    throw new DbSyncException($"No primary key found for table {Name}");
            }
            else
                PrimaryKey = PrimaryKey.ToLowerInvariant();

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
