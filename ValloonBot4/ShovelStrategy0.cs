//#define LICENSE_MODE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using IO.Swagger.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Valloon.BitMEX.Utils;

/**
 * @author Valloon Project
 * @version 4.0
 * @2022-01-04
 */
namespace Valloon.BitMEX
{
    public class ShovelStrategy0
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
            int loopIndex = 0;
            DateTime? lastLoopTime = null;
            decimal lastWalletBalance = 0;
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
                    List<TradeBin> binList = apiHelper.GetRencentBinList(shovelConfig.BinSize, 1000, true);
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
                    if (positionQty == 0)
                    {
                        if (config.Exit > 0)
                        {
                            if (activeOrders.Count > 0) apiHelper.CancelAllOrders();
                            Logger.WriteLine($"        No order. (exit = {config.Exit})", ConsoleColor.DarkGray);
                            Logger.WriteWait("", 60);
                            continue;
                        }
#if LICENSE_MODE
                        if (serverTime.Year != 2022 || serverTime.Month != 1 && serverTime.Month != 2)
                        {
                            if (activeOrders.Count > 0) apiHelper.CancelAllOrders();
                            Logger.WriteLine("This bot is too old. Please contact support.  https://valloontrader.com", ConsoleColor.Green);
                            Logger.WriteWait("", 60);
                            continue;
                        }
#endif
                        int upperHeight = (int)(sma * shovelConfig.UpperLimitX);
                        int upperLimit = sma + upperHeight;
                        //int upperClose = (int)(upperLimit - upperHeight * shovelConfig.UpperCloseX);
                        //int upperStop = (int)(upperLimit + upperHeight * shovelConfig.UpperCloseX * shovelConfig.UpperStopX);

                        int lowerHeight = (int)(sma * shovelConfig.LowerLimitX);
                        int lowerLimit = sma - lowerHeight;
                        //int lowerClose = (int)(lowerLimit + lowerHeight * shovelConfig.LowerCloseX);
                        //int lowerStop = (int)(lowerLimit - lowerHeight * shovelConfig.LowerCloseX * shovelConfig.LowerStopX);

                        int upperQty = BitMEXApiHelper.FixQty((int)(margin.WalletBalance.Value * lastPrice * config.UpperQtyX / 100000000));
                        int lowerQty = BitMEXApiHelper.FixQty((int)(margin.WalletBalance.Value * lastPrice * config.LowerQtyX / 100000000));

                        bool needUpperLimit = true, needLowerLimit = true;
                        List<string> cancelOrderList = new List<string>();
                        int lastSma;
                        foreach (Order order in botOrderList)
                        {
                            lastSma = GetSMA(order.Text);
                            if (order.Side == "Buy" && order.Text.Contains("<LIMIT>"))
                            {
                                if (config.BuyOrSell == 2)
                                {
                                    cancelOrderList.Add(order.OrderID);
#if !LICENSE_MODE
                                    Logger.WriteLine($"        Lower limit has been canceled (buy blocked): qty = {order.OrderQty}, sma = {sma}, height = {lowerHeight}, price = {lowerLimit}");
#endif
                                }
                                else if (Math.Abs(lastSma - sma) > sma * shovelConfig.LowerLimitX * .05m)
                                {
                                    if (lowerLimit < lastPrice)
                                    {
                                        Order newOrder = apiHelper.OrderAmend(new Order()
                                        {
                                            OrderID = order.OrderID,
                                            OrderQty = order.OrderQty,
                                            Price = lowerLimit,
                                            Text = $"<BOT><LIMIT><A={sma}></BOT>",
                                        });
#if !LICENSE_MODE
                                        Logger.WriteLine($"        Lower limit order have been amended: qty = {order.OrderQty}, sma = {sma}, height = {lowerHeight}, price = {lowerLimit}");
#endif
                                        Logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                                    }
                                    else
                                    {
                                        cancelOrderList.Add(order.OrderID);
#if !LICENSE_MODE
                                        Logger.WriteLine($"        Lower limit has been canceled: qty = {order.OrderQty}, sma = {sma}, height = {lowerHeight}, price = {lowerLimit} >= {lastPrice}");
#endif
                                    }
                                }
                                needLowerLimit = false;
                            }
                            else if (order.Side == "Sell" && order.Text.Contains("<LIMIT>"))
                            {
                                if (config.BuyOrSell == 1)
                                {
                                    cancelOrderList.Add(order.OrderID);
#if !LICENSE_MODE
                                    Logger.WriteLine($"        Upper limit has been canceled (sell blocked): qty = {order.OrderQty}, sma = {sma}, height = {lowerHeight}, price = {lowerLimit}");
#endif
                                }
                                else if (Math.Abs(lastSma - sma) > sma * shovelConfig.UpperLimitX * .05m)
                                {
                                    if (upperLimit > lastPrice)
                                    {
                                        Order newOrder = apiHelper.OrderAmend(new Order()
                                        {
                                            OrderID = order.OrderID,
                                            OrderQty = order.OrderQty,
                                            Price = upperLimit,
                                            Text = $"<BOT><LIMIT><A={sma}></BOT>",
                                        });
#if !LICENSE_MODE
                                        Logger.WriteLine($"        Upper limit order have been amended: qty = {order.OrderQty}, sma = {sma}, height = {upperHeight}, price = {upperLimit}");
#endif
                                        Logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                                    }
                                    else
                                    {
                                        cancelOrderList.Add(order.OrderID);
#if !LICENSE_MODE
                                        Logger.WriteLine($"        Upper limit has been canceled: qty = {order.OrderQty}, sma = {sma}, height = {upperHeight}, price = {upperLimit} <= {lastPrice}");
#endif
                                    }
                                }
                                needUpperLimit = false;
                            }
                            else
                            {
                                cancelOrderList.Add(order.OrderID);
                            }
                        }
                        if (cancelOrderList.Count > 0)
                        {
                            int canceledCount = apiHelper.CancelOrders(cancelOrderList).Count;
                            Logger.WriteLine($"        {canceledCount} orders have been canceled.");
                        }
                        if (needLowerLimit && config.BuyOrSell != 2)
                        {
                            if (lowerLimit < lastPrice)
                            {
                                Order newOrder = apiHelper.OrderNew(new Order
                                {
                                    Symbol = BitMEXApiHelper.SYMBOL,
                                    Side = "Buy",
                                    OrderQty = lowerQty,
                                    Price = lowerLimit,
                                    OrdType = "Limit",
                                    Text = $"<BOT><LIMIT><A={sma}></BOT>",
                                });
#if !LICENSE_MODE
                                Logger.WriteLine($"        New lower limit order have been created: qty = {lowerQty}, sma = {sma}, height = {lowerHeight}, price = {lowerLimit}");
#endif
                                Logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                            }
                            else
                            {
#if !LICENSE_MODE
                                Logger.WriteLine($"        No lower limit order: qty = {lowerQty}, sma = {sma}, height = {lowerHeight}, price = {lowerLimit} >= {lastPrice}");
#endif
                            }
                        }
                        if (needUpperLimit && config.BuyOrSell != 1)
                        {
                            if (upperLimit > lastPrice)
                            {
                                Order newOrder = apiHelper.OrderNew(new Order
                                {
                                    Symbol = BitMEXApiHelper.SYMBOL,
                                    Side = "Sell",
                                    OrderQty = upperQty,
                                    Price = upperLimit,
                                    OrdType = "Limit",
                                    Text = $"<BOT><LIMIT><A={sma}></BOT>",
                                });
#if !LICENSE_MODE
                                Logger.WriteLine($"        New upper limit order have been created: qty = {upperQty}, sma = {sma}, height = {upperHeight}, price = {upperLimit}");
#endif
                                Logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                            }
                            else
                            {
#if !LICENSE_MODE
                                Logger.WriteLine($"        No upper limit order: qty = {upperQty}, sma = {sma}, height = {upperHeight}, price = {upperLimit} <= {lastPrice}");
#endif
                            }
                        }
                    }
                    else
                    {
                        if (positionQty > 0)
                        {
                            string closeSide = "Sell";
                            bool needCloseOrder = true;
                            bool needStopOrder = true;
                            List<string> cancelOrderList = new List<string>();
                            foreach (Order order in activeOrders)
                            {
                                if (order.Side == closeSide && order.OrdType == "Limit" && ((order.OrderQty == null || order.OrderQty.Value == Math.Abs(positionQty)) && order.ExecInst.Contains("Close") || order.OrderQty != null && order.OrderQty.Value == Math.Abs(positionQty) && order.ExecInst.Contains("ReduceOnly")))
                                    needCloseOrder = false;
                                else if (order.Side == closeSide && order.OrdType == "Stop" && ((order.OrderQty == null || order.OrderQty.Value == Math.Abs(positionQty)) && order.ExecInst.Contains("Close") || order.OrderQty != null && order.OrderQty.Value == Math.Abs(positionQty) && order.ExecInst.Contains("ReduceOnly")))
                                    needStopOrder = false;
                                else if (order.Text.Contains("<BOT>"))
                                    cancelOrderList.Add(order.OrderID);
                            }
                            if (cancelOrderList.Count > 0)
                            {
                                int canceledCount = apiHelper.CancelOrders(cancelOrderList).Count;
                                Logger.WriteLine($"        {canceledCount} orders have been canceled.");
                            }
                            if (needCloseOrder || needStopOrder)
                            {
                                List<Order> lastFilledOrders = apiHelper.GetOrders("{\"ordStatus\":\"Filled\",\"ordType\":\"Limit\"}");
                                int lastSma = 0;
                                foreach (Order order in lastFilledOrders)
                                {
                                    if (order.Side == "Buy" && order.Text.Contains("<BOT>") && order.Text.Contains("<LIMIT>"))
                                    {
                                        lastSma = GetSMA(order.Text);
                                        break;
                                    }
                                }
                                if (lastSma == 0)
                                {
                                    if (config.ForceClose == 1)
                                    {
                                        apiHelper.OrderNewMarketClose(closeSide);
                                        Logger.WriteLine($"        Unrecognized position forcibly closed.", ConsoleColor.Red);
                                    }
                                    else
                                    {
                                        Logger.WriteLine($"        Unrecognized position exists. Waiting to be closed...", ConsoleColor.Red);
                                        Logger.WriteWait("", 30);
                                    }
                                }
                                else
                                {
                                    int lowerHeight = (int)(lastSma * shovelConfig.LowerLimitX);
                                    int lowerLimit = lastSma - lowerHeight;
                                    int lowerClose = (int)(lowerLimit + lowerHeight * shovelConfig.LowerCloseX);
                                    int lowerStop = (int)(lowerLimit - lowerHeight * shovelConfig.LowerCloseX * shovelConfig.LowerStopX);

                                    if (needCloseOrder)
                                    {
                                        Order newOrder = apiHelper.OrderNew(new Order
                                        {
                                            Symbol = BitMEXApiHelper.SYMBOL,
                                            Side = closeSide,
                                            OrderQty = positionQty,
                                            Price = lowerClose,
                                            OrdType = "Limit",
                                            ExecInst = "ReduceOnly",
                                            Text = $"<BOT><CLOSE><A={lastSma}></BOT>",
                                        });
#if !LICENSE_MODE
                                        Logger.WriteLine($"        New lower close order has been created: qty = {positionQty}, sma = {lastSma}, height = {lowerHeight}, price = {lowerClose}");
#endif
                                        Logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                                    }
                                    if (needStopOrder)
                                    {
                                        Order newOrder = apiHelper.OrderNew(new Order
                                        {
                                            Symbol = BitMEXApiHelper.SYMBOL,
                                            Side = closeSide,
                                            OrderQty = positionQty,
                                            StopPx = lowerStop,
                                            OrdType = "Stop",
                                            ExecInst = "LastPrice,ReduceOnly",
                                            Text = $"<BOT><STOP><A={lastSma}></BOT>",
                                        });
#if !LICENSE_MODE
                                        Logger.WriteLine($"        New lower stop order has been created: qty = {positionQty}, sma = {lastSma}, height = {lowerHeight}, price = {lowerStop}");
#endif
                                        Logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                                    }
                                }
                            }
                        }
                        else if (positionQty < 0)
                        {
                            string closeSide = "Buy";
                            bool needCloseOrder = true;
                            bool needStopOrder = true;
                            List<string> cancelOrderList = new List<string>();
                            foreach (Order order in activeOrders)
                            {
                                if (order.Side == closeSide && order.OrdType == "Limit" && ((order.OrderQty == null || order.OrderQty.Value == Math.Abs(positionQty)) && order.ExecInst.Contains("Close") || order.OrderQty != null && order.OrderQty.Value == Math.Abs(positionQty) && order.ExecInst.Contains("ReduceOnly")))
                                    needCloseOrder = false;
                                else if (order.Side == closeSide && order.OrdType == "Stop" && ((order.OrderQty == null || order.OrderQty.Value == Math.Abs(positionQty)) && order.ExecInst.Contains("Close") || order.OrderQty != null && order.OrderQty.Value == Math.Abs(positionQty) && order.ExecInst.Contains("ReduceOnly")))
                                    needStopOrder = false;
                                else if (order.Text.Contains("<BOT>"))
                                    cancelOrderList.Add(order.OrderID);
                            }
                            if (cancelOrderList.Count > 0)
                            {
                                int canceledCount = apiHelper.CancelOrders(cancelOrderList).Count;
                                Logger.WriteLine($"        {canceledCount} orders have been canceled.");
                            }
                            if (needCloseOrder || needStopOrder)
                            {
                                List<Order> lastFilledOrders = apiHelper.GetOrders("{\"ordStatus\":\"Filled\",\"ordType\":\"Limit\"}");
                                int lastSma = 0;
                                foreach (Order order in lastFilledOrders)
                                {
                                    if (order.Side == "Sell" && order.Text.Contains("<BOT>") && order.Text.Contains("<LIMIT>"))
                                    {
                                        lastSma = GetSMA(order.Text);
                                        break;
                                    }
                                }
                                if (lastSma == 0)
                                {
                                    if (config.ForceClose == 1)
                                    {
                                        apiHelper.OrderNewMarketClose(closeSide);
                                        Logger.WriteLine($"        Unrecognized position forcibly closed.", ConsoleColor.Red);
                                    }
                                    else
                                    {
                                        Logger.WriteLine($"        Unrecognized position exists. Waiting to be closed...", ConsoleColor.Red);
                                        Logger.WriteWait("", 30);
                                    }
                                }
                                else
                                {

                                    int upperHeight = (int)(lastSma * shovelConfig.UpperLimitX);
                                    int upperLimit = lastSma + upperHeight;
                                    int upperClose = (int)(upperLimit - upperHeight * shovelConfig.UpperCloseX);
                                    int upperStop = (int)(upperLimit + upperHeight * shovelConfig.UpperCloseX * shovelConfig.UpperStopX);

                                    if (needCloseOrder)
                                    {
                                        Order newOrder = apiHelper.OrderNew(new Order
                                        {
                                            Symbol = BitMEXApiHelper.SYMBOL,
                                            Side = closeSide,
                                            OrderQty = Math.Abs(positionQty),
                                            Price = upperClose,
                                            OrdType = "Limit",
                                            ExecInst = "ReduceOnly",
                                            Text = $"<BOT><CLOSE><A={lastSma}></BOT>",
                                        });
#if !LICENSE_MODE
                                        Logger.WriteLine($"        New upper close order has been created: qty = {positionQty}, sma = {lastSma}, height = {upperHeight}, price = {upperClose}");
#endif
                                        Logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                                    }
                                    if (needStopOrder)
                                    {
                                        Order newOrder = apiHelper.OrderNew(new Order
                                        {
                                            Symbol = BitMEXApiHelper.SYMBOL,
                                            Side = closeSide,
                                            OrderQty = Math.Abs(positionQty),
                                            StopPx = upperStop,
                                            OrdType = "Stop",
                                            ExecInst = "LastPrice,ReduceOnly",
                                            Text = $"<BOT><STOP><A={lastSma}></BOT>",
                                        });
#if !LICENSE_MODE
                                        Logger.WriteLine($"        New upper stop order has been created: qty = {positionQty}, sma = {lastSma}, height = {upperHeight}, price = {upperStop}");
#endif
                                        Logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                                    }
                                }
                            }
                        }
                    }
                    if (lastWalletBalance != walletBalance)
                    {
                        if (lastWalletBalance != 0)
                        {
                            string filenameSuffix = "-balance";
                            if (!Logger.ExistFile(filenameSuffix))
                                Logger.WriteFile($"[{timeText}  ({++loopIndex:D5})]    {lastWalletBalance:F8}", filenameSuffix);
                            string suffix = null;
                            if (positionQty != 0) suffix = $"{positionQty}";
                            Logger.WriteFile($"[{timeText}  ({++loopIndex:D5})]    {walletBalance:F8}    {walletBalance - lastWalletBalance:F8}    {suffix}", filenameSuffix);
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