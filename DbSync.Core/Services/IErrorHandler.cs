using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbSync.Core.Services
{
    interface IErrorHandler
    {
        void Error(string message);
        void Warning(string message);
    }
    public class DefaultErrorHandler
    {
        void WriteInColour(string message, ConsoleColor color)
        {

            var foregroundColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Error.WriteLine(message);
            Console.ForegroundColor = foregroundColor;
        }
        public bool ContinueOnError { get; set; }
        public bool WarningsAsErrors { get; set; }
        public bool PauseOnError { get; set; }
        public void Error(string message)
        {
            WriteInColour($"Error: {message}", ConsoleColor.Red);
            if (PauseOnError)
                Console.ReadKey();
            if (!ContinueOnError)
                throw new DbSyncException(message);
        }
        public void Warning(string message)
        {
            WriteInColour($"Warning: {message}", ConsoleColor.Yellow);
            if (WarningsAsErrors)
            {
                if (PauseOnError)
                    Console.ReadKey();
                if (!ContinueOnError)
                    throw new DbSyncException(message);
            }
        }
    }
}
