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

/**
 * @author Valloon Project
 * @version 1.0 @2020-04-07
 * @version 1.1 @2020-05-08
 * @version 1.2 @2020-05-24
 */
namespace Valloon.Trading
{
    public class BitMEXApiHelper
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

        public const string SYMBOL_XBTUSD = "XBTUSD";
        public const string SYMBOL_SOLUSD = "SOLUSD";
        public const string CURRENCY_XBt = "XBt";

        public readonly string API_KEY;
        public readonly string API_SECRET;

        private readonly InstrumentApi InstrumentApiInstance;
        private readonly TradeApi TradeApiInstance;
        private readonly UserApi UserApiInstance;
        private readonly OrderApi OrderApiInstance;
        private readonly PositionApi PositionApiInstance;

        private static long _serverTimeDiff;
        public static DateTime ServerTime
        {
            get
            {
                return DateTime.UtcNow.AddTicks(_serverTimeDiff);
            }
            set
            {
                _serverTimeDiff = (value - DateTime.Now).Ticks;
            }
        }

        public int RequestCount { get; set; }
        public static string LastPlain4Sign { get; set; }

        private static long ServerTimeDiff = 0;

        public BitMEXApiHelper(string apiKey = null, string apiSecret = null, bool testnet = false)
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

        public Instrument GetInstrument(string symbol)
        {
            RequestCount++;
            ApiResponse<List<Instrument>> localVarResponse = InstrumentApiInstance.InstrumentGetWithHttpInfo(symbol, null, null, 1, null, true);
            ServerTime = DateTime.Parse(localVarResponse.Headers["Date"]);
            return localVarResponse.Data[0];
        }

        public List<TradeBin> GetRencentBinList(string symbol, string binSize, int count, bool? partial = null)
        {
            RequestCount++;
            return TradeApiInstance.TradeGetBucketed(binSize, partial, symbol, null, null, count, null, true);
        }

        public List<TradeBin> GetBinList(string binSize = null, bool? partial = null, string symbol = null, decimal? count = null, bool? reverse = null, DateTime? startTime = null, DateTime? endTime = null)
        {
            return TradeApiInstance.TradeGetBucketed(binSize, partial, symbol, null, null, count, null, reverse, startTime, endTime);
        }

        public Margin GetMargin(string currency = CURRENCY_XBt)
        {
            RequestCount++;
            Dictionary<string, string> param = new Dictionary<string, string>
            {
                ["currency"] = currency
            };
            CreateSignature("GET", "/user/margin", BuildQueryData(param));
            ApiResponse<Margin> localVarResponse = UserApiInstance.UserGetMarginWithHttpInfo(CURRENCY_XBt);
            ServerTime = DateTime.Parse(localVarResponse.Headers["Date"]);
            return localVarResponse.Data;
        }

        public List<Order> GetOrders(string symbol, string filter = null)
        {
            RequestCount++;
            Dictionary<string, string> param = new Dictionary<string, string>
            {
                ["symbol"] = symbol,
                ["filter"] = filter,
                ["reverse"] = true.ToString()
            };
            CreateSignature("GET", "/order", BuildQueryData(param));
            return OrderApiInstance.OrderGetOrders(symbol, filter, null, null, null, true);
        }

        public List<Order> GetActiveOrders(string symbol)
        {
            List<Order> list = GetOrders(symbol);
            List<Order> resultList = new List<Order>();
            foreach (Order order in list)
            {
                if ((order.OrdStatus == "New" || order.OrdStatus == "PartiallyFilled"))
                    resultList.Add(order);
            }
            return resultList;
        }

        public List<Order> GetLimitOrders(string symbol)
        {
            RequestCount++;
            string filter = "{\"ordType\":\"Limit\"}";
            Dictionary<string, string> param = new Dictionary<string, string>
            {
                ["symbol"] = symbol,
                ["filter"] = filter,
                ["reverse"] = true.ToString()
            };
            CreateSignature("GET", "/order", BuildQueryData(param));
            List<Order> list = OrderApiInstance.OrderGetOrders(symbol, filter, null, null, null, true);
            List<Order> resultList = new List<Order>();
            foreach (Order order in list)
            {
                if ((order.OrdStatus == "New" || order.OrdStatus == "PartiallyFilled"))
                    resultList.Add(order);
            }
            return resultList;
        }

        public List<Order> CancelOrder(string orderID)
        {
            RequestCount++;
            Dictionary<string, string> param = new Dictionary<string, string>
            {
                ["orderID"] = orderID
            };
            CreateSignature("DELETE", "/order", BuildQueryData(param));
            return OrderApiInstance.OrderCancel(orderID);
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

        public List<Order> CancelAllOrders(string symbol)
        {
            RequestCount++;
            Dictionary<string, string> param = new Dictionary<string, string>
            {
                ["symbol"] = symbol
            };
            CreateSignature("DELETE", "/order/all", BuildQueryData(param));
            return OrderApiInstance.OrderCancelAll(symbol);
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

        public Order OrderNew(Order order)
        {
            RequestCount++;
            Dictionary<string, string> param = new Dictionary<string, string>
            {
                ["symbol"] = order.Symbol,
                ["side"] = order.Side,
                ["orderQty"] = order.OrderQty.ToString(),
                ["price"] = order.Price.ToString(),
                ["stopPx"] = order.StopPx.ToString(),
                ["ordType"] = order.OrdType,
                ["execInst"] = order.ExecInst,
                ["text"] = order.Text
            };
            CreateSignature("POST", "/order", null, BuildQueryData(param));
            return OrderApiInstance.OrderNew(order.Symbol, order.Side, null, order.OrderQty, order.Price, null, order.StopPx, null, null, null, null, order.OrdType, null, order.ExecInst, null, order.Text);
        }

        public Object OrderCancelAllAfter(long timeoutMilliseconds)
        {
            RequestCount++;
            Dictionary<string, string> param = new Dictionary<string, string>
            {
                ["timeout"] = timeoutMilliseconds.ToString(),
            };
            CreateSignature("POST", "/order/cancelAllAfter", null, BuildQueryData(param));
            return OrderApiInstance.OrderCancelAllAfter(timeoutMilliseconds);
        }

        public Position GetPosition(string symbol)
        {
            RequestCount++;
            string filter = "{\"symbol\":\"" + symbol + "\"}";
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

        public User GetUser()
        {
            RequestCount++;
            CreateSignature("GET", "/user");
            return UserApiInstance.UserGet();
        }

        public Wallet GetWallet(string currency = CURRENCY_XBt)
        {
            RequestCount++;
            Dictionary<string, string> param = new Dictionary<string, string>
            {
                ["currency"] = currency
            };
            CreateSignature("GET", "/user/wallet", BuildQueryData(param));
            return UserApiInstance.UserGetWallet(CURRENCY_XBt);
        }

        public List<Transaction> GetWalletHistory(string currency = CURRENCY_XBt, int count = 10)
        {
            RequestCount++;
            Dictionary<string, string> param = new Dictionary<string, string>
            {
                ["currency"] = currency,
                ["count"] = count.ToString()
            };
            CreateSignature("GET", "/user/walletHistory", BuildQueryData(param));
            return UserApiInstance.UserGetWalletHistory(CURRENCY_XBt, count);
        }

    }
}
