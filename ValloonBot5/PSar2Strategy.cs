//#define LICENSE_MODE

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using IO.Swagger.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Skender.Stock.Indicators;
using Valloon.Stock.Indicators;
using Valloon.Utils;

/**
 * @author Valloon Present
 * @version 2022-02-10
 */
namespace Valloon.Trading
{
    public class PSar2Strategy
    {
        private class ParamConfig
        {
            [JsonProperty("bin1")]
            public int BinSize1 { get; set; }
            [JsonProperty("start1")]
            public decimal PSarStart1 { get; set; }
            [JsonProperty("step1")]
            public decimal PSarStep1 { get; set; }
            [JsonProperty("max1")]
            public decimal PSarMax1 { get; set; }
            [JsonProperty("bin2")]
            public int BinSize2 { get; set; }
            [JsonProperty("start2")]
            public decimal PSarStart2 { get; set; }
            [JsonProperty("step2")]
            public decimal PSarStep2 { get; set; }
            [JsonProperty("max2")]
            public decimal PSarMax2 { get; set; }
            [JsonProperty("stopLoss")]
            public decimal StopLoss { get; set; }
        }

        public void Run(Config config = null)
        {
            int loopIndex = 0;
            DateTime? lastLoopTime = null;
            decimal lastWalletBalance = 0;
            ParamConfig param = null;
            Logger logger = null;
            DateTime fileModifiedTime = File.GetLastWriteTimeUtc(Assembly.GetExecutingAssembly().Location);
            while (true)
            {
                try
                {
                    DateTime currentLoopTime = DateTime.UtcNow;
                    config = Config.Load(out bool configUpdated, lastLoopTime != null && lastLoopTime.Value.Day != currentLoopTime.Day);
                    BitMEXApiHelper apiHelper = new BitMEXApiHelper(config.ApiKey, config.ApiSecret, config.TestnetMode);
                    //Instrument instrumentBTC = apiHelper.GetInstrument(BitMEXApiHelper.SYMBOL_XBTUSD);
                    //decimal btcPrice = instrumentBTC.LastPrice.Value;
                    logger = new Logger($"{BitMEXApiHelper.ServerTime:yyyy-MM-dd}");
                    if (configUpdated)
                    {
                        loopIndex = 0;
                        logger.WriteLine($"\r\n[{BitMEXApiHelper.ServerTime:yyyy-MM-dd  HH:mm:ss}]  Config loaded.", ConsoleColor.Green);
                        logger.WriteLine(JObject.FromObject(config).ToString(Formatting.Indented));
                    }
                    string symbol = config.Symbol.ToUpper();
                    if (param == null || BitMEXApiHelper.ServerTime.Minute % 30 == 0 && BitMEXApiHelper.ServerTime.Second < 15)
                    {
                        string url = $"https://raw.githubusercontent.com/maksimg1002/_upload1002/main/bitmex-solusd-sar2-0412.json";
                        string paramText = HttpClient2.HttpGet(url);
                        param = JsonConvert.DeserializeObject<ParamConfig>(paramText);
                        logger.WriteLine($"\r\n[{BitMEXApiHelper.ServerTime:yyyy-MM-dd  HH:mm:ss}]  ParamMap loaded.", ConsoleColor.Green);
                        logger.WriteLine(JObject.FromObject(param).ToString(Formatting.Indented));
                        logger.WriteLine();
                    }
                    else if (configUpdated)
                    {
                        logger.WriteLine();
                    }

                    List<TradeBin> binList_5m = apiHelper.GetBinList("5m", false, symbol, 1000, null, true);
                    List<TradeBin> binList_1h = apiHelper.GetBinList("1h", false, symbol, 1000, null, true);
                    binList_1h.AddRange(apiHelper.GetBinList("1h", false, symbol, 1000, null, true, null, binList_1h.Last().Timestamp.Value.AddHours(-1)));

                    Margin margin = apiHelper.GetMargin(BitMEXApiHelper.CURRENCY_XBt);
                    decimal walletBalance = margin.WalletBalance.Value / 100000000m;
                    List<Order> activeOrderList = apiHelper.GetActiveOrders(symbol);
                    Instrument instrument = apiHelper.GetInstrument(symbol);
                    decimal lastPrice = instrument.LastPrice.Value;
                    decimal markPrice = instrument.MarkPrice.Value;
                    List<Order> botOrderList = new List<Order>();
                    foreach (Order order in activeOrderList)
                        if (order.Text.Contains("<BOT>")) botOrderList.Add(order);
                    {
                        decimal unavailableMarginPercent = 100m * (margin.WalletBalance.Value - margin.AvailableMargin.Value) / margin.WalletBalance.Value;
                        string balanceChange = null;
                        if (lastWalletBalance != 0 && lastWalletBalance != walletBalance)
                        {
                            decimal value = (walletBalance - lastWalletBalance) / lastWalletBalance * 100;
                            if (value >= 0)
                                balanceChange = $"    ( +{value:N4} % )";
                            else
                                balanceChange = $"    ( {value:N4} % )";
                        }
                        logger.WriteLine($"[{BitMEXApiHelper.ServerTime:yyyy-MM-dd  HH:mm:ss fff}  ({++loopIndex})]    $ {lastPrice:F2}  /  $ {markPrice:F3}    {walletBalance:N8} XBT    {botOrderList.Count} / {activeOrderList.Count} / {unavailableMarginPercent:N2} %{balanceChange}", ConsoleColor.White);
                    }
                    int qty = (int)(margin.WalletBalance.Value * config.Leverage / 10000);
                    if (qty > 50) qty = BitMEXApiHelper.FixQty(qty);
                    Position position = apiHelper.GetPosition(symbol);
                    decimal positionEntryPrice = 0;
                    int positionQty = 0;
                    {
                        string consoleTitle = $"$ {lastPrice:N2}  /  {walletBalance:N8} XBT  /  {qty:N0} Cont";
                        if (position != null && position.CurrentQty.Value != 0) consoleTitle += "  /  1 Position";
                        consoleTitle += $"    <{config.Username}>  |   {Config.APP_NAME}  v{fileModifiedTime:yyyy.MM.dd HH:mm:ss}   by  ValloonTrader.com";
                        Console.Title = consoleTitle;
                        if (position != null && position.CurrentQty.Value != 0)
                        {
                            positionEntryPrice = position.AvgEntryPrice.Value;
                            positionQty = position.CurrentQty.Value;
                            decimal leverage = 1 / (Math.Abs(positionEntryPrice - position.LiquidationPrice.Value) / positionEntryPrice);
                            decimal unrealisedPercent = 100m * position.UnrealisedPnl.Value / margin.WalletBalance.Value;
                            decimal nowLoss = 100m * (positionEntryPrice - lastPrice) / (position.LiquidationPrice.Value - positionEntryPrice);
                            logger.WriteLine($"    <Position>    entry = {positionEntryPrice:F2}    qty = {positionQty}    liq = {position.LiquidationPrice}    leverage = {leverage:F2}    {unrealisedPercent:N2} % / {nowLoss:N2} %", ConsoleColor.Green);
                        }
                    }

                    List<TradeBin> reversedBinList1 = new List<TradeBin>(binList_5m);
                    reversedBinList1.Reverse();
                    List<TradeBin> binListConverted1 = BitMEXApiHelper.LoadBinListFrom5m($"{param.BinSize1}m", reversedBinList1);
                    var quoteList1 = IndicatorHelper.TradeBinToQuote(binListConverted1);
                    var parabolicSarList1 = quoteList1.GetParabolicSar(param.PSarStep1, param.PSarMax1, param.PSarStart1).ToList();
                    List<TradeBin> reversedBinList2 = new List<TradeBin>(binList_1h);
                    reversedBinList2.Reverse();
                    var quoteList2 = IndicatorHelper.TradeBinToQuote(reversedBinList2);
                    var parabolicSarList2 = quoteList2.GetParabolicSar(param.PSarStep2, param.PSarMax2, param.PSarStart2).ToList();
                    var lastPSar1 = parabolicSarList1.Last();
                    var lastPSar2 = parabolicSarList2.Last();
                    string trend1 = null, trend2 = null;
                    if (lastPSar1.Sar > quoteList1.Last().High)
                        trend1 = "\\/ Bearish \t ";
                    else if (lastPSar1.Sar < quoteList1.Last().Low)
                        trend1 = "/\\ Bullish \t ";
                    if (lastPSar2.Sar > quoteList2.Last().High)
                        trend2 = "\\/ Bearish \t ";
                    else if (lastPSar2.Sar < quoteList2.Last().Low)
                        trend2 = "/\\ Bullish \t ";
                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  {trend1}psar1 ({param.BinSize1}) = {parabolicSarList1[parabolicSarList1.Count - 2].Sar:F4} / {lastPSar1.Sar:F4} \t {parabolicSarList1[parabolicSarList1.Count - 2].IsReversal} / {lastPSar1.IsReversal}", ConsoleColor.DarkGray);
                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  {trend2}psar2 ({param.BinSize2}) = {parabolicSarList2[parabolicSarList2.Count - 2].Sar:F4} / {lastPSar2.Sar:F4} \t {parabolicSarList2[parabolicSarList2.Count - 2].IsReversal} / {lastPSar2.IsReversal}", ConsoleColor.DarkGray);
                    if (config.Exit == 2)
                    {
                        logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  No order. (exit = {config.Exit})", ConsoleColor.DarkGray);
                        goto endLoop;
                    }
                    if (config.Leverage == 0)
                    {
                        logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  No order. (qty = {config.Leverage})", ConsoleColor.DarkGray);
                        goto endLoop;
                    }
                    decimal stopPrice = (int)Math.Round(lastPSar1.Sar.Value * 100) / 100m;
                    if (positionQty == 0)
                    {
#if LICENSE_MODE
                        if (serverTime.Year != 2022 || serverTime.Month != 2)
                        {
                            List<string> cancelOrderList = new List<string>();
                            foreach (Order order in botOrderList)
                            {
                                cancelOrderList.Add(order.OrderID);
                            }
                            if (cancelOrderList.Count > 0)
                            {
                                var canceledOrders = apiHelper.CancelOrders(cancelOrderList);
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  {canceledOrders.Count} old orders have been canceled.");
                            }
                            logger.WriteLine($"This bot is too old. Please contact support.  https://valloontrader.com", ConsoleColor.Green);
                            goto endLoop;
                        }
#endif
                        if (config.Exit == 1)
                        {
                            List<string> cancelOrderList = new List<string>();
                            foreach (Order order in botOrderList)
                            {
                                cancelOrderList.Add(order.OrderID);
                            }
                            if (cancelOrderList.Count > 0)
                            {
                                var canceledOrders = apiHelper.CancelOrders(cancelOrderList);
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  {canceledOrders.Count} old orders have been canceled.");
                            }
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  No order. (exit = {config.Exit})", ConsoleColor.DarkGray);
                            goto endLoop;
                        }
                        {
                            List<string> cancelOrderList = new List<string>();
                            foreach (Order order in botOrderList)
                            {
                                if (order.Text.Contains("<STOP>"))
                                {
                                    cancelOrderList.Add(order.OrderID);
                                }
                            }
                            if (cancelOrderList.Count > 0)
                            {
                                var canceledOrders = apiHelper.CancelOrders(cancelOrderList);
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  {canceledOrders.Count} old STOP orders have been canceled.");
                            }
                        }
                        {
                            Order newStopLimitOrder = null;
                            if (lastPSar1.Sar.Value > quoteList1.Last().High && (config.BuyOrSell == 3 && lastPSar2.Sar.Value < quoteList2.Last().Low || config.BuyOrSell == 1))
                            {
                                var limitPrice = (int)Math.Round(lastPSar1.Sar.Value * 99.9m) / 100m;
                                newStopLimitOrder = new Order
                                {
                                    Symbol = BitMEXApiHelper.SYMBOL_SOLUSD,
                                    Side = "Buy",
                                    OrderQty = qty,
                                    Price = limitPrice,
                                    StopPx = stopPrice,
                                    OrdType = "StopLimit",
                                    ExecInst = "LastPrice",
                                    Text = $"<BOT><STOP-LIMIT><LONG></BOT>"
                                };
                            }
                            else if (lastPSar1.Sar.Value < quoteList1.Last().Low && (config.BuyOrSell == 3 && lastPSar2.Sar.Value > quoteList2.Last().High || config.BuyOrSell == 2))
                            {
                                var limitPrice = (int)Math.Round(lastPSar1.Sar.Value * 100.1m) / 100m;
                                newStopLimitOrder = new Order
                                {
                                    Symbol = BitMEXApiHelper.SYMBOL_SOLUSD,
                                    Side = "Sell",
                                    OrderQty = qty,
                                    Price = limitPrice,
                                    StopPx = stopPrice,
                                    OrdType = "StopLimit",
                                    ExecInst = "LastPrice",
                                    Text = $"<BOT><STOP-LIMIT><SHORT></BOT>"
                                };
                            }
                            bool autoCancelAllOrders = activeOrderList.Count == botOrderList.Count;
                            Order oldStopLimitOrder = null;
                            List<string> cancelOrderList = new List<string>();
                            foreach (Order order in botOrderList)
                            {
                                if (order.Text.Contains("<STOP-LIMIT>"))
                                {
                                    oldStopLimitOrder = order;
                                    cancelOrderList.Add(order.OrderID);
                                }
                            }
                            if (cancelOrderList.Count > 1 || newStopLimitOrder == null && oldStopLimitOrder != null && string.IsNullOrEmpty(oldStopLimitOrder.Triggered) || newStopLimitOrder != null && oldStopLimitOrder != null && (oldStopLimitOrder.Side != newStopLimitOrder.Side || !string.IsNullOrEmpty(oldStopLimitOrder.Triggered)))
                            {
                                var canceledOrders = apiHelper.CancelOrders(cancelOrderList);
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  {canceledOrders.Count} STOP-LIMIT orders have been canceled.");
                                oldStopLimitOrder = null;
                            }
                            if (newStopLimitOrder == null)
                            {
                                autoCancelAllOrders = false;
                            }
                            else if (oldStopLimitOrder == null)
                            {
                                Order resultOrder = apiHelper.OrderNew(newStopLimitOrder);
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New STOP-LIMIT order: qty = {qty}, limit = {newStopLimitOrder.Price}, stop = {newStopLimitOrder.StopPx}");
                                logger.WriteFile("--- " + JObject.FromObject(resultOrder).ToString(Formatting.None));
                            }
                            else if (oldStopLimitOrder.OrderQty != qty || oldStopLimitOrder.Price != newStopLimitOrder.Price || oldStopLimitOrder.StopPx != newStopLimitOrder.StopPx)
                            {
                                Order amendOrder = new Order
                                {
                                    OrderID = oldStopLimitOrder.OrderID,
                                    OrderQty = newStopLimitOrder.OrderQty,
                                    Price = newStopLimitOrder.Price,
                                    StopPx = newStopLimitOrder.StopPx,
                                    ExecInst = newStopLimitOrder.ExecInst,
                                    Text = newStopLimitOrder.Text,
                                };
                                Order resultOrder = apiHelper.OrderAmend(amendOrder);
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Amend STOP-LIMIT order: qty = {qty}, limit = {newStopLimitOrder.Price}, stop = {newStopLimitOrder.StopPx}");
                                logger.WriteFile("--- " + JObject.FromObject(resultOrder).ToString(Formatting.None));
                            }
                            if (autoCancelAllOrders)
                            {
                                apiHelper.OrderCancelAllAfter(15 * 60000);
                            }
                        }
                    }
                    else
                    {
                        {
                            List<string> cancelOrderList = new List<string>();
                            foreach (Order order in botOrderList)
                            {
                                if (order.Text.Contains("<STOP-LIMIT>")) cancelOrderList.Add(order.OrderID);
                            }
                            if (cancelOrderList.Count > 0)
                            {
                                var canceledOrders = apiHelper.CancelOrders(cancelOrderList);
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  {canceledOrders.Count} STOP-LIMIT orders have been canceled.");
                            }
                        }
                        if (positionQty > 0)
                        {
                            if (lastPSar1.Sar.Value < quoteList1.Last().Low)
                            {
                                var stopLossPrice = Math.Ceiling(positionEntryPrice * (1 - param.StopLoss) * 100) / 100m;
                                stopPrice = Math.Max(stopPrice, stopLossPrice);
                                Order oldStopOrder = null;
                                List<string> cancelOrderList = new List<string>();
                                foreach (Order order in botOrderList)
                                {
                                    if (order.Text.Contains("<STOP>"))
                                    {
                                        oldStopOrder = order;
                                        cancelOrderList.Add(order.OrderID);
                                    }
                                }
                                if (cancelOrderList.Count > 1 || oldStopOrder != null && oldStopOrder.Side != "Sell")
                                {
                                    var canceledOrders = apiHelper.CancelOrders(cancelOrderList);
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  {canceledOrders.Count} STOP orders have been canceled.");
                                    oldStopOrder = null;
                                }
                                if (oldStopOrder == null)
                                {
                                    Order stopOrder = apiHelper.OrderNew(new Order
                                    {
                                        Symbol = symbol,
                                        Side = "Sell",
                                        OrderQty = positionQty,
                                        StopPx = stopPrice,
                                        OrdType = "Stop",
                                        ExecInst = "LastPrice,ReduceOnly",
                                        Text = $"<BOT><STOP></BOT>",
                                    });
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New STOP close order: qty = {positionQty}, price = {stopPrice}");
                                    logger.WriteFile("--- " + JObject.FromObject(stopOrder).ToString(Formatting.None));
                                }
                                else if (oldStopOrder.OrderQty != positionQty || oldStopOrder.StopPx != stopPrice)
                                {
                                    Order amendOrder = apiHelper.OrderAmend(new Order
                                    {
                                        OrderID = oldStopOrder.OrderID,
                                        OrderQty = positionQty,
                                        StopPx = stopPrice,
                                        ExecInst = "LastPrice,ReduceOnly",
                                        Text = $"<BOT><STOP></BOT>",
                                    });
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Amend STOP close order: qty = {positionQty}, price = {stopPrice}");
                                    logger.WriteFile("--- " + JObject.FromObject(amendOrder).ToString(Formatting.None));
                                }
                            }
                            else
                            {
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Wrong position: qty = {positionQty}");
                            }
                        }
                        else if (positionQty < 0)
                        {
                            if (lastPSar1.Sar.Value > quoteList1.Last().High)
                            {
                                var stopLossPrice = Math.Floor(positionEntryPrice * (1 + param.StopLoss) * 100) / 100m;
                                stopPrice = Math.Min(stopPrice, stopLossPrice);
                                Order oldStopOrder = null;
                                List<string> cancelOrderList = new List<string>();
                                foreach (Order order in botOrderList)
                                {
                                    if (order.Text.Contains("<STOP>"))
                                    {
                                        oldStopOrder = order;
                                        cancelOrderList.Add(order.OrderID);
                                    }
                                }
                                if (cancelOrderList.Count > 1 || oldStopOrder != null && oldStopOrder.Side != "Buy")
                                {
                                    var canceledOrders = apiHelper.CancelOrders(cancelOrderList);
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  {canceledOrders.Count} STOP orders have been canceled.");
                                    oldStopOrder = null;
                                }
                                if (oldStopOrder == null)
                                {
                                    Order stopOrder = apiHelper.OrderNew(new Order
                                    {
                                        Symbol = symbol,
                                        Side = "Buy",
                                        OrderQty = -positionQty,
                                        StopPx = stopPrice,
                                        OrdType = "Stop",
                                        ExecInst = "LastPrice,ReduceOnly",
                                        Text = $"<BOT><STOP></BOT>",
                                    });
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New STOP close order: qty = {-positionQty}, price = {stopPrice}");
                                    logger.WriteFile("--- " + JObject.FromObject(stopOrder).ToString(Formatting.None));
                                }
                                else if (oldStopOrder.OrderQty != -positionQty || oldStopOrder.StopPx != stopPrice)
                                {
                                    Order amendOrder = apiHelper.OrderAmend(new Order
                                    {
                                        OrderID = oldStopOrder.OrderID,
                                        OrderQty = -positionQty,
                                        StopPx = stopPrice,
                                        ExecInst = "LastPrice,ReduceOnly",
                                        Text = $"<BOT><STOP></BOT>",
                                    });
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Amend STOP close order: qty = {-positionQty}, price = {stopPrice}");
                                    logger.WriteFile("--- " + JObject.FromObject(amendOrder).ToString(Formatting.None));
                                }
                            }
                            else
                            {
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Wrong position: qty = {-positionQty}");
                            }
                        }
                        else
                        {
                            logger.WriteLine($"        Invalid position qty: {positionQty}");
                        }
                    }

                endLoop:;
                    //margin = apiHelper.GetMargin(BitMEXApiHelper.CURRENCY_XBt);
                    //walletBalance = margin.WalletBalance.Value / 100000000m;
                    if (lastWalletBalance != walletBalance)
                    {
                        if (lastWalletBalance != 0)
                        {
                            string logFilename = $"{BitMEXApiHelper.ServerTime:yyyy-MM}-balance";
                            Logger logger2 = new Logger(logFilename);
                            if (!logger2.ExistFile()) logger2.WriteFile($"[{BitMEXApiHelper.ServerTime:yyyy-MM-dd  HH:mm:ss}  ({++loopIndex:D6})]    {lastWalletBalance:F8}");
                            string suffix = null;
                            if (positionQty != 0) suffix = $"        position = {positionQty}";
                            logger2.WriteFile($"[{BitMEXApiHelper.ServerTime:yyyy-MM-dd  HH:mm:ss}  ({++loopIndex:D6})]    {walletBalance:F8}    {walletBalance - lastWalletBalance:F8}    price = {lastPrice:F1}  /  {markPrice:F3}        balance = {(walletBalance - lastWalletBalance) / lastWalletBalance * 100:N4}%{suffix}");
                        }
                        lastWalletBalance = walletBalance;
                    }

                    int waitMilliseconds = (int)(binList_5m[0].Timestamp.Value - BitMEXApiHelper.ServerTime).TotalMilliseconds % 20000;
                    if (waitMilliseconds >= 0)
                    {
                        int waitSeconds = waitMilliseconds / 1000;
                        if (waitSeconds == 0)
                        {
                            waitSeconds = 20;
                            waitMilliseconds = 20000 - waitMilliseconds;
                        }
                        logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Sleeping {waitSeconds:N0} seconds ({apiHelper.RequestCount} requests) ...", ConsoleColor.DarkGray, false);
                        Thread.Sleep(waitMilliseconds);
                        //Logger.WriteWait($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Sleeping {waitSeconds:N0} seconds ({apiHelper.RequestCount} requests) ", waitSeconds, 1);
                        //Thread.Sleep(waitMilliseconds % 1000);
                    }
                    else
                    {
                        logger.WriteLine($"[{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Error: waitMilliseconds = {waitMilliseconds} < 0", ConsoleColor.Red);
                    }
                }
                catch (Exception ex)
                {
                    if (logger == null) logger = new Logger($"{BitMEXApiHelper.ServerTime:yyyy-MM-dd}");
                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]    {ex.Message}", ConsoleColor.Red);
                    logger.WriteFile(ex.ToString());
                    logger.WriteFile($"LastPlain4Sign = {BitMEXApiHelper.LastPlain4Sign}");
                    Thread.Sleep(30000);
                }
                lastLoopTime = DateTime.UtcNow;
            }
        }

    }
}