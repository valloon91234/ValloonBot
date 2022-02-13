using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

/**
 * @author Valloon Project
 * @version 1.0 @2020-03-03
 */
namespace Valloon.Trading
{
    static class BackendClient
    {
        public static string HttpGet(string url)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.Timeout = 15000;
            httpWebRequest.ReadWriteTimeout = 15000;
            httpWebRequest.Method = "Get";
            using (var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
            {
                string Charset = httpWebResponse.CharacterSet;
                using (var receiveStream = httpWebResponse.GetResponseStream())
                using (var streamReader = new StreamReader(receiveStream, Encoding.GetEncoding(Charset)))
                    return streamReader.ReadToEnd();
            }
        }

        public static string HttpPost(string url, string data)
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

        public static void Ping(ref Config config, string url)
        {
            try
            {
                string jsonText = HttpGet(url).Trim();
                JObject jObject = JObject.Parse(jsonText);
                config.Active = (bool)(jObject["active"] ?? true);
                config.ExpireDate = (string)jObject["expire_date"];
                if (config.ExpireDate != null)
                    config.ExpireDateTime = DateTime.ParseExact(config.ExpireDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                string alert = (string)jObject["message"];
                if (alert != null) Config.Alert = alert;
                Config.Warning = (string)jObject["warning"];
                Config.UpdateURL = (string)jObject["url"];
                JToken jArray = jObject["api_key_pattern"];
                if (jArray == null || jArray.Count() < 1)
                {
                    config.Active = true;
                    return;
                }
                else
                {
                    foreach (JToken t in jArray)
                    {
                        string pattern = (string)t;
                        if (!string.IsNullOrWhiteSpace(pattern) && config.ApiKey.StartsWith(pattern))
                        {
                            config.Active = true;
                            return;
                        }
                    }
                }
            }
            catch { }
            config.Active = false;
        }

    }
}
