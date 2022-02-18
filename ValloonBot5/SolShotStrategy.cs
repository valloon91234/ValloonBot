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
    public class ParamMap
    {
        public int BuyOrSell { get; set; }
        public decimal MinRSI { get; set; }
        public decimal MaxRSI { get; set; }
        public decimal MinDiff { get; set; }
        public decimal MaxDiff { get; set; }
        public decimal CloseX { get; set; }
        public decimal StopX { get; set; }
        public decimal QtyX { get; set; }
    }

    public class SolShotStrategy
    {
        public void Run(Config config = null)
        {
            const string SYMBOL = BitMEXApiHelper.SYMBOL_SOLUSD;
            const int RSILength = 14;

            ParamMap[] paramMapArray;
            {
                string paramText;
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Valloon.BitMEX.SolShot.txt"))
                using (StreamReader reader = new StreamReader(stream))
                {
                    paramText = reader.ReadToEnd();
                }
                string[] paramLines = paramText.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                List<ParamMap> paramMapList = new List<ParamMap>();
                foreach (string paramLine in paramLines)
                {
                    string[] paramValues = paramLine.Split(new string[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
                    paramMapList.Add(new ParamMap
                    {
                        BuyOrSell = Int32.Parse(paramValues[0]),
                        MinRSI = decimal.Parse(paramValues[1]),
                        MaxRSI = decimal.Parse(paramValues[2]),
                        MinDiff = decimal.Parse(paramValues[3]),
                        MaxDiff = decimal.Parse(paramValues[4]),
                        CloseX = decimal.Parse(paramValues[5]),
                        StopX = decimal.Parse(paramValues[6]),
                        QtyX = decimal.Parse(paramValues[7]),
                    });
                }
                paramMapArray = paramMapList.ToArray();
            }

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

                    Margin margin = apiHelper.GetMargin();
                    decimal walletBalance = margin.WalletBalance.Value / 100000000m;
                    List<Order> activeOrders = apiHelper.GetActiveOrders(SYMBOL);
                    Instrument instrumentSOL = apiHelper.GetInstrument(SYMBOL);
                    decimal lastPrice = instrumentSOL.LastPrice.Value;
                    decimal markPrice = instrumentSOL.MarkPrice.Value;
                    List<TradeBin> binList = apiHelper.GetRencentBinList(SYMBOL, "5m", 1000, true);
                    List<Order> botOrderList = new List<Order>();
                    foreach (Order order in activeOrders)
                        if (order.Text.Contains("<BOT>")) botOrderList.Add(order);
                    {
                        decimal unavailableMarginPercent = 100m * (margin.WalletBalance.Value - margin.AvailableMargin.Value) / margin.WalletBalance.Value;
                        logger.WriteLine($"[{BitMEXApiHelper.ServerTime:yyyy-MM-dd  HH:mm:ss fff}  ({++loopIndex})]    $ {lastPrice:F2}  /  $ {markPrice:F3}  /  {binList[1].Volume:N0}  /  {binList[0].Volume:N0}    {walletBalance:N8} XBT    {botOrderList.Count} / {activeOrders.Count} / {unavailableMarginPercent:N2} %", ConsoleColor.White);
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

                    if (lastCandle == null || lastCandle.Timestamp.Value.Minute != binList[0].Timestamp.Value.Minute)
                    {
                        if (positionQty == 0)
                        {
                            waitingPositionClosed = false;
                            if (activeOrders.Count > 0)
                            {
                                List<Order> list = apiHelper.CancelAllOrders(SYMBOL);
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  {list.Count} old orders have been canceled.", ConsoleColor.DarkGray);
                            }
#if LICENSE_MODE
                            if (serverTime.Year != 2022 || serverTime.Month != 2)
                            {
                                logger.WriteLine($"This bot is too old. Please contact support.  https://valloontrader.com", ConsoleColor.Green);
                                Logger.WriteWait("", 60, ConsoleColor.DarkGray);
                                continue;
                            }
#endif
                            if (config.Exit > 0)
                            {
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  No order. (exit = {config.Exit})", ConsoleColor.DarkGray);
                                Logger.WriteWait("", 60, ConsoleColor.DarkGray);
                                continue;
                            }
                            if (config.QtyX == 0)
                            {
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  No order. (qty = {config.QtyX})", ConsoleColor.DarkGray);
                                Logger.WriteWait("", 60, ConsoleColor.DarkGray);
                                continue;
                            }
                            if (BitMEXApiHelper.ServerTime.Minute % 5 == 0 && BitMEXApiHelper.ServerTime.Second < 5)
                            {
                                List<TradeBin> reversedBinList = new List<TradeBin>(binList);
                                reversedBinList.Reverse();
                                double[] rsiArray = RSI.CalculateRSIValues(reversedBinList.ToArray(), RSILength);
                                int rsiArrayLength = rsiArray.Length - 1;
#if !LICENSE_MODE
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  {rsiArray[rsiArrayLength - 3]:F2},  {rsiArray[rsiArrayLength - 2]:F2},  {rsiArray[rsiArrayLength - 1]:F2}", ConsoleColor.DarkGray);
#endif
                                foreach (ParamMap paramMap in paramMapArray)
                                {
                                    if (paramMap.BuyOrSell == 1 && paramMap.QtyX > 0 && rsiArray[rsiArrayLength - 3] > rsiArray[rsiArrayLength - 2] && rsiArray[rsiArrayLength - 2] >= (double)paramMap.MinRSI && rsiArray[rsiArrayLength - 2] < (double)paramMap.MaxRSI && rsiArray[rsiArrayLength - 1] - rsiArray[rsiArrayLength - 2] >= (double)paramMap.MinDiff && (rsiArray[rsiArrayLength - 1] - rsiArray[rsiArrayLength - 2]) < (double)paramMap.MaxDiff)
                                    {
                                        int qty = BitMEXApiHelper.FixQty((int)(margin.WalletBalance.Value * btcPrice * config.QtyX * paramMap.QtyX / 100000000));
                                        decimal entryPrice = binList[1].Close.Value;
                                        {
                                            Order newOrder = apiHelper.OrderNew(new Order
                                            {
                                                Symbol = SYMBOL,
                                                Side = "Buy",
                                                OrderQty = qty,
                                                OrdType = "Market",
                                                Text = $"<BOT><MARKET></BOT>",
                                            });
                                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New market order has been created: qty = {qty}, price = {entryPrice}");
                                            logger.WriteFile($"--- [ {rsiArray[rsiArrayLength - 3]}, {rsiArray[rsiArrayLength - 2]}, {rsiArray[rsiArrayLength - 1]} ]");
                                            logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                                        }
                                        {
                                            int tryCount = 0;
                                            bool positionCreated = false;
                                            while (tryCount < 60)
                                            {
                                                position = apiHelper.GetPosition(SYMBOL);
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
                                                Logger.WriteWait("", 60, ConsoleColor.DarkGray);
                                                continue;
                                            }
                                            positionQty = position.CurrentQty.Value;
                                            positionEntryPrice = position.AvgEntryPrice.Value;
                                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Position has been created (try = {tryCount}): qty = {positionQty}, entry = {positionEntryPrice}");
                                        }
                                        {
                                            decimal closePrice = Math.Floor((entryPrice + entryPrice * paramMap.CloseX) * 100) / 100m;
                                            Order newOrder = apiHelper.OrderNew(new Order
                                            {
                                                Symbol = SYMBOL,
                                                Side = "Sell",
                                                OrderQty = qty,
                                                Price = closePrice,
                                                OrdType = "Limit",
                                                ExecInst = "ReduceOnly",
                                                Text = $"<BOT><CLOSE></BOT>",
                                            });
                                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New close order has been created: qty = {qty}, price = {closePrice}");
                                            logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                                        }
                                        {
                                            decimal stopPrice = Math.Ceiling((entryPrice - entryPrice * paramMap.StopX) * 100) / 100m;
                                            Order newOrder = apiHelper.OrderNew(new Order
                                            {
                                                Symbol = SYMBOL,
                                                Side = "Sell",
                                                OrderQty = qty,
                                                StopPx = stopPrice,
                                                OrdType = "Stop",
                                                ExecInst = "LastPrice,ReduceOnly",
                                                Text = $"<BOT><STOP></BOT>",
                                            });
                                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New stop order has been created: qty = {qty}, price = {stopPrice}");
                                            logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                                        }
                                        goto positionCreated;
                                    }
                                    if (paramMap.BuyOrSell == 2 && paramMap.QtyX > 0 && rsiArray[rsiArrayLength - 3] < rsiArray[rsiArrayLength - 2] && rsiArray[rsiArrayLength - 2] >= (double)paramMap.MinRSI && rsiArray[rsiArrayLength - 2] < (double)paramMap.MaxRSI && rsiArray[rsiArrayLength - 2] - rsiArray[rsiArrayLength - 1] >= (double)paramMap.MinDiff && (rsiArray[rsiArrayLength - 2] - rsiArray[rsiArrayLength - 1]) < (double)paramMap.MaxDiff)
                                    {
                                        int qty = BitMEXApiHelper.FixQty((int)(margin.WalletBalance.Value * btcPrice * config.QtyX * paramMap.QtyX / 100000000));
                                        decimal entryPrice = binList[1].Close.Value;
                                        {
                                            Order newOrder = apiHelper.OrderNew(new Order
                                            {
                                                Symbol = SYMBOL,
                                                Side = "Sell",
                                                OrderQty = qty,
                                                OrdType = "Market",
                                                Text = $"<BOT><MARKET></BOT>",
                                            });
                                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New market order has been created: qty = {qty}, price = {entryPrice}");
                                            logger.WriteFile($"--- [ {rsiArray[rsiArrayLength - 3]}, {rsiArray[rsiArrayLength - 2]}, {rsiArray[rsiArrayLength - 1]} ]");
                                            logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                                        }
                                        {
                                            int tryCount = 0;
                                            bool positionCreated = false;
                                            while (tryCount < 60)
                                            {
                                                position = apiHelper.GetPosition(SYMBOL);
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
                                                Logger.WriteWait("", 60, ConsoleColor.DarkGray);
                                                continue;
                                            }
                                            positionQty = position.CurrentQty.Value;
                                            positionEntryPrice = position.AvgEntryPrice.Value;
                                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Position has been created (try = {tryCount}): qty = {positionQty}, entry = {positionEntryPrice}");
                                        }
                                        {
                                            decimal closePrice = Math.Ceiling((entryPrice - entryPrice * paramMap.CloseX) * 100) / 100m;
                                            Order newOrder = apiHelper.OrderNew(new Order
                                            {
                                                Symbol = SYMBOL,
                                                Side = "Buy",
                                                OrderQty = qty,
                                                Price = closePrice,
                                                OrdType = "Limit",
                                                ExecInst = "ReduceOnly",
                                                Text = $"<BOT><CLOSE></BOT>",
                                            });
                                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New close order has been created: qty = {qty}, price = {closePrice}");
                                            logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                                        }
                                        {
                                            decimal stopPrice = Math.Floor((entryPrice + entryPrice * paramMap.StopX) * 100) / 100m;
                                            Order newOrder = apiHelper.OrderNew(new Order
                                            {
                                                Symbol = SYMBOL,
                                                Side = "Buy",
                                                OrderQty = qty,
                                                StopPx = stopPrice,
                                                OrdType = "Stop",
                                                ExecInst = "LastPrice,ReduceOnly",
                                                Text = $"<BOT><STOP></BOT>",
                                            });
                                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New stop order has been created: qty = {qty}, price = {stopPrice}");
                                            logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                                        }
                                        goto positionCreated;
                                    }
                                }
                                {
                                    int price = BitMEXApiHelper.ServerTime.Minute / 5;
                                    if (price == 0) price = 12;
                                    Order newOrder = apiHelper.OrderNew(new Order
                                    {
                                        Symbol = SYMBOL,
                                        Side = "Buy",
                                        OrderQty = 100,
                                        Price = price,
                                        OrdType = "Limit",
                                        ExecInst = "ParticipateDoNotInitiate",
                                        Text = $"<BOT><POST-ONLY></BOT>",
                                    });
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  No order, and new post-only order has been created: qty = {100}, price = {price}", ConsoleColor.DarkGray);
                                    apiHelper.OrderCancelAllAfter(290000);
                                }
                            positionCreated:;
                            }
                            else
                            {
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Too late.", ConsoleColor.DarkGray);
                            }
                        }
                        else
                        {
                            bool closeExist = false;
                            bool stopExist = false;
                            bool stop2Exist = false;
                            foreach (Order oldOrder in activeOrders)
                            {
                                string text = oldOrder.Text;
                                if (!text.Contains("<BOT>")) continue;
                                if (text.Contains("<CLOSE>"))
                                    closeExist = true;
                                else if (text.Contains("<STOP>"))
                                    stopExist = true;
                                else if (text.Contains("<STOP2>"))
                                    stop2Exist = true;
                            }
                            if (!closeExist || !stopExist && !stop2Exist)
                            {
                                if (waitingPositionClosed)
                                {
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Old position exists. Waiting to be closed...", ConsoleColor.Red);
                                }
                                else if (positionQty > 0)
                                {
                                    Order newOrder = apiHelper.OrderNew(new Order
                                    {
                                        Symbol = SYMBOL,
                                        Side = "Sell",
                                        OrderQty = positionQty,
                                        OrdType = "Market",
                                        ExecInst = "ReduceOnly",
                                        Text = $"<BOT><MARKET-CLOSE></BOT>",
                                    });
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New market close order has been created: qty = {positionQty}, price = {lastPrice}");
                                    logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                                }
                                else if (positionQty < 0)
                                {
                                    Order newOrder = apiHelper.OrderNew(new Order
                                    {
                                        Symbol = SYMBOL,
                                        Side = "Buy",
                                        OrderQty = -positionQty,
                                        OrdType = "Market",
                                        ExecInst = "ReduceOnly",
                                        Text = $"<BOT><MARKET-CLOSE></BOT>",
                                    });
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New market close order has been created: qty = {positionQty}, price = {lastPrice}");
                                    logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                                }
                            }
                            else if (!stopExist)
                            {
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Stop order does not exist.", ConsoleColor.Red);
                            }
                        }
                    }
                    lastCandle = binList[0];

                    margin = apiHelper.GetMargin();
                    walletBalance = margin.WalletBalance.Value / 100000000m;
                    if (lastWalletBalance != walletBalance)
                    {
                        if (lastWalletBalance != 0)
                        {
                            string logFilename = $"{BitMEXApiHelper.ServerTime:yyyy-MM}-balance";
                            Logger logger2 = new Logger(logFilename);
                            if (!logger2.ExistFile()) logger2.WriteFile($"[{BitMEXApiHelper.ServerTime:yyyy-MM-dd  HH:mm:ss}  ({++loopIndex:D6})]    {lastWalletBalance:F8}");
                            string suffix = null;
                            if (positionQty == 0)
                                suffix = $"        profit = {(walletBalance - lastWalletBalance) / lastWalletBalance * 100:N2} %";
                            else
                                suffix = $"        position = {positionQty}";
                            logger2.WriteFile($"[{BitMEXApiHelper.ServerTime:yyyy-MM-dd  HH:mm:ss}  ({++loopIndex:D6})]    {walletBalance:F8}    {walletBalance - lastWalletBalance:F8}    last = {lastPrice:F2}    mark = {markPrice:F3}{suffix}");
                        }
                        lastWalletBalance = walletBalance;
                    }

                    int waitMilliseconds = (int)(binList[0].Timestamp.Value - BitMEXApiHelper.ServerTime).TotalMilliseconds;
                    if (waitMilliseconds >= 0)
                    {
                        int waitSeconds = waitMilliseconds / 1000;
                        if (waitSeconds > 10)
                            waitSeconds -= 10;
                        Logger.WriteWait($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Sleeping {waitMilliseconds:N0} milliseconds ({apiHelper.RequestCount} requests) ", waitSeconds, ConsoleColor.DarkGray);
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
                    logger.WriteLine(ex.Message, ConsoleColor.Red, false);
                    logger.WriteFile(ex.ToString());
                    logger.WriteFile($"LastPlain4Sign = {BitMEXApiHelper.LastPlain4Sign}");
                    Thread.Sleep(5000);
                }
            }
        }
    }
}