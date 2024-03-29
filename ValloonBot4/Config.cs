﻿using Newtonsoft.Json;
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
namespace Valloon.Trading
{
    public class DropConfig
    {
        [JsonProperty("connection_interval")]
        public int ConnectionInverval { get; set; }

        [JsonProperty("buy_or_sell")]
        public int BuyOrSell { get; set; }

        [JsonProperty("bin_size")]
        public string BinSize { get; set; }

        [JsonProperty("qty_ratio")]
        public decimal QtyRatio { get; set; }

        [JsonProperty("order_count")]
        public int OrderCount { get; set; }

        [DataMember(Name = "qty_stairs")]
        public decimal[] QtyStairs { get; set; }

        [JsonProperty("height_ratio")]
        public decimal HeightRatio { get; set; }

        [DataMember(Name = "height_stairs")]
        public decimal[] HeightStairs { get; set; }

        [JsonProperty("close_height_ratio")]
        public decimal CloseHeightRatio { get; set; }

    }

    public class BStepConfig
    {
        [JsonProperty("connection_interval")]
        public int ConnectionInverval { get; set; }

        [JsonProperty("buy_or_sell")]
        public int BuyOrSell { get; set; }

        [JsonProperty("bin_size")]
        public string BinSize { get; set; }

        [JsonProperty("bb_length")]
        public int BBLength { get; set; }

        [JsonProperty("bb_x")]
        public int BBX { get; set; }

        [JsonProperty("bb_upper_x")]
        public decimal BBUpperX { get; set; }

        [JsonProperty("bb_lower_x")]
        public decimal BBLowerX { get; set; }

        [JsonProperty("rsi_length")]
        public int RSILength { get; set; }

        [JsonProperty("rsi_upper")]
        public int RSIUpper { get; set; }

        [JsonProperty("rsi_lower")]
        public int RSILower { get; set; }

        [JsonProperty("qty_ratio")]
        public decimal QtyRatio { get; set; }

        [JsonProperty("upper_qty_x")]
        public decimal UpperQtyX { get; set; }

        [JsonProperty("lower_qty_x")]
        public decimal LowerQtyX { get; set; }

        [JsonProperty("min_band_ratio")]
        public decimal MinBandRatio { get; set; }

        [JsonProperty("close_height_ratio")]
        public decimal CloseHeightRatio { get; set; }

        [JsonProperty("force_close_position")]
        public int ForceClosePosition { get; set; }
    }

    public class ShovelConfig
    {
        [JsonProperty("sma")]
        public int SMALength { get; set; } = 660;

        [JsonProperty("upper_limit_1")]
        public decimal UpperLimitX1 { get; set; } = 0.0107m;

        [JsonProperty("upper_close_1")]
        public decimal UpperCloseX1 { get; set; } = 1.04m;

        [JsonProperty("upper_stop_1")]
        public decimal UpperStopX1 { get; set; } = 7.6m;

        [JsonProperty("lower_limit_1")]
        public decimal LowerLimitX1 { get; set; } = 0.009m;

        [JsonProperty("lower_close_1")]
        public decimal LowerCloseX1 { get; set; } = 0.95m;

        [JsonProperty("lower_stop_1")]
        public decimal LowerStopX1 { get; set; } = 6.2m;

        [JsonProperty("upper_limit_2")]
        public decimal UpperLimitX2 { get; set; } = 0.0202m;

        [JsonProperty("upper_close_2")]
        public decimal UpperCloseX2 { get; set; } = 1.50m;

        [JsonProperty("upper_stop_2")]
        public decimal UpperStopX2 { get; set; } = 6.5m;

        [JsonProperty("lower_limit_2")]
        public decimal LowerLimitX2 { get; set; } = 0.0246m;

        [JsonProperty("lower_close_2")]
        public decimal LowerCloseX2 { get; set; } = 1.01m;

        [JsonProperty("lower_stop_2")]
        public decimal LowerStopX2 { get; set; } = 2.8m;
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

        [JsonProperty("connection_interval")]
        public int ConnectionInverval { get; set; } = 15;

        [JsonProperty("upper_qty_1")]
        public decimal UpperQtyX1 { get; set; } = 1;

        [JsonProperty("lower_qty_1")]
        public decimal LowerQtyX1 { get; set; } = 1;

        [JsonProperty("upper_qty_2")]
        public decimal UpperQtyX2 { get; set; } = 0;

        [JsonProperty("lower_qty_2")]
        public decimal LowerQtyX2 { get; set; } = 0;

        [JsonProperty("strategy")]
        public string Strategy { get; set; }

        [JsonProperty("drop")]
        public DropConfig Drop { get; set; }

        [JsonProperty("bstep")]
        public BStepConfig BStep { get; set; }

        [JsonProperty("shovel")]
        public ShovelConfig shovel { get; set; }

        [JsonProperty("exit")]
        public int Exit { get; set; }

        public static Config Load(out bool updated, bool forceUpdate = false)
        {
            string configJson = File.ReadAllText(FILENAME);
            if (LastConfig == null || configJson != LastJson || forceUpdate)
            {
                updated = true;
                Logger.WriteLine();
                Logger.WriteLine("Loading config ...", ConsoleColor.Green);
                Config config = JsonConvert.DeserializeObject<Config>(configJson);
                Logger.WriteLine(configJson);
                Logger.WriteLine();
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
        public const string APP_VERSION = "2022.01.20";

    }
}