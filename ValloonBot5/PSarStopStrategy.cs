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
using Valloon.Utils;

/**
 * @author Valloon Present
 * @version 2022-02-10
 */
namespace Valloon.Trading
{
    public class PSarStopStrategy
    {
        private class ParamConfig
        {
            [JsonProperty("bin")]
            public int BinSize { get; set; }
            [JsonProperty("start")]
            public decimal PSarStart { get; set; }
            [JsonProperty("step")]
            public decimal PSarStep { get; set; }
            [JsonProperty("max")]
            public decimal PSarMax { get; set; }
            [JsonProperty("close")]
            public decimal CloseLimit { get; set; }
            [JsonProperty("stop")]
            public decimal StopLoss { get; set; }
        }

        public void Run(Config config = null)
        {
            int loopIndex = 0;
            DateTime? lastLoopTime = null;
            decimal lastWalletBalance = 0;
            decimal targetEntryPrice = 0;
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
                        string url = $"https://raw.githubusercontent.com/maksimg1002/_upload1002/main/bitmex-solusd-sar-limit-0418.json";
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
                            logger.WriteLine($"    <Position>    entry = {positionEntryPrice:F2} / {targetEntryPrice}    qty = {positionQty}    liq = {position.LiquidationPrice}    leverage = {leverage:F2}    {unrealisedPercent:N2} % / {nowLoss:N2} %", ConsoleColor.Green);
                        }
                    }

                    List<TradeBin> reversedBinList = new List<TradeBin>(binList_5m);
                    reversedBinList.Reverse();
                    List<TradeBin> binListConverted = BitMEXApiHelper.LoadBinListFrom5m($"{param.BinSize}m", reversedBinList);
                    var quoteList = IndicatorHelper.TradeBinToQuote(binListConverted);
                    var parabolicSarList = quoteList.GetParabolicSar(param.PSarStep, param.PSarMax, param.PSarStart).ToList();
                    var lastPSar = parabolicSarList.Last();
                    string trend = null;
                    if (lastPSar.Sar > quoteList.Last().High)
                        trend = "\\/ Bearish \t ";
                    else if (lastPSar.Sar < quoteList.Last().Low)
                        trend = "/\\ Bullish \t ";
                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  {trend}sar = {parabolicSarList[parabolicSarList.Count - 2].Sar:F4} / {lastPSar.Sar:F4} \t {parabolicSarList[parabolicSarList.Count - 2].IsReversal} / {lastPSar.IsReversal}", ConsoleColor.DarkGray);
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
                                if (order.Text.Contains("<STOP-CLOSE>") || order.Text.Contains("<LIMIT-CLOSE>"))
                                {
                                    cancelOrderList.Add(order.OrderID);
                                }
                            }
                            if (cancelOrderList.Count > 0)
                            {
                                var canceledOrders = apiHelper.CancelOrders(cancelOrderList);
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  {canceledOrders.Count} old CLOSE orders have been canceled.");
                            }
                        }

                    }

                    //{
                    //    List<string> cancelOrderList = new List<string>();
                    //    foreach (Order order in botOrderList)
                    //    {
                    //        if (order.Text.Contains("<STOP-OPEN>")) cancelOrderList.Add(order.OrderID);
                    //    }
                    //    if (cancelOrderList.Count > 0)
                    //    {
                    //        var canceledOrders = apiHelper.CancelOrders(cancelOrderList);
                    //        logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  {canceledOrders.Count} STOP-OPEN orders have been canceled.");
                    //    }
                    //}
                    if (positionQty > 0)
                    {
                        if (lastPSar.Sar.Value < quoteList.Last().Low)
                        {
                            {
                                decimal stopPrice = (int)Math.Floor(lastPSar.Sar.Value * 100) / 100m;
                                if (param.StopLoss > 0 && targetEntryPrice > 0)
                                {
                                    var stopLossPrice = Math.Floor(targetEntryPrice * (1 - param.StopLoss) * 100) / 100m;
                                    stopPrice = Math.Max(stopPrice, stopLossPrice);
                                }
                                Order oldStopOrder = null;
                                List<string> cancelOrderList = new List<string>();
                                foreach (Order order in botOrderList)
                                {
                                    if (order.Text.Contains("<STOP-CLOSE>"))
                                    {
                                        oldStopOrder = order;
                                        cancelOrderList.Add(order.OrderID);
                                    }
                                }
                                if (cancelOrderList.Count > 1 || oldStopOrder != null && oldStopOrder.Side != "Sell")
                                {
                                    var canceledOrders = apiHelper.CancelOrders(cancelOrderList);
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  {canceledOrders.Count} STOP-CLOSE orders have been canceled.");
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
                                        ExecInst = "LastPrice",
                                        Text = $"<BOT><STOP-CLOSE></BOT>",
                                    });
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New STOP-CLOSE order: qty = {positionQty}, price = {stopPrice}");
                                    logger.WriteFile("--- " + JObject.FromObject(stopOrder).ToString(Formatting.None));
                                }
                                else if (oldStopOrder.OrderQty != positionQty || oldStopOrder.StopPx != stopPrice)
                                {
                                    Order amendOrder = apiHelper.OrderAmend(new Order
                                    {
                                        OrderID = oldStopOrder.OrderID,
                                        OrderQty = positionQty,
                                        StopPx = stopPrice,
                                        ExecInst = "LastPrice",
                                        Text = $"<BOT><STOP-CLOSE></BOT>",
                                    });
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Amend STOP-CLOSE order: qty = {positionQty}, price = {stopPrice}");
                                    logger.WriteFile("--- " + JObject.FromObject(amendOrder).ToString(Formatting.None));
                                }
                            }
                            if (param.CloseLimit > 0)
                            {
                                var closeLimitPrice = Math.Ceiling(targetEntryPrice * (1 + param.CloseLimit) * 100) / 100m;
                                Order oldCloseOrder = null;
                                List<string> cancelOrderList = new List<string>();
                                foreach (Order order in botOrderList)
                                {
                                    if (order.Text.Contains("<LIMIT-CLOSE>"))
                                    {
                                        oldCloseOrder = order;
                                        cancelOrderList.Add(order.OrderID);
                                    }
                                }
                                if (cancelOrderList.Count > 1 || oldCloseOrder != null && oldCloseOrder.Side != "Sell")
                                {
                                    var canceledOrders = apiHelper.CancelOrders(cancelOrderList);
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  {canceledOrders.Count} LIMIT-CLOSE orders have been canceled.");
                                    oldCloseOrder = null;
                                }
                                if (oldCloseOrder == null)
                                {
                                    Order stopOrder = apiHelper.OrderNew(new Order
                                    {
                                        Symbol = symbol,
                                        Side = "Sell",
                                        OrderQty = positionQty,
                                        OrdType = "Limit",
                                        Price = closeLimitPrice,
                                        ExecInst = "ReduceOnly",
                                        Text = $"<BOT><LIMIT-CLOSE></BOT>",
                                    });
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New LIMIT-CLOSE order: qty = {positionQty}, price = {closeLimitPrice}");
                                    logger.WriteFile("--- " + JObject.FromObject(stopOrder).ToString(Formatting.None));
                                }
                                else if (oldCloseOrder.OrderQty != positionQty)
                                {
                                    Order amendOrder = apiHelper.OrderAmend(new Order
                                    {
                                        OrderID = oldCloseOrder.OrderID,
                                        OrderQty = positionQty,
                                        Price = closeLimitPrice,
                                        ExecInst = "ReduceOnly",
                                        Text = $"<BOT><LIMIT-CLOSE></BOT>",
                                    });
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Amend LIMIT-CLOSE close order: qty = {positionQty}, price = {closeLimitPrice}");
                                    logger.WriteFile("--- " + JObject.FromObject(amendOrder).ToString(Formatting.None));
                                }
                            }
                        }
                        else
                        {
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Wrong position: qty = {positionQty}");
                            if (param.StopLoss > 0)
                            {
                                decimal stopPrice = (int)Math.Floor(positionEntryPrice * (1 - param.StopLoss) * 100) / 100m;
                                Order oldStopOrder = null;
                                List<string> cancelOrderList = new List<string>();
                                foreach (Order order in botOrderList)
                                {
                                    if (order.Text.Contains("<STOP-CLOSE>"))
                                    {
                                        oldStopOrder = order;
                                        cancelOrderList.Add(order.OrderID);
                                    }
                                }
                                if (cancelOrderList.Count > 1 || oldStopOrder != null && oldStopOrder.Side != "Sell")
                                {
                                    var canceledOrders = apiHelper.CancelOrders(cancelOrderList);
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  {canceledOrders.Count} STOP-CLOSE orders have been canceled.");
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
                                        Text = $"<BOT><STOP-CLOSE></BOT>",
                                    });
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New STOP-CLOSE order: qty = {positionQty}, price = {stopPrice}");
                                    logger.WriteFile("--- " + JObject.FromObject(stopOrder).ToString(Formatting.None));
                                }
                                else if (oldStopOrder.OrderQty != positionQty)
                                {
                                    Order amendOrder = apiHelper.OrderAmend(new Order
                                    {
                                        OrderID = oldStopOrder.OrderID,
                                        OrderQty = positionQty,
                                        StopPx = stopPrice,
                                        ExecInst = "LastPrice,ReduceOnly",
                                        Text = $"<BOT><STOP-CLOSE></BOT>",
                                    });
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Amend STOP-CLOSE order: qty = {positionQty}, price = {stopPrice}");
                                    logger.WriteFile("--- " + JObject.FromObject(amendOrder).ToString(Formatting.None));
                                }
                            }
                            if (param.CloseLimit > 0)
                            {
                                var closeLimitPrice = Math.Ceiling(positionEntryPrice * (1 + param.CloseLimit) * 100) / 100m;
                                Order oldCloseOrder = null;
                                List<string> cancelOrderList = new List<string>();
                                foreach (Order order in botOrderList)
                                {
                                    if (order.Text.Contains("<LIMIT-CLOSE>"))
                                    {
                                        oldCloseOrder = order;
                                        cancelOrderList.Add(order.OrderID);
                                    }
                                }
                                if (cancelOrderList.Count > 1 || oldCloseOrder != null && oldCloseOrder.Side != "Sell")
                                {
                                    var canceledOrders = apiHelper.CancelOrders(cancelOrderList);
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  {canceledOrders.Count} LIMIT-CLOSE orders have been canceled.");
                                    oldCloseOrder = null;
                                }
                                if (oldCloseOrder == null)
                                {
                                    Order stopOrder = apiHelper.OrderNew(new Order
                                    {
                                        Symbol = symbol,
                                        Side = "Sell",
                                        OrderQty = positionQty,
                                        OrdType = "Limit",
                                        Price = closeLimitPrice,
                                        ExecInst = "ReduceOnly",
                                        Text = $"<BOT><LIMIT-CLOSE></BOT>",
                                    });
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New LIMIT-CLOSE order: qty = {positionQty}, price = {closeLimitPrice}");
                                    logger.WriteFile("--- " + JObject.FromObject(stopOrder).ToString(Formatting.None));
                                }
                                else if (oldCloseOrder.OrderQty != positionQty)
                                {
                                    Order amendOrder = apiHelper.OrderAmend(new Order
                                    {
                                        OrderID = oldCloseOrder.OrderID,
                                        OrderQty = positionQty,
                                        Price = closeLimitPrice,
                                        ExecInst = "ReduceOnly",
                                        Text = $"<BOT><LIMIT-CLOSE></BOT>",
                                    });
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Amend LIMIT-CLOSE close order: qty = {positionQty}, price = {closeLimitPrice}");
                                    logger.WriteFile("--- " + JObject.FromObject(amendOrder).ToString(Formatting.None));
                                }
                            }
                        }
                    }
                    else if (positionQty < 0)
                    {
                        if (lastPSar.Sar.Value > quoteList.Last().High)
                        {
                            {
                                decimal stopPrice = (int)Math.Ceiling(lastPSar.Sar.Value * 100) / 100m;
                                if (param.StopLoss > 0 && targetEntryPrice > 0)
                                {
                                    var stopLossPrice = Math.Ceiling(targetEntryPrice * (1 + param.StopLoss) * 100) / 100m;
                                    stopPrice = Math.Min(stopPrice, stopLossPrice);
                                }
                                Order oldStopOrder = null;
                                List<string> cancelOrderList = new List<string>();
                                foreach (Order order in botOrderList)
                                {
                                    if (order.Text.Contains("<STOP-CLOSE>"))
                                    {
                                        oldStopOrder = order;
                                        cancelOrderList.Add(order.OrderID);
                                    }
                                }
                                if (cancelOrderList.Count > 1 || oldStopOrder != null && oldStopOrder.Side != "Buy")
                                {
                                    var canceledOrders = apiHelper.CancelOrders(cancelOrderList);
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  {canceledOrders.Count} STOP-CLOSE orders have been canceled.");
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
                                        ExecInst = "LastPrice",
                                        Text = $"<BOT><STOP-CLOSE></BOT>",
                                    });
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New STOP-CLOSE order: qty = {-positionQty}, price = {stopPrice}");
                                    logger.WriteFile("--- " + JObject.FromObject(stopOrder).ToString(Formatting.None));
                                }
                                else if (oldStopOrder.OrderQty != -positionQty || oldStopOrder.StopPx != stopPrice)
                                {
                                    Order amendOrder = apiHelper.OrderAmend(new Order
                                    {
                                        OrderID = oldStopOrder.OrderID,
                                        OrderQty = -positionQty,
                                        StopPx = stopPrice,
                                        ExecInst = "LastPrice",
                                        Text = $"<BOT><STOP-CLOSE></BOT>",
                                    });
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Amend STOP-CLOSE order: qty = {-positionQty}, price = {stopPrice}");
                                    logger.WriteFile("--- " + JObject.FromObject(amendOrder).ToString(Formatting.None));
                                }
                            }
                            if (param.CloseLimit > 0)
                            {
                                var closeLimitPrice = Math.Floor(targetEntryPrice * (1 - param.CloseLimit) * 100) / 100m;
                                Order oldCloseOrder = null;
                                List<string> cancelOrderList = new List<string>();
                                foreach (Order order in botOrderList)
                                {
                                    if (order.Text.Contains("<LIMIT-CLOSE>"))
                                    {
                                        oldCloseOrder = order;
                                        cancelOrderList.Add(order.OrderID);
                                    }
                                }
                                if (cancelOrderList.Count > 1 || oldCloseOrder != null && oldCloseOrder.Side != "Buy")
                                {
                                    var canceledOrders = apiHelper.CancelOrders(cancelOrderList);
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  {canceledOrders.Count} LIMIT-CLOSE orders have been canceled.");
                                    oldCloseOrder = null;
                                }
                                if (oldCloseOrder == null)
                                {
                                    Order stopOrder = apiHelper.OrderNew(new Order
                                    {
                                        Symbol = symbol,
                                        Side = "Buy",
                                        OrderQty = -positionQty,
                                        OrdType = "Limit",
                                        Price = closeLimitPrice,
                                        ExecInst = "ReduceOnly",
                                        Text = $"<BOT><LIMIT-CLOSE></BOT>",
                                    });
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New LIMIT-CLOSE order: qty = {-positionQty}, price = {closeLimitPrice}");
                                    logger.WriteFile("--- " + JObject.FromObject(stopOrder).ToString(Formatting.None));
                                }
                                else if (oldCloseOrder.OrderQty != -positionQty)
                                {
                                    Order amendOrder = apiHelper.OrderAmend(new Order
                                    {
                                        OrderID = oldCloseOrder.OrderID,
                                        OrderQty = -positionQty,
                                        Price = closeLimitPrice,
                                        ExecInst = "ReduceOnly",
                                        Text = $"<BOT><LIMIT-CLOSE></BOT>",
                                    });
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Amend LIMIT-CLOSE close order: qty = {-positionQty}, price = {closeLimitPrice}");
                                    logger.WriteFile("--- " + JObject.FromObject(amendOrder).ToString(Formatting.None));
                                }
                            }
                        }
                        else
                        {
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Wrong position: qty = {positionQty}");
                            if (param.StopLoss > 0)
                            {
                                decimal stopPrice = (int)Math.Ceiling(positionEntryPrice * (1 + param.StopLoss) * 100) / 100m;
                                Order oldStopOrder = null;
                                List<string> cancelOrderList = new List<string>();
                                foreach (Order order in botOrderList)
                                {
                                    if (order.Text.Contains("<STOP-CLOSE>"))
                                    {
                                        oldStopOrder = order;
                                        cancelOrderList.Add(order.OrderID);
                                    }
                                }
                                if (cancelOrderList.Count > 1 || oldStopOrder != null && oldStopOrder.Side != "Buy")
                                {
                                    var canceledOrders = apiHelper.CancelOrders(cancelOrderList);
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  {canceledOrders.Count} STOP-CLOSE orders have been canceled.");
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
                                        Text = $"<BOT><STOP-CLOSE></BOT>",
                                    });
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New STOP-CLOSE order: qty = {-positionQty}, price = {stopPrice}");
                                    logger.WriteFile("--- " + JObject.FromObject(stopOrder).ToString(Formatting.None));
                                }
                                else if (oldStopOrder.OrderQty != -positionQty)
                                {
                                    Order amendOrder = apiHelper.OrderAmend(new Order
                                    {
                                        OrderID = oldStopOrder.OrderID,
                                        OrderQty = -positionQty,
                                        StopPx = stopPrice,
                                        ExecInst = "LastPrice,ReduceOnly",
                                        Text = $"<BOT><STOP-CLOSE></BOT>",
                                    });
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Amend STOP-CLOSE order: qty = {-positionQty}, price = {stopPrice}");
                                    logger.WriteFile("--- " + JObject.FromObject(amendOrder).ToString(Formatting.None));
                                }
                            }
                            if (param.CloseLimit > 0)
                            {
                                var closeLimitPrice = Math.Floor(positionEntryPrice * (1 - param.CloseLimit) * 100) / 100m;
                                Order oldCloseOrder = null;
                                List<string> cancelOrderList = new List<string>();
                                foreach (Order order in botOrderList)
                                {
                                    if (order.Text.Contains("<LIMIT-CLOSE>"))
                                    {
                                        oldCloseOrder = order;
                                        cancelOrderList.Add(order.OrderID);
                                    }
                                }
                                if (cancelOrderList.Count > 1 || oldCloseOrder != null && oldCloseOrder.Side != "Buy")
                                {
                                    var canceledOrders = apiHelper.CancelOrders(cancelOrderList);
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  {canceledOrders.Count} LIMIT-CLOSE orders have been canceled.");
                                    oldCloseOrder = null;
                                }
                                if (oldCloseOrder == null)
                                {
                                    Order stopOrder = apiHelper.OrderNew(new Order
                                    {
                                        Symbol = symbol,
                                        Side = "Buy",
                                        OrderQty = -positionQty,
                                        OrdType = "Limit",
                                        Price = closeLimitPrice,
                                        ExecInst = "ReduceOnly",
                                        Text = $"<BOT><LIMIT-CLOSE></BOT>",
                                    });
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New LIMIT-CLOSE order: qty = {-positionQty}, price = {closeLimitPrice}");
                                    logger.WriteFile("--- " + JObject.FromObject(stopOrder).ToString(Formatting.None));
                                }
                                else if (oldCloseOrder.OrderQty != -positionQty)
                                {
                                    Order amendOrder = apiHelper.OrderAmend(new Order
                                    {
                                        OrderID = oldCloseOrder.OrderID,
                                        OrderQty = -positionQty,
                                        Price = closeLimitPrice,
                                        ExecInst = "ReduceOnly",
                                        Text = $"<BOT><LIMIT-CLOSE></BOT>",
                                    });
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Amend LIMIT-CLOSE close order: qty = {-positionQty}, price = {closeLimitPrice}");
                                    logger.WriteFile("--- " + JObject.FromObject(amendOrder).ToString(Formatting.None));
                                }
                            }
                        }
                    }
                    //else
                    //{
                    //    logger.WriteLine($"        Invalid position qty: {positionQty}");
                    //}

                    if (BitMEXApiHelper.ServerTime.Hour > 3)
                    {
                        Order newStopOpenOrder = null;
                        if (lastPSar.Sar.Value > quoteList.Last().High && (config.BuyOrSell == 3 || config.BuyOrSell == 1))
                        {
                            targetEntryPrice = (int)Math.Ceiling(lastPSar.Sar.Value * 100) / 100m;
                            newStopOpenOrder = new Order
                            {
                                Symbol = BitMEXApiHelper.SYMBOL_SOLUSD,
                                Side = "Buy",
                                OrderQty = qty,
                                StopPx = targetEntryPrice,
                                OrdType = "Stop",
                                ExecInst = "LastPrice",
                                Text = $"<BOT><STOP-OPEN></BOT>"
                            };
                        }
                        else if (lastPSar.Sar.Value < quoteList.Last().Low && (config.BuyOrSell == 3 || config.BuyOrSell == 2))
                        {
                            targetEntryPrice = (int)Math.Floor(lastPSar.Sar.Value * 100) / 100m;
                            newStopOpenOrder = new Order
                            {
                                Symbol = BitMEXApiHelper.SYMBOL_SOLUSD,
                                Side = "Sell",
                                OrderQty = qty,
                                StopPx = targetEntryPrice,
                                OrdType = "Stop",
                                ExecInst = "LastPrice",
                                Text = $"<BOT><STOP-OPEN></BOT>"
                            };
                        }
                        else
                        {
                            targetEntryPrice = 0;
                        }
                        bool autoCancelAllOrders = positionQty == 0 && activeOrderList.Count == botOrderList.Count;
                        Order oldStopLimitOrder = null;
                        List<string> cancelOrderList = new List<string>();
                        foreach (Order order in botOrderList)
                        {
                            if (order.Text.Contains("<STOP-OPEN>"))
                            {
                                oldStopLimitOrder = order;
                                cancelOrderList.Add(order.OrderID);
                            }
                        }
                        if (cancelOrderList.Count > 1 || oldStopLimitOrder != null && newStopOpenOrder == null || newStopOpenOrder != null && oldStopLimitOrder != null && oldStopLimitOrder.Side != newStopOpenOrder.Side)
                        {
                            var canceledOrders = apiHelper.CancelOrders(cancelOrderList);
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  {canceledOrders.Count} STOP-OPEN orders have been canceled.");
                            oldStopLimitOrder = null;
                        }
                        if (newStopOpenOrder == null)
                        {
                            autoCancelAllOrders = false;
                        }
                        else if (oldStopLimitOrder == null)
                        {
                            Order resultOrder = apiHelper.OrderNew(newStopOpenOrder);
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New STOP-LIMIT order: qty = {qty}, price = {newStopOpenOrder.StopPx}");
                            logger.WriteFile("--- " + JObject.FromObject(resultOrder).ToString(Formatting.None));
                        }
                        else if (oldStopLimitOrder.OrderQty != qty || oldStopLimitOrder.Price != newStopOpenOrder.Price || oldStopLimitOrder.StopPx != newStopOpenOrder.StopPx)
                        {
                            Order amendOrder = new Order
                            {
                                OrderID = oldStopLimitOrder.OrderID,
                                OrderQty = newStopOpenOrder.OrderQty,
                                StopPx = newStopOpenOrder.StopPx,
                                ExecInst = newStopOpenOrder.ExecInst,
                                Text = newStopOpenOrder.Text,
                            };
                            Order resultOrder = apiHelper.OrderAmend(amendOrder);
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Amend STOP-LIMIT order: qty = {qty}, price = {newStopOpenOrder.StopPx}");
                            logger.WriteFile("--- " + JObject.FromObject(resultOrder).ToString(Formatting.None));
                        }
                        if (autoCancelAllOrders)
                        {
                            //apiHelper.OrderCancelAllAfter(5 * 60000);
                        }
                    }
                    else
                    {
                        logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Invalid time: {BitMEXApiHelper.ServerTime:HH:mm}", ConsoleColor.DarkGray);
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

                    int waitMilliseconds = (int)(binList_5m[0].Timestamp.Value - BitMEXApiHelper.ServerTime).TotalMilliseconds % 15000;
                    if (waitMilliseconds >= 0)
                    {
                        int waitSeconds = waitMilliseconds / 1000;
                        if (waitSeconds == 0)
                        {
                            waitSeconds = 15;
                            waitMilliseconds = 15000 - waitMilliseconds;
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