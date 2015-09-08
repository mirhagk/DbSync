using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbSync
{
    class DbSyncException:Exception
    {
        public DbSyncException(string message) : base(message) { }
    }
}
