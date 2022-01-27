using System;
using System.IO;
using System.Threading;

/**
 * @author Valloon Project
 * @version 1.0 @2020-03-03
 */
namespace Valloon.BitMEX
{
    public class Logger
    {
        public string logFilename { get; set; }

        public Logger(string logFilename)
        {
            this.logFilename = logFilename;
        }

        public void WriteLine(string text = null, ConsoleColor color = ConsoleColor.White, bool writeFile = true)
        {
            if (text == null)
            {
                Console.WriteLine();
                return;
            }
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = ConsoleColor.White;
            if (writeFile) WriteFile(text);
        }

        public void WriteFile(string text = null)
        {
            try
            {
                string filename = logFilename + ".txt";
                using (var streamWriter = new StreamWriter(filename, true))
                {
                    streamWriter.WriteLine(text);
                }
            }
            catch (Exception ex)
            {
                WriteLine("Cannot write log file : " + ex.Message, ConsoleColor.Red, false);
            }
        }

    }
}