using Newtonsoft.Json;
using System;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using Valloon.Utils;

/**
 * @author Valloon Project
 * @version 2.0 @2020-05-10
 */
namespace Valloon.BitMEX
{
    [DataContract]
    public class Config
    {
        public static readonly string FILENAME = "config.json";
        public static readonly string DATE_FORMAT = "yyyy-MM-dd";
        private static string LastJson = null;
        private static string LastJsonOnline = null;
        private static Config LastConfig = null;

        //[DataMember(Name = "username", EmitDefaultValue = false)]
        [DataMember(Name = "username")]
        public string Username { get; set; }

        [DataMember(Name = "api_key")]
        public string ApiKey { get; set; }

        [DataMember(Name = "api_secret")]
        public string ApiSecret { get; set; }

        [DataMember(Name = "expire_date")]
        public string ExpireDate { get; set; }

        [DataMember(Name = "activation_code")]
        public string ActivationCode { get; set; }

        [DataMember(Name = "testnet_mode")]
        public bool TestnetMode { get; set; }

        [DataMember(Name = "connection_interval")]
        public int ConnectionInverval { get; set; }

        [DataMember(Name = "auto_config")]
        public bool AutoConfig { get; set; }

        [DataMember(Name = "buy_or_sell")]
        public int BuyOrSell { get; set; }

        [DataMember(Name = "bin_size")]
        public string BinSize { get; set; }

        [DataMember(Name = "bb_length")]
        public int BBLength { get; set; }

        [DataMember(Name = "bb_multiplier_upper")]
        public double BBMultiplierUpper { get; set; }

        [DataMember(Name = "bb_multiplier_lower")]
        public double BBMultiplierLower { get; set; }

        [DataMember(Name = "rsi_length")]
        public int RSILength { get; set; }

        [DataMember(Name = "rsi_upper")]
        public int RSIUpper { get; set; }

        [DataMember(Name = "rsi_lower")]
        public int RSILower { get; set; }

        [DataMember(Name = "qty_ratio")]
        public double QtyRatio { get; set; }

        [DataMember(Name = "upper_qty_x")]
        public double UpperQtyX { get; set; }

        [DataMember(Name = "lower_qty_x")]
        public double LowerQtyX { get; set; }

        [DataMember(Name = "raise_qty_x")]
        public int RaiseQtyX { get; set; }

        [DataMember(Name = "stop_qty_x")]
        public int StopQtyX { get; set; }

        [DataMember(Name = "max_qty_x")]
        public int MaxQtyX { get; set; }

        [DataMember(Name = "min_order_distance")]
        public int MinOrderDistance { get; set; }

        [DataMember(Name = "min_upper_height")]
        public int MinUpperHeight { get; set; }

        [DataMember(Name = "min_lower_height")]
        public int MinLowerHeight { get; set; }

        [DataMember(Name = "min_upper_close_height")]
        public int MinUpperCloseHeight { get; set; }

        [DataMember(Name = "min_lower_close_height")]
        public int MinLowerCloseHeight { get; set; }

        [DataMember(Name = "exponential")]
        public bool Exponential { get; set; }

        [DataMember(Name = "exit")]
        public int Exit { get; set; }

        [JsonIgnore]
        public bool Active { get; set; }

        [JsonIgnore]
        public DateTime ExpireDateTime { get; set; }

        public static Config Load(out bool updated, bool forceUpdate = false)
        {
            string configJson = File.ReadAllText(FILENAME);
            if (LastConfig == null || configJson != LastJson || forceUpdate)
            {
                updated = true;
                Logger.WriteLine();
                Logger.WriteLine("Loading config ...", ConsoleColor.Green);
                Config config = JsonConvert.DeserializeObject<Config>(configJson);
                config.ExpireDate = "2020-12-31";
                Logger.WriteLine("username = " + config.Username);
                Logger.WriteLine("api_key = " + config.ApiKey);
                //Logger.WriteLine("expire_date = " + config.ExpireDate);
                //Logger.WriteLine("activation_code = " + config.ActivationCode);
                Logger.WriteLine("testnet_mode = " + config.TestnetMode.ToString().ToLower());
                Logger.WriteLine("connection_interval = " + config.ConnectionInverval);
                Logger.WriteLine("auto_config = " + config.AutoConfig);
                Logger.WriteLine("buy_or_sell = " + config.BuyOrSell);
                Logger.WriteLine("bin_size = " + config.BinSize);
                Logger.WriteLine("bb_length = " + config.BBLength);
                Logger.WriteLine("bb_multiplier_upper = " + config.BBMultiplierUpper);
                Logger.WriteLine("bb_multiplier_lower = " + config.BBMultiplierLower);
                Logger.WriteLine("rsi_length = " + config.RSILength);
                Logger.WriteLine("rsi_upper = " + config.RSIUpper);
                Logger.WriteLine("rsi_lower = " + config.RSILower);
                Logger.WriteLine("qty_ratio = " + config.QtyRatio);
                Logger.WriteLine("sell_qty_x = " + config.UpperQtyX);
                Logger.WriteLine("buy_qty_x = " + config.LowerQtyX);
                Logger.WriteLine("plus_qty_x = " + config.RaiseQtyX);
                Logger.WriteLine("stop_qty_x = " + config.StopQtyX);
                Logger.WriteLine("max_qty_x = " + config.MaxQtyX);
                Logger.WriteLine("min_order_distance = " + config.MinOrderDistance);
                Logger.WriteLine("min_sell_order_height = " + config.MinUpperHeight);
                Logger.WriteLine("min_buy_order_height = " + config.MinLowerHeight);
                Logger.WriteLine("min_sell_close_height = " + config.MinUpperCloseHeight);
                Logger.WriteLine("min_buy_close_height = " + config.MinLowerCloseHeight);
                Logger.WriteLine("exponential = " + config.Exponential);
                Logger.WriteLine("exit = " + config.Exit);
                Logger.WriteLine();
                if (config.Username == null) config.Username = config.ApiKey;
                if (config.ApiKey == null) throw new Exception($"Error in config : api_key is empty."); ;
                if (config.ApiSecret == null) throw new Exception($"Error in config : api_secret is empty.");
                //config.Activated = CheckActivationCode(config.ApiKey, config.ExpireDate, config.ActivationCode);
                config.Active = true;
                if (config.ExpireDate != null) config.ExpireDateTime = DateTime.ParseExact(config.ExpireDate, DATE_FORMAT, CultureInfo.InvariantCulture);
                LastJson = configJson;
                LastConfig = config;
            }
            else
            {
                updated = false;
            }
            return LastConfig;
        }

