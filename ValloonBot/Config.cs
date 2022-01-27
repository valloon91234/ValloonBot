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
 * @version 1.0 @2020-04-08
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

        [DataMember(Name = "limit_lower")]
        public int LimitLower { get; set; }

        [DataMember(Name = "limit_lower_cancel")]
        public int LimitLowerCancel { get; set; }

        [DataMember(Name = "limit_higher")]
        public int LimitHigher { get; set; }

        [DataMember(Name = "limit_higher_cancel")]
        public int LimitHigherCancel { get; set; }

        [DataMember(Name = "limit_mark")]
        public int LimitMark { get; set; }

        [DataMember(Name = "limit_mark_cancel")]
        public int LimitMarkCancel { get; set; }

        [DataMember(Name = "profit_target")]
        public int ProfitTarget { get; set; }

        [DataMember(Name = "stairs_count")]
        public int StairsCount { get; set; }

        [DataMember(Name = "stairs_direction")]
        public int StairsDirection { get; set; }

        [DataMember(Name = "stairs_reset_distance")]
        public int StairsResetDistance { get; set; }

        [DataMember(Name = "buy_height_distance")]
        public int BuyHeightDistance { get; set; }

        [DataMember(Name = "sell_height_distance")]
        public int SellHeightDistance { get; set; }

        [DataMember(Name = "buy_height_rate")]
        public decimal BuyHeightRate { get; set; }

        [DataMember(Name = "sell_height_rate")]
        public decimal SellHeightRate { get; set; }

        [DataMember(Name = "stairs_height")]
        public int[] StairsHeight { get; set; }

        [DataMember(Name = "stairs_invest")]
        public decimal[] StairsInvest { get; set; }

        [DataMember(Name = "invest_ratio")]
        public decimal InvestRatio { get; set; }

        [DataMember(Name = "stop_loss")]
        public decimal StopLoss { get; set; }

        [DataMember(Name = "exit")]
        public int Exit { get; set; }

        [JsonIgnore]
        public bool Activated { get; set; }

        [JsonIgnore]
        public DateTime ExpireDateTime { get; set; }

        public static Config Load(out bool updated)
        {
            String configJson = File.ReadAllText(FILENAME);
            if (LastConfig == null || configJson != LastJson)
            {
                updated = true;
                Logger.WriteLine("Loading config ...", ConsoleColor.Green);
                Config config = JsonConvert.DeserializeObject<Config>(configJson);
                //config.ExpireDate = "2020-04-30";
                Logger.WriteLine("username = " + config.Username);
                Logger.WriteLine("api_key = " + config.ApiKey);
                Logger.WriteLine("expire_date = " + config.ExpireDate);
                Logger.WriteLine("activation_code = " + config.ActivationCode);
                Logger.WriteLine("testnet_mode = " + config.TestnetMode.ToString().ToLower());
                Logger.WriteLine("limit_lower = " + config.LimitLower);
                Logger.WriteLine("limit_lower_cancel = " + config.LimitLowerCancel);
                Logger.WriteLine("limit_higher = " + config.LimitHigher);
                Logger.WriteLine("limit_higher_cancel = " + config.LimitHigherCancel);
                Logger.WriteLine("limit_mark = " + config.LimitMark);
                Logger.WriteLine("limit_mark_cancel = " + config.LimitMarkCancel);
                Logger.WriteLine("profit_target = " + config.ProfitTarget);
                Logger.WriteLine("stairs_count = " + config.StairsCount);
                Logger.WriteLine("stairs_direction = " + config.StairsDirection);
                Logger.WriteLine("stairs_reset_distance = " + config.StairsResetDistance);
                Logger.WriteLine("buy_height_distance = " + config.BuyHeightDistance);
                Logger.WriteLine("sell_height_distance = " + config.SellHeightDistance);
                Logger.WriteLine("buy_height_rate = " + config.BuyHeightRate);
                Logger.WriteLine("sell_height_rate = " + config.SellHeightRate);
                Logger.WriteLine("stairs_height = " + string.Join(", ", config.StairsHeight));
                Logger.WriteLine("stairs_invest = " + string.Join(", ", config.StairsInvest));
                Logger.WriteLine("invest_ratio = " + config.InvestRatio);
                Logger.WriteLine("stop_loss = " + config.StopLoss);
                Logger.WriteLine("exit = " + config.Exit);
                Logger.WriteLine();
                if (config.Username == null) config.Username = config.ApiKey;
                if (config.ApiKey == null) throw new Exception($"Error in config : api_key is empty."); ;
                if (config.ApiSecret == null) throw new Exception($"Error in config : api_secret is empty.");
                if (config.BuyHeightRate <= 0) throw new Exception($"Error in config : buy_height_rate is wrong.");
                if (config.SellHeightRate <= 0) throw new Exception($"Error in config : sell_height_rate is wrong.");
                if (config.StairsHeight == null || config.StairsHeight.Length < config.StairsCount) throw new Exception($"Error in config : length of stairs_height < stairs_count");
                if (config.StairsInvest == null || config.StairsInvest.Length < config.StairsCount) throw new Exception($"Error in config : length of stairs_invest < stairs_count");
                config.Activated = CheckActivationCode(config.ApiKey, config.ExpireDate, config.ActivationCode);
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
    }
}