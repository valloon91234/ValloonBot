//#define LICENSE_MODE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using IO.Swagger.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/**
 * @author Valloon Project
 * @version 4.0
 * @2022-01-04
 */
namespace Valloon.BitMEX
{
    public class ShovelStrategy
    {
        private static int GetSMA(string text)
        {
            string prefix = "<A=";
            int offset = text.IndexOf(prefix);
            if (offset == -1) return 0;
            offset += prefix.Length;
            int limit = text.IndexOf(">", offset);
            string value = text.Substring(offset, limit - offset);
            return Int32.Parse(value);
        }

        public void Run(Config config)
        {
            string roundId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            int loopIndex = 0;
            DateTime? lastLoopTime = null;
            decimal lastWalletBalance = 0;
            //bool waitingPositionClosed = true;
            while (true)
            {
                BitMEXApiHelper apiHelper = null;
                try
                {
                    DateTime currentLoopTime = DateTime.Now;
                    config = Config.Load(out bool configUpdated, lastLoopTime != null && lastLoopTime.Value.Day != currentLoopTime.Day);
                    ShovelConfig shovelConfig = config.shovel;
                    if (shovelConfig == null) shovelConfig = new ShovelConfig();
                    apiHelper = new BitMEXApiHelper(config.ApiKey, config.ApiSecret, config.TestnetMode);
                    lastLoopTime = currentLoopTime;
                    if (configUpdated) loopIndex = 0;
                    Margin margin = apiHelper.GetMargin();
                    decimal walletBalance = margin.WalletBalance.Value / 100000000m;
                    List<Order> activeOrders = apiHelper.GetActiveOrders();
                    Instrument instrument = apiHelper.GetInstrument();
                    decimal lastPrice = instrument.LastPrice.Value;
                    decimal markPrice = instrument.MarkPrice.Value;
                    List<TradeBin> binList = apiHelper.GetRencentBinList("1m", 1000, true);
                    DateTime serverTime = BitMEXApiHelper.ServerTime.Value;
                    string timeText = serverTime.ToString("yyyy-MM-dd  HH:mm:ss");
                    List<Order> botOrderList = new List<Order>();
                    foreach (Order order in activeOrders)
                        if (order.Text.Contains("<BOT>")) botOrderList.Add(order);
                    {
                        decimal unavailableMarginPercent = 100m * (margin.WalletBalance.Value - margin.AvailableMargin.Value) / margin.WalletBalance.Value;
                        Logger.WriteLine($"[{timeText}  ({++loopIndex})]    $ {lastPrice:F1}  /  $ {markPrice:F2}  /  {binList[1].Volume:N0}  /  {binList[0].Volume:N0}    {walletBalance:N8} XBT    {botOrderList.Count} / {activeOrders.Count} / {unavailableMarginPercent:N2} %", ConsoleColor.White);
                    }
                    Position position = apiHelper.GetPosition();
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
                            decimal nowLoss = 100m * (lastPrice - position.AvgEntryPrice.Value) / (position.LiquidationPrice.Value - position.AvgEntryPrice.Value);
                            Logger.WriteFile($"        wallet_balance = {margin.WalletBalance}    unrealised_pnl = {position.UnrealisedPnl}    {unrealisedPercent:N2} %    leverage = {position.Leverage}");
                            Logger.WriteLine($"    <Position>    entry = {positionEntryPrice:F1}    qty = {positionQty}    liq = {position.LiquidationPrice}    {unrealisedPercent:N2} % / {nowLoss:N2} %", ConsoleColor.Green);
                        }
                    }
                    int sma;
                    {
                        int candleCount = shovelConfig.SMALength;
                        decimal[] closeArray = new decimal[candleCount];
                        decimal[] sd2Array = new decimal[candleCount];
                        for (int i = 0; i < candleCount; i++)
                        {
                            decimal? close = binList[candleCount - 1 - i].Close;
                            if (close == null)
                            {
                                close = binList[candleCount - i].Close;
                                binList[candleCount - 1 - i].Close = close;
                            }
                            closeArray[i] = close.Value;
                        }
                        sma = (int)closeArray.Average();
                    }
                    //if (positionQty == 0)
                    //{
                    //    waitingPositionClosed = false;
                    //}
                    //else if (waitingPositionClosed)
                    //{
                    //    Logger.WriteLine($"        Old position exists. Waiting to be closed...", ConsoleColor.Red);
                    //    Logger.WriteWait("", 30);
                    //    continue;
                    //}

                    List<Order> lastFilledOrders = apiHelper.GetOrders("{\"ordStatus\":\"Filled\"}");

                    bool needUpperLimit1 = true, needUpperClose1 = false, needUpperStop1 = false;
                    bool needLowerLimit1 = true, needLowerClose1 = false, needLowerStop1 = false;
                    int lastUpperSma1 = 0, lastLowerSma1 = 0;

                    int upperQty1 = BitMEXApiHelper.FixQty((int)(margin.WalletBalance.Value * lastPrice * config.UpperQtyX1 / 100000000));
                    foreach (Order order in lastFilledOrders)
                    {
                        if (order.Side == "Buy" && order.Text.Contains($"<{roundId}>") && (order.ExecInst.Contains("Close") || order.Text.Contains("<CLOSE>") || order.Text.Contains("<STOP>")))
                        {
                            break;
                        }
                        else if (order.Side == "Sell" && order.Text.Contains($"<{roundId}>") && order.Text.Contains("<LIMIT>"))
                        {
                            upperQty1 = order.OrderQty.Value;
                            needUpperLimit1 = false;
                            needUpperClose1 = true;
                            needUpperStop1 = true;
                            lastUpperSma1 = GetSMA(order.Text);
                            break;
                        }
                    }

                    int lowerQty1 = BitMEXApiHelper.FixQty((int)(margin.WalletBalance.Value * lastPrice * config.LowerQtyX1 / 100000000));
                    foreach (Order order in lastFilledOrders)
                    {
                        if (order.Side == "Sell" && order.Text.Contains($"<{roundId}>") && (order.OrderID == "81c53d62-c75e-4a01-9068-b2ca15bf8c84" || order.ExecInst.Contains("Close") || order.Text.Contains("<CLOSE>") || order.Text.Contains("<STOP>")))
                        {
                            break;
                        }
                        else if (order.Side == "Buy" && order.Text.Contains($"<{roundId}>") && order.Text.Contains("<LIMIT>"))
                        {
                            lowerQty1 = order.OrderQty.Value;
                            needLowerLimit1 = false;
                            needLowerClose1 = true;
                            needLowerStop1 = true;
                            lastLowerSma1 = GetSMA(order.Text);
                            break;
                        }
                    }

                    bool needUpperLimit2 = true, needUpperClose2 = false, needUpperStop2 = false;
                    bool needLowerLimit2 = true, needLowerClose2 = false, needLowerStop2 = false;
                    int lastUpperSma2 = 0, lastLowerSma2 = 0;

                    int upperQty2 = BitMEXApiHelper.FixQty((int)(margin.WalletBalance.Value * lastPrice * config.UpperQtyX2 / 100000000));
                    foreach (Order order in lastFilledOrders)
                    {
                        string text = order.Text;
                        if (order.Side == "Buy" && text.Contains("<2>") && text.Contains($"<{roundId}>") && (order.ExecInst.Contains("Close") || text.Contains("<CLOSE>") || text.Contains("<STOP>")))
                        {
                            break;
                        }
                        else if (order.Side == "Sell" && text.Contains("<2>") && text.Contains($"<{roundId}>") && text.Contains("<LIMIT>"))
                        {
                            upperQty2 = order.OrderQty.Value;
                            needUpperLimit2 = false;
                            needUpperClose2 = true;
                            needUpperStop2 = true;
                            lastUpperSma2 = GetSMA(order.Text);
                            break;
                        }
                    }

                    int lowerQty2 = BitMEXApiHelper.FixQty((int)(margin.WalletBalance.Value * lastPrice * config.LowerQtyX2 / 100000000));
                    foreach (Order order in lastFilledOrders)
                    {
                        string text = order.Text;
                        if (order.Side == "Sell" && text.Contains($"<{roundId}>") && (order.ExecInst.Contains("Close") || text.Contains("<CLOSE>") || text.Contains("<STOP>")))
                        {
                            break;
                        }
                        else if (order.Side == "Buy" && text.Contains("<2>") && text.Contains($"<{roundId}>") && text.Contains("<LIMIT>"))
                        {
                            lowerQty2 = order.OrderQty.Value;
                            needLowerLimit2 = false;
                            needLowerClose2 = true;
                            needLowerStop2 = true;
                            lastLowerSma2 = GetSMA(order.Text);
                            break;
                        }
                    }

                    int upperHeight1 = (int)(sma * shovelConfig.UpperLimitX1);
                    int upperLimit1 = sma + upperHeight1;
                    int lowerHeight1 = (int)(sma * shovelConfig.LowerLimitX1);
                    int lowerLimit1 = sma - lowerHeight1;
                    int upperHeight2 = (int)(sma * shovelConfig.UpperLimitX2);
                    int upperLimit2 = sma + upperHeight2;
                    int lowerHeight2 = (int)(sma * shovelConfig.LowerLimitX2);
                    int lowerLimit2 = sma - lowerHeight2;

                    List<string> cancelOrderList = new List<string>();
                    foreach (Order order in botOrderList)
                    {
                        string text = order.Text;
                        if (order.Side == "Buy" && text.Contains("<1>") && text.Contains("<LIMIT>") && text.Contains($"<{roundId}>"))
                        {
                            if (config.LowerQtyX1 == 0)
                            {
                                cancelOrderList.Add(order.OrderID);
#if !LICENSE_MODE
                                Logger.WriteLine($"        Lower limit <1> has been canceled (LowerQtyX1 = 0): qty = {order.OrderQty}, sma = {sma}, height = {lowerHeight1}, price = {lowerLimit1}");
#endif
                            }
                            else if (needLowerLimit1)
                            {
                                lastLowerSma1 = GetSMA(order.Text);
                                if (Math.Abs(lastLowerSma1 - sma) > 10)
                                {
                                    if (lowerLimit1 < lastPrice)
                                    {
                                        Order newOrder = apiHelper.OrderAmend(new Order()
                                        {
                                            OrderID = order.OrderID,
                                            OrderQty = order.OrderQty,
                                            Price = lowerLimit1,
                                            Text = $"<BOT><LIMIT><1><A={sma}></BOT>",
                                        });
#if !LICENSE_MODE
                                        Logger.WriteLine($"        Lower limit <1> have been amended: qty = {order.OrderQty}, sma = {sma}, height = {lowerHeight1}, price = {lowerLimit1}");
#endif
                                        Logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                                    }
                                    else
                                    {
                                        cancelOrderList.Add(order.OrderID);
#if !LICENSE_MODE
                                        Logger.WriteLine($"        Lower limit <1> has been canceled: qty = {order.OrderQty}, sma = {sma}, height = {lowerHeight1}, price = {lowerLimit1} >= {lastPrice}");
#endif
                                    }
                                }
                                needLowerLimit1 = false;
                            }
                            else
                            {
                                cancelOrderList.Add(order.OrderID);
#if !LICENSE_MODE
                                Logger.WriteLine($"        Lower limit has been canceled (needLowerLimit1 = 0): qty = {order.OrderQty}, sma = {sma}, height = {lowerHeight1}, price = {lowerLimit1}");
#endif
                            }
                        }
                        else if (order.Side == "Sell" && text.Contains("<1>") && text.Contains("<LIMIT>") && text.Contains($"<{roundId}>"))
                        {
                            if (config.UpperQtyX1 == 0)
                            {
                                cancelOrderList.Add(order.OrderID);
#if !LICENSE_MODE
                                Logger.WriteLine($"        Upper limit <1> has been canceled (UpperQtyX1 = 0): qty = {order.OrderQty}, sma = {sma}, height = {lowerHeight1}, price = {lowerLimit1}");
#endif
                            }
                            else if (needUpperLimit1)
                            {
                                lastUpperSma1 = GetSMA(order.Text);
                                if (Math.Abs(lastUpperSma1 - sma) > 10)
                                {
                                    if (upperLimit1 > lastPrice)
                                    {
                                        Order newOrder = apiHelper.OrderAmend(new Order()
                                        {
                                            OrderID = order.OrderID,
                                            OrderQty = order.OrderQty,
                                            Price = upperLimit1,
                                            Text = $"<BOT><LIMIT><1><A={sma}></BOT>",
                                        });
#if !LICENSE_MODE
                                        Logger.WriteLine($"        Upper limit <1> have been amended: qty = {order.OrderQty}, sma = {sma}, height = {upperHeight1}, price = {upperLimit1}");
#endif
                                        Logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                                    }
                                    else
                                    {
                                        cancelOrderList.Add(order.OrderID);
#if !LICENSE_MODE
                                        Logger.WriteLine($"        Upper limit <1> has been canceled: qty = {order.OrderQty}, sma = {sma}, height = {upperHeight1}, price = {upperLimit1} <= {lastPrice}");
#endif
                                    }
                                }
                                needUpperLimit1 = false;
                            }
                            else
                            {
                                cancelOrderList.Add(order.OrderID);
#if !LICENSE_MODE
                                Logger.WriteLine($"        Upper limit <1> has been canceled (needUpperLimit1 = 0): qty = {order.OrderQty}, sma = {sma}, height = {lowerHeight1}, price = {lowerLimit1}");
#endif
                            }
                        }
                        else if (order.Side == "Buy" && text.Contains("<1>") && text.Contains("<CLOSE>") && text.Contains($"<{roundId}>"))
                        {
                            if (!needUpperClose1) cancelOrderList.Add(order.OrderID);
                            needUpperClose1 = false;
                        }
                        else if (order.Side == "Buy" && text.Contains("<1>") && text.Contains("<STOP>") && text.Contains($"<{roundId}>"))
                        {
                            if (!needUpperStop1) cancelOrderList.Add(order.OrderID);
                            needUpperStop1 = false;
                        }
                        else if (order.Side == "Sell" && text.Contains("<1>") && text.Contains("<CLOSE>") && text.Contains($"<{roundId}>"))
                        {
                            if (!needLowerClose1) cancelOrderList.Add(order.OrderID);
                            needLowerClose1 = false;
                        }
                        else if (order.Side == "Sell" && text.Contains("<1>") && text.Contains("<STOP>") && text.Contains($"<{roundId}>"))
                        {
                            if (!needLowerStop1) cancelOrderList.Add(order.OrderID);
                            needLowerStop1 = false;
                        }
                        else if (order.Side == "Buy" && text.Contains("<2>") && text.Contains("<LIMIT>") && text.Contains($"<{roundId}>"))
                        {
                            if (config.LowerQtyX2 == 0)
                            {
                                cancelOrderList.Add(order.OrderID);
#if !LICENSE_MODE
                                Logger.WriteLine($"        Lower limit <2> has been canceled (LowerQtyX2 = 0): qty = {order.OrderQty}, sma = {sma}, height = {lowerHeight2}, price = {lowerLimit2}");
#endif
                            }
                            else if (needLowerLimit2)
                            {
                                lastLowerSma2 = GetSMA(order.Text);
                                if (Math.Abs(lastLowerSma2 - sma) > 10)
                                {
                                    if (lowerLimit2 < lastPrice)
                                    {
                                        Order newOrder = apiHelper.OrderAmend(new Order()
                                        {
                                            OrderID = order.OrderID,
                                            OrderQty = order.OrderQty,
                                            Price = lowerLimit2,
                                            Text = $"<BOT><LIMIT><2><A={sma}></BOT>",
                                        });
#if !LICENSE_MODE
                                        Logger.WriteLine($"        Lower limit <2> have been amended: qty = {order.OrderQty}, sma = {sma}, height = {lowerHeight2}, price = {lowerLimit2}");
#endif
                                        Logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                                    }
                                    else
                                    {
                                        cancelOrderList.Add(order.OrderID);
#if !LICENSE_MODE
                                        Logger.WriteLine($"        Lower limit <2> has been canceled: qty = {order.OrderQty}, sma = {sma}, height = {lowerHeight2}, price = {lowerLimit2} >= {lastPrice}");
#endif
                                    }
                                }
                                needLowerLimit2 = false;
                            }
                            else
                            {
                                cancelOrderList.Add(order.OrderID);
#if !LICENSE_MODE
                                Logger.WriteLine($"        Lower limit <2> has been canceled (needLowerLimit2 = 0): qty = {order.OrderQty}, sma = {sma}, height = {lowerHeight2}, price = {lowerLimit2}");
#endif
                            }
                        }
                        else if (order.Side == "Sell" && text.Contains("<2>") && text.Contains("<LIMIT>") && text.Contains($"<{roundId}>"))
                        {
                            if (config.UpperQtyX2 == 0)
                            {
                                cancelOrderList.Add(order.OrderID);
#if !LICENSE_MODE
                                Logger.WriteLine($"        Upper limit <2> has been canceled (UpperQtyX2 = 0): qty = {order.OrderQty}, sma = {sma}, height = {lowerHeight2}, price = {lowerLimit2}");
#endif
                            }
                            else if (needUpperLimit2)
                            {
                                lastUpperSma2 = GetSMA(order.Text);
                                if (Math.Abs(lastUpperSma2 - sma) > 10)
                                {
                                    if (upperLimit2 > lastPrice)
                                    {
                                        Order newOrder = apiHelper.OrderAmend(new Order()
                                        {
                                            OrderID = order.OrderID,
                                            OrderQty = order.OrderQty,
                                            Price = upperLimit2,
                                            Text = $"<BOT><LIMIT><2><A={sma}></BOT>",
                                        });
#if !LICENSE_MODE
                                        Logger.WriteLine($"        Upper limit <2> have been amended: qty = {order.OrderQty}, sma = {sma}, height = {upperHeight2}, price = {upperLimit2}");
#endif
                                        Logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                                    }
                                    else
                                    {
                                        cancelOrderList.Add(order.OrderID);
#if !LICENSE_MODE
                                        Logger.WriteLine($"        Upper limit <2> has been canceled: qty = {order.OrderQty}, sma = {sma}, height = {upperHeight2}, price = {upperLimit2} <= {lastPrice}");
#endif
                                    }
                                }
                                needUpperLimit2 = false;
                            }
                            else
                            {
                                cancelOrderList.Add(order.OrderID);
#if !LICENSE_MODE
                                Logger.WriteLine($"        Upper limit <2> has been canceled (needUpperLimit2 = 0): qty = {order.OrderQty}, sma = {sma}, height = {lowerHeight2}, price = {lowerLimit2}");
#endif
                            }
                        }
                        else if (order.Side == "Buy" && text.Contains("<2>") && text.Contains("<CLOSE>") && text.Contains($"<{roundId}>"))
                        {
                            if (!needUpperClose2) cancelOrderList.Add(order.OrderID);
                            needUpperClose2 = false;
                        }
                        else if (order.Side == "Buy" && text.Contains("<2>") && text.Contains("<STOP>") && text.Contains($"<{roundId}>"))
                        {
                            if (!needUpperStop2) cancelOrderList.Add(order.OrderID);
                            needUpperStop2 = false;
                        }
                        else if (order.Side == "Sell" && text.Contains("<2>") && text.Contains("<CLOSE>") && text.Contains($"<{roundId}>"))
                        {
                            if (!needLowerClose2) cancelOrderList.Add(order.OrderID);
                            needLowerClose2 = false;
                        }
                        else if (order.Side == "Sell" && text.Contains("<2>") && text.Contains("<STOP>") && text.Contains($"<{roundId}>"))
                        {
                            if (!needLowerStop2) cancelOrderList.Add(order.OrderID);
                            needLowerStop2 = false;
                        }
                        else if (text.Contains("<BOT>"))
                        {
                            cancelOrderList.Add(order.OrderID);
#if !LICENSE_MODE
                            Logger.WriteLine($"        Old bot order has been canceled: {order}", ConsoleColor.Yellow);
#endif
                        }
                    }

                    if (cancelOrderList.Count > 0)
                    {
                        int canceledCount = apiHelper.CancelOrders(cancelOrderList).Count;
                        Logger.WriteLine($"        {canceledCount} orders have been canceled.");
                    }

                    bool exit = config.Exit > 0;
                    bool isExpired = serverTime.Year != 2022 || serverTime.Month != 2;
                    if (positionQty == 0 && !needLowerClose1 && !needLowerClose2 && !needLowerStop1 && !needLowerStop2 && !needUpperClose1 && !needUpperClose2 && !needUpperStop1 && !needUpperStop2)
                    {
                        if (exit)
                        {
                            if (activeOrders.Count > 0) apiHelper.CancelAllOrders();
                            Logger.WriteLine($"        No order. (exit = {config.Exit})", ConsoleColor.DarkGray);
                            Logger.WriteWait("", 60);
                            continue;
                        }
#if LICENSE_MODE
                        if (isExpired)
                        {
                            if (activeOrders.Count > 0) apiHelper.CancelAllOrders();
                            Logger.WriteLine("This bot is too old. Please contact support.  https://valloontrader.com", ConsoleColor.Green);
                            Logger.WriteWait("", 60);
                            continue;
                        }
#endif
                    }
#if LICENSE_MODE
                    if (isExpired)exit=true;
#endif

                    if (!exit && config.LowerQtyX1 > 0 && needLowerLimit1)
                    {
                        if (lowerLimit1 < lastPrice)
                        {
                            Order newOrder = apiHelper.OrderNew(new Order
                            {
                                Symbol = BitMEXApiHelper.SYMBOL,
                                Side = "Buy",
                                OrderQty = lowerQty1,
                                Price = lowerLimit1,
                                OrdType = "Limit",
                                Text = $"<BOT><LIMIT><1><A={sma}><{roundId}></BOT>",
                            });
#if !LICENSE_MODE
                            Logger.WriteLine($"        New lower limit <1> have been created: qty = {lowerQty1}, sma = {sma}, height = {lowerHeight1}, price = {lowerLimit1}");
#endif
                            Logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                        }
                        else
                        {
#if !LICENSE_MODE
                            Logger.WriteLine($"        No lower limit <1>: qty = {lowerQty1}, sma = {sma}, height = {lowerHeight1}, price = {lowerLimit1} >= {lastPrice}");
#endif
                        }
                    }

                    if (!exit && config.UpperQtyX1 > 0 && needUpperLimit1)
                    {
                        if (upperLimit1 > lastPrice)
                        {
                            Order newOrder = apiHelper.OrderNew(new Order
                            {
                                Symbol = BitMEXApiHelper.SYMBOL,
                                Side = "Sell",
                                OrderQty = upperQty1,
                                Price = upperLimit1,
                                OrdType = "Limit",
                                Text = $"<BOT><LIMIT><1><A={sma}><{roundId}></BOT>",
                            });
#if !LICENSE_MODE
                            Logger.WriteLine($"        New upper limit <1> have been created: qty = {upperQty1}, sma = {sma}, height = {upperHeight1}, price = {upperLimit1}");
#endif
                            Logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                        }
                        else
                        {
#if !LICENSE_MODE
                            Logger.WriteLine($"        No upper limit <1>: qty = {upperQty1}, sma = {sma}, height = {upperHeight1}, price = {upperLimit1} <= {lastPrice}");
#endif
                        }
                    }

                    if (needLowerClose1 || needLowerStop1)
                    {
                        string closeSide = "Sell";
                        if (lastLowerSma1 == 0)
                        {
#if !LICENSE_MODE
                            Logger.WriteLine($"        Warning: lastLowerSma1 = 0", ConsoleColor.Yellow);
#endif
                        }
                        else
                        {
                            int lastLowerHeight = (int)(lastLowerSma1 * shovelConfig.LowerLimitX1);
                            int lastLowerLimit = lastLowerSma1 - lastLowerHeight;
                            int lowerClose = (int)(lastLowerLimit + lastLowerHeight * shovelConfig.LowerCloseX1);
                            int lowerStop = (int)(lastLowerLimit - lastLowerHeight * shovelConfig.LowerCloseX1 * shovelConfig.LowerStopX1);
                            if (needLowerClose1)
                            {
                                Order newOrder = apiHelper.OrderNew(new Order
                                {
                                    Symbol = BitMEXApiHelper.SYMBOL,
                                    Side = closeSide,
                                    OrderQty = lowerQty1,
                                    Price = lowerClose,
                                    OrdType = "Limit",
                                    Text = $"<BOT><CLOSE><1><A={lastLowerSma1}><{roundId}></BOT>",
                                });
#if !LICENSE_MODE
                                Logger.WriteLine($"        New lower close <1> has been created: qty = {lowerQty1}, sma = {lastLowerSma1}, height = {lastLowerHeight}, price = {lowerClose}");
#endif
                                Logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                            }
                            if (needLowerStop1)
                            {
                                Order newOrder = apiHelper.OrderNew(new Order
                                {
                                    Symbol = BitMEXApiHelper.SYMBOL,
                                    Side = closeSide,
                                    OrderQty = lowerQty1,
                                    StopPx = lowerStop,
                                    OrdType = "Stop",
                                    ExecInst = "LastPrice",
                                    Text = $"<BOT><STOP><1><A={lastLowerSma1}><{roundId}></BOT>",
                                });
#if !LICENSE_MODE
                                Logger.WriteLine($"        New lower stop <1> has been created: qty = {lowerQty1}, sma = {lastLowerSma1}, height = {lastLowerHeight}, price = {lowerStop}");
#endif
                                Logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                            }
                        }
                    }

                    if (needUpperClose1 || needUpperStop1)
                    {
                        string closeSide = "Buy";
                        if (lastUpperSma1 == 0)
                        {
#if !LICENSE_MODE
                            Logger.WriteLine($"        Warning: lastUpperSma1 = 0", ConsoleColor.Yellow);
#endif
                        }
                        else
                        {
                            int lastUpperHeight = (int)(lastUpperSma1 * shovelConfig.UpperLimitX1);
                            int lastUpperLimit = lastUpperSma1 + lastUpperHeight;
                            int upperClose = (int)(lastUpperLimit - lastUpperHeight * shovelConfig.UpperCloseX1);
                            int upperStop = (int)(lastUpperLimit + lastUpperHeight * shovelConfig.UpperCloseX1 * shovelConfig.UpperStopX1);
                            if (needUpperClose1)
                            {
                                Order newOrder = apiHelper.OrderNew(new Order
                                {
                                    Symbol = BitMEXApiHelper.SYMBOL,
                                    Side = closeSide,
                                    OrderQty = Math.Abs(upperQty1),
                                    Price = upperClose,
                                    OrdType = "Limit",
                                    Text = $"<BOT><CLOSE><1><A={lastUpperSma1}><{roundId}></BOT>",
                                });
#if !LICENSE_MODE
                                Logger.WriteLine($"        New upper close <1> has been created: qty = {upperQty1}, sma = {lastUpperSma1}, height = {lastUpperHeight}, price = {upperClose}");
#endif
                                Logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                            }
                            if (needUpperStop1)
                            {
                                Order newOrder = apiHelper.OrderNew(new Order
                                {
                                    Symbol = BitMEXApiHelper.SYMBOL,
                                    Side = closeSide,
                                    OrderQty = Math.Abs(upperQty1),
                                    StopPx = upperStop,
                                    OrdType = "Stop",
                                    ExecInst = "LastPrice",
                                    Text = $"<BOT><STOP><1><A={lastUpperSma1}><{roundId}></BOT>",
                                });
#if !LICENSE_MODE
                                Logger.WriteLine($"        New upper stop <1> has been created: qty = {upperQty1}, sma = {lastUpperSma1}, height = {lastUpperHeight}, price = {upperStop}");
#endif
                                Logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                            }
                        }
                    }

                    if (!exit && config.LowerQtyX2 > 0 && needLowerLimit2)
                    {
                        if (lowerLimit2 < lastPrice)
                        {
                            Order newOrder = apiHelper.OrderNew(new Order
                            {
                                Symbol = BitMEXApiHelper.SYMBOL,
                                Side = "Buy",
                                OrderQty = lowerQty2,
                                Price = lowerLimit2,
                                OrdType = "Limit",
                                Text = $"<BOT><LIMIT><2><A={sma}><{roundId}></BOT>",
                            });
#if !LICENSE_MODE
                            Logger.WriteLine($"        New lower limit <2> have been created: qty = {lowerQty2}, sma = {sma}, height = {lowerHeight2}, price = {lowerLimit2}");
#endif
                            Logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                        }
                        else
                        {
#if !LICENSE_MODE
                            Logger.WriteLine($"        No lower limit <2>: qty = {lowerQty2}, sma = {sma}, height = {lowerHeight2}, price = {lowerLimit2} >= {lastPrice}");
#endif
                        }
                    }

                    if (!exit && config.UpperQtyX2 > 0 && needUpperLimit2)
                    {
                        if (upperLimit2 > lastPrice)
                        {
                            Order newOrder = apiHelper.OrderNew(new Order
                            {
                                Symbol = BitMEXApiHelper.SYMBOL,
                                Side = "Sell",
                                OrderQty = upperQty2,
                                Price = upperLimit2,
                                OrdType = "Limit",
                                Text = $"<BOT><LIMIT><2><A={sma}><{roundId}></BOT>",
                            });
#if !LICENSE_MODE
                            Logger.WriteLine($"        New upper limit <2> have been created: qty = {upperQty2}, sma = {sma}, height = {upperHeight2}, price = {upperLimit2}");
#endif
                            Logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                        }
                        else
                        {
#if !LICENSE_MODE
                            Logger.WriteLine($"        No upper limit <2>: qty = {upperQty2}, sma = {sma}, height = {upperHeight2}, price = {upperLimit2} <= {lastPrice}");
#endif
                        }
                    }

                    if (needLowerClose2 || needLowerStop2)
                    {
                        string closeSide = "Sell";
                        if (lastLowerSma2 == 0)
                        {
#if !LICENSE_MODE
                            Logger.WriteLine($"        Warning: lastLowerSma2 = 0", ConsoleColor.Yellow);
#endif
                        }
                        else
                        {
                            int lastLowerHeight = (int)(lastLowerSma2 * shovelConfig.LowerLimitX2);
                            int lastLowerLimit = lastLowerSma2 - lastLowerHeight;
                            int lowerClose = (int)(lastLowerLimit + lastLowerHeight * shovelConfig.LowerCloseX2);
                            int lowerStop = (int)(lastLowerLimit - lastLowerHeight * shovelConfig.LowerCloseX2 * shovelConfig.LowerStopX2);
                            if (needLowerClose2)
                            {
                                Order newOrder = apiHelper.OrderNew(new Order
                                {
                                    Symbol = BitMEXApiHelper.SYMBOL,
                                    Side = closeSide,
                                    OrderQty = lowerQty2,
                                    Price = lowerClose,
                                    OrdType = "Limit",
                                    Text = $"<BOT><CLOSE><2><A={lastLowerSma2}><{roundId}></BOT>",
                                });
#if !LICENSE_MODE
                                Logger.WriteLine($"        New lower close <2> has been created: qty = {lowerQty2}, sma = {lastLowerSma2}, height = {lastLowerHeight}, price = {lowerClose}");
#endif
                                Logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                            }
                            if (needLowerStop2)
                            {
                                Order newOrder = apiHelper.OrderNew(new Order
                                {
                                    Symbol = BitMEXApiHelper.SYMBOL,
                                    Side = closeSide,
                                    OrderQty = lowerQty2,
                                    StopPx = lowerStop,
                                    OrdType = "Stop",
                                    ExecInst = "LastPrice",
                                    Text = $"<BOT><STOP><2><A={lastLowerSma2}><{roundId}></BOT>",
                                });
#if !LICENSE_MODE
                                Logger.WriteLine($"        New lower stop <2> has been created: qty = {lowerQty2}, sma = {lastLowerSma2}, height = {lastLowerHeight}, price = {lowerStop}");
#endif
                                Logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                            }
                        }
                    }

                    if (needUpperClose2 || needUpperStop2)
                    {
                        string closeSide = "Buy";
                        if (lastUpperSma2 == 0)
                        {
#if !LICENSE_MODE
                            Logger.WriteLine($"        Warning: lastUpperSma2 = 0", ConsoleColor.Yellow);
#endif
                        }
                        else
                        {
                            int lastUpperHeight = (int)(lastUpperSma2 * shovelConfig.UpperLimitX2);
                            int lastUpperLimit = lastUpperSma2 + lastUpperHeight;
                            int upperClose = (int)(lastUpperLimit - lastUpperHeight * shovelConfig.UpperCloseX2);
                            int upperStop = (int)(lastUpperLimit + lastUpperHeight * shovelConfig.UpperCloseX2 * shovelConfig.UpperStopX2);
                            if (needUpperClose2)
                            {
                                Order newOrder = apiHelper.OrderNew(new Order
                                {
                                    Symbol = BitMEXApiHelper.SYMBOL,
                                    Side = closeSide,
                                    OrderQty = Math.Abs(upperQty2),
                                    Price = upperClose,
                                    OrdType = "Limit",
                                    Text = $"<BOT><CLOSE><2><A={lastUpperSma2}><{roundId}></BOT>",
                                });
#if !LICENSE_MODE
                                Logger.WriteLine($"        New upper close <2> has been created: qty = {upperQty2}, sma = {lastUpperSma2}, height = {lastUpperHeight}, price = {upperClose}");
#endif
                                Logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                            }
                            if (needUpperStop2)
                            {
                                Order newOrder = apiHelper.OrderNew(new Order
                                {
                                    Symbol = BitMEXApiHelper.SYMBOL,
                                    Side = closeSide,
                                    OrderQty = Math.Abs(upperQty2),
                                    StopPx = upperStop,
                                    OrdType = "Stop",
                                    ExecInst = "LastPrice",
                                    Text = $"<BOT><STOP><2><A={lastUpperSma2}><{roundId}></BOT>",
                                });
#if !LICENSE_MODE
                                Logger.WriteLine($"        New upper stop <2> has been created: qty = {upperQty2}, sma = {lastUpperSma2}, height = {lastUpperHeight}, price = {upperStop}");
#endif
                                Logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                            }
                        }
                    }

                    if (lastWalletBalance != walletBalance)
                    {
                        if (lastWalletBalance != 0)
                        {
                            string logFilename = $"{serverTime:yyyy-MM}-balance";
                            Logger2 logger2 = new Logger2(logFilename);
                            if (!logger2.ExistFile()) logger2.WriteFile($"[{timeText}  ({++loopIndex:D5})]    {lastWalletBalance:F8}");
                            string suffix = null;
                            if (positionQty != 0) suffix = $"    {positionQty}";
                            logger2.WriteFile($"[{timeText}  ({++loopIndex:D5})]    {walletBalance:F8}    {walletBalance - lastWalletBalance:F8}{suffix}");
                        }
                        lastWalletBalance = walletBalance;
                    }
                    int sleep = config.ConnectionInverval * 1000 - (int)(DateTime.Now - currentLoopTime).TotalMilliseconds;
                    sleep = Math.Max(sleep, apiHelper.RequestCount * 1000 / 2 - (int)(DateTime.Now - currentLoopTime).TotalMilliseconds + 1000);
                    if (sleep > 0)
                    {
                        Logger.WriteLine($"        Sleeping {sleep:N0} ms for {apiHelper.RequestCount} requests ...", ConsoleColor.DarkGray);
                        Thread.Sleep(sleep);
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLine(ex.ToString(), ConsoleColor.Red, false);
                    Logger.WriteFile(ex.ToString());
                    Logger.WriteFile($"LastPlain4Sign = {BitMEXApiHelper.LastPlain4Sign}");
                    Thread.Sleep(5000);
                }
            }
        }
    }
}