﻿using System;
using System.IO;
using System.Threading;

/**
 * @author Valloon Project
 * @version 1.0 @2020-03-03
 */
namespace Valloon.Trading
{
    public static class Logger
    {
        public static readonly string LOG_DIRECTORY = "log";

        public static void WriteLine(string text = null, ConsoleColor color = ConsoleColor.White, bool writeFile = true)
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

        public static bool ExistFile(string filenameSuffix = null)
        {
            DirectoryInfo logDirectoryInfo = new DirectoryInfo(LOG_DIRECTORY);
            if (!logDirectoryInfo.Exists) return false;
            DateTime now = BitMEXApiHelper.ServerTime == null ? DateTime.UtcNow : BitMEXApiHelper.ServerTime.Value;
            string logFilename = Path.Combine(LOG_DIRECTORY, now.ToString("yyyy-MM-dd") + filenameSuffix + ".txt");
            return File.Exists(logFilename);
        }

        public static void WriteFile(string text = null, string filenameSuffix = null)
        {
            try
            {
                DirectoryInfo logDirectoryInfo = new DirectoryInfo(LOG_DIRECTORY);
                if (!logDirectoryInfo.Exists) logDirectoryInfo.Create();
                DateTime now = BitMEXApiHelper.ServerTime == null ? DateTime.UtcNow : BitMEXApiHelper.ServerTime.Value;
                string logFilename = Path.Combine(LOG_DIRECTORY, now.ToString("yyyy-MM-dd") + filenameSuffix + ".txt");
                using (var streamWriter = new StreamWriter(logFilename, true))
                {
                    streamWriter.WriteLine(text);
                }
            }
            catch (Exception ex)
            {
                WriteLine("Cannot write log file : " + ex.Message, ConsoleColor.Red, false);
            }
        }

        public static void WriteWait(string text, int seconds)
        {
            Console.Write(text);
            for (int i = 0; i < seconds; i++)
            {
                Console.Write('.');
                Thread.Sleep(1000);
            }
            Console.WriteLine();
        }

    }
}