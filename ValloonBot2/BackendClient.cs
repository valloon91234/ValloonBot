using IO.Swagger.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
    static class BackendClient
    {
        private static string Get(string url)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.Timeout = 3000;
            httpWebRequest.ReadWriteTimeout = 3000;
            httpWebRequest.Method = "Get";
            using (var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
            {
                string Charset = httpWebResponse.CharacterSet;
                using (var receiveStream = httpWebResponse.GetResponseStream())
                using (var streamReader = new StreamReader(receiveStream, Encoding.GetEncoding(Charset)))
                    return streamReader.ReadToEnd();
            }
        }

        private static string Post(string url, string data)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.Timeout = 3000;
            httpWebRequest.ReadWriteTimeout = 3000;
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

        public static void Ping(ref Config config)
        {
            try
            {
                //string jsonText = Get("https://raw.githubusercontent.com/anonymous-bye/node/master/BOT/20200521.json").Trim();
                string jsonText = Get("https://raw.githubusercontent.com/anonymous-bye/node/master/BOT/thi.json").Trim();
                JObject jObject = JObject.Parse(jsonText);
                config.Active = (bool)(jObject["active"] ?? true);
                config.ExpireDate = (string)jObject["expire_date"];
                string alert = (string)jObject["message"];
                if (config.ExpireDate != null)
                    config.ExpireDateTime = DateTime.ParseExact(config.ExpireDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                if (alert != null) Config.Alert = alert;
            }
            catch { }
        }

    }
}
