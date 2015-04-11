using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbSync
{
    class Settings
    {
        public string ConnectionString { get; set; }
        public List<string> Tables { get; set; } = new List<string>();
        public string Path { get; set; } = System.IO.Path.GetFullPath(".");

        public Merge.Strategy MergeStrategy { get; set; } = Merge.Strategy.MergeWithoutDelete;

    }
}
