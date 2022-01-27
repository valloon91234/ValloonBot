using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using Valloon.Utils;

/**
 * @author Valloon Project
 * @version 1.0 @2020-05-08
 */
namespace Valloon.BitMEX
{
    [DataContract]
    public class Config
    {
        public static readonly string FILENAME = "config.json";
        public static readonly string DATE_FORMAT = "yyyy-MM-dd";
        private static string LastJson = null;
        private static Config LastConfig = null;

        //[DataMember(Name = "username", EmitDefaultValue = false)]
        [DataMember(Name = "username")]
        public string Username { get; set; }

        [DataMember(Name = "api_key")]
        public string ApiKey { get; set; }

        [DataMember(Name = "api_secret")]
        public string ApiSecret { get; set; }

        [DataMember(Name = "testnet_mode")]
        public bool TestnetMode { get; set; }

        [DataMember(Name = "strategy")]
        public string Strategy { get; set; }

        [DataMember(Name = "connection_interval")]
        public int ConnectionInverval { get; set; }

        [DataMember(Name = "volume_min")]
        public int VolumeMin { get; set; }

        [DataMember(Name = "volume_bin_size")]
        public string VolumeBinSize { get; set; }

        [DataMember(Name = "volume_count")]
        public int VolumeCount { get; set; }

        [DataMember(Name = "limit_profit")]
        public int LimitProfit { get; set; }

        [DataMember(Name = "take_profit")]
        public int TakeProfit { get; set; }

        [DataMember(Name = "order_count")]
        public int OrderCount { get; set; }

        [DataMember(Name = "order_distance")]
        public int OrderDistance { get; set; }

        [DataMember(Name = "qty_rate")]
        public decimal QtyRate { get; set; }

        [DataMember(Name = "stop_loss")]
        public int StopLoss { get; set; }

        [DataMember(Name = "force_stop_loss")]
        public bool ForceStopLoss { get; set; }

        [DataMember(Name = "buy_sell")]
        public int BuySell { get; set; }

        [DataMember(Name = "martingale")]
        public double Martingale { get; set; }

        [DataMember(Name = "inverse")]
        public bool InverseMode { get; set; }

        public static Config Load(out bool updated)
        {
            String configJson = File.ReadAllText(FILENAME);
            if (LastConfig == null || configJson != LastJson)
            {
                updated = true;
                //Logger.WriteLine("Loading config ...", ConsoleColor.Green);
                Config config = JsonConvert.DeserializeObject<Config>(configJson);
                if (config.Martingale == 0) config.Martingale = 1;
                //Logger.WriteLine("username = " + config.Username);
                //Logger.WriteLine("api_key = " + config.ApiKey);
                //Logger.WriteLine("testnet_mode = " + config.TestnetMode.ToString().ToLower());
                //Logger.WriteLine("strategy = " + config.Strategy);
                //Logger.WriteLine("connection_interval = " + config.ConnectionInverval);
                //Logger.WriteLine("volume_min = " + config.VolumeMin);
                //Logger.WriteLine("volume_bin_size = " + config.VolumeBinSize);
                //Logger.WriteLine("volume_count = " + config.VolumeCount);
                //Logger.WriteLine("limit_profit = " + config.LimitProfit);
                //Logger.WriteLine("take_profit = " + config.TakeProfit);
                //Logger.WriteLine("order_count = " + config.OrderCount);
                //Logger.WriteLine("order_distance = " + config.OrderDistance);
                //Logger.WriteLine("qty_rate = " + config.QtyRate);
                //Logger.WriteLine("stop_loss = " + config.StopLoss);
                //Logger.WriteLine("force_stop_loss = " + config.ForceStopLoss);
                //Logger.WriteLine("martingale = " + config.Martingale);
                //Logger.WriteLine("inverse_mode = " + config.InverseMode);
                //Logger.WriteLine();
                if (config.Username == null) config.Username = config.ApiKey;
                if (config.ApiKey == null) throw new Exception($"Error in config : api_key is empty.");
                if (config.LimitProfit > 0 && config.TakeProfit > config.LimitProfit) Logger.WriteLine("Warning : take_profit is bigger than limit_profit.");
                LastJson = configJson;
                LastConfig = config;
            }
            else
            {
                updated = false;
            }
            return LastConfig;
        }

        public static Config Load(string jsonText, out bool updated)
        {
            JObject jObject = JObject.Parse(jsonText);
            Config.Active = (bool)jObject["success"];
            Config.Message = (string)jObject["message"];
            if (LastConfig == null || jsonText != LastJson)
            {
                updated = true;
                //Logger.WriteLine("Loading config ...", ConsoleColor.Green);
                Config.BackendConnectionInterval = (int)jObject["backend_connection_interval"];
                Config.ExpireDate = (string)jObject["expire_date"];
                Config.RemainingDays = (int)(jObject["remaining_days"] ?? 0);
                var parameter = jObject["parameter"];
                Config config = parameter == null ?
                    new Config
                    {
                        ApiKey = (string)jObject["api"],
                        ApiSecret = (string)jObject["secret_key"],
                    } :
                    new Config
                    {
                        ApiKey = (string)jObject["api"],
                        ApiSecret = (string)jObject["secret_key"],
                        Strategy = (string)parameter["name"],
                        ConnectionInverval = (int)(parameter["connection_interval"] ?? 0),
                        VolumeMin = (int)(parameter["vol_min"] ?? 0),
                        VolumeBinSize = (string)parameter["vol_bin_size"],
                        VolumeCount = (int)(parameter["vol_count"] ?? 0),
                        LimitProfit = (int)(parameter["limit_profit"] ?? 0),
                        OrderCount = (int)(parameter["order_count"] ?? 0),
                        OrderDistance = (int)(parameter["order_distance"] ?? 0),
                        QtyRate = (decimal)(parameter["qty_rate"] ?? 0),
                        StopLoss = (int)(parameter["stop_loss"] ?? 0),
                        ForceStopLoss = (int)(parameter["force_stop"] ?? 1) == 1 ? true : false,
                        BuySell = (int)(parameter["buy_sell"] ?? 0),
                        Martingale = (double)(parameter["martingale"] ?? 1),
                        InverseMode = (int)(parameter["inverse"] ?? 0) == 0 ? false : true
                    };
                if (Config.Email.EndsWith("@testnet.com")) config.TestnetMode = true;
                else config.TestnetMode = false;
                if (config.Martingale == 0) config.Martingale = 1;
                //Logger.WriteLine("username = " + config.Username);
                //Logger.WriteLine("email = " + Config.Email);
                //Logger.WriteLine("api_key = " + config.ApiKey);
                //Logger.WriteLine("testnet_mode = " + config.TestnetMode.ToString().ToLower());
                //Logger.WriteLine("strategy = " + config.Strategy);
                //Logger.WriteLine("connection_interval = " + config.ConnectionInverval);
                //Logger.WriteLine("volume_min = " + config.VolumeMin);
                //Logger.WriteLine("volume_bin_size = " + config.VolumeBinSize);
                //Logger.WriteLine("volume_count = " + config.VolumeCount);
                //Logger.WriteLine("limit_profit = " + config.LimitProfit);
                //Logger.WriteLine("take_profit = " + config.TakeProfit);
                //Logger.WriteLine("order_count = " + config.OrderCount);
                //Logger.WriteLine("order_distance = " + config.OrderDistance);
                //Logger.WriteLine("qty_rate = " + config.QtyRate);
                //Logger.WriteLine("stop_loss = " + config.StopLoss);
                //Logger.WriteLine("force_stop_loss = " + config.ForceStopLoss);
                //Logger.WriteLine("martingale = " + config.Martingale);
                //Logger.WriteLine("inverse_mode = " + config.InverseMode);
                //Logger.WriteLine();
                if (config.Username == null) config.Username = config.ApiKey;
                //if (config.ApiKey == null) throw new Exception($"Error in config : api_key is empty.");
                if (config.LimitProfit > 0 && config.TakeProfit > config.LimitProfit) Logger.WriteLine("Warning : take_profit is bigger than limit_profit.");
                LastJson = jsonText;
                LastConfig = config;
            }
            else
            {
                updated = false;
            }
            return LastConfig;
        }

        [JsonIgnore]
        public static string Email { get; set; }
        [JsonIgnore]
        public static string License { get; set; }
        [JsonIgnore]
        public const string APP_NAME = "ValloonBitmexBot";
        [JsonIgnore]
        public const string APP_VERSION = "2020.05.08";
        [JsonIgnore]
        public static string APP_HASH { get; set; }
        [JsonIgnore]
        public static bool Active { get; set; }
        [JsonIgnore]
        public static string Message { get; set; }
        [JsonIgnore]
        public static string ExpireDate { get; set; }
        [JsonIgnore]
        public static int RemainingDays { get; set; }

        [JsonIgnore]
        public static int BackendConnectionInterval { get; set; }
        [JsonIgnore]
        public static DateTime? LastBackendConnect { get; set; }

        static Config()
        {
        }

    }
}