using System;

/**
 * @author Valloon Project
 * @version 1.0 @2020-03-03
 */
namespace Valloon.BitMEX
{
    static class GlobalParam
    {
        public const string APP_NAME = "ValloonBot";
        public const string APP_VERSION = "2020.04.07";
        public static string APP_HASH { get; set; }
        public static bool Paused { get; set; }
        public static string Message { get; set; }

        public static int ConnectionInterval { get; set; }
        public static DateTime LastAdminConnect { get; set; }

        static GlobalParam()
        {
            ConnectionInterval = 60;
        }
    }
}
