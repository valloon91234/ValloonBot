using IO.Swagger.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Valloon.Trading.Utils;
using Valloon.Utils;

/**
 * @author Valloon Project
 * @version 1.0 @2020-03-03
 */
namespace Valloon.Trading
{
    static class BackendClient
    {
        //private static readonly string URL = "http://192.168.104.51:8002/api/ping";
        private static readonly string URL = "https://adm.traderxx.com/api/ping";
        private static readonly string KEY = "dFFdgWBUp8G8dhcX2cEUGBFWx2WRnaHPKZHogwptbyc=";
        private static readonly AES256CBC AES = new AES256CBC(Convert.FromBase64String(KEY));

        static BackendClient()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        private static string Post(string url, string data)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.Timeout = 10000;
            httpWebRequest.ReadWriteTimeout = 10000;
            httpWebRequest.Method = "POST";
            httpWebRequest.ContentType = "application/x-www-form-urlencoded";
            if (data != null)
            {
                httpWebRequest.ContentLength = data.Length;
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(data);
                }
            }
            using (var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
            {
                string Charset = httpWebResponse.CharacterSet;
                using (var receiveStream = httpWebResponse.GetResponseStream())
                using (var streamReader = new StreamReader(receiveStream, Encoding.GetEncoding(Charset)))
                    return streamReader.ReadToEnd();
            }
        }

        private static int RequestIndex = 0;

        public static Config Ping(Config config = null, Wallet wallet = null, List<Transaction> walletHistory = null)
        {
            try
            {
                DateTime startTime = DateTime.Now;
                var json = new JObject
                {
                    { "email", Config.Email},
                    { "license", Config.License },
                    { "request_index", RequestIndex++ },
                    { "app_name", Config.APP_NAME },
                    { "app_version", Config.APP_VERSION },
                    { "app_hash", Config.APP_HASH },
                };
                if (wallet != null)
                {
                    json["wallet"] = JObject.FromObject(wallet);
                }
                if (walletHistory != null)
                {
                    json["wallet_history"] = JArray.FromObject(walletHistory);
                }
                string jsonString = json.ToString(Newtonsoft.Json.Formatting.None);
                string responseString = Post(URL, "q=" + AES.Encrypt(jsonString));
                string responseJsonString = AES.Decrypt(responseString);
                config = Config.Load(responseJsonString, out _);
                DateTime endTime = DateTime.Now;
                Logger.WriteLine($"--- Backend connected : {DateTime.Now:yyyy-MM-dd  HH:mm:ss} / {(endTime - startTime).TotalMilliseconds:N0} seconds.");
                return config;
            }
            catch (Exception ex)
            {
                Logger.WriteLine("<Server connection error>  " + ex.Message, ConsoleColor.Red, false);
                Config.Active = false;
                Config.Message = "Server connection error.\r\n" + ex.Message;
                return null;
            }
        }

        public static void CheckPing(ref Config config, Wallet wallet = null, List<Transaction> walletHistory = null)
        {
            if (config != null && Config.LastBackendConnect != null && (DateTime.UtcNow - Config.LastBackendConnect.Value).TotalSeconds < Config.BackendConnectionInterval) return;
            config = Ping(config, wallet, walletHistory);
            Config.LastBackendConnect = DateTime.UtcNow;
        }

    }
}
