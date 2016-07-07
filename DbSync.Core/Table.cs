using Dapper;
using DbSync.Core.Services;
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
        public bool Initialize(SqlConnection connection, JobSettings settings, IErrorHandler errorHandler)
        {
            this.connection = connection;
            this.settings = settings;

            Fields.AddRange(connection.Query<Field>(@"
SELECT c.Name, c.is_identity as IsPrimaryKey, c.is_nullable AS IsNullable, df.definition AS DefaultValue
FROM sys.all_objects o
INNER JOIN sys.all_columns c ON o.object_id = c.object_id
INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
LEFT JOIN sys.default_constraints df ON c.default_object_id =  df.object_id
WHERE o.Name = @table AND s.Name = @schema
ORDER BY column_id
", new { table = BasicName, schema = SchemaName }));

            var auditColumns = settings.AuditColumns.AuditColumnNames().Select(c => c.ToLowerInvariant()).ToList();
            foreach (var field in Fields)
            {
                if (auditColumns.Contains(field.CanonicalName))
                    field.IsAuditingColumn = true;
            }

            if (!Fields.Any())
            {
                errorHandler.Error($"Could not find any information for table {Name}. Make sure it exists in the target database");
                return false;
            }

            var data = Fields.Select(f => f.Name.ToLowerInvariant());

            if (PrimaryKey == null)
            {
                var primaryKeys = Fields.Where(f => f.IsPrimaryKey).ToList();
                if (primaryKeys.Count == 0)
                {
                    //errorHandler.Warning($"No primary key set for table {Name}, trying to infer from name");
                    primaryKeys = Fields.Where(f => f.Name.ToLowerInvariant() == "id" || f.Name.ToLowerInvariant() == BasicName.ToLowerInvariant() + "id").ToList();
                }
                if (primaryKeys.Count > 1)
                {
                    errorHandler.Error($"Multiple primary keys found for table {Name} ({string.Join(", ", primaryKeys.Select(pk => pk.Name))}). Please specify one manually.");
                    return false;
                }
                if (!primaryKeys.Any())
                {
                    errorHandler.Error($"No primary key could be found for table {Name}. Please specify one manually");
                    return false;
                }

                PrimaryKey = primaryKeys.Single().Name;
            }
            PrimaryKey = PrimaryKey.ToLowerInvariant();

            data = data.Where(f => f != PrimaryKey);

            if (settings.UseAuditColumnsOnImport ?? false)
                    data = data.Where(f => !settings.AuditColumns.AuditColumnNames().Select(a => a.ToLowerInvariant()).Contains(f));

            if (IsEnvironmentSpecific)
                data = data.Where(f => f != "isenvironmentspecific");

            DataFields = data.ToList();
            return true;
        }

        [XmlIgnore]
        public string BasicName => Name.Split('.').Last();
		[XmlIgnore]
		public string SchemaName => Name.Contains(".") ? Name.Split('.')[0]:"[dbo]";
        [XmlIgnore]
        public string QualifiedName => $"{SchemaName}.[{BasicName}]";
        [XmlIgnore]
        public string EnvironmentSpecificFileName => Path.Combine(settings.Path, Name) + "." + settings.CurrentEnvironment;
        public class Field
        {
            public string Name { get; set; }
            public string CanonicalName => Name.ToLowerInvariant();
            public bool IsPrimaryKey { get; set; }
            public bool IsNullable { get; set; }
            public bool IsAuditingColumn { get; set; }
            public string DefaultValue { get; set; }
        }
        [XmlIgnore]
        public List<Field> Fields { get; } = new List<Field>();
        [XmlIgnore]
        public List<string> DataFields { get; private set; }
        [XmlAttribute]
        public string PrimaryKey { get; set; }
        public bool UseDefaults { get; set; }
    }
}
