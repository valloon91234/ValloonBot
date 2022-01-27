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

        [DataMember(Name = "stairs_direction")]
        public int StairsDirection { get; set; }

        [DataMember(Name = "stairs_reset_distance")]
        public int StairsResetDistance { get; set; }

        [DataMember(Name = "buy_height_distance")]
        public int BuyHeightDistance { get; set; }

        [DataMember(Name = "sell_height_distance")]
        public int SellHeightDistance { get; set; }

        [DataMember(Name = "first_qty_ratio")]
        public decimal FirstQtyRatio { get; set; }

        [DataMember(Name = "exit")]
        public int Exit { get; set; }

        [JsonIgnore]
        public bool Active { get; set; }

        [JsonIgnore]
        public DateTime ExpireDateTime { get; set; }

        public static Config Load(out bool updated)
        {
            String configJson = File.ReadAllText(FILENAME);
            if (LastConfig == null || configJson != LastJson)
            {
                updated = true;
                Logger.WriteLine();
                Logger.WriteLine("Loading config ...", ConsoleColor.Green);
                Config config = JsonConvert.DeserializeObject<Config>(configJson);
                config.ExpireDate = "2099-12-31";
                Logger.WriteLine("username = " + config.Username);
                Logger.WriteLine("api_key = " + config.ApiKey);
                Logger.WriteLine("expire_date = " + config.ExpireDate);
                Logger.WriteLine("activation_code = " + config.ActivationCode);
                Logger.WriteLine("testnet_mode = " + config.TestnetMode.ToString().ToLower());
                Logger.WriteLine("connection_interval = " + config.ConnectionInverval);
                Logger.WriteLine("stairs_direction = " + config.StairsDirection);
                Logger.WriteLine("stairs_reset_distance = " + config.StairsResetDistance);
                Logger.WriteLine("buy_height_distance = " + config.BuyHeightDistance);
                Logger.WriteLine("sell_height_distance = " + config.SellHeightDistance);
                Logger.WriteLine("first_qty_ratio = " + config.FirstQtyRatio);
                Logger.WriteLine("exit = " + config.Exit);
                Logger.WriteLine();
                if (config.Username == null) config.Username = config.ApiKey;
                if (config.ApiKey == null) throw new Exception($"Error in config : api_key is empty."); ;
                if (config.ApiSecret == null) throw new Exception($"Error in config : api_secret is empty.");
                //config.Activated = CheckActivationCode(config.ApiKey, config.ExpireDate, config.ActivationCode);
                config.Active = true;
                config.ExpireDateTime = DateTime.ParseExact(config.ExpireDate, DATE_FORMAT, CultureInfo.InvariantCulture);
                LastJson = configJson;
                LastConfig = config;
            }
            else
            {
                updated = false;
            }
            return LastConfig;
        }

        public static readonly byte[] KEY_BYTES_1 = { 111, 102, 199, 18 };
        public static readonly byte[] KEY_BYTES_2 = { 150, 225, 9, 29 };

        private static string GenerateActivationCode(string apiKey, string expireDate)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(apiKey + "\t" + expireDate);
            bytes = StringUtils.XorBytes(bytes, KEY_BYTES_1);
            using (SHA256 sha256 = SHA256Managed.Create())
            {
                bytes = sha256.ComputeHash(bytes);
            }
            bytes = StringUtils.XorBytes(bytes, KEY_BYTES_2);
            using (MD5 md5 = MD5CryptoServiceProvider.Create())
            {
                bytes = md5.ComputeHash(bytes);
            }
            return StringUtils.ToHexString(bytes);
        }

        public static bool CheckActivationCode(string apiKey, string expireDate, string activationCode)
        {
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(expireDate) || string.IsNullOrEmpty(activationCode)) return false;
            string checkCode = GenerateActivationCode(apiKey, expireDate);
            return activationCode == checkCode;
        }

        [JsonIgnore]
        public const string APP_NAME = "ValloonBot";
        [JsonIgnore]
        public const string APP_VERSION = "2020.05.27";
        [JsonIgnore]
        public static string APP_HASH { get; set; }
        [JsonIgnore]
        public static string Warning { get; set; }
        [JsonIgnore]
        public static string Alert { get; set; }

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