using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbSync
{
    class Transfer
    {
        protected string Get2PartName(string tableName)
        {
            if (!tableName.Contains("."))
                return $"[dbo].[{tableName}]";
            if (!tableName.Contains("["))
                return $"[{tableName.Split('.')[0]}].[{tableName.Split('.')[1]}]";
            return tableName;
        }
        protected string Get1PartName(string tableName)
        {
            return tableName.Split('.').Last();
        }
    }
}
