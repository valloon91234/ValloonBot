using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Forms;
using Valloon.Utils;

/**
 * @author Valloon Project
 * @version 1.0 @2020-04-07
 */
namespace Valloon.Trading
{
    class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        public enum StdHandle : int
        {
            STD_INPUT_HANDLE = -10,
            STD_OUTPUT_HANDLE = -11,
            STD_ERROR_HANDLE = -12,
        }

        public enum ConsoleMode : uint
        {
            ENABLE_ECHO_INPUT = 0x0004,
            ENABLE_EXTENDED_FLAGS = 0x0080,
            ENABLE_INSERT_MODE = 0x0020,
            ENABLE_LINE_INPUT = 0x0002,
            ENABLE_MOUSE_INPUT = 0x0010,
            ENABLE_PROCESSED_INPUT = 0x0001,
            ENABLE_QUICK_EDIT_MODE = 0x0040,
            ENABLE_WINDOW_INPUT = 0x0008,
            ENABLE_VIRTUAL_TERMINAL_INPUT = 0x0200,

            //screen buffer handle
            ENABLE_PROCESSED_OUTPUT = 0x0001,
            ENABLE_WRAP_AT_EOL_OUTPUT = 0x0002,
            ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004,
            DISABLE_NEWLINE_AUTO_RETURN = 0x0008,
            ENABLE_LVB_GRID_WORLDWIDE = 0x0010
        }

        private static bool QuickEditMode(bool enable)
        {
            IntPtr consoleHandle = GetStdHandle((int)StdHandle.STD_INPUT_HANDLE);
            if (!GetConsoleMode(consoleHandle, out uint consoleMode))
                return false;
            if (enable)
                consoleMode |= ((uint)ConsoleMode.ENABLE_QUICK_EDIT_MODE);
            else
                consoleMode &= ~((uint)ConsoleMode.ENABLE_QUICK_EDIT_MODE);
            consoleMode |= ((uint)ConsoleMode.ENABLE_EXTENDED_FLAGS);
            if (!SetConsoleMode(consoleHandle, consoleMode))
                return false;
            return true;
        }

        public const bool TEST_MODE = false;
        //public const bool TEST_MODE = true;

        public const bool GOLD_VERSION = false;
        //public const bool GOLD_VERSION = true;

        static void Main(string[] args)
        {
            //Utils.AES256CBC aes = new Utils.AES256CBC(Convert.FromBase64String("dFFdgWBUp8G8dhcX2cEUGBFWx2WRnaHPKZHogwptbyc="));
            //string encrypted = aes.Encrypt("{sdfwefwefef----------++++++++#######}");
            //Console.WriteLine(encrypted);
            //string Decrypted = aes.Decrypt("eyJpdiI6IklXVkw1eEdzbktoSllXR3FvTmk5OUFcdTAwM2RcdTAwM2QiLCJ2YWx1ZSI6IjJLTzF3T3ZDckprLytPTEVSaDg3VUFcdTAwM2RcdTAwM2QiLCJtYWMiOiJEMkFEMEY5Q0VCOTdGMEZDMjcwNzZFN0VDRkY3NjcyMUUwMjQzMjY3NjFCNTE0RjIxOTFFNEEyQkYyNUE3MjQ1In0=");
            //Console.WriteLine(Decrypted);
            //Decrypted = aes.Decrypt(encrypted);
            //Console.WriteLine(Decrypted);
            //encrypted = aes.Encrypt(Decrypted);
            //Console.WriteLine(encrypted);

            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            QuickEditMode(false);
            //Console.BufferHeight = Int16.MaxValue - 1;
            try
            {
                Config.APP_HASH = GetMyMD5();
            }
            catch (Exception ex)
            {
                Config.APP_HASH = ex.Message;
            }

            if (TEST_MODE)
            {
                InertiaStrategy.Run();
                Logger.WriteLine($"\r\nPress any key to exit... ");
                Console.ReadKey(false);
            }
            else
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Main());
            }
        }

        public static string GetMyMD5()
        {
            using (var md5 = new MD5CryptoServiceProvider())
            {
                using (var stream = new FileStream(Process.GetCurrentProcess().MainModule.FileName, FileMode.Open, FileAccess.Read))
                {
                    return StringUtils.ToHexString(md5.ComputeHash(stream));
                }
            }
        }
    }
}
