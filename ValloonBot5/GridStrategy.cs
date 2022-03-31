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
    public class GridStrategy
    {

        public void Run(Config config = null)
        {
            int loopIndex = 0;
            DateTime? lastLoopTime = null;
            bool lastBusy = false;
            bool stopMode = false;
            int prevPrice = 0;
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
                    string symbol = config.Symbol.ToUpper();
                    Margin margin = apiHelper.GetMargin(BitMEXApiHelper.CURRENCY_XBt);
                    decimal walletBalance = margin.WalletBalance.Value / 100000000m;
                    List<Order> activeOrderList = apiHelper.GetActiveOrders(symbol);
                    Instrument instrument = apiHelper.GetInstrument(symbol);
                    decimal lastPrice = instrument.LastPrice.Value;
                    decimal markPrice = instrument.MarkPrice.Value;
                    Position position = apiHelper.GetPosition(symbol);
                    decimal positionEntryPrice = 0;
                    int positionQty = 0;
                    if (symbol == "XBTUSD")
                    {
                        if (config.PriceHeight < 10)
                        {
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Height is too low for {symbol}. Height = {config.PriceHeight}", ConsoleColor.Red);
                            goto endLoop;
                        }
                        if (config.MinPrice < 10000 || config.MinPrice >= config.MaxPrice)
                        {
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Invalid MinPrice or MaxPrice for {symbol}.", ConsoleColor.Red);
                            goto endLoop;
                        }
                    }
                    else if (symbol == "SOLUSD")
                    {
                        if (config.PriceHeight > 50)
                        {
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Height is too high for {symbol}. Height = {config.PriceHeight}", ConsoleColor.Red);
                            goto endLoop;
                        }
                        if (config.MinPrice > 10000 || config.MinPrice >= config.MaxPrice)
                        {
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Invalid MinPrice or MaxPrice for {symbol}.", ConsoleColor.Red);
                            goto endLoop;
                        }
                    }
                    else
                    {
                        logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Invalid symbol: {symbol}.", ConsoleColor.Red);
                        goto endLoop;
                    }
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

                    decimal height = config.PriceHeight;
                    if (lastBusy)
                    {
                        logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  lastBusy");
                        lastBusy = prevPrice - lastPrice > height || lastPrice - prevPrice > height;
                    }
                    else if (config.BuyOrSell == 1)
                    {
                        Order oldLimitOrder = null;
                        foreach (Order order in botOrderList)
                        {
                            if (order.Side == "Buy")
                            {
                                oldLimitOrder = order;
                                break;
                            }
                        }

                        if (prevPrice > 0 && prevPrice - lastPrice > height)
                        {
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  price is falling. diff = {lastPrice - prevPrice}");
                            if (oldLimitOrder != null) apiHelper.CancelOrder(oldLimitOrder.OrderID);
                        }
                        else if (prevPrice > 0 && lastPrice - prevPrice > height)
                        {
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  price is rising. diff = {lastPrice - prevPrice}");
                        }
                        else if (stopMode || lastPrice < config.MinPrice)
                        {
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  price < MinPrice. stopMode = {stopMode}");
                            stopMode = true;
                        }
                        else if (lastPrice > config.MaxPrice)
                        {
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  price > MaxPrice.");
                        }
                        else
                        {
                            lastBusy = false;
                            List<Order> lastFilledOrders = apiHelper.GetFilledOrders(symbol);
                            Order lastFilledOrder = null;
                            foreach (Order order in lastFilledOrders)
                            {
                                if (order.Text.Contains("<GRID-LIMIT>") && order.Text.Contains($"<H={height}>"))
                                {
                                    lastFilledOrder = order;
                                    break;
                                }
                            }

                            decimal limitPrice = config.StartPrice, closePrice = 0;
                            while (limitPrice >= lastPrice)
                                limitPrice -= config.PriceHeight;
                            if (positionQty <= 0)
                            {
                            }
                            else if (lastFilledOrder == null)
                            {
                                closePrice = positionEntryPrice + height;
                            }
                            else if (lastFilledOrder.Side == "Buy")
                            {
                                //limitPrice = lastFilledOrder.Price.Value - height;
                                closePrice = lastFilledOrder.Price.Value + height;
                                if (limitPrice > lastFilledOrder.Price.Value - height / 2)
                                {
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  No new limit order. limitPrice = {limitPrice}, lastFilledShort = {lastFilledOrder.Price.Value}");
                                    limitPrice = 0;
                                }
                                while (closePrice <= lastPrice)
                                {
                                    closePrice += height;
                                }
                            }
                            else if (lastFilledOrder.Side == "Sell")
                            {
                                //limitPrice = lastFilledOrder.Price.Value - height;
                                closePrice = lastFilledOrder.Price.Value + height;
                                if (limitPrice > lastFilledOrder.Price.Value - height / 2)
                                {
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  No new limit order. limitPrice = {limitPrice}, lastFilledShort = {lastFilledOrder.Price.Value}");
                                    limitPrice = 0;
                                }
                                while (closePrice <= lastPrice)
                                {
                                    closePrice += height;
                                }
                            }

                            //if (closePrice - lastPrice < height / 2)
                            //    closePrice = 0;
                            int closeQty = config.Qty;
                            if (closePrice > 0)
                            {
                                int closeQtySum = 0;
                                foreach (Order order in activeOrderList)
                                {
                                    if (order.Side == "Sell") closeQtySum += order.OrderQty.Value;
                                }
                                if (closeQtySum >= positionQty) closeQty = 0;
                                if (positionQty - closeQtySum < closeQty) closeQty = positionQty - closeQtySum;
                            }

                            foreach (Order order in botOrderList)
                            {
                                if (order.Side == "Buy" && order.Price.Value == limitPrice) limitPrice = 0;
                                if (order.Side == "Sell" && order.Price.Value == closePrice) closePrice = 0;
                            }

                            if (limitPrice > 0)
                            {
                                if (oldLimitOrder == null)
                                {
                                    Order newOrder = apiHelper.OrderNew(new Order
                                    {
                                        Symbol = symbol,
                                        Side = "Buy",
                                        OrderQty = config.Qty,
                                        Price = limitPrice,
                                        OrdType = "Limit",
                                        Text = $"<BOT><GRID-LIMIT><H={height}></BOT>",
                                    });
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New limit order created: qty = {config.Qty}, price = {limitPrice}");
                                    logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                                }
                                else if (oldLimitOrder.Price.Value != limitPrice)
                                {
                                    //Order newOrder = apiHelper.OrderAmend(new Order()
                                    //{
                                    //    OrderID = oldLimitOrder.OrderID,
                                    //    OrderQty = config.Qty,
                                    //    Price = limitPrice,
                                    //    Text = $"<BOT><GRID-LIMIT><H={height}></BOT>",
                                    //});
                                    //logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New limit order amended: qty = {oldLimitOrder.OrderQty}, price = {oldLimitOrder.Price.Value}  ->  {limitPrice}");
                                    //logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                                }
                            }
                            if (closePrice > 0 && closeQty > 0)
                            {
                                Order limitOrder = apiHelper.OrderNew(new Order
                                {
                                    Symbol = symbol,
                                    Side = "Sell",
                                    OrderQty = closeQty,
                                    Price = closePrice,
                                    OrdType = "Limit",
                                    ExecInst = "ReduceOnly",
                                    Text = $"<BOT><GRID-LIMIT><CLOSE><H={height}></BOT>",
                                });
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New close order created: qty = {config.Qty}, price = {closePrice}");
                                logger.WriteFile("--- " + JObject.FromObject(limitOrder).ToString(Formatting.None));
                            }
                        }
                    }
                    prevPrice = (int)lastPrice;

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
                            if (positionQty != 0) suffix = $"        p = {positionQty}";
                            logger2.WriteFile($"[{BitMEXApiHelper.ServerTime:yyyy-MM-dd  HH:mm:ss}  ({++loopIndex:D6})]    price = {lastPrice:F1}  /  {markPrice:F2}    balance = {walletBalance:F8}  /  {walletBalance - lastWalletBalance:F8}  /  {(walletBalance - lastWalletBalance) / lastWalletBalance * 100:N4} %{suffix}");
                        }
                        lastWalletBalance = walletBalance;
                    }

                    int waitSeconds = config.Interval;
                    Logger.WriteWait($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Sleeping {waitSeconds:N0} seconds ({apiHelper.RequestCount} requests) ", waitSeconds, 1);
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