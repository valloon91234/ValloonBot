using Newtonsoft.Json;
using System;
using System.IO;

/**
 * @author Valloon Present
 * @version 2022-02-10
 */
namespace Valloon.Trading
{
    public class ShotConfig
    {
        [JsonIgnore]
        //[JsonProperty("rsi_length")]
        public int RSILength { get; set; } = 14;

        [JsonProperty("buy_or_sell")]
        public int BuyOrSell { get; set; }

        [JsonProperty("upper_limit")]
        public decimal UpperLimit { get; set; }

        [JsonProperty("upper_min_diff")]
        public decimal UpperMinDiff { get; set; }

        [JsonProperty("upper_max_diff")]
        public decimal UpperMaxDiff { get; set; }

        [JsonProperty("upper_close")]
        public decimal UpperClose { get; set; }

        [JsonProperty("upper_stop")]
        public decimal UpperStop { get; set; }

        [JsonProperty("upper_stop_2")]
        public decimal UpperStop2 { get; set; }

        [JsonProperty("lower_limit")]
        public decimal LowerLimit { get; set; }

        [JsonProperty("lower_min_diff")]
        public decimal LowerMinDiff { get; set; }

        [JsonProperty("lower_max_diff")]
        public decimal LowerMaxDiff { get; set; }

        [JsonProperty("lower_close")]
        public decimal LowerClose { get; set; }

        [JsonProperty("lower_stop")]
        public decimal LowerStop { get; set; }

        [JsonProperty("lower_stop_2")]
        public decimal LowerStop2 { get; set; }

    }

    public class Config
    {
        private static readonly string FILENAME = "config.json";
        private static string LastJson = null;
        private static Config LastConfig = null;

        //[JsonProperty("username", EmitDefaultValue = false)]
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("api_key")]
        public string ApiKey { get; set; }

        [JsonProperty("api_secret")]
        public string ApiSecret { get; set; }

        [JsonProperty("testnet_mode")]
        public bool TestnetMode { get; set; }

        [JsonProperty("upper_qty")]
        public decimal UpperQtyX { get; set; } = 1;

        [JsonProperty("lower_qty")]
        public decimal LowerQtyX { get; set; } = 1;

        [JsonProperty("shot")]
        public ShotConfig Shot { get; set; }

        [JsonProperty("exit")]
        public int Exit { get; set; }

        public static Config Load(out bool updated, bool forceUpdate = false)
        {
            string configJson = File.ReadAllText(FILENAME);
            if (LastConfig == null || configJson != LastJson || forceUpdate)
            {
                updated = true;
                Config config = JsonConvert.DeserializeObject<Config>(configJson);
                if (config.Username == null) config.Username = config.ApiKey;
                if (config.ApiKey == null) throw new Exception($"Error in config : api_key is empty."); ;
                if (config.ApiSecret == null) throw new Exception($"Error in config : api_secret is empty.");
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
        public const string APP_VERSION = "2022.02.10";

    }
}