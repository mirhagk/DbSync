using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbSync.Core.Transfers
{
    public abstract class Transfer
    {
        public abstract void Run(JobSettings settings, string environment);
    }
}