        public static Config UpdateOnline(Config config, out bool updated, bool forceUpdate = false)
        {
            string configJsonOnline = BackendClient.HttpGet("https://bitmex-20200620.firebaseio.com/config-0625.json");
            if (configJsonOnline != LastJsonOnline || forceUpdate)
            {
                updated = true;
                Logger.WriteLine();
                Logger.WriteLine("Loading auto-config ...", ConsoleColor.Green);
                Config configOnline = JsonConvert.DeserializeObject<Config>(configJsonOnline);
                config.BuyOrSell = configOnline.BuyOrSell;
                config.BinSize = configOnline.BinSize;
                config.BBLength = configOnline.BBLength;
                config.BBMultiplierUpper = configOnline.BBMultiplierUpper;
                config.BBMultiplierLower = configOnline.BBMultiplierLower;
                config.RSILength = configOnline.RSILength;
                config.RSIUpper = configOnline.RSIUpper;
                config.RSILower = configOnline.RSILower;
                config.QtyRatio = configOnline.QtyRatio;
                config.UpperQtyX = configOnline.UpperQtyX;
                config.LowerQtyX = configOnline.LowerQtyX;
                config.RaiseQtyX = configOnline.RaiseQtyX;
                config.StopQtyX = configOnline.StopQtyX;
                config.MaxQtyX = configOnline.MaxQtyX;
                config.MinOrderDistance = configOnline.MinOrderDistance;
                config.MinUpperHeight = configOnline.MinUpperHeight;
                config.MinLowerHeight = configOnline.MinLowerHeight;
                config.MinUpperCloseHeight = configOnline.MinUpperCloseHeight;
                config.MinLowerCloseHeight = configOnline.MinLowerCloseHeight;
                Logger.WriteLine("buy_or_sell = " + config.BuyOrSell);
                Logger.WriteLine("bin_size = " + config.BinSize);
                Logger.WriteLine("bb_length = " + config.BBLength);
                Logger.WriteLine("bb_multiplier_upper = " + config.BBMultiplierUpper);
                Logger.WriteLine("bb_multiplier_lower = " + config.BBMultiplierLower);
                Logger.WriteLine("rsi_length = " + config.RSILength);
                Logger.WriteLine("rsi_upper = " + config.RSIUpper);
                Logger.WriteLine("rsi_lower = " + config.RSILower);
                Logger.WriteLine("qty_ratio = " + config.QtyRatio);
                Logger.WriteLine("sell_qty_x = " + config.UpperQtyX);
                Logger.WriteLine("buy_qty_x = " + config.LowerQtyX);
                Logger.WriteLine("raise_qty_x = " + config.RaiseQtyX);
                Logger.WriteLine("stop_qty_x = " + config.StopQtyX);
                Logger.WriteLine("max_qty_x = " + config.MaxQtyX);
                Logger.WriteLine("min_order_distance = " + config.MinOrderDistance);
                Logger.WriteLine("min_sell_order_height = " + config.MinUpperHeight);
                Logger.WriteLine("min_buy_order_height = " + config.MinLowerHeight);
                Logger.WriteLine("min_sell_close_height = " + config.MinUpperCloseHeight);
                Logger.WriteLine("min_buy_close_height = " + config.MinLowerCloseHeight);
                Logger.WriteLine();
                LastJsonOnline = configJsonOnline;
                LastConfig = config;
            }
            else
            {
                updated = false;
            }
            return LastConfig;
        }

        //public static readonly byte[] KEY_BYTES_1 = { 111, 102, 199, 18 };
        //public static readonly byte[] KEY_BYTES_2 = { 150, 225, 9, 29 };

        //private static string GenerateActivationCode(string apiKey, string expireDate)
        //{
        //    byte[] bytes = Encoding.UTF8.GetBytes(apiKey + "\t" + expireDate);
        //    bytes = StringUtils.XorBytes(bytes, KEY_BYTES_1);
        //    using (SHA256 sha256 = SHA256Managed.Create())
        //    {
        //        bytes = sha256.ComputeHash(bytes);
        //    }
        //    bytes = StringUtils.XorBytes(bytes, KEY_BYTES_2);
        //    using (MD5 md5 = MD5CryptoServiceProvider.Create())
        //    {
        //        bytes = md5.ComputeHash(bytes);
        //    }
        //    return StringUtils.ToHexString(bytes);
        //}

        //public static bool CheckActivationCode(string apiKey, string expireDate, string activationCode)
        //{
        //    if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(expireDate) || string.IsNullOrEmpty(activationCode)) return false;
        //    string checkCode = GenerateActivationCode(apiKey, expireDate);
        //    return activationCode == checkCode;
        //}

        [JsonIgnore]
        public const string APP_NAME = "ValloonBot";
        [JsonIgnore]
        public const string APP_VERSION = "2020.06.25";
        [JsonIgnore]
        public static string APP_HASH { get; set; }
        [JsonIgnore]
        public static string Warning { get; set; }
        [JsonIgnore]
        public static string Alert { get; set; }
        [JsonIgnore]
        public static string UpdateURL { get; set; }

        [JsonIgnore]
        public static int AdminConnectionInterval { get; set; }
        [JsonIgnore]
        public static DateTime LastAdminConnect { get; set; }

        static Config()
        {
            AdminConnectionInterval = 60;
        }
    }
}