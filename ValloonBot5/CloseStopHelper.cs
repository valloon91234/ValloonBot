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
    public class CloseStopHelper
    {

        public void Run(Config config = null)
        {
            int loopIndex = 0;
            DateTime? lastLoopTime = null;
            decimal lastWalletBalance = 0;
            decimal targetEntryPrice = 0;
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
                        logger.WriteLine();
                    }
                    string symbol = config.Symbol.ToUpper();
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
                    Position position = apiHelper.GetPosition(symbol);
                    decimal positionEntryPrice = 0;
                    int positionQty = 0;
                    {
                        string consoleTitle = $"$ {lastPrice:N2}  /  {walletBalance:N8} XBT";
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

                    if (config.Exit > 0)
                    {
                        logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  No order. (exit = {config.Exit})", ConsoleColor.DarkGray);
                        goto endLoop;
                    }

                    if (positionQty == 0)
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
                    else if (positionQty > 0)
                    {
                        if (config.Stop > 0)
                        {
                            decimal stopPrice = (int)Math.Floor(positionEntryPrice * (1 - config.Stop) * 100) / 100m;
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
                        if (config.Close > 0)
                        {
                            var closeLimitPrice = Math.Ceiling(positionEntryPrice * (1 + config.Close) * 100) / 100m;
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
                    else if (positionQty < 0)
                    {
                        if (config.Stop > 0)
                        {
                            decimal stopPrice = (int)Math.Ceiling(positionEntryPrice * (1 + config.Stop) * 100) / 100m;
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
                        if (config.Close > 0)
                        {
                            var closeLimitPrice = Math.Floor(positionEntryPrice * (1 - config.Close) * 100) / 100m;
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
                        logger.WriteLine($"        Invalid position qty: {positionQty}");
                    }

                endLoop:;
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

                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Sleeping {config.Interval:N0} seconds ({apiHelper.RequestCount} requests) ...", ConsoleColor.DarkGray, false);
                    Thread.Sleep(config.Interval * 1000);
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