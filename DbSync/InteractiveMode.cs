using DbSync.Core;
using DbSync.Core.Transfers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Console;

namespace DbSync
{
    class InteractiveMode
    {
        string GetLineNormalized()
        {
            var result = ReadLine().Trim().ToLowerInvariant();
            if (result == "")
                return null;
            return result;
        }
        JobSettings settings;
        Program.CommandLineArguments cmdArgs;
        public InteractiveMode(JobSettings settings, Program.CommandLineArguments cmdArgs)
        {
            this.settings = settings;
            this.cmdArgs = cmdArgs;
        }
        void Export()
        {
            var datasetName = $"{settings.Name}-{DateTime.Now.ToString("yyyy-MM-dd hh-mm")}";
            WriteLine($"Choose a dataset name (or leave blank to choose the default of {datasetName})");
            datasetName = GetLineNormalized() ?? datasetName;
            var rootPath = settings.Path;
            settings.Path = Path.Combine(settings.Path, "data", datasetName);
            WriteLine($"Exporting current data to {Path.GetFullPath(settings.Path)}");
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            Exporter.Instance.Run(settings, cmdArgs.Environment);
            stopwatch.Stop();
            WriteLine($"Finished export, {stopwatch.ElapsedMilliseconds}ms elapsed");
            settings.Path = rootPath;
        }
        void Import()
        {
            throw new NotImplementedException();
        }
        public void Run()
        {
            WriteLine("Welcome to interactive DbSync mode. Using this tool you can import/export the database to a series of folders");
            bool done = false;
            while (!done)
            {
                WriteLine("Please enter a command (or type help to see a list of commands)");
                var line = GetLineNormalized();
                switch (line)
                {
                    case "import":
                        Import();
                        break;
                    case "export":
                        Export();
                        break;
                    case "quit":
                        done = true;
                        break;
                    case "info":
                        WriteLine();
                        WriteLine($"{settings.Name}");
                        WriteLine("===");
                        WriteLine($"Environment: {cmdArgs.Environment}");
                        WriteLine($"Path: {settings.Path}");
                        WriteLine($"Merge Strategy: {settings.MergeStrategy}");
                        WriteLine($"Connection String: {settings.ConnectionString}");
                        WriteLine($"Audit Columns: {string.Join(", ",settings.AuditColumns)}");
                        WriteLine($"Tables:");
                        foreach(var table in settings.Tables)
                        {
                            WriteLine($"+ {table.QualifiedName}");
                        }
                        break;
                    case "help":
                        WriteLine("import - choose a dataset to import to the database");
                        WriteLine("export - export a dataset from the database");
                        WriteLine("info - show information about the currently selected job");
                        WriteLine("quit - stops interactive mode");
                        WriteLine("help - show this message");
                        break;
                    default:
                        WriteLine($"{line} was not understood as a command");
                        break;
                }
            }
        }
    }
}
