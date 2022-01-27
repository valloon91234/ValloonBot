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
    public class BollingerBandConfig
    {
        public static readonly string FILENAME = "config.json";
        public static readonly string DATE_FORMAT = "yyyy-MM-dd";
        private static string LastJson = null;
        private static BollingerBandConfig LastConfig = null;

        //[DataMember(Name = "username", EmitDefaultValue = false)]
        [DataMember(Name = "username")]
        public string Username { get; set; }

        [DataMember(Name = "api_key")]
        public string ApiKey { get; set; }

        [DataMember(Name = "api_secret")]
        public string ApiSecret { get; set; }

        [DataMember(Name = "testnet_mode")]
        public bool TestnetMode { get; set; }

        [DataMember(Name = "connection_interval")]
        public int ConnectionInverval { get; set; }

        [DataMember(Name = "buy_or_sell")]
        public int BuyOrSell { get; set; }

        [DataMember(Name = "bin_size")]
        public string BinSize { get; set; }

        [DataMember(Name = "bb_length_1")]
        public int BBLength1 { get; set; }

        [DataMember(Name = "bb_upper_x_1")]
        public double BBUpperX1 { get; set; }

        [DataMember(Name = "bb_lower_x_1")]
        public double BBLowerX1 { get; set; }

        [DataMember(Name = "bb_length_2")]
        public int BBLength2 { get; set; }

        [DataMember(Name = "bb_upper_x_2")]
        public double BBUpperX2 { get; set; }

        [DataMember(Name = "bb_lower_x_2")]
        public double BBLowerX2 { get; set; }

        [DataMember(Name = "bb_length_3")]
        public int BBLength3 { get; set; }

        [DataMember(Name = "bb_upper_x_3")]
        public double BBUpperX3 { get; set; }

        [DataMember(Name = "bb_lower_x_3")]
        public double BBLowerX3 { get; set; }

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

        [DataMember(Name = "min_order_distance_ratio")]
        public decimal MinOrderDistanceRatio { get; set; }

        [DataMember(Name = "min_upper_height_ratio")]
        public decimal MinUpperHeightRatio { get; set; }

        [DataMember(Name = "min_lower_height_ratio")]
        public decimal MinLowerHeightRatio { get; set; }

        [DataMember(Name = "min_upper_close_height_ratio")]
        public decimal MinUpperCloseHeightRatio { get; set; }

        [DataMember(Name = "min_lower_close_height_ratio")]
        public decimal MinLowerCloseHeightRatio { get; set; }

        [DataMember(Name = "exponential")]
        public bool Exponential { get; set; }

        [DataMember(Name = "exit")]
        public int Exit { get; set; }

        [JsonIgnore]
        public bool Active { get; set; }

        [JsonIgnore]
        public DateTime ExpireDateTime { get; set; }

        public static BollingerBandConfig Load(out bool updated, bool forceUpdate = false)
        {
            string configJson = File.ReadAllText(FILENAME);
            if (LastConfig == null || configJson != LastJson || forceUpdate)
            {
                updated = true;
                Logger.WriteLine();
                Logger.WriteLine("Loading config ...", ConsoleColor.Green);
                BollingerBandConfig config = JsonConvert.DeserializeObject<BollingerBandConfig>(configJson);
                Logger.WriteLine("username = " + config.Username);
                Logger.WriteLine("api_key = " + config.ApiKey);
                Logger.WriteLine("testnet_mode = " + config.TestnetMode.ToString().ToLower());
                Logger.WriteLine("connection_interval = " + config.ConnectionInverval);
                Logger.WriteLine("buy_or_sell = " + config.BuyOrSell);
                Logger.WriteLine("bin_size = " + config.BinSize);
                Logger.WriteLine("bb_length_1 = " + config.BBLength1);
                Logger.WriteLine("bb_multiplier_upper_1 = " + config.BBUpperX1);
                Logger.WriteLine("bb_multiplier_lower_1 = " + config.BBLowerX1);
                Logger.WriteLine("bb_length_2 = " + config.BBLength2);
                Logger.WriteLine("bb_multiplier_upper_2 = " + config.BBUpperX2);
                Logger.WriteLine("bb_multiplier_lower_2 = " + config.BBLowerX2);
                Logger.WriteLine("bb_length_3 = " + config.BBLength3);
                Logger.WriteLine("bb_multiplier_upper_3 = " + config.BBUpperX3);
                Logger.WriteLine("bb_multiplier_lower_3 = " + config.BBLowerX3);
                Logger.WriteLine("rsi_length = " + config.RSILength);
                Logger.WriteLine("rsi_upper = " + config.RSIUpper);
                Logger.WriteLine("rsi_lower = " + config.RSILower);
                Logger.WriteLine("qty_ratio = " + config.QtyRatio);
                Logger.WriteLine("sell_qty_x = " + config.UpperQtyX);
                Logger.WriteLine("buy_qty_x = " + config.LowerQtyX);
                Logger.WriteLine("plus_qty_x = " + config.RaiseQtyX);
                Logger.WriteLine("stop_qty_x = " + config.StopQtyX);
                Logger.WriteLine("max_qty_x = " + config.MaxQtyX);
                Logger.WriteLine("min_order_distance_ratop = " + config.MinOrderDistanceRatio);
                Logger.WriteLine("min_sell_order_height_ratio = " + config.MinUpperHeightRatio);
                Logger.WriteLine("min_buy_order_height_ratio = " + config.MinLowerHeightRatio);
                Logger.WriteLine("min_sell_close_height_ratio = " + config.MinUpperCloseHeightRatio);
                Logger.WriteLine("min_buy_close_height_ratio = " + config.MinLowerCloseHeightRatio);
                Logger.WriteLine("exponential = " + config.Exponential);
                Logger.WriteLine("exit = " + config.Exit);
                Logger.WriteLine();
                if (config.Username == null) config.Username = config.ApiKey;
                if (config.ApiKey == null) throw new Exception($"Error in config : api_key is empty."); ;
                if (config.ApiSecret == null) throw new Exception($"Error in config : api_secret is empty.");
                //config.Activated = CheckActivationCode(config.ApiKey, config.ExpireDate, config.ActivationCode);
                config.Active = true;
                LastJson = configJson;
                LastConfig = config;
            }
            else
            {
                updated = false;
            }
            return LastConfig;
        }

        [JsonIgnore]
        public const string APP_NAME = "ValloonBot";
        [JsonIgnore]
        public const string APP_VERSION = "2020.06.25";
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

        static BollingerBandConfig()
        {
            AdminConnectionInterval = 60;
        }
    }
}