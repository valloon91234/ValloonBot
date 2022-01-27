using IO.Swagger.Model;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Valloon.Utils;

/**
 * @author Valloon Project
 * @version 1.0 @2020-03-03
 */
namespace Valloon.BitMEX
{
    class ValloonClient : IDisposable
    {
        private static readonly string VALLOON_URL = "http://valloonbitmex.com/bitmex/api/";
        //private static readonly string VALLOON_URL = "http://localhost:8080/bitmex/api/";
        private static readonly string KEY_REQUEST = "E78199B0FAF77003383113F54DE67ADDEA44B073529A5B6E";
        private static readonly string KEY_RESPONSE = "6452443EFFC9E9894506A8A331F492FD95C29C2383AB9D6B";

        private readonly TripleDES Encryptor;
        private readonly TripleDES Decryptor;

        public ValloonClient()
        {
            Encryptor = new TripleDES(StringUtils.ParseHexString(KEY_REQUEST), true, false);
            Decryptor = new TripleDES(StringUtils.ParseHexString(KEY_RESPONSE), false, true);
        }

        public void Dispose()
        {
            Encryptor.Dispose();
            Decryptor.Dispose();
        }

        private static byte[] Post(string url, byte[] bytes)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = 5000;
            request.ReadWriteTimeout = 5000;
            request.Method = "POST";
            request.ContentType = "application/octet-stream";
            var requestWriter = request.GetRequestStream();
            requestWriter.Write(bytes, 0, bytes.Length);
            using (var response = (HttpWebResponse)request.GetResponse())
            {
                using (var ms = new MemoryStream())
                {
                    response.GetResponseStream().CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }

        private string Post(string url, string text)
        {
            byte[] requestBytes = Encryptor.Encrypt(Encoding.UTF8.GetBytes(text));
            byte[] responseBytes = Post(url, requestBytes);
            return Encoding.UTF8.GetString(Decryptor.Decrypt(responseBytes));
        }

        private int RequestIndex = 0;

        private string GenerateRequestJsonstring(Config config, decimal lastPrice, decimal markPrice, int volume, int activeOrdersCount, Position position, Margin margin)
        {
            var json = new JObject
            {
                { "request_index", RequestIndex++ },
                { "app_name", GlobalParam.APP_NAME },
                { "app_version", GlobalParam.APP_VERSION },
                { "app_hash", GlobalParam.APP_HASH },
                { "username", config.Username},
                { "api_key", config.ApiKey },
                { "api_secret", config.ApiSecret},
                { "expire_date", config.ExpireDateTime.ToString(Config.DATE_FORMAT) },
                { "activation_code", config.ActivationCode },
                { "last_price", lastPrice },
                { "mark_price", markPrice },
                { "volume", volume },
                { "active_orders_count", activeOrdersCount }
            };
            if (position != null && position.CurrentQty != 0)
            {
                var jsonActivePosition = new JObject
                {
                    { "qty", position.CurrentQty },
                    { "price", position.AvgEntryPrice },
                    { "liquidation", position.LiquidationPrice },
                    { "leverage", position.Leverage }
                };
                json.Add("active_position", jsonActivePosition);
            }
            if (margin != null)
            {
                var jsonBalance = new JObject
                {
                    { "wallet", margin.WalletBalance },
                    { "margin", margin.MarginBalance },
                    { "available", margin.AvailableMargin }
                };
                json.Add("balance", jsonBalance);
            }
            return json.ToString(Newtonsoft.Json.Formatting.None);
        }

        private bool ParseResponseJsonstring(string responseJsonstring)
        {
            JObject responseJson = JObject.Parse(responseJsonstring);
            bool success = (bool)responseJson["success"];
            GlobalParam.Paused = (bool)responseJson["paused"];
            GlobalParam.Message = (string)responseJson["message"];
            GlobalParam.ConnectionInterval = (int)responseJson["params"]["admin_connection_interval"];
            return success;
        }

        public void Ping(Config config, decimal lastPrice, decimal markPrice, int volume, int activeOrdersCount, Position position, Margin margin)
        {
            try
            {
                string requestJsonstring = GenerateRequestJsonstring(config, lastPrice, markPrice, volume, activeOrdersCount, position, margin);
                string responseJsonstring = Post(VALLOON_URL + "ping", requestJsonstring);
                LastPingResult = responseJsonstring;
            }
            catch (Exception ex)
            {
                Logger.WriteFile("error in ping : " + ex.Message);
            }
        }

        private string LastPingResult = null;

        public void CheckPing(Config config, decimal lastPrice, decimal markPrice, int volume, int activeOrdersCount, Position position, Margin margin)
        {
            if ((DateTime.UtcNow - GlobalParam.LastAdminConnect).TotalSeconds < GlobalParam.ConnectionInterval) return;
            Thread thread = new Thread(() => Ping(config, lastPrice, markPrice, volume, activeOrdersCount, position, margin));
            thread.Start();
        }

        public void ParsePingResult()
        {
            if (LastPingResult != null)
            {
                try
                {
                    if (ParseResponseJsonstring(LastPingResult))
                    {
                        GlobalParam.LastAdminConnect = DateTime.UtcNow;
                        LastPingResult = null;
                        return;
                    }
                }
                catch// (Exception ex)
                {
                    //Logger.WriteFile("error in parsing ping response : " + ex.Message);
                }
                LastPingResult = null;
            }
        }

    }
}
