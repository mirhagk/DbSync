using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PerformanceTests
{
    class Program
    {
        public class MachineInfo
        {
            public bool HighResStopwatch { get; internal set; }
            public bool Is64Bit { get; internal set; }
            public bool Is64BitOS { get; internal set; }
            public OperatingSystem OS { get; internal set; }
            public int ProcessorCount { get; internal set; }
            public long StopwatchFrequency { get; internal set; }
            public long WorkingSet { get; internal set; }
            public double WorkingSetGB { get; internal set; }
        }
        public class PerformanceResult
        {
        }
        static MachineInfo GatherMachineInfo()
        {
            var info = new MachineInfo();
            info.ProcessorCount = Environment.ProcessorCount;
            info.Is64BitOS = Environment.Is64BitOperatingSystem;
            info.Is64Bit = Environment.Is64BitProcess;
            info.OS = Environment.OSVersion;
            info.WorkingSet = Environment.WorkingSet;
            info.WorkingSetGB = Environment.WorkingSet / 1024.0 / 1024 / 1024;
            info.HighResStopwatch = Stopwatch.IsHighResolution;
            info.StopwatchFrequency = Stopwatch.Frequency;
            return info;
        }
        static void RunTest(Action TestToRun, int iterations = 5, int bestOf = 5)
        {
            GC.Collect(3, GCCollectionMode.Forced, true);
            Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(2);
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;

            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            for (int i = 0; i < bestOf; i++)
            {
                var watch = new Stopwatch();
                var startTime = Process.GetCurrentProcess().TotalProcessorTime;

                watch.Start();

                watch.Stop();
                var endTime = Process.GetCurrentProcess().TotalProcessorTime;
            }

        }
        static void Main(string[] args)
        {
            var info = GatherMachineInfo();

            Console.WriteLine(JsonConvert.SerializeObject(info, Formatting.Indented));

            Console.ReadKey();
        }
    }
}
