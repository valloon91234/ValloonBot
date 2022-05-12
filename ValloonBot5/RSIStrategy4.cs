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
using Valloon.Utils;

/**
 * @author Valloon Present
 * @version 2022-05-04
 */
namespace Valloon.Trading
{
    public class RSIStrategy4
    {
        private class ParamConfig
        {
            public int BinSize { get; set; }
            public int BuyOrSell { get; set; }
            public double BuyMinRSI { get; set; }
            public double BuyMaxRSI { get; set; }
            public decimal BuyCloseX { get; set; }
            public decimal BuyStopX { get; set; }
            public double SellMinRSI { get; set; }
            public double SellMaxRSI { get; set; }
            public decimal SellCloseX { get; set; }
            public decimal SellStopX { get; set; }
        }

        private static ParamConfig GetParamConfig(Logger logger = null)
        {
            string url = $"https://raw.githubusercontent.com/valloon91234/_shared/master/bitmex-solusd-rsi-0502.json";
            string paramText = HttpClient2.HttpGet(url);
            ParamConfig param = JsonConvert.DeserializeObject<ParamConfig>(paramText);
            if (logger != null)
            {
                logger.WriteLine($"\r\n[{BitMEXApiHelper.ServerTime:yyyy-MM-dd  HH:mm:ss}]  Param loaded.", ConsoleColor.Green);
                logger.WriteLine(JObject.FromObject(param).ToString(Formatting.Indented));
                logger.WriteLine();
            }
            return param;
        }


        public void Run(Config config = null)
        {
            int loopIndex = 0;
            DateTime? lastLoopTime = null;
            decimal lastWalletBalance = 0;
            TradeBin lastCandle = null;
            ParamConfig param = null;
            DateTime? lastParamTime = null;
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
                    if (param == null)
                    {
                        param = GetParamConfig(logger);
                        lastParamTime = BitMEXApiHelper.ServerTime;
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
                        if (order.Text.Contains("<BOT>") && order.Text.Contains("<R2>")) botOrderList.Add(order);
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
                    var reversedBinArray = binListConverted.ToArray();
                    double[] rsiArray = RSI.CalculateRSIValues(reversedBinArray, 14);
                    double lastRSI = rsiArray[rsiArray.Length - 2];
                    double rsi = rsiArray.Last();
                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  rsi = {lastRSI:F4}  /  {rsi:F4}", ConsoleColor.DarkGray);
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
                        List<string> cancelOrderList = new List<string>();
                        foreach (Order order in botOrderList)
                        {
                            cancelOrderList.Add(order.OrderID);
                        }
                        if (cancelOrderList.Count > 0)
                        {
                            var canceledOrders = apiHelper.CancelOrders(cancelOrderList);
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  {canceledOrders.Count} old bot orders have been canceled.", ConsoleColor.DarkGray);
                        }
#if LICENSE_MODE
                        if (serverTime.Year != 2022 || serverTime.Month != 2)
                        {
                            logger.WriteLine($"This bot is too old. Please contact support.  https://valloontrader.com", ConsoleColor.Green);
                            goto endLoop;
                        }
#endif
                        if (config.Exit > 0)
                        {
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  No order. (exit = {config.Exit})", ConsoleColor.DarkGray);
                            goto endLoop;
                        }
                    }
                    if (lastCandle != null && lastCandle.Timestamp.Value.Minute != reversedBinArray.Last().Timestamp.Value.Minute)
                    {
                        if ((param.BuyOrSell == 1 || param.BuyOrSell == 3) && lastRSI >= param.BuyMinRSI && lastRSI < param.BuyMaxRSI && reversedBinArray[reversedBinArray.Length - 3].Open.Value < reversedBinArray[reversedBinArray.Length - 3].Close.Value && reversedBinArray[reversedBinArray.Length - 2].Open.Value < reversedBinArray[reversedBinArray.Length - 2].Close.Value)
                        {
                            decimal entryPrice = reversedBinArray.Last().Open.Value;
                            {
                                Order newOrder = apiHelper.OrderNew(new Order
                                {
                                    Symbol = symbol,
                                    Side = "Buy",
                                    OrderQty = qty,
                                    OrdType = "Market",
                                    Text = $"<BOT><R2></BOT>",
                                });
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New market BUY order: qty = {qty}, price = {entryPrice}");
                                logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                            }
                            {
                                int tryCount = 0;
                                bool positionCreated = false;
                                while (tryCount < 60)
                                {
                                    position = apiHelper.GetPosition(symbol);
                                    if (position != null && position.CurrentQty.Value != 0)
                                    {
                                        positionCreated = true;
                                        break;
                                    }
                                    Thread.Sleep(tryCount < 20 ? 500 : 1000);
                                    tryCount++;
                                }
                                if (!positionCreated)
                                {
                                    logger.WriteLine($"    [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Position did not created: qty = {qty}", ConsoleColor.Red);
                                    Logger.WriteWait("", 60, 5, ConsoleColor.DarkGray);
                                    continue;
                                }
                                positionQty = position.CurrentQty.Value;
                                positionEntryPrice = position.AvgEntryPrice.Value;
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Position (try = {tryCount}): qty = {positionQty}, entry = {positionEntryPrice}, liq = {position.LiquidationPrice}");
                            }
                            {
                                decimal closePrice = Math.Floor((entryPrice + entryPrice * param.BuyCloseX) * 100) / 100m;
                                Order newOrder = apiHelper.OrderNew(new Order
                                {
                                    Symbol = symbol,
                                    Side = "Sell",
                                    OrderQty = qty,
                                    Price = closePrice,
                                    OrdType = "Limit",
                                    ExecInst = "ReduceOnly",
                                    Text = $"<BOT><R2><CLOSE></BOT>",
                                });
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New CLOSE order: qty = {qty}, price = {closePrice}");
                                logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                            }
                            {
                                decimal stopPrice = Math.Ceiling((entryPrice - entryPrice * param.BuyStopX) * 100) / 100m;
                                Order newOrder = apiHelper.OrderNew(new Order
                                {
                                    Symbol = symbol,
                                    Side = "Sell",
                                    OrderQty = qty,
                                    StopPx = stopPrice,
                                    OrdType = "Stop",
                                    ExecInst = "LastPrice,ReduceOnly",
                                    Text = $"<BOT><R2><STOP></BOT>",
                                });
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New STOP order: qty = {qty}, price = {stopPrice}");
                                logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                            }
                        }
                        else if ((param.BuyOrSell == 2 || param.BuyOrSell == 3) && lastRSI >= param.SellMinRSI && lastRSI < param.SellMaxRSI && reversedBinArray[reversedBinArray.Length - 3].Open.Value > reversedBinArray[reversedBinArray.Length - 3].Close.Value && reversedBinArray[reversedBinArray.Length - 2].Open.Value > reversedBinArray[reversedBinArray.Length - 2].Close.Value)
                        {
                            decimal entryPrice = reversedBinArray.Last().Open.Value;
                            {
                                Order newOrder = apiHelper.OrderNew(new Order
                                {
                                    Symbol = symbol,
                                    Side = "Sell",
                                    OrderQty = qty,
                                    OrdType = "Market",
                                    Text = $"<BOT><R2></BOT>",
                                });
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New market SELL order: qty = {qty}, price = {entryPrice}");
                                logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                            }
                            {
                                int tryCount = 0;
                                bool positionCreated = false;
                                while (tryCount < 60)
                                {
                                    position = apiHelper.GetPosition(symbol);
                                    if (position != null && position.CurrentQty.Value != 0)
                                    {
                                        positionCreated = true;
                                        break;
                                    }
                                    Thread.Sleep(tryCount < 20 ? 500 : 1000);
                                    tryCount++;
                                }
                                if (!positionCreated)
                                {
                                    logger.WriteLine($"    [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Position did not created: qty = {qty}", ConsoleColor.Red);
                                    Logger.WriteWait("", 60, 5, ConsoleColor.DarkGray);
                                    continue;
                                }
                                positionQty = position.CurrentQty.Value;
                                positionEntryPrice = position.AvgEntryPrice.Value;
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Position (try = {tryCount}): qty = {positionQty}, entry = {positionEntryPrice}, liq = {position.LiquidationPrice}");
                            }
                            {
                                decimal closePrice = Math.Floor((entryPrice - entryPrice * param.SellCloseX) * 100) / 100m;
                                Order newOrder = apiHelper.OrderNew(new Order
                                {
                                    Symbol = symbol,
                                    Side = "Buy",
                                    OrderQty = qty,
                                    Price = closePrice,
                                    OrdType = "Limit",
                                    ExecInst = "ReduceOnly",
                                    Text = $"<BOT><R2><CLOSE></BOT>",
                                });
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New close order: qty = {qty}, price = {closePrice}");
                                logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                            }
                            {
                                decimal stopPrice = Math.Ceiling((entryPrice + entryPrice * param.SellStopX) * 100) / 100m;
                                Order newOrder = apiHelper.OrderNew(new Order
                                {
                                    Symbol = symbol,
                                    Side = "Buy",
                                    OrderQty = qty,
                                    StopPx = stopPrice,
                                    OrdType = "Stop",
                                    ExecInst = "LastPrice,ReduceOnly",
                                    Text = $"<BOT><R2><STOP></BOT>",
                                });
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New stop order: qty = {qty}, price = {stopPrice}");
                                logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                            }
                        }
                        if (positionQty == 0)
                        {
                            decimal price = BitMEXApiHelper.ServerTime.Hour + BitMEXApiHelper.ServerTime.Minute / 60m;
                            if (price == 0) price = 24;
                            Order newOrder = apiHelper.OrderNew(new Order
                            {
                                Symbol = symbol,
                                Side = "Buy",
                                OrderQty = 1,
                                Price = price,
                                OrdType = "Limit",
                                ExecInst = "ParticipateDoNotInitiate",
                                Text = $"<BOT><B2><POST-ONLY></BOT>",
                            });
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New test order: qty = {1}, price = {price}", ConsoleColor.DarkGray);
                            apiHelper.OrderCancelAllAfter(25 * 60 * 1000);
                        }
                    }
                    else if (lastParamTime == null || lastParamTime.Value.Hour != BitMEXApiHelper.ServerTime.Hour)
                    {
                        param = GetParamConfig(logger);
                        lastParamTime = BitMEXApiHelper.ServerTime;
                    }
                    lastCandle = reversedBinArray.Last();

                endLoop:;
                    margin = apiHelper.GetMargin(BitMEXApiHelper.CURRENCY_XBt);
                    walletBalance = margin.WalletBalance.Value / 100000000m;
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

                    int waitMilliseconds = (int)(binList[0].Timestamp.Value - BitMEXApiHelper.ServerTime).TotalMilliseconds;
                    if (waitMilliseconds >= 0)
                    {
                        int waitSeconds = waitMilliseconds / 1000;
                        if (waitSeconds > 10)
                            waitSeconds -= 10;
                        Logger.WriteWait($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Sleeping {waitSeconds:N0} seconds ({apiHelper.RequestCount} requests) ", waitSeconds, 15, ConsoleColor.DarkGray);
                        Thread.Sleep(waitMilliseconds % 1000);
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