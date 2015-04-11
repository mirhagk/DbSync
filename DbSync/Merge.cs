using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbSync
{
    class Merge
    {
        public enum Strategy
        {
            MergeWithoutDelete, MergeWithDelete, Add, Overwrite
        }
        private static string mergeWithoutDeleteSQL = @"
UPDATE t
SET @columnUpdateList
FROM @target t
INNER JOIN @source s ON t.@id = s.@id


SET IDENTITY_INSERT @target ON

INSERT INTO @target (@id, @columns)
SELECT @id, @columns
FROM @source s
WHERE s.@id NOT IN (SELECT @id FROM @target t)

SET IDENTITY_INSERT @target OFF

";
        private static string mergeWithDeleteSQL = mergeWithoutDeleteSQL + @"
DELETE FROM @target
WHERE @target.@id NOT IN (SELECT @id FROM @source)
";

        public static string GetSqlForMergeStrategy(Strategy mergeStrategy, string target, string source, string primaryKey, List<string> restOfColumns)
        {
            var configObject = new
            {
                target = target,
                id = primaryKey,
                columns = string.Join(",", restOfColumns),
                source = source,
                columnUpdateList = string.Join(",", restOfColumns.Select(r => r + "=s." + r))
            };
            switch (mergeStrategy)
            {
                case Strategy.MergeWithoutDelete:
                    return mergeWithoutDeleteSQL.FormatWith(configObject);
                case Strategy.MergeWithDelete:
                    return mergeWithDeleteSQL.FormatWith(configObject);
                default:
                    throw new NotImplementedException("That merge strategy is not yet supported");
            }
        }
    }
}
