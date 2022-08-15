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
using Valloon.Indicators;
using Valloon.Utils;
using SmaResult = Skender.Stock.Indicators.SmaResult;

/**
 * @author Valloon Present
 * @version 2022-02-10
 */
namespace Valloon.Trading
{
    public class PSarSmaStrategy
    {
        private class ParamConfig
        {
            [JsonProperty("bin")]
            public int BinSize { get; set; }
            [JsonProperty("sma")]
            public int SmaLength { get; set; }
            [JsonProperty("diff")]
            public decimal SmaDiff { get; set; }
            [JsonProperty("start")]
            public decimal PSarStart { get; set; }
            [JsonProperty("step")]
            public decimal PSarStep { get; set; }
            [JsonProperty("max")]
            public decimal PSarMax { get; set; }
        }

        public void Run(Config config = null)
        {
            int loopIndex = 0;
            DateTime? lastLoopTime = null;
            decimal lastWalletBalance = 0;
            ParamConfig param = null;
            Logger logger = null;
            DateTime fileModifiedTime = File.GetLastWriteTimeUtc(Assembly.GetExecutingAssembly().Location);
            Skender.Stock.Indicators.ParabolicSarResult lastPSar = null;
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
                        string url = $"https://raw.githubusercontent.com/maksimg1002/_upload1002/main/bitmex-solusd-sar-0410.json";
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
                    Margin margin = apiHelper.GetMargin(BitMEXApiHelper.CURRENCY_XBt);
                    decimal walletBalance = margin.WalletBalance.Value / 100000000m;
                    List<Order> activeOrderList = apiHelper.GetActiveOrders(symbol);
                    List<TradeBin> binList = apiHelper.GetBinList("5m", false, symbol, 1000, null, true);
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

                    List<TradeBin> reversedBinList = new List<TradeBin>(binList);
                    reversedBinList.Reverse();
                    List<TradeBin> binListConverted = BitMEXApiHelper.LoadBinListFrom5m($"{param.BinSize}m", reversedBinList);
                    var quoteList = IndicatorHelper.ToQuote(binListConverted);
                    List<SmaResult> smaList = null;
                    if (param.SmaLength > 0) quoteList.GetSma(param.SmaLength).ToList();
                    var parabolicSarList = quoteList.GetParabolicSar(param.PSarStep, param.PSarMax, param.PSarStart).ToList();
                    string trend = null;
                    if (parabolicSarList[parabolicSarList.Count - 1].Sar > quoteList[quoteList.Count - 1].Open)
                        trend = "\\/ Bearish \t ";
                    else if (parabolicSarList[parabolicSarList.Count - 1].Sar < quoteList[quoteList.Count - 1].Open)
                        trend = "/\\ Bullish \t ";
                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  {trend}sma ({param.SmaLength}) = {(smaList == null ? 0 : smaList[smaList.Count - 2].Sma):F4} / {(smaList == null ? 0 : smaList[smaList.Count - 1].Sma):F4} \t psar = {parabolicSarList[parabolicSarList.Count - 2].Sar:F4} / {parabolicSarList[parabolicSarList.Count - 1].Sar:F4} \t {parabolicSarList[parabolicSarList.Count - 2].IsReversal} / {parabolicSarList[parabolicSarList.Count - 1].IsReversal}", ConsoleColor.DarkGray);
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
                    decimal stopPrice = Math.Round(parabolicSarList[parabolicSarList.Count - 1].Sar.Value * 100) / 100m;
                    if (positionQty == 0)
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
#if LICENSE_MODE
                        if (serverTime.Year != 2022 || serverTime.Month != 2)
                        {
                            logger.WriteLine($"This bot is too old. Please contact support.  https://valloontrader.com", ConsoleColor.Green);
                            goto endLoop;
                        }
#endif
                        if (config.Exit == 1)
                        {
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  No order. (exit = {config.Exit})", ConsoleColor.DarkGray);
                            goto endLoop;
                        }
                        if (lastPSar != null && !lastPSar.IsReversal.Value && parabolicSarList[parabolicSarList.Count - 1].IsReversal.Value)
                        {
                            if (parabolicSarList[parabolicSarList.Count - 2].Sar < quoteList[quoteList.Count - 1].Open && parabolicSarList[parabolicSarList.Count - 1].Sar >= quoteList[quoteList.Count - 1].Open && (smaList == null || parabolicSarList[parabolicSarList.Count - 1].Sar < smaList[smaList.Count - 1].Sma * (1 - param.SmaDiff)))
                            {
                                {
                                    Order newOrder = apiHelper.OrderNew(new Order
                                    {
                                        Symbol = symbol,
                                        Side = "Sell",
                                        OrderQty = qty,
                                        OrdType = "Market",
                                        Text = $"<BOT><OPEN></BOT>",
                                    });
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New MARKET sell order: qty = {qty}, price = {quoteList[quoteList.Count - 1].Open}");
                                    logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                                }
                                {
                                    Order stopOrder = apiHelper.OrderNew(new Order
                                    {
                                        Symbol = symbol,
                                        Side = "Buy",
                                        OrderQty = qty,
                                        StopPx = stopPrice,
                                        OrdType = "Stop",
                                        ExecInst = "LastPrice,ReduceOnly",
                                        Text = $"<BOT><STOP></BOT>",
                                    });
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New STOP close order: qty = {qty}, price = {stopPrice}");
                                    logger.WriteFile("--- " + JObject.FromObject(stopOrder).ToString(Formatting.None));
                                }
                                positionQty = -qty;
                            }
                            else if (parabolicSarList[parabolicSarList.Count - 2].Sar > quoteList[quoteList.Count - 1].Open && parabolicSarList[parabolicSarList.Count - 1].Sar <= quoteList[quoteList.Count - 1].Open && (smaList == null || parabolicSarList[parabolicSarList.Count - 1].Sar > smaList[smaList.Count - 1].Sma * (1 + param.SmaDiff)))
                            {
                                {
                                    Order newOrder = apiHelper.OrderNew(new Order
                                    {
                                        Symbol = symbol,
                                        Side = "Buy",
                                        OrderQty = qty,
                                        OrdType = "Market",
                                        Text = $"<BOT><OPEN></BOT>",
                                    });
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New MARKET buy order: qty = {qty}, price = {quoteList[quoteList.Count - 1].Open}");
                                    logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                                }
                                {
                                    Order stopOrder = apiHelper.OrderNew(new Order
                                    {
                                        Symbol = symbol,
                                        Side = "Sell",
                                        OrderQty = qty,
                                        StopPx = stopPrice,
                                        OrdType = "Stop",
                                        ExecInst = "LastPrice,ReduceOnly",
                                        Text = $"<BOT><STOP></BOT>",
                                    });
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New STOP close order: qty = {qty}, price = {stopPrice}");
                                    logger.WriteFile("--- " + JObject.FromObject(stopOrder).ToString(Formatting.None));
                                }
                                positionQty = qty;
                            }
                        }
                    }
                    else if (positionQty > 0)
                    {
                        if (lastPSar != null && !lastPSar.IsReversal.Value && parabolicSarList[parabolicSarList.Count - 1].IsReversal.Value && parabolicSarList[parabolicSarList.Count - 2].Sar < quoteList[quoteList.Count - 1].Open && parabolicSarList[parabolicSarList.Count - 1].Sar >= quoteList[quoteList.Count - 1].Open)
                        {
                            Order newOrder = apiHelper.OrderNew(new Order
                            {
                                Symbol = symbol,
                                Side = "Sell",
                                OrderQty = positionQty,
                                OrdType = "Market",
                                ExecInst = "ReduceOnly",
                                Text = $"<BOT><CLOSE></BOT>",
                            });
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New MARKET close order: qty = {positionQty}, price = {quoteList[quoteList.Count - 1].Open}");
                            logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
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
                            }
                            positionQty = 0;
                        }
                        else if (parabolicSarList[parabolicSarList.Count - 1].Sar < quoteList[quoteList.Count - 1].Open)
                        {
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
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Wrong position: qty = {-positionQty}");
                        }
                    }
                    else if (positionQty < 0)
                    {
                        if (lastPSar != null && !lastPSar.IsReversal.Value && parabolicSarList[parabolicSarList.Count - 1].IsReversal.Value && parabolicSarList[parabolicSarList.Count - 2].Sar > quoteList[quoteList.Count - 1].Open && parabolicSarList[parabolicSarList.Count - 1].Sar <= quoteList[quoteList.Count - 1].Open)
                        {
                            Order newOrder = apiHelper.OrderNew(new Order
                            {
                                Symbol = symbol,
                                Side = "Buy",
                                OrderQty = -positionQty,
                                OrdType = "Market",
                                ExecInst = "ReduceOnly",
                                Text = $"<BOT><CLOSE></BOT>",
                            });
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New MARKET close order: qty = {-positionQty}, price = {quoteList[quoteList.Count - 1].Open}");
                            logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
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
                            }
                            positionQty = 0;
                        }
                        else if (parabolicSarList[parabolicSarList.Count - 1].Sar > quoteList[quoteList.Count - 1].Open)
                        {
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

                endLoop:;
                    lastPSar = parabolicSarList[parabolicSarList.Count - 1];
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

                    int waitMilliseconds = (int)(binList[0].Timestamp.Value - BitMEXApiHelper.ServerTime).TotalMilliseconds % 30000;
                    if (waitMilliseconds >= 0)
                    {
                        int waitSeconds = waitMilliseconds / 1000;
                        if (waitSeconds == 0)
                        {
                            waitSeconds = 30;
                            waitMilliseconds = 30000 - waitMilliseconds;
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