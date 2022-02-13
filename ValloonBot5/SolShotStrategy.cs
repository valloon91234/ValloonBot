//#define LICENSE_MODE

using System;
using System.Collections.Generic;
using System.Linq;
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
    public class SolShotStrategy
    {
        public void Run(Config config = null)
        {
            const string SYMBOL = BitMEXApiHelper.SYMBOL_SOLUSD;
            int loopIndex = 0;
            DateTime? lastLoopTime = null;
            TradeBin lastCandle = null;
            decimal lastWalletBalance = 0;
            ShotConfig shotConfig = null;
            bool waitingPositionClosed = true;
            Logger logger = null;
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
                    if (config.Shot == null)
                    {
                        if (shotConfig == null || configUpdated || lastLoopTime != null && lastLoopTime.Value.Hour != currentLoopTime.Hour)
                        {
                            try
                            {
                                string url = $"https://raw.githubusercontent.com/maksimg1002/_upload1002/main/solshot.json";
                                string shotJson = BackendClient.HttpGet(url);
                                shotConfig = JsonConvert.DeserializeObject<ShotConfig>(shotJson);
                                logger.WriteLine($"[{BitMEXApiHelper.ServerTime:yyyy-MM-dd  HH:mm:ss}]  ParameterMap loaded.", ConsoleColor.Green);
                                logger.WriteLine(JObject.FromObject(shotConfig).ToString(Formatting.Indented));
                                logger.WriteLine();
                            }
                            catch (Exception ex)
                            {
                                logger.WriteLine($"[{BitMEXApiHelper.ServerTime:yyyy-MM-dd  HH:mm:ss}]  Cannot get parameter map: {ex.Message}", ConsoleColor.Red);
                                Logger.WriteWait("", 60, ConsoleColor.DarkGray);
                                continue;
                            }
                        }
                    }
                    else
                    {
                        shotConfig = config.Shot;
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
                        logger.WriteLine($"[{BitMEXApiHelper.ServerTime:yyyy-MM-dd  HH:mm:ss}  ({++loopIndex})]    $ {lastPrice:F1}  /  $ {markPrice:F2}  /  {binList[1].Volume:N0}  /  {binList[0].Volume:N0}    {walletBalance:N8} XBT    {botOrderList.Count} / {activeOrders.Count} / {unavailableMarginPercent:N2} %", ConsoleColor.White);
                    }
                    Position position = apiHelper.GetPosition(SYMBOL);
                    decimal positionEntryPrice = 0;
                    int positionQty = 0;
                    {
                        string consoleTitle = $"$ {lastPrice:N0}  /  {walletBalance:N8} XBT";
                        if (position != null && position.CurrentQty.Value != 0) consoleTitle += "  /  1 Position";
                        consoleTitle += $"  <{config.Username}>  |   {Config.APP_NAME}  v{Config.APP_VERSION}   by  ValloonTrader.com";
                        Console.Title = consoleTitle;
                        if (position != null && position.CurrentQty.Value != 0)
                        {
                            positionEntryPrice = position.AvgEntryPrice.Value;
                            positionQty = position.CurrentQty.Value;
                            decimal unrealisedPercent = 100m * position.UnrealisedPnl.Value / margin.WalletBalance.Value;
                            decimal nowLoss = 100m * (position.AvgEntryPrice.Value - lastPrice) / (position.LiquidationPrice.Value - position.AvgEntryPrice.Value);
                            logger.WriteFile($"        wallet_balance = {margin.WalletBalance}    unrealised_pnl = {position.UnrealisedPnl}    {unrealisedPercent:N2} %    leverage = {position.Leverage}");
                            logger.WriteLine($"    <Position>    entry = {positionEntryPrice:F1}    qty = {positionQty}    liq = {position.LiquidationPrice}    {unrealisedPercent:N2} % / {nowLoss:N2} %", ConsoleColor.Green);
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
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss FFF}]  {list.Count} old orders have been canceled.", ConsoleColor.DarkGray);
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
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss FFF}]  No order. (exit = {config.Exit})", ConsoleColor.DarkGray);
                                Logger.WriteWait("", 60, ConsoleColor.DarkGray);
                                continue;
                            }
                            if (BitMEXApiHelper.ServerTime.Second < 3 && (DateTime.UtcNow - currentLoopTime).TotalMilliseconds < 1500)
                            {
                                List<TradeBin> reversedBinList = new List<TradeBin>(binList);
                                reversedBinList.Reverse();
                                double[] rsiArray = RSI.CalculateRSIValues(reversedBinList.ToArray(), shotConfig.RSILength);
                                int rsiArrayLength = rsiArray.Length - 1;
                                if ((shotConfig.BuyOrSell == 1 || shotConfig.BuyOrSell == 3) && rsiArray[rsiArrayLength - 3] > rsiArray[rsiArrayLength - 2] && rsiArray[rsiArrayLength - 2] < (double)shotConfig.LowerLimit && rsiArray[rsiArrayLength - 1] - rsiArray[rsiArrayLength - 2] >= (double)shotConfig.LowerMinDiff && (rsiArray[rsiArrayLength - 1] - rsiArray[rsiArrayLength - 2]) < (double)shotConfig.LowerMaxDiff)
                                {
                                    int qty = BitMEXApiHelper.FixQty((int)(margin.WalletBalance.Value * btcPrice * config.LowerQtyX / 100000000));
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
                                        logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss FFF}]  New market order has been created: qty = {qty}, price = {entryPrice}");
                                        logger.WriteFile($"[ {rsiArray[rsiArrayLength - 3]}, {rsiArray[rsiArrayLength - 2]}, {rsiArray[rsiArrayLength - 1]} ]");
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
                                            logger.WriteLine($"    [{BitMEXApiHelper.ServerTime:HH:mm:ss FFF}]  Position did not created: qty = {qty}", ConsoleColor.Red);
                                            Logger.WriteWait("", 60, ConsoleColor.DarkGray);
                                            continue;
                                        }
                                        positionQty = position.CurrentQty.Value;
                                        positionEntryPrice = position.AvgEntryPrice.Value;
                                        logger.WriteLine($"    [{BitMEXApiHelper.ServerTime:HH:mm:ss FFF}]  Position has been created (try = {tryCount}): qty = {positionQty}, entry = {positionEntryPrice}");
                                    }
                                    {
                                        decimal closePrice = Math.Floor((entryPrice + entryPrice * shotConfig.LowerClose) * 100) / 100m;
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
                                        logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss FFF}]  New close order has been created: qty = {qty}, price = {closePrice}");
                                        logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                                    }
                                    {
                                        decimal stopPrice = Math.Ceiling((entryPrice - entryPrice * shotConfig.LowerStop) * 100) / 100m;
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
                                        logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss FFF}]  New stop order has been created: qty = {qty}, price = {stopPrice}");
                                        logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                                    }
                                    if (shotConfig.LowerStop2 > shotConfig.LowerStop)
                                    {
                                        decimal stop2Price = Math.Ceiling((entryPrice - entryPrice * shotConfig.LowerStop2) * 100) / 100m;
                                        Order newOrder = apiHelper.OrderNew(new Order
                                        {
                                            Symbol = SYMBOL,
                                            Side = "Sell",
                                            OrderQty = qty,
                                            StopPx = stop2Price,
                                            OrdType = "Stop",
                                            ExecInst = "LastPrice,ReduceOnly",
                                            Text = $"<BOT><STOP2></BOT>",
                                        });
                                        logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss FFF}]  New stop2 order has been created: qty = {qty}, price = {stop2Price}");
                                        logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                                    }
                                }
                                else if ((shotConfig.BuyOrSell == 2 || shotConfig.BuyOrSell == 3) && rsiArray[rsiArrayLength - 3] < rsiArray[rsiArrayLength - 2] && rsiArray[rsiArrayLength - 2] > (double)shotConfig.UpperLimit && rsiArray[rsiArrayLength - 2] - rsiArray[rsiArrayLength - 1] >= (double)shotConfig.UpperMinDiff && (rsiArray[rsiArrayLength - 2] - rsiArray[rsiArrayLength - 1]) < (double)shotConfig.UpperMaxDiff)
                                {
                                    int qty = BitMEXApiHelper.FixQty((int)(margin.WalletBalance.Value * btcPrice * config.UpperQtyX / 100000000));
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
                                        logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss FFF}]  New market order has been created: qty = {qty}, price = {entryPrice}");
                                        logger.WriteFile($"[ {rsiArray[rsiArrayLength - 3]}, {rsiArray[rsiArrayLength - 2]}, {rsiArray[rsiArrayLength - 1]} ]");
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
                                            logger.WriteLine($"    [{BitMEXApiHelper.ServerTime:HH:mm:ss FFF}]  Position did not created: qty = {qty}", ConsoleColor.Red);
                                            Logger.WriteWait("", 60, ConsoleColor.DarkGray);
                                            continue;
                                        }
                                        positionQty = position.CurrentQty.Value;
                                        positionEntryPrice = position.AvgEntryPrice.Value;
                                        logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss FFF}]  Position has been created (try = {tryCount}): qty = {positionQty}, entry = {positionEntryPrice}");
                                    }
                                    {
                                        decimal closePrice = Math.Ceiling((entryPrice - entryPrice * shotConfig.UpperClose) * 100) / 100m;
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
                                        logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss FFF}]  New close order has been created: qty = {qty}, price = {closePrice}");
                                        logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                                    }
                                    {
                                        decimal stopPrice = Math.Floor((entryPrice + entryPrice * shotConfig.UpperStop) * 100) / 100m;
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
                                        logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss FFF}]  New stop order has been created: qty = {qty}, price = {stopPrice}");
                                        logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                                    }
                                    if (shotConfig.UpperStop2 > shotConfig.UpperStop)
                                    {
                                        decimal stop2Price = Math.Floor((entryPrice + entryPrice * shotConfig.UpperStop2) * 100) / 100m;
                                        Order newOrder = apiHelper.OrderNew(new Order
                                        {
                                            Symbol = SYMBOL,
                                            Side = "Buy",
                                            OrderQty = qty,
                                            StopPx = stop2Price,
                                            OrdType = "Stop",
                                            ExecInst = "LastPrice,ReduceOnly",
                                            Text = $"<BOT><STOP2></BOT>",
                                        });
                                        logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss FFF}]  New stop2 order has been created: qty = {qty}, price = {stop2Price}");
                                        logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                                    }
                                }
                                else
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
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss FFF}]  No order, and new post-only order has been created: qty = {100}, price = {price}", ConsoleColor.DarkGray);
                                    logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                                    apiHelper.OrderCancelAllAfter(290000);
                                }
                            }
                            else
                            {
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss FFF}]  Too late: delay = {(DateTime.UtcNow - currentLoopTime).TotalMilliseconds:N0}", ConsoleColor.DarkGray);
                            }
                        }
                        else if (waitingPositionClosed)
                        {
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss FFF}]  Old position exists. Waiting to be closed...", ConsoleColor.Red);
                            Logger.WriteWait("", 60, ConsoleColor.DarkGray);
                            continue;
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
                                if (positionQty > 0)
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
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss FFF}]  New market close order has been created: qty = {positionQty}, price = {lastPrice}");
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
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss FFF}]  New market close order has been created: qty = {positionQty}, price = {lastPrice}");
                                    logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                                }
                            }
                            else if (!stopExist)
                            {
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss FFF}]  stop order does not exist.", ConsoleColor.Red);
                            }
                        }
                    }
                    lastCandle = binList[0];
                    if (lastWalletBalance != walletBalance)
                    {
                        if (lastWalletBalance != 0)
                        {
                            string logFilename = $"{BitMEXApiHelper.ServerTime:yyyy-MM}-balance";
                            Logger logger2 = new Logger(logFilename);
                            if (!logger2.ExistFile()) logger2.WriteFile($"[{BitMEXApiHelper.ServerTime:yyyy-MM-dd  HH:mm:ss}  ({++loopIndex:D5})]    {lastWalletBalance:F8}");
                            string suffix = null;
                            if (positionQty != 0) suffix = $"        position = {positionQty}";
                            logger2.WriteFile($"[{BitMEXApiHelper.ServerTime:yyyy-MM-dd  HH:mm:ss}  ({++loopIndex:D5})]    {walletBalance:F8}    {walletBalance - lastWalletBalance:F8}    last = {lastPrice}    mark = {markPrice}{suffix}");
                        }
                        lastWalletBalance = walletBalance;
                    }

                    apiHelper.GetInstrument(SYMBOL);
                    int waitMilliseconds = (int)(binList[0].Timestamp.Value - BitMEXApiHelper.ServerTime).TotalMilliseconds;
                    if (waitMilliseconds >= 0)
                    {
                        int waitSeconds = waitMilliseconds / 1000;
                        if (waitSeconds > 10)
                            waitSeconds -= 10;
                        Logger.WriteWait($"    Sleeping {waitSeconds} seconds ({apiHelper.RequestCount} requests) ", waitSeconds, ConsoleColor.DarkGray);
                        Thread.Sleep(waitMilliseconds % 1000);
                    }
                    else
                    {
                        logger.WriteLine($"[{BitMEXApiHelper.ServerTime:HH:mm:ss FFF}]  Error: waitMilliseconds = {waitMilliseconds} < 0", ConsoleColor.Red);
                    }
                }
                catch (Exception ex)
                {
                    if (logger == null) logger = new Logger($"{BitMEXApiHelper.ServerTime:yyyy-MM-dd}");
                    logger.WriteLine(ex.ToString(), ConsoleColor.Red, false);
                    logger.WriteFile(ex.ToString());
                    logger.WriteFile($"LastPlain4Sign = {BitMEXApiHelper.LastPlain4Sign}");
                    Thread.Sleep(5000);
                }
            }
        }
    }
}