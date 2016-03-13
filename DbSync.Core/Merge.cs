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
            MergeWithoutDelete, MergeWithDelete, Add, Overwrite
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
        private static string mergeWithoutDeleteSQL = loadScript(nameof(mergeWithDeleteSQL));
        private static string mergeWithDeleteSQL = loadScript(nameof(mergeWithDeleteSQL));
        private static string mergeWithoutDeleteWithAuditSQL = loadScript(nameof(mergeWithoutDeleteWithAuditSQL));
        private static string mergeWithDeleteWithAuditSQL = loadScript(nameof(mergeWithDeleteWithAuditSQL));

        public static string GetSqlForMergeStrategy(JobSettings settings, string target, string source, string primaryKey, IEnumerable<string> restOfColumns)
        {
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
            switch (settings.MergeStrategy)
            {
                case Strategy.MergeWithoutDelete:
                    if (settings.UseAuditColumnsOnImport)
                        sqlToUse =  mergeWithoutDeleteWithAuditSQL;
                    else
                        sqlToUse = mergeWithoutDeleteSQL;
                    break;
                case Strategy.MergeWithDelete:
                    if (settings.UseAuditColumnsOnImport)
                        sqlToUse = mergeWithDeleteWithAuditSQL;
                    else
                        sqlToUse = mergeWithDeleteSQL;
                    break;
                default:
                    throw new NotImplementedException("That merge strategy is not yet supported");
            }
            return sqlToUse.FormatWith(configObject);
        }
    }
}
