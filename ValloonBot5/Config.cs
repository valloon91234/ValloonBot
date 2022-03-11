using Newtonsoft.Json;
using System;
using System.IO;

/**
 * @author Valloon Present
 * @version 2022-02-10
 */
namespace Valloon.Trading
{
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

        [JsonProperty("leverage")]
        public decimal Leverage { get; set; } = 1;

        [JsonProperty("qty")]
        public int Qty { get; set; } = 100;

        [JsonProperty("limit_height")]
        public int LimitHeight { get; set; } = 100;

        [JsonProperty("close_height")]
        public int CloseHeight { get; set; } = 100;

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
        public const string APP_VERSION = "2022.03.06";

    }
}