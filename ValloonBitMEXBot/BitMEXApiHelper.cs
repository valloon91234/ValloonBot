using IO.Swagger.Api;
using IO.Swagger.Client;
using IO.Swagger.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Valloon.BitMEX.Utils;

/**
 * @author Valloon Project
 * @version 1.0 @2020-04-07
 * @version 1.1 @2020-05-08
 * @version 1.2 @2020-05-24
 */
namespace Valloon.BitMEX
{
    class BitMEXApiHelper
    {
        private static readonly DateTime Jan1st1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static long GetExpires()
        {
            return (long)(DateTime.UtcNow - Jan1st1970).TotalSeconds + 3600; // set expires one hour in the future
        }

        private static string ByteArrayToString(byte[] ba)
        {
            var hex = new StringBuilder(ba.Length * 2);
            foreach (var b in ba)
            {
                hex.AppendFormat("{0:x2}", b);
            }
            return hex.ToString();
        }

        private static byte[] Hmacsha256(byte[] keyByte, byte[] messageBytes)
        {
            using (var hash = new HMACSHA256(keyByte))
            {
                return hash.ComputeHash(messageBytes);
            }
        }

        private static string BuildQueryData(Dictionary<string, string> param)
        {
            if (param == null || param.Count == 0)
                return "";

            StringBuilder b = new StringBuilder();
            foreach (var item in param)
                if (item.Value != null && item.Value != "")
                    b.Append(string.Format("&{0}={1}", item.Key, WebUtility.UrlEncode(item.Value)));

            try
            {
                LastPlain4Sign = b.ToString().Substring(1);
                return LastPlain4Sign;
            }
            catch (Exception e)
            {
                LastPlain4Sign = e.ToString();
                return "";
            }
        }

        public static int FixQty(int qty)
        {
            return Math.Max(qty / 100, 1) * 100;
        }

        public const string SYMBOL = "XBTUSD";
        public const string CURRENCY = "XBt";

        public readonly string API_KEY;
        public readonly string API_SECRET;

        private readonly InstrumentApi InstrumentApiInstance;
        private readonly TradeApi TradeApiInstance;
        private readonly UserApi UserApiInstance;
        private readonly OrderApi OrderApiInstance;
        private readonly PositionApi PositionApiInstance;

        public static DateTime? ServerTime { get; set; }
        public int RequestCount { get; set; }
        public static string LastPlain4Sign { get; set; }

        public BitMEXApiHelper(string apiKey, string apiSecret, bool testnet = false)
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            API_KEY = apiKey;
            API_SECRET = apiSecret;
            if (testnet) Configuration.Default.BasePath = "https://testnet.bitmex.com/api/v1";
            InstrumentApiInstance = new InstrumentApi();
            TradeApiInstance = new TradeApi();
            UserApiInstance = new UserApi();
            OrderApiInstance = new OrderApi();
            PositionApiInstance = new PositionApi();
        }

        private string CreateSignature(string method, string function, string paramData = null, string postData = null)
        {
            string url = "/api/v1" + function + (paramData != null ? "?" + paramData : "");
            string expires = GetExpires().ToString();
            string message = method + url + expires + postData;
            string signatureString = null;
            if (API_SECRET != null)
            {
                byte[] signatureBytes = Hmacsha256(Encoding.UTF8.GetBytes(API_SECRET), Encoding.UTF8.GetBytes(message));
                signatureString = ByteArrayToString(signatureBytes);
            }
            Configuration.Default.ApiKey["api-expires"] = expires;
            Configuration.Default.ApiKey["api-key"] = API_KEY;
            Configuration.Default.ApiKey["api-signature"] = signatureString;
            return message;
        }

        public Instrument GetInstrument()
        {
            RequestCount++;
            List<Instrument> list = InstrumentApiInstance.InstrumentGet(SYMBOL, null, null, 1, null, true);
            Instrument item = list[0];
            ServerTime = item.Timestamp.Value;
            return item;
        }

        public TradeBin GetVolume(string binSize, int volumeCount, out decimal ema)
        {
            RequestCount++;
            List<TradeBin> list = TradeApiInstance.TradeGetBucketed(binSize, true, SYMBOL, null, null, 1000, null, true);
            int count = list.Count;
            if (count < 1)
            {
                ema = 0;
                return null;
            }
            //List<decimal> priceList = new List<decimal>();
            EMA eMA = new EMA(volumeCount);
            float f = 0;
            for (int i = count - 1; i >= 0; i--)
            {
                //priceList.Add(list[i].Close.Value);
                f = eMA.NextValue((float)list[i].Close.Value);
                //Console.WriteLine(list[i].Close + "\t" + f);
            }
            //ema = priceList.Average();
            ema = (decimal)f;
            return list[0];
        }

        public int GetRecentVolume(int minutes = 5)
        {
            RequestCount++;
            List<TradeBin> list = TradeApiInstance.TradeGetBucketed("1m", false, SYMBOL, null, null, minutes + 1, null, true);
            List<decimal> volumeList = new List<decimal>();
            int count = list.Count;
            for (int i = 1; i < count; i++)
            {
                var item = list[i];
                volumeList.Add(item.Volume.Value);
            }
            return (int)volumeList.Average();
        }

        public Margin GetMargin(string currency = CURRENCY)
        {
            RequestCount++;
            Dictionary<string, string> param = new Dictionary<string, string>
            {
                ["currency"] = currency
            };
            CreateSignature("GET", "/user/margin", BuildQueryData(param));
            return UserApiInstance.UserGetMargin(CURRENCY);
        }

        public List<Order> GetOrders(string filter = null)
        {
            RequestCount++;
            Dictionary<string, string> param = new Dictionary<string, string>
            {
                ["symbol"] = SYMBOL,
                ["filter"] = filter,
                ["reverse"] = true.ToString()
            };
            CreateSignature("GET", "/order", BuildQueryData(param));
            return OrderApiInstance.OrderGetOrders(SYMBOL, filter, null, null, null, true);
        }

        public List<Order> GetActiveOrders()
        {
            RequestCount++;
            List<Order> list = GetOrders();
            List<Order> resultList = new List<Order>();
            foreach (Order order in list)
            {
                if ((order.OrdStatus == "New" || order.OrdStatus == "PartiallyFilled"))
                    resultList.Add(order);
            }
            return resultList;
        }

        public List<Order> GetLimitOrders()
        {
            RequestCount++;
            string filter = "{\"ordType\":\"Limit\"}";
            Dictionary<string, string> param = new Dictionary<string, string>
            {
                ["symbol"] = SYMBOL,
                ["filter"] = filter,
                ["reverse"] = true.ToString()
            };
            CreateSignature("GET", "/order", BuildQueryData(param));
            List<Order> list = OrderApiInstance.OrderGetOrders(SYMBOL, filter, null, null, null, true);
            List<Order> resultList = new List<Order>();
            foreach (Order order in list)
            {
                if ((order.OrdStatus == "New" || order.OrdStatus == "PartiallyFilled"))
                    resultList.Add(order);
            }
            return resultList;
        }

        public List<Order> CancelOrders(List<string> orderIDList)
        {
            RequestCount++;
            JObject jObject = new JObject()
            {
                ["orderID"] = JArray.FromObject(orderIDList)
            };
            string json = jObject.ToString(Formatting.None);
            CreateSignature("DELETE", "/order", null, json);
            return OrderApiInstance.OrderCancelBulk(json);
        }

        public List<Order> CancelAllOrders()
        {
            RequestCount++;
            Dictionary<string, string> param = new Dictionary<string, string>
            {
                ["symbol"] = SYMBOL
            };
            CreateSignature("DELETE", "/order/all", BuildQueryData(param));
            return OrderApiInstance.OrderCancelAll(SYMBOL);
        }

        public Order OrderAmend(Order order, String newClOrdID = null)
        {
            RequestCount++;
            Dictionary<string, string> param = new Dictionary<string, string>
            {
                ["orderID"] = order.OrderID,
                ["origClOrdID"] = order.ClOrdID,
                ["clOrdID"] = newClOrdID,
                ["simpleOrderQty"] = order.SimpleOrderQty.ToString(),
                ["orderQty"] = order.OrderQty.ToString(),
                ["simpleLeavesQty"] = order.SimpleLeavesQty.ToString(),
                ["leavesQty"] = order.LeavesQty.ToString(),
                ["price"] = order.Price.ToString(),
                ["stopPx"] = order.StopPx.ToString(),
                ["pegOffsetValue"] = order.PegOffsetValue.ToString(),
                ["text"] = order.Text
            };
            CreateSignature("PUT", "/order", null, BuildQueryData(param));
            return OrderApiInstance.OrderAmend(order.OrderID, order.ClOrdID, newClOrdID, order.SimpleOrderQty, order.OrderQty, order.SimpleLeavesQty, order.LeavesQty, order.Price, order.StopPx, order.PegOffsetValue, order.Text);
        }

        public Order OrderNew(string side, int? orderQty, decimal? price, decimal? stopPx = null, string ordType = null, string execInst = null, string text = null)
        {
            RequestCount++;
            Dictionary<string, string> param = new Dictionary<string, string>
            {
                ["symbol"] = SYMBOL,
                ["side"] = side,
                ["orderQty"] = orderQty.ToString(),
                ["price"] = price.ToString(),
                ["stopPx"] = stopPx.ToString(),
                ["ordType"] = ordType,
                ["execInst"] = execInst,
                ["text"] = text
            };
            CreateSignature("POST", "/order", null, BuildQueryData(param));
            return OrderApiInstance.OrderNew(SYMBOL, side, null, orderQty, price, null, stopPx, null, null, null, null, ordType, null, execInst, null, text);
        }

        public Order OrderNewLimit(string side, decimal? price, int? qty, string text = null)
        {
            RequestCount++;
            Dictionary<string, string> param = new Dictionary<string, string>
            {
                ["symbol"] = SYMBOL,
                ["side"] = side,
                ["orderQty"] = qty.ToString(),
                ["price"] = price.ToString(),
                ["ordType"] = "Limit",
                ["text"] = text
            };
            CreateSignature("POST", "/order", null, BuildQueryData(param));
            return OrderApiInstance.OrderNew(SYMBOL, side, null, qty, price, null, null, null, null, null, null, "Limit", null, null, null, text);
        }

        public Order OrderNewLimitClose(string side, decimal? price, int? qty = null, string text = null)
        {
            RequestCount++;
            Dictionary<string, string> param = new Dictionary<string, string>
            {
                ["symbol"] = SYMBOL,
                ["side"] = side,
                ["orderQty"] = qty?.ToString(),
                ["price"] = price.ToString(),
                ["ordType"] = "Limit",
                ["execInst"] = "Close",
                ["text"] = text
            };
            CreateSignature("POST", "/order", null, BuildQueryData(param));
            return OrderApiInstance.OrderNew(SYMBOL, side, null, qty, price, null, null, null, null, null, null, "Limit", null, "Close", null, text);
        }

        public Order OrderNewMarket(string side, int? qty, string text = null)
        {
            RequestCount++;
            Dictionary<string, string> param = new Dictionary<string, string>
            {
                ["symbol"] = SYMBOL,
                ["side"] = side,
                ["orderQty"] = qty.ToString(),
                ["ordType"] = "Market",
                ["text"] = text
            };
            CreateSignature("POST", "/order", null, BuildQueryData(param));
            return OrderApiInstance.OrderNew(SYMBOL, side, null, qty, null, null, null, null, null, null, null, "Market", null, null, null, text);
        }

        public Order OrderNewMarketClose(string side, int? qty = null, string text = null)
        {
            RequestCount++;
            Dictionary<string, string> param = new Dictionary<string, string>
            {
                ["symbol"] = SYMBOL,
                ["side"] = side,
                ["orderQty"] = qty?.ToString(),
                ["ordType"] = "Market",
                ["execInst"] = "Close",
                ["text"] = text
            };
            CreateSignature("POST", "/order", null, BuildQueryData(param));
            return OrderApiInstance.OrderNew(SYMBOL, side, null, qty, null, null, null, null, null, null, null, "Market", null, "Close", null, text);
        }

        public Order OrderNewStopMarket(string side, int stopPx, int? qty, string execInst = null, string text = null)
        {
            RequestCount++;
            Dictionary<string, string> param = new Dictionary<string, string>
            {
                ["symbol"] = SYMBOL,
                ["side"] = side,
                ["orderQty"] = qty?.ToString(),
                ["stopPx"] = stopPx.ToString(),
                ["ordType"] = "Stop",
                ["execInst"] = execInst,
                ["text"] = text
            };
            CreateSignature("POST", "/order", null, BuildQueryData(param));
            return OrderApiInstance.OrderNew(SYMBOL, side, null, qty, null, null, stopPx, null, null, null, null, "Stop", null, execInst, null, text);
        }

        public Order OrderNewStopMarketClose(string side, int stopPx, string text = null)
        {
            RequestCount++;
            Dictionary<string, string> param = new Dictionary<string, string>
            {
                ["symbol"] = SYMBOL,
                ["side"] = side,
                ["stopPx"] = stopPx.ToString(),
                ["ordType"] = "Stop",
                ["execInst"] = "Close",
                ["text"] = text
            };
            CreateSignature("POST", "/order", null, BuildQueryData(param));
            return OrderApiInstance.OrderNew(SYMBOL, side, null, null, null, null, stopPx, null, null, null, null, "Stop", null, "Close", null, text);
        }

        public Order OrderNewTakeProfitMarketClose(string side, int stopPx, int? qty = null, string text = null)
        {
            RequestCount++;
            Dictionary<string, string> param = new Dictionary<string, string>
            {
                ["symbol"] = SYMBOL,
                ["side"] = side,
                ["orderQty"] = qty?.ToString(),
                ["stopPx"] = stopPx.ToString(),
                ["ordType"] = "MarketIfTouched",
                ["execInst"] = "Close",
                ["text"] = text
            };
            CreateSignature("POST", "/order", null, BuildQueryData(param));
            return OrderApiInstance.OrderNew(SYMBOL, side, null, qty, null, null, stopPx, null, null, null, null, "MarketIfTouched", null, "Close", null, text);
        }

        public Position GetPosition()
        {
            RequestCount++;
            string filter = "{\"symbol\":\"" + SYMBOL + "\"}";
            Dictionary<string, string> param = new Dictionary<string, string>
            {
                ["filter"] = filter,
                ["count"] = 1.ToString()
            };
            CreateSignature("GET", "/position", BuildQueryData(param));
            List<Position> list = PositionApiInstance.PositionGet(filter, null, 1);
            if (list.Count > 0) return list[0];
            return null;
        }


        public Wallet GetWallet(string currency = CURRENCY)
        {
            RequestCount++;
            Dictionary<string, string> param = new Dictionary<string, string>
            {
                ["currency"] = currency
            };
            CreateSignature("GET", "/user/wallet", BuildQueryData(param));
            return UserApiInstance.UserGetWallet(CURRENCY);
        }

        public List<Transaction> GetWalletHistory(string currency = CURRENCY, int count = 10)
        {
            RequestCount++;
            Dictionary<string, string> param = new Dictionary<string, string>
            {
                ["currency"] = currency,
                ["count"] = count.ToString()
            };
            CreateSignature("GET", "/user/walletHistory", BuildQueryData(param));
            return UserApiInstance.UserGetWalletHistory(CURRENCY, count);
        }

    }
}
