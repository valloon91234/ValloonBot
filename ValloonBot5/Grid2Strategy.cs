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
    public class Grid2Strategy
    {

        public void Run(Config config = null)
        {
            int loopIndex = 0;
            DateTime? lastLoopTime = null;
            int lastPositionQty = 0;
            decimal lastWalletBalance = 0;
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
                    string symbol = config.Symbol.ToUpper();
                    if (configUpdated)
                    {
                        loopIndex = 0;
                        logger.WriteLine($"\r\n[{BitMEXApiHelper.ServerTime:yyyy-MM-dd  HH:mm:ss}]  Config loaded.", ConsoleColor.Green);
                        logger.WriteLine(JObject.FromObject(config).ToString(Formatting.Indented));
                        logger.WriteLine();
                    }
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
                        string consoleTitle = $"$ {lastPrice:N2}  /  {walletBalance:N8}";
                        if (position != null && position.CurrentQty.Value != 0) consoleTitle += "  /  1 Position";
                        consoleTitle += $"  <{config.Username}>  |   {Config.APP_NAME}  v{fileModifiedTime:yyyy.MM.dd HH:mm:ss}   by  ValloonTrader.com";
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

                    if (config.Exit == 2)
                    {
                        logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  No order. (exit = {config.Exit})", ConsoleColor.DarkGray);
                        goto endLoop;
                    }
                    if (positionQty == 0)
                    {
                        bool exit = false;
#if LICENSE_MODE
                        if (serverTime.Year != 2022 || serverTime.Month != 2)
                        {
                            logger.WriteLine($"This bot is too old. Please contact support.  https://valloontrader.com", ConsoleColor.Green);
                            exist = true;
                        } else 
#endif
                        if (config.Exit == 1)
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
                            goto endLoop;
                        }
                    }

                    {
                        //List<string> cancelOrderList = new List<string>();
                        //foreach (Order order in botOrderList)
                        //{
                        //    if (!order.Text.Contains($"<H={config.MinPrice}/{config.MaxPrice}/{config.PriceHeight}>"))
                        //        cancelOrderList.Add(order.OrderID);
                        //}
                        //if (cancelOrderList.Count > 0)
                        //{
                        //    int canceledCount = apiHelper.CancelOrders(cancelOrderList).Count;
                        //    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  {canceledCount} old orders have been canceled.");
                        //}

                        Order lastFilledOrder = null;
                        List<Order> lastFilledOrders = apiHelper.GetFilledOrders(symbol);
                        foreach (Order order in lastFilledOrders)
                        {
                            if (order.Text.Contains("<GRID-LIMIT>"))
                            {
                                lastFilledOrder = order;
                                break;
                            }
                        }

                        if (lastFilledOrder != null)
                            logger.WriteLine($"        lastPositionQty = {lastPositionQty}    lastFilledPrice = {lastFilledOrder.Price.Value}    qty = {lastFilledOrder.OrderQty} / {lastFilledOrder.CumQty} / {lastFilledOrder.LeavesQty}    OrdStatus = {lastFilledOrder.OrdStatus}", ConsoleColor.DarkGray);

                        List<Order> newOrderList = new List<Order>();
                        if (config.BuyOrSell == 1)
                        {
                            int restPositionQty = positionQty;
                            for (decimal price = config.MinPrice; price <= config.MaxPrice; price += config.PriceHeight)
                            {
                                Order newOrder;
                                if (lastFilledOrder != null && lastFilledOrder.Price.Value == price) continue;
                                else if (price < lastPrice && (lastFilledOrder == null || price <= lastFilledOrder.Price.Value)) newOrder = new Order
                                {
                                    Symbol = symbol,
                                    Side = "Buy",
                                    OrderQty = config.Qty,
                                    Price = price,
                                    OrdType = "Limit",
                                    ExecInst = "ParticipateDoNotInitiate",
                                    Text = $"<BOT><GRID-LIMIT><H={config.MinPrice}/{config.MaxPrice}/{config.PriceHeight}></BOT>",
                                };
                                else if (price > lastPrice && (restPositionQty -= config.Qty) >= 0 && (lastFilledOrder == null || price >= lastFilledOrder.Price.Value)) newOrder = new Order
                                {
                                    Symbol = symbol,
                                    Side = "Sell",
                                    OrderQty = config.Qty,
                                    Price = price,
                                    OrdType = "Limit",
                                    ExecInst = "ParticipateDoNotInitiate,ReduceOnly",
                                    Text = $"<BOT><GRID-LIMIT><H={config.MinPrice}/{config.MaxPrice}/{config.PriceHeight}></BOT>",
                                };
                                else continue;
                                newOrderList.Add(newOrder);
                            }
                        }
                        else if (config.BuyOrSell == 2)
                        {
                            int restPositionQty = positionQty;
                            for (decimal price = config.MinPrice; price <= config.MaxPrice; price += config.PriceHeight)
                            {
                                Order newOrder;
                                if (lastFilledOrder != null && lastFilledOrder.Price.Value == price) continue;
                                else if (price < lastPrice && (restPositionQty += config.Qty) <= 0 && (lastFilledOrder == null || price <= lastFilledOrder.Price.Value)) newOrder = new Order
                                {
                                    Symbol = symbol,
                                    Side = "Buy",
                                    OrderQty = config.Qty,
                                    Price = price,
                                    OrdType = "Limit",
                                    ExecInst = "ParticipateDoNotInitiate,ReduceOnly",
                                    Text = $"<BOT><GRID-LIMIT><H={config.MinPrice}/{config.MaxPrice}/{config.PriceHeight}></BOT>",
                                };
                                else if (price > lastPrice && (lastFilledOrder == null || price >= lastFilledOrder.Price.Value)) newOrder = new Order
                                {
                                    Symbol = symbol,
                                    Side = "Sell",
                                    OrderQty = config.Qty,
                                    Price = price,
                                    OrdType = "Limit",
                                    ExecInst = "ParticipateDoNotInitiate",
                                    Text = $"<BOT><GRID-LIMIT><H={config.MinPrice}/{config.MaxPrice}/{config.PriceHeight}></BOT>",
                                };
                                else continue;
                                newOrderList.Add(newOrder);
                            }
                        }
                        else if (config.BuyOrSell == 3)
                        {
                            for (decimal price = config.MinPrice; price <= config.MaxPrice; price += config.PriceHeight)
                            {
                                string side;
                                //if (lastFilledOrder != null && lastFilledOrder.Price.Value == price && (positionQty < 0 && positionEntryPrice - price < config.PriceHeight || positionQty > 0 && price - positionEntryPrice < config.PriceHeight)) continue;
                                if (lastFilledOrder != null && lastFilledOrder.Price.Value == price) continue;
                                else if (price < lastPrice && (lastFilledOrder == null || price <= lastFilledOrder.Price.Value)) side = "Buy";
                                else if (price > lastPrice && (lastFilledOrder == null || price >= lastFilledOrder.Price.Value)) side = "Sell";
                                else continue;
                                Order newOrder = new Order
                                {
                                    Symbol = symbol,
                                    Side = side,
                                    OrderQty = config.Qty,
                                    Price = price,
                                    OrdType = "Limit",
                                    ExecInst = "ParticipateDoNotInitiate",
                                    Text = $"<BOT><GRID-LIMIT><H={config.MinPrice}/{config.MaxPrice}/{config.PriceHeight}></BOT>",
                                };
                                newOrderList.Add(newOrder);
                            }
                        }

                        foreach (Order newOrder in newOrderList)
                        {
                            foreach (Order oldOrder in botOrderList)
                            {
                                if (oldOrder.Side == newOrder.Side && oldOrder.OrdType == newOrder.OrdType && oldOrder.Price == newOrder.Price)
                                {
                                    botOrderList.Remove(oldOrder);
                                    goto nextNewOrder;
                                }
                            }
                            Order resultOrder = apiHelper.OrderNew(newOrder);
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New limit order created: qty = {config.Qty}, price = {newOrder.Price}, lastFilled = {(lastFilledOrder == null ? 0 : lastFilledOrder.Price.Value)}");
                            logger.WriteFile("--- " + JObject.FromObject(resultOrder).ToString(Formatting.None));
                        nextNewOrder:;
                        }

                        List<string> cancelOrderList = new List<string>();
                        foreach (Order order in botOrderList)
                        {
                            if (order.OrdStatus == "New")
                                cancelOrderList.Add(order.OrderID);
                        }
                        if (cancelOrderList.Count > 0)
                        {
                            int canceledCount = apiHelper.CancelOrders(cancelOrderList).Count;
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  {canceledCount} old orders have been canceled.");
                        }
                    }
                    lastPositionQty = positionQty;

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

                    int waitSeconds = config.Interval;
                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Sleeping {waitSeconds:N0} seconds ({apiHelper.RequestCount} requests) ...", ConsoleColor.DarkGray, false);
                    Thread.Sleep(waitSeconds * 1000);
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