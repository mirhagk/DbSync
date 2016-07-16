using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DbSync.Core.Services;

namespace DbSync.Core.Transfers
{
    class SmartTransfer : Transfer
    {
        public override void Run(JobSettings settings, string environment, IErrorHandler errorHandler)
        {
            var target = new IDataReader
            foreach(var table in settings.Tables)
            {

            }
        }
    }
}
