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
        public string ConnectionString { get; set; }
        [XmlArray(ElementName = "Tables")]
        [XmlArrayItem(ElementName = "Table")]
        public List<string> Tables { get; set; } = new List<string>();
        public string Path { get; set; } = System.IO.Path.GetFullPath(".");
        public Merge.Strategy MergeStrategy { get; set; } = Merge.Strategy.MergeWithoutDelete;
    }
}
