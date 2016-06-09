using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbSync.Core.Services
{
    public interface IErrorHandler
    {
        void Error(string message);
        void Warning(string message);
    }
    public class DefaultErrorHandler : IErrorHandler
    {
        void WriteInColour(string message, ConsoleColor color)
        {

            var foregroundColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Error.WriteLine(message);
            Console.ForegroundColor = foregroundColor;
        }
        public bool ContinueOnError { get; set; } = false;
        public bool WarningsAsErrors { get; set; } = false;
        public bool PauseOnError { get; set; } = false;
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
