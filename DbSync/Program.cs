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
        class CommandLineArguments
        {
            public bool Import { get; set; }
            public bool ImportScript { get; set; }
            public bool Export { get; set; }
            [PowerCommandParser.Required]
            public string Config { get; set; }
            public string Job { get; set; }
            public string ImportScriptName { get; set; } = "ImportScript.sql";
        }
        public class Settings
        {
            [XmlElement("Job")]
            public List<JobSettings> Jobs { get; set; }
        }
        static void RunJob(JobSettings job, CommandLineArguments cmdArgs)
        {
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            if (cmdArgs.Export)
                try
                {
                    Exporter.Instance.Export(job).Wait();
                }
                catch(AggregateException aggEx) 
                {
                    foreach(var ex in aggEx.InnerExceptions)
                    {
                        Console.Error.WriteLine($"Job failed because of exception {ex.Message}");
                    }
                }
            if (cmdArgs.Import)
                Importer.Instance.Import(job);
            if (cmdArgs.ImportScript)
                File.WriteAllText(cmdArgs.ImportScriptName,Importer.Instance.GenerateImportScript(job));

            watch.Stop();
            Console.WriteLine($"Executed job {job.Name}, Elapsed {watch.ElapsedMilliseconds}ms");
        }
        static void Main(string[] args)
        {
            var cmdArgs = PowerCommandParser.Parser.ParseArguments<CommandLineArguments>(args);
            if (cmdArgs == null)
                return;
            var serializer = new XmlSerializer(typeof(Settings));
            StreamReader configFileStream = new StreamReader(cmdArgs.Config);

            var settings = (Settings)serializer.Deserialize(configFileStream);

            if (settings.Jobs.Count == 0)
            {
                Console.Error.WriteLine("Must specify at least one job in the config file");
                return;
            }

            if (string.IsNullOrWhiteSpace(cmdArgs.Job))
            {
                foreach (var job in settings.Jobs)
                {
                    RunJob(job, cmdArgs);
                }
            }
            else
            {
                var selectedJob = settings.Jobs.SingleOrDefault(j => j.Name.Equals(cmdArgs.Job, StringComparison.InvariantCultureIgnoreCase));
                if (selectedJob == null)
                {
                    Console.Error.WriteLine($"No job found that matches {cmdArgs.Job}");
                    return;
                }
                RunJob(selectedJob, cmdArgs);
            }
        }
    }
}
