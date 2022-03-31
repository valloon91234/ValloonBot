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

/**
 * @author Valloon Present
 * @version 2022-02-10
 */
namespace Valloon.Trading
{
    public class BinaryStrategy
    {

        public void Run(Config config = null)
        {
            const string SYMBOL = BitMEXApiHelper.SYMBOL_XBTUSD;
            int loopIndex = 0;
            DateTime? lastLoopTime = null;
            TradeBin lastCandle = null;
            decimal lastWalletBalance = 0;
            bool waitingPositionClosed = true;
            Logger logger = null;
            DateTime fileModifiedTime = File.GetLastWriteTimeUtc(Assembly.GetExecutingAssembly().Location);
            while (true)
            {
                try
                {
                    DateTime currentLoopTime = DateTime.UtcNow;
                    config = Config.Load(out bool configUpdated, lastLoopTime != null && lastLoopTime.Value.Day != currentLoopTime.Day);
                    BitMEXApiHelper apiHelper = new BitMEXApiHelper(config.ApiKey, config.ApiSecret, config.TestnetMode);
                    Instrument instrumentBTC = apiHelper.GetInstrument(BitMEXApiHelper.SYMBOL_XBTUSD);
                    decimal btcPrice = instrumentBTC.LastPrice.Value;
                    logger = new Logger($"{BitMEXApiHelper.ServerTime:yyyy-MM-dd}");
                    if (configUpdated)
                    {
                        loopIndex = 0;
                        logger.WriteLine($"\r\n[{BitMEXApiHelper.ServerTime:yyyy-MM-dd  HH:mm:ss}]  Config loaded.", ConsoleColor.Green);
                        logger.WriteLine(JObject.FromObject(config).ToString(Formatting.Indented));
                        logger.WriteLine();
                    }
                    Margin margin = apiHelper.GetMargin(BitMEXApiHelper.CURRENCY_XBt);
                    decimal walletBalance = margin.WalletBalance.Value / 100000000m;
                    List<Order> activeOrderList = apiHelper.GetActiveOrders(SYMBOL);
                    List<TradeBin> binList = apiHelper.GetBinList("5m", false, SYMBOL, 1000, null, true);

                    Instrument instrument = apiHelper.GetInstrument(SYMBOL);
                    decimal lastPrice = instrument.LastPrice.Value;
                    decimal markPrice = instrument.MarkPrice.Value;
                    List<Order> botOrderList = new List<Order>();
                    foreach (Order order in activeOrderList)
                        if (order.Text.Contains("<BOT>")) botOrderList.Add(order);
                    {
                        decimal unavailableMarginPercent = 100m * (margin.WalletBalance.Value - margin.AvailableMargin.Value) / margin.WalletBalance.Value;
                        logger.WriteLine($"[{BitMEXApiHelper.ServerTime:yyyy-MM-dd  HH:mm:ss fff}  ({++loopIndex})]    $ {lastPrice:F2}  /  $ {markPrice:F3}  /  {binList[1].Volume:N0}  /  {binList[0].Volume:N0}    {walletBalance:N8} XBT    {botOrderList.Count} / {activeOrderList.Count} / {unavailableMarginPercent:N2} %", ConsoleColor.White);
                    }
                    Position position = apiHelper.GetPosition(SYMBOL);
                    decimal positionEntryPrice = 0;
                    int positionQty = 0;
                    {
                        string consoleTitle = $"$ {lastPrice:N2}  /  {walletBalance:N8} XBT";
                        if (position != null && position.CurrentQty.Value != 0) consoleTitle += "  /  1 Position";
                        consoleTitle += $"  <{config.Username}>  |   {Config.APP_NAME}  v{fileModifiedTime:yyyy.MM.dd HH:mm:ss}   by  ValloonTrader.com";
                        Console.Title = consoleTitle;
                        if (position != null && position.CurrentQty.Value != 0)
                        {
                            positionEntryPrice = position.AvgEntryPrice.Value;
                            positionQty = position.CurrentQty.Value;
                            decimal unrealisedPercent = 100m * position.UnrealisedPnl.Value / margin.WalletBalance.Value;
                            decimal nowLoss = 100m * (position.AvgEntryPrice.Value - lastPrice) / (position.LiquidationPrice.Value - position.AvgEntryPrice.Value);
                            logger.WriteFile($"        wallet_balance = {margin.WalletBalance}    unrealised_pnl = {position.UnrealisedPnl}    {unrealisedPercent:N2} %    leverage = {position.Leverage}");
                            logger.WriteLine($"    <Position>    entry = {positionEntryPrice:F2}    qty = {positionQty}    liq = {position.LiquidationPrice}    {unrealisedPercent:N2} % / {nowLoss:N2} %", ConsoleColor.Green);
                        }
                    }

                    int bbLength = 7;
                    decimal lastSD, lastSMA;
                    {
                        int candleCount = bbLength;
                        double[] closeArray = new double[candleCount];
                        double[] sd2Array = new double[candleCount];
                        for (int i = 0; i < candleCount; i++)
                        {
                            closeArray[i] = (double)binList[candleCount - i].Close.Value;
                        }
                        double movingAverage = closeArray.Average();
                        for (int i = 0; i < candleCount; i++)
                        {
                            sd2Array[i] = Math.Pow(closeArray[i] - movingAverage, 2);
                        }
                        lastSD = (decimal)Math.Pow(sd2Array.Average(), 0.5d);
                        lastSMA = (decimal)movingAverage;
                    }

                    double lastRSI;
                    {
                        List<TradeBin> reversedBinList = new List<TradeBin>(binList);
                        reversedBinList.Reverse();
                        double[] rsiArray = RSI.CalculateRSIValues(reversedBinList.ToArray(), 14);
                        int rsiArrayLength = rsiArray.Length;
                        lastRSI = rsiArray[rsiArrayLength - 2];
                    }

                    if (positionQty == 0)
                    {
                        waitingPositionClosed = false;
                        if (lastCandle != null && lastCandle.Timestamp.Value.Minute != binList[0].Timestamp.Value.Minute)
                        {
                            List<string> cancelOrderList = new List<string>();
                            foreach (Order order in botOrderList)
                            {
                                cancelOrderList.Add(order.OrderID);
                            }
                            if (cancelOrderList.Count > 0)
                            {
                                int canceledCount = apiHelper.CancelOrders(cancelOrderList).Count;
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  {canceledCount} old orders have been canceled.");
                            }

                            bool exit = false;
#if LICENSE_MODE
                            if (serverTime.Year != 2022 || serverTime.Month != 2)
                            {
                                logger.WriteLine($"This bot is too old. Please contact support.  https://valloontrader.com", ConsoleColor.Green);
                                exist = true;
                            } else 
#endif
                            if (config.Exit > 0)
                            {
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  No order. (exit = {config.Exit})", ConsoleColor.DarkGray);
                                exit = true;
                            }
                            else if (config.Leverage == 0)
                            {
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  No order. (qty = {config.Leverage})", ConsoleColor.DarkGray);
                                exit = true;
                            }
                            if (exit)
                            {
                                Logger.WriteWait("", 60, 5);
                                continue;
                            }

                            List<Order> lastFilledOrders = apiHelper.GetFilledOrders(SYMBOL);
                            Order lastFilledStopOrder = null;
                            foreach (Order order in lastFilledOrders)
                            {
                                if (order.OrdType == "Stop" && order.Text.Contains("<STOP>"))
                                {
                                    lastFilledStopOrder = order;
                                    break;
                                }
                            }
                            if (lastFilledStopOrder != null && (BitMEXApiHelper.ServerTime - lastFilledStopOrder.Timestamp.Value).TotalMinutes < bbLength * 5)
                            {
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  stop-market order filled on {lastFilledStopOrder.TransactTime.Value:yyyy-MM-dd HH:mm:ss}");
                            }
                            else
                            {
                                int orderQty = BitMEXApiHelper.FixQty((int)(margin.WalletBalance.Value * btcPrice * config.Leverage / 100000000));
                                decimal limitPrice = lastSMA * (1 + .0075m);
                                decimal closePrice = limitPrice * (1 - .0115m);
                                decimal stopPrice = limitPrice * (1 + .013m);
                                if (limitPrice < lastPrice)
                                {
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  limitPrice > lastPrice    limitPrice = {limitPrice}");
                                }
                                else if (limitPrice / closePrice < 1.0005m)
                                {
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  closePrice / limitPrice < 1.0005    x = {closePrice / limitPrice}");
                                }
                                else
                                {
                                    Order limitOrder = apiHelper.OrderNew(new Order
                                    {
                                        Symbol = SYMBOL,
                                        Side = "Sell",
                                        OrderQty = orderQty,
                                        Price = (int)Math.Floor(limitPrice),
                                        OrdType = "Limit",
                                        Text = $"<BOT><LIMIT></BOT>",
                                    });
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New upper limit order: qty = {orderQty}, price = {limitPrice}");
                                    logger.WriteFile("--- " + JObject.FromObject(limitOrder).ToString(Formatting.None));

                                    Order closeOrder = apiHelper.OrderNew(new Order
                                    {
                                        Symbol = SYMBOL,
                                        Side = "Buy",
                                        OrderQty = orderQty,
                                        Price = (int)Math.Ceiling(closePrice),
                                        StopPx = (int)Math.Floor(limitPrice),
                                        OrdType = "StopLimit",
                                        ExecInst = "LastPrice,ReduceOnly",
                                        Text = $"<BOT><CLOSE></BOT>",
                                    });
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New upper close order: qty = {orderQty}, price = {closePrice}");
                                    logger.WriteFile("--- " + JObject.FromObject(closeOrder).ToString(Formatting.None));

                                    Order stopOrder = apiHelper.OrderNew(new Order
                                    {
                                        Symbol = SYMBOL,
                                        Side = "Buy",
                                        OrderQty = orderQty,
                                        StopPx = (int)Math.Ceiling(stopPrice),
                                        OrdType = "Stop",
                                        ExecInst = "LastPrice,ReduceOnly",
                                        Text = $"<BOT><STOP></BOT>",
                                    });
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New upper stop order: qty = {orderQty}, price = {stopPrice}");
                                    logger.WriteFile("--- " + JObject.FromObject(stopOrder).ToString(Formatting.None));
                                }
                            }
                        }
                    }
                    else
                    {
                        bool closeExist = false;
                        bool stopExist = false;
                        foreach (Order oldOrder in botOrderList)
                        {
                            string text = oldOrder.Text;
                            if (!text.Contains("<BOT>")) continue;
                            if (text.Contains("<CLOSE>"))
                                closeExist = true;
                            else if (text.Contains("<STOP>"))
                                stopExist = true;
                        }
                        if (!closeExist || !stopExist)
                        {
                            if (waitingPositionClosed)
                            {
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Old position exists. Waiting to be closed...", ConsoleColor.Red);
                            }
                            else
                            {
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Position exists without CLOSE or STOP order.", ConsoleColor.Yellow);
                            }
                            //else if (positionQty > 0)
                            //{
                            //    Order newOrder = apiHelper.OrderNew(new Order
                            //    {
                            //        Symbol = SYMBOL,
                            //        Side = "Sell",
                            //        OrderQty = positionQty,
                            //        OrdType = "Market",
                            //        ExecInst = "ReduceOnly",
                            //        Text = $"<BOT><MARKET-CLOSE></BOT>",
                            //    });
                            //    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New market close order has been created: qty = {positionQty}, price = {lastPrice}");
                            //    logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                            //}
                            //else if (positionQty < 0)
                            //{
                            //    Order newOrder = apiHelper.OrderNew(new Order
                            //    {
                            //        Symbol = SYMBOL,
                            //        Side = "Buy",
                            //        OrderQty = -positionQty,
                            //        OrdType = "Market",
                            //        ExecInst = "ReduceOnly",
                            //        Text = $"<BOT><MARKET-CLOSE></BOT>",
                            //    });
                            //    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New market close order has been created: qty = {positionQty}, price = {lastPrice}");
                            //    logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                            //}
                        }

                    }


                    //decimal limitPrice = binList[1].Open.Value + (binList[1].Close.Value - binList[1].Open.Value) * .05m;
                    //decimal closePrice = Math.Min(binList[1].High.Value * .9997m, limitPrice * 1.0012m);
                    //if (closePrice / limitPrice < 1.0005m) continue;
                    //decimal closeHeight = closePrice - limitPrice;
                    //decimal stopPrice = limitPrice * .971m;

                    //Order limitOrder = apiHelper.OrderNew(new Order
                    //{
                    //    Symbol = SYMBOL,
                    //    Side = "Buy",
                    //    OrderQty = orderQty,
                    //    Price = (int)Math.Ceiling(limitPrice),
                    //    OrdType = "Limit",
                    //    Text = $"<BOT><LIMIT></BOT>",
                    //});
                    //logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New lower limit order: qty = {orderQty}, price = {limitPrice}");
                    //logger.WriteFile("--- " + JObject.FromObject(limitOrder).ToString(Formatting.None));

                    //Order closeOrder = apiHelper.OrderNew(new Order
                    //{
                    //    Symbol = SYMBOL,
                    //    Side = "Sell",
                    //    OrderQty = orderQty,
                    //    Price = (int)Math.Floor(closePrice),
                    //    StopPx = (int)Math.Ceiling(limitPrice),
                    //    OrdType = "StopLimit",
                    //    ExecInst = "LastPrice,ReduceOnly",
                    //    Text = $"<BOT><CLOSE></BOT>",
                    //});
                    //logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New lower close order: qty = {orderQty}, price = {closePrice}");
                    //logger.WriteFile("--- " + JObject.FromObject(closeOrder).ToString(Formatting.None));

                    //Order stopOrder = apiHelper.OrderNew(new Order
                    //{
                    //    Symbol = SYMBOL,
                    //    Side = "Sell",
                    //    OrderQty = orderQty,
                    //    StopPx = (int)Math.Floor(stopPrice),
                    //    OrdType = "Stop",
                    //    ExecInst = "LastPrice,ReduceOnly",
                    //    Text = $"<BOT><STOP></BOT>",
                    //});
                    //logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New lower stop order: qty = {orderQty}, price = {stopPrice}");
                    //logger.WriteFile("--- " + JObject.FromObject(closeOrder).ToString(Formatting.None));

                    lastCandle = binList[0];

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
                        if (waitSeconds > 8)
                        {
                            waitSeconds -= 8;
                            Logger.WriteWait($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Sleeping {waitSeconds:N0} seconds ({apiHelper.RequestCount} requests) ", waitSeconds, 5);
                            Thread.Sleep(waitMilliseconds % 1000);
                        }
                        else
                        {
                            Thread.Sleep(waitMilliseconds);
                        }
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