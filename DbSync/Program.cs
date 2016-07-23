using DbSync.Core;
using DbSync.Core.Transfers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace DbSync
{
    public class Program
    {
        public class CommandLineArguments
        {
            public bool Interactive { get; set; }
            public bool Import { get; set; }
            public bool ImportScript { get; set; }
            public bool ImportDiff { get; set; }
            public bool Export { get; set; }
            [PowerCommandParser.Required]
            public string Config { get; set; }
            public string Job { get; set; }
            public string ImportScriptName { get; set; } = "ImportScript.sql";
            public string Environment { get; set; } = "local";
            public string ConnectionString { get; set; }
            public string WorkingDirectory { get; set; }
        }
        public class Settings
        {
            public string ConnectionString { get; set; }
            public Merge.Strategy MergeStrategy { get; set; } = Merge.Strategy.MergeWithoutDelete;
            public bool IgnoreAuditColumnsOnExport { get; set; } = false;
            public bool UseAuditColumnsOnImport { get; set; } = false;
            public bool DisableConstraintsOnImport { get; set; } = false;
            public JobSettings.AuditSettings AuditColumns { get; set; }
            [XmlElement("Job")]
            public List<JobSettings> Jobs { get; set; }
        }
        static void ApplyGlobalSettings(JobSettings jobSettings, Settings globalSettings)
        {
            jobSettings.ConnectionString = jobSettings.ConnectionString ?? globalSettings.ConnectionString;
            jobSettings.MergeStrategy = jobSettings.MergeStrategy ?? globalSettings.MergeStrategy;
            jobSettings.IgnoreAuditColumnsOnExport = jobSettings.IgnoreAuditColumnsOnExport ?? globalSettings.IgnoreAuditColumnsOnExport;
            jobSettings.UseAuditColumnsOnImport = jobSettings.UseAuditColumnsOnImport ?? globalSettings.UseAuditColumnsOnImport;
            jobSettings.DisableConstraintsOnImport = jobSettings.DisableConstraintsOnImport ?? globalSettings.DisableConstraintsOnImport;
            jobSettings.AuditColumns = jobSettings.AuditColumns ?? globalSettings.AuditColumns;
        }
        static void RunJob(JobSettings job, CommandLineArguments cmdArgs)
        {
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            try
            {
                var errorHandler = new Core.Services.DefaultErrorHandler();
                job.CurrentEnvironment = cmdArgs.Environment;
                if (cmdArgs.Export)
                    Exporter.Instance.Run(job, cmdArgs.Environment, errorHandler);
                if (cmdArgs.Import)
                    Importer.Instance.Run(job, cmdArgs.Environment, errorHandler);
                if (cmdArgs.ImportScript)
                {
                    var importScriptGenerator = GenerateImportScript.Instance;
                    importScriptGenerator.Filename = cmdArgs.ImportScriptName;
                    GenerateImportScript.Instance.Run(job, cmdArgs.Environment, errorHandler);
                }

            }
            catch(DbSyncException ex)
            {
                var foregroundColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"Job failed");
                Console.ForegroundColor = foregroundColor;
            }
            watch.Stop();
            Console.WriteLine($"Executed job {job.Name}, Elapsed {watch.ElapsedMilliseconds}ms");
        }
        static void Main(string[] args)
        {
            var cmdArgs = PowerCommandParser.Parser.ParseArguments<CommandLineArguments>(args);
            if (cmdArgs == null)
                return;
            if (cmdArgs.WorkingDirectory != null)
                Directory.SetCurrentDirectory(cmdArgs.WorkingDirectory);

            var serializer = new XmlSerializer(typeof(Settings));
            StreamReader configFileStream = new StreamReader(cmdArgs.Config);

            var settings = (Settings)serializer.Deserialize(configFileStream);
            if (cmdArgs.ConnectionString != null)
                settings.Jobs.ForEach(j => j.ConnectionString = cmdArgs.ConnectionString);

                if (settings.Jobs.Count == 0)
            {
                Console.Error.WriteLine("Must specify at least one job in the config file");
                return;
            }

            if (string.IsNullOrWhiteSpace(cmdArgs.Job))
            {
                foreach (var job in settings.Jobs)
                {
                    ApplyGlobalSettings(job, settings);
                    RunJob(job, cmdArgs);
                }
            }
            else
            {
                var selectedJobs = cmdArgs.Job.Split(',').Select(j => j.ToLowerInvariant().Trim());
                var jobs = settings.Jobs.Where(j => selectedJobs.Contains(j.Name.ToLowerInvariant()));
                if (jobs.Any())
                {
                    foreach(var job in jobs)
                    {
                        ApplyGlobalSettings(job, settings);
                        if (cmdArgs.Interactive)
                            new InteractiveMode(job, cmdArgs).Run();
                        else
                            RunJob(job, cmdArgs);
                    }
                }
                else
                {
                    Console.Error.WriteLine($"No job found that matches {cmdArgs.Job}");
                    return;
                }
            }
        }
    }
}
