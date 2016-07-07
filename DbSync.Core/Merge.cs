using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DbSync.Core
{
    public static class Merge
    {
        public enum Strategy
        {
            MergeWithoutDelete, MergeWithDelete, AddOnly, Override, DropReadd
        }
        private static string loadScript(string scriptName)
        {
            scriptName = scriptName.Replace("SQL", "");
            //resource names are case-sensitive and files are named UpperCamelCase, while private vars are not.
            scriptName = scriptName.First().ToString().ToUpper() + String.Join("", scriptName.Skip(1));
            scriptName = Path.ChangeExtension(scriptName, "sql");

            //assumes Merge is located in the default namespace.  There is no way to get default namespace of assembly.
            var manifestResourceName = $@"{typeof(Merge).Namespace}.Scripts.{scriptName}";

            //load from assembly manifest and return as string
            var stream = Assembly
                .GetExecutingAssembly()
                .GetManifestResourceStream(manifestResourceName);
            return new StreamReader(stream).ReadToEnd();
        }
        static string delete = loadScript(nameof(delete));
        static string insert = loadScript(nameof(insert));
        static string insertWithAudit = loadScript(nameof(insertWithAudit));
        static string update = loadScript(nameof(update));
        static string updateWithAudit = loadScript(nameof(updateWithAudit));
        static string dropReadd = loadScript(nameof(dropReadd));
        static string getUpdate(JobSettings settings) => settings.UseAuditColumnsOnImport.Value ? updateWithAudit : update;
        static string getInsert(JobSettings settings) => settings.UseAuditColumnsOnImport.Value ? insertWithAudit : insert;
        static string overwriteSql(JobSettings settings, string target, string source, string primaryKey, IEnumerable<string> restOfColumns)
        {
            string sql = $"UPDATE t SET ";
            var firstColumn = true;
            foreach(var column in restOfColumns)
            {
                if (firstColumn)
                    firstColumn = false;
                else
                    sql += ",\n";
                sql += $"[{column}] = ISNULL(s.[{column}],t.[{column}])";
            }
            if (settings.UseAuditColumnsOnImport.Value)
            {
                sql+= @",
@modifiedUser = SUSER_NAME(),
@modifiedDate = GETDATE()";
            }
            sql += $"\nFROM {target} t\nINNER JOIN {source} s ON t.{primaryKey} = s.{primaryKey}";
            return sql;
        }

        public static string GetSqlForMergeStrategy(JobSettings settings, Table table)
        {
            var target = table.QualifiedName;
            var source = "##" + table.BasicName;
            var primaryKey = table.PrimaryKey;
            var restOfColumns = table.DataFields;
            var configObject = new
            {
                target = target,
                id = primaryKey,
                columns = string.Join(",", restOfColumns.Select(c=>$"[{c}]")),
                source = source,
                columnUpdateList = string.Join(",", restOfColumns.Select(r => $"[{r}]=s.[{r}]")),
                modifiedUser = settings.AuditColumns?.ModifiedUser,
                modifiedDate = settings.AuditColumns?.ModifiedDate,
                createdUser = settings.AuditColumns?.CreatedUser,
                createdDate = settings.AuditColumns?.CreatedDate
            };
            string sqlToUse = null;

            switch (table.MergeStrategy ?? settings.MergeStrategy)
            {
                case Strategy.MergeWithoutDelete:
                    sqlToUse = getInsert(settings) + "\n" + getUpdate(settings);
                    break;
                case Strategy.MergeWithDelete:
                    sqlToUse = getInsert(settings) + "\n" + getUpdate(settings) + "\n" + delete;
                    break;
                case Strategy.AddOnly:
                    sqlToUse = getInsert(settings);
                    break;
                case Strategy.Override:
                    sqlToUse = getInsert(settings) + "\n" + overwriteSql(settings, target, source, primaryKey, restOfColumns);
                    break;
                case Strategy.DropReadd:
                    sqlToUse = dropReadd;
                    break;
                default:
                    throw new NotImplementedException("That merge strategy is not yet supported");
            }
            return sqlToUse.FormatWith(configObject);
        }
    }
}
