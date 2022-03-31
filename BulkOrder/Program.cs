using IO.Swagger.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valloon.Trading;

namespace BulkOrder
{
    internal class Program
    {
        class ParamMap
        {
            public int Leverage { get; set; }
            public decimal Limit { get; set; }
            public decimal Stop { get; set; }
            public decimal Close { get; set; }
        }

        static List<KeyValuePair<string, string>> ReadApiKey()
        {
            string text = File.ReadAllText("api-key.txt");
            string[] lines = text.Split(new char[] { '\n' });
            List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] split = line.Trim().Split(new char[] { '\t' });
                if (split.Length < 2) continue;
                string key = split[0];
                string value = split[1];
                list.Add(new KeyValuePair<string, string>(key, value));
            }
            return list;
        }

        static void Main(string[] args)
        {
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd HH.mm.ss}");
            ParamMap param = JsonConvert.DeserializeObject<ParamMap>(File.ReadAllText("config.json"));
            logger.WriteLine(JObject.FromObject(param).ToString(Formatting.Indented));
            logger.WriteLine("\r\n");
            if (param.Limit <= param.Stop || param.Limit >= param.Close)
            {
                logger.WriteLine($"Invalid Limit/Stop/Close values.", ConsoleColor.Red);
                goto end;
            }
            string symbol = BitMEXApiHelper.SYMBOL_SOLUSD;
            var apiKeyList = ReadApiKey();
            logger.WriteLine($"{apiKeyList.Count} api-keys loaded.", ConsoleColor.Green);

            Console.WriteLine($"\r\nPress any key to continue... ");
            Console.ReadKey(false);

            foreach (KeyValuePair<string, string> pair in apiKeyList)
            {
                logger.WriteLine("\r\n");
                try
                {
                    BitMEXApiHelper apiHelper = new BitMEXApiHelper(pair.Key, pair.Value);
                    logger.WriteLine($"api-key = {pair.Key}");
                    Margin margin = apiHelper.GetMargin(BitMEXApiHelper.CURRENCY_XBt);
                    decimal walletBalance = margin.WalletBalance.Value / 100000000m;
                    int orderQty = BitMEXApiHelper.FixQty((int)(margin.WalletBalance.Value * param.Leverage / 10000));
                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:yyyy-MM-dd  HH:mm:ss fff}  orderQty = {orderQty}, walletBalance = {walletBalance}");
                    List<Order> canceledOrders = apiHelper.CancelAllOrders(symbol);
                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:yyyy-MM-dd  HH:mm:ss fff}  {canceledOrders.Count} orders have been canceled.");
                    {
                        Order newOrder = apiHelper.OrderNew(new Order
                        {
                            Symbol = symbol,
                            Side = "Buy",
                            OrderQty = orderQty,
                            Price = param.Limit,
                            OrdType = "Limit",
                            Text = $"<BOT><BUY-LIMIT></BOT>"
                        });
                        logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New LIMIT buy order: qty = {orderQty}, price = {param.Limit}");
                        logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                    }
                    {
                        Order newOrder = apiHelper.OrderNew(new Order
                        {
                            Symbol = symbol,
                            Side = "Sell",
                            OrderQty = orderQty,
                            StopPx = param.Stop,
                            OrdType = "Stop",
                            ExecInst = "LastPrice,ReduceOnly",
                            Text = $"<BOT><BUY-STOP></BOT>"
                        });
                        logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New STOP Close order: qty = {orderQty}, stop = {param.Limit}");
                        logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                    }
                    {
                        Order newOrder = apiHelper.OrderNew(new Order
                        {
                            Symbol = symbol,
                            Side = "Sell",
                            OrderQty = orderQty,
                            Price = param.Close,
                            StopPx = param.Limit,
                            OrdType = "StopLimit",
                            ExecInst = "LastPrice,ReduceOnly",
                            Text = $"<BOT><BUY-CLOSE></BOT>"
                        });
                        logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New STOP-LIMIT Close order: qty = {orderQty}, price = {param.Close}, stop = {param.Limit}");
                        logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                    }
                }
                catch (Exception ex)
                {
                    logger.WriteLine(ex.ToString());
                }
            }

        end:;
            Console.WriteLine($"\r\nPress any key to exit... ");
            Console.ReadKey(false);
        }
    }
}
