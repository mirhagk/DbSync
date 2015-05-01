using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DbSync
{
    public class JobSettings
    {
        public class AuditSettings
        {
            public string CreatedDate { get; set; }
            public string CreatedUser { get; set; }
            public string ModifiedDate { get; set; }
            public string ModifiedUser { get; set; }
        }
        public string ConnectionString { get; set; }
        [XmlArray(ElementName = "Tables")]
        [XmlArrayItem(ElementName = "Table")]
        public List<string> Tables { get; set; } = new List<string>();
        public string Path { get; set; } = System.IO.Path.GetFullPath(".");
        public Merge.Strategy MergeStrategy { get; set; } = Merge.Strategy.MergeWithoutDelete;
        public string Name { get; set; }
        public AuditSettings AuditColumns { get; set; }
        public bool IgnoreAuditColumnsOnExport { get; set; } = false;

        public bool IsAuditColumn(string fieldName)
        {
            var lowerCaseField = fieldName.ToLowerInvariant();
            return AuditColumns?.ModifiedDate?.ToLowerInvariant() == lowerCaseField
                || AuditColumns?.ModifiedUser?.ToLowerInvariant() == lowerCaseField
                || AuditColumns?.CreatedDate?.ToLowerInvariant() == lowerCaseField
                || AuditColumns?.CreatedUser?.ToLowerInvariant() == lowerCaseField;
        }
    }
}
