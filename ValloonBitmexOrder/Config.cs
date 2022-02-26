using Newtonsoft.Json;
using System;
using System.IO;
using System.Runtime.Serialization;

/**
 * @author Valloon Project
 * @version 1.0 @2020-05-21
 */
namespace Valloon.BitMEX
{
    [DataContract]
    public class Config
    {
        public static readonly string FILENAME = "config.json";

        //[DataMember(Name = "username", EmitDefaultValue = false)]
        [DataMember(Name = "api_key")]
        public string ApiKey { get; set; }

        [DataMember(Name = "api_secret")]
        public string ApiSecret { get; set; }

        [DataMember(Name = "testnet_mode")]
        public bool TestnetMode { get; set; }

        [DataMember(Name = "qty")]
        public int Qty { get; set; }

        [DataMember(Name = "limit_profit")]
        public int LimitProfit { get; set; }

        [DataMember(Name = "stop_market")]
        public int StopMarket { get; set; }

        public static Config Load()
        {
            String configJson = File.ReadAllText(FILENAME);
            Logger.WriteLine("Loading config ...", ConsoleColor.Green);
            Config config = JsonConvert.DeserializeObject<Config>(configJson);
            Logger.WriteLine("api_key = " + config.ApiKey);
            Logger.WriteLine("testnet_mode = " + config.TestnetMode.ToString().ToLower());
            Logger.WriteLine("qty = " + config.Qty);
            Logger.WriteLine("limit_profit = " + config.LimitProfit);
            Logger.WriteLine("stop_market = " + config.StopMarket);
            Logger.WriteLine();
            if (config.ApiKey == null) throw new Exception($"Error in config : api_key is empty."); ;
            return config;
        }

    }
}