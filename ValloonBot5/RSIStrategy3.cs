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
 * @version 2022-02-10
 */
namespace Valloon.Trading
{
    public class RSIStrategy3
    {
        private class ParamConfig
        {
            public int BinSize { get; set; }
            public int Window { get; set; }
            public double UpperDiff { get; set; }
            public double LowerDiff { get; set; }
            public double UpperLimit { get; set; }
            public double LowerLimit { get; set; }
        }

        public void Run(Config config = null)
        {
            int loopIndex = 0;
            DateTime? lastLoopTime = null;
            decimal lastWalletBalance = 0;
            ParamConfig param = null;
            Logger logger = null;
            DateTime fileModifiedTime = File.GetLastWriteTimeUtc(Assembly.GetExecutingAssembly().Location);
            double upperTopRSI = 0, lowerTopRSI = 100;
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
                        string url = $"https://raw.githubusercontent.com/maksimg1002/_upload1002/main/sol_0407.json";
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
                    var reversedBinArray = binListConverted.ToArray();
                    double[] rsiArray = RSI.CalculateRSIValues(reversedBinArray, param.Window);
                    double rsi = rsiArray[rsiArray.Length - 1];
                    TradeBin[] array2 = (TradeBin[])reversedBinArray.Clone();
                    {
                        array2[array2.Length - 1].Close = array2[array2.Length - 1].High;
                        double[] rsiArray2 = RSI.CalculateRSIValues(array2, param.Window);
                        double rsi2 = rsiArray2[rsiArray2.Length - 1];
                        if (rsi2 > upperTopRSI) upperTopRSI = rsi2;
                    }
                    {
                        array2[array2.Length - 1].Close = array2[array2.Length - 1].Low;
                        double[] rsiArray2 = RSI.CalculateRSIValues(array2, param.Window);
                        double rsi2 = rsiArray2[rsiArray2.Length - 1];
                        if (rsi2 < lowerTopRSI) lowerTopRSI = rsi2;
                    }
                    double upperStopRSI = upperTopRSI - param.UpperDiff * (upperTopRSI / 100);
                    double lowerStopRSI = lowerTopRSI + param.LowerDiff * (1 - lowerTopRSI / 100);
                    decimal lastClose = Math.Round(array2[array2.Length - 1].Close.Value * 10) / 10m;
                    decimal upperClosePrice, lowerClosePrice, upperStopPrice, lowerStopPrice;
                    {
                        array2[array2.Length - 1].Close = lastClose;
                        while (true)
                        {
                            double[] rsiArray2 = RSI.CalculateRSIValues(array2, param.Window);
                            double rsi2 = rsiArray2[rsiArray2.Length - 1];
                            if (rsi2 >= param.UpperLimit)
                            {
                                upperClosePrice = array2[array2.Length - 1].Close.Value;
                                break;
                            }
                            array2[array2.Length - 1].Close += .1m;
                        }
                    }
                    {
                        array2[array2.Length - 1].Close = lastClose;
                        while (true)
                        {
                            double[] rsiArray2 = RSI.CalculateRSIValues(array2, param.Window);
                            double rsi2 = rsiArray2[rsiArray2.Length - 1];
                            if (rsi2 <= param.LowerLimit)
                            {
                                lowerClosePrice = array2[array2.Length - 1].Close.Value;
                                break;
                            }
                            array2[array2.Length - 1].Close -= .1m;
                        }
                    }
                    {
                        array2[array2.Length - 1].Close = lastClose;
                        while (true)
                        {
                            double[] rsiArray2 = RSI.CalculateRSIValues(array2, param.Window);
                            double rsi2 = rsiArray2[rsiArray2.Length - 1];
                            if (rsi2 < upperStopRSI)
                            {
                                upperStopPrice = array2[array2.Length - 1].Close.Value + .1m;
                                break;
                            }
                            array2[array2.Length - 1].Close -= .1m;
                        }
                    }
                    {
                        array2[array2.Length - 1].Close = lastClose;
                        while (true)
                        {
                            double[] rsiArray2 = RSI.CalculateRSIValues(array2, param.Window);
                            double rsi2 = rsiArray2[rsiArray2.Length - 1];
                            if (rsi2 > lowerStopRSI)
                            {
                                lowerStopPrice = array2[array2.Length - 1].Close.Value - .1m;
                                break;
                            }
                            array2[array2.Length - 1].Close += .1m;
                        }
                    }
                    bool sellSignal = positionQty >= 0 && (rsi >= param.UpperLimit || rsi < upperStopRSI);
                    bool buySignal = positionQty <= 0 && (rsi <= param.LowerLimit || rsi > lowerStopRSI);
                    logger.WriteLine($"        rsi ({param.Window}) = {rsiArray[rsiArray.Length - 3]:F4}  /  {rsiArray[rsiArray.Length - 2]:F4}  /  {rsiArray[rsiArray.Length - 1]:F4} \t upper = {upperTopRSI:F2} / {upperStopRSI:F2} / {sellSignal} \t lower = {lowerTopRSI:F2} / {lowerStopRSI:F2} / {buySignal}", ConsoleColor.DarkGray);
                    logger.WriteLine($"        upper = {upperClosePrice}  /  {upperStopPrice}, lowerClosePrice, upperStopPrice, lowerStopPrice", ConsoleColor.DarkGray);
                    if (sellSignal) upperTopRSI = 0;
                    if (buySignal) lowerTopRSI = 100;
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
                    bool exit = false;
#if LICENSE_MODE
                    if (serverTime.Year != 2022 || serverTime.Month != 2)
                    {
                        exit = true;
                    }
#endif
                    if (positionQty == 0)
                    {
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
                    }
                    {
                        bool needUpperClose = false, needLowerClose = false, needUpperStop = false, needLowerStop = false;
                        int closeQty = 0, stopQty = 0;
                        if (positionQty == 0)
                        {
                            if (config.BuyOrSell == 1)
                            {
                                needLowerStop = true;
                            }
                            else if (config.BuyOrSell == 2)
                            {
                                needUpperStop = true;
                            }
                            else if (config.BuyOrSell == 3)
                            {
                                needUpperStop = true;
                                needLowerStop = true;
                            }
                            stopQty = qty;
                        }
                        else if (positionQty > 0)
                        {
                            if (rsi >= param.LowerLimit)
                            {
                                needUpperClose = true;
                                needUpperStop = true;
                                closeQty = positionQty;
                                if (closeQty > qty * 1.5) closeQty = qty;
                                if (exit || config.BuyOrSell == 1)
                                {
                                    stopQty = closeQty;
                                }
                                else if (config.BuyOrSell == 2 || config.BuyOrSell == 3)
                                {
                                    stopQty = closeQty + qty;
                                }
                            }
                            else
                            {
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  positionQty > 0, rsi < {param.LowerLimit}");
                            }
                        }
                        else if (positionQty < 0)
                        {
                            if (rsi <= param.UpperLimit)
                            {
                                needLowerClose = true;
                                needLowerStop = true;
                                closeQty = -positionQty;
                                if (closeQty > qty * 1.5) closeQty = qty;
                                if (exit || config.BuyOrSell == 2)
                                {
                                    stopQty = closeQty;
                                }
                                else if (config.BuyOrSell == 1 || config.BuyOrSell == 3)
                                {
                                    stopQty = closeQty + qty;
                                }
                            }
                            else
                            {
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  positionQty < 0, rsi > {param.UpperLimit}");
                            }
                        }

                        Order oldUpperCloseOrder = null, oldUpperStopOrder = null, oldLowerCloseOrder = null, oldLowerStopOrder = null;
                        foreach (Order order in botOrderList)
                        {
                            string text = order.Text;
                            if (text.Contains("<UPPER-CLOSE>"))
                                oldUpperCloseOrder = order;
                            if (text.Contains("<UPPER-STOP>"))
                                oldUpperStopOrder = order;
                            if (text.Contains("<LOWER-CLOSE>"))
                                oldLowerCloseOrder = order;
                            if (text.Contains("<LOWER-STOP>"))
                                oldLowerStopOrder = order;
                        }
                        if (needUpperClose)
                        {
                            if (oldUpperCloseOrder == null)
                            {
                                Order newOrder = apiHelper.OrderNew(new Order
                                {
                                    Symbol = symbol,
                                    Side = "Sell",
                                    OrderQty = closeQty,
                                    Price = upperClosePrice,
                                    OrdType = "Limit",
                                    Text = $"<BOT><UPPER-CLOSE></BOT>",
                                });
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New UPPER CLOSE order: qty = {closeQty}, price = {upperClosePrice}");
                                logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                            }
                            else if (oldUpperCloseOrder.OrderQty != closeQty || oldUpperCloseOrder.Price != upperClosePrice)
                            {
                                Order amendOrder = apiHelper.OrderAmend(new Order
                                {
                                    OrderID = oldUpperCloseOrder.OrderID,
                                    OrderQty = closeQty,
                                    Price = upperClosePrice,
                                    Text = $"<BOT><UPPER-CLOSE></BOT>",
                                });
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Amend UPPER CLOSE order: qty = {closeQty}, price = {upperClosePrice}");
                                logger.WriteFile("--- " + JObject.FromObject(amendOrder).ToString(Formatting.None));
                            }
                        }
                        if (needUpperStop)
                        {
                            if (oldUpperStopOrder == null)
                            {
                                Order newOrder = apiHelper.OrderNew(new Order
                                {
                                    Symbol = symbol,
                                    Side = "Sell",
                                    OrderQty = stopQty,
                                    StopPx = upperStopPrice,
                                    OrdType = "Stop",
                                    ExecInst = "LastPrice",
                                    Text = $"<BOT><UPPER-STOP></BOT>",
                                });
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New UPPER STOP order: qty = {stopQty}, price = {upperStopPrice}");
                                logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                            }
                            else if (oldUpperStopOrder.OrderQty != stopQty || oldUpperStopOrder.StopPx != upperStopPrice)
                            {
                                Order amendOrder = apiHelper.OrderAmend(new Order
                                {
                                    OrderID = oldUpperStopOrder.OrderID,
                                    OrderQty = stopQty,
                                    StopPx = upperStopPrice,
                                    ExecInst = "LastPrice",
                                    Text = $"<BOT><UPPER-STOP></BOT>",
                                });
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Amend UPPER STOP order: qty = {stopQty}, price = {upperStopPrice}");
                                logger.WriteFile("--- " + JObject.FromObject(amendOrder).ToString(Formatting.None));
                            }
                        }
                        if (needLowerClose)
                        {
                            if (oldLowerCloseOrder == null)
                            {
                                Order newOrder = apiHelper.OrderNew(new Order
                                {
                                    Symbol = symbol,
                                    Side = "Buy",
                                    OrderQty = closeQty,
                                    Price = lowerClosePrice,
                                    OrdType = "Limit",
                                    Text = $"<BOT><LOWER-CLOSE></BOT>",
                                });
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New LOWER LIMIT order: qty = {closeQty}, price = {lowerClosePrice}");
                                logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                            }
                            else if (oldLowerCloseOrder.OrderQty != closeQty || oldLowerCloseOrder.Price != lowerClosePrice)
                            {
                                Order amendOrder = apiHelper.OrderAmend(new Order
                                {
                                    OrderID = oldLowerCloseOrder.OrderID,
                                    OrderQty = closeQty,
                                    Price = lowerClosePrice,
                                    Text = $"<BOT><LOWER-CLOSE></BOT>",
                                });
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Amend LOWER LIMIT order: qty = {closeQty}, price = {lowerClosePrice}");
                                logger.WriteFile("--- " + JObject.FromObject(amendOrder).ToString(Formatting.None));
                            }
                        }
                        if (needLowerStop)
                        {
                            if (oldLowerStopOrder == null)
                            {
                                Order newOrder = apiHelper.OrderNew(new Order
                                {
                                    Symbol = symbol,
                                    Side = "Buy",
                                    OrderQty = stopQty,
                                    StopPx = lowerStopPrice,
                                    OrdType = "Stop",
                                    ExecInst = "LastPrice",
                                    Text = $"<BOT><LOWER-STOP></BOT>",
                                });
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New LOWER STOP buy order: qty = {stopQty}, price = {lowerStopPrice}");
                                logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                            }
                            else if (oldLowerStopOrder.OrderQty != stopQty || oldLowerStopOrder.StopPx != lowerStopPrice)
                            {
                                Order amendOrder = apiHelper.OrderAmend(new Order
                                {
                                    OrderID = oldLowerStopOrder.OrderID,
                                    OrderQty = stopQty,
                                    StopPx = lowerStopPrice,
                                    ExecInst = "LastPrice",
                                    Text = $"<BOT><LOWER-STOP></BOT>",
                                });
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Amend LOWER STOP buy order: qty = {stopQty}, price = {lowerStopPrice}");
                                logger.WriteFile("--- " + JObject.FromObject(amendOrder).ToString(Formatting.None));
                            }
                        }
                    }

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