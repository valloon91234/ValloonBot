//#define LICENSE_MODE

using System;
using System.Collections.Generic;
using System.Threading;
using IO.Swagger.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/**
 * @author Valloon Project
 * @version 4.0
 * @2022-01-20
 */
namespace Valloon.BitMEX
{
    public class DropStrategy
    {

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
                    config = Config.Load(out bool configUpdated, lastLoopTime == null || lastLoopTime.Value.Day != currentLoopTime.Day);
                    DropConfig dropConfig = config.Drop;
                    apiHelper = new BitMEXApiHelper(config.ApiKey, config.ApiSecret, config.TestnetMode);
                    lastLoopTime = currentLoopTime;
                    if (configUpdated) loopIndex = 0;
                    Margin margin = apiHelper.GetMargin();
                    decimal walletBalance = margin.WalletBalance.Value / 100000000m;
                    List<Order> activeOrders = apiHelper.GetActiveOrders();
                    Instrument instrument = apiHelper.GetInstrument();
                    decimal lastPrice = instrument.LastPrice.Value;
                    decimal markPrice = instrument.MarkPrice.Value;
                    List<TradeBin> binList = apiHelper.GetRencentBinList(dropConfig.BinSize, 1000, true);
                    string currentCandleTimestamp = binList[0].Timestamp.Value.ToString("yyyy-MM-ddTHH:mm:ss");
                    DateTime serverTime = BitMEXApiHelper.ServerTime.Value;
                    string timeText = serverTime.ToString("yyyy-MM-dd  HH:mm:ss");
                    List<Order> botLimitOrderList = new List<Order>();
                    foreach (Order order in activeOrders)
                        if (order.OrdType == "Limit" && order.Text.Contains("<BOT>")) botLimitOrderList.Add(order);
                    {
                        decimal unavailableMarginPercent = 100m * (margin.WalletBalance.Value - margin.AvailableMargin.Value) / margin.WalletBalance.Value;
                        Logger.WriteLine($"[{timeText}  ({++loopIndex})]    $ {lastPrice:F1}  /  $ {markPrice:F2}  /  {binList[1].Volume:N0}  /  {binList[0].Volume:N0}    {walletBalance:N8} XBT    {botLimitOrderList.Count} / {activeOrders.Count} / {unavailableMarginPercent:N2} %", ConsoleColor.White);
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
                            Logger.WriteLine($"        entry = {positionEntryPrice:F1}    qty = {positionQty}    liq = {position.LiquidationPrice}    {unrealisedPercent:N2} % / {nowLoss:N2} %", ConsoleColor.Green);
                        }
                    }
                    if (positionQty == 0)
                    {
                        if (config.Exit > 0)
                        {
                            if (activeOrders.Count > 0) apiHelper.CancelAllOrders();
                            Logger.WriteLine($"No order. (exit = {config.Exit})", ConsoleColor.DarkGray);
                            Logger.WriteWait("", 60);
                            continue;
                        }
#if LICENSE_MODE
                        if (serverTime.Year != 2022 && serverTime.Month != 1)
                        {
                            if (activeOrders.Count > 0) apiHelper.CancelAllOrders();
                            Logger.WriteLine("This bot is too old. Please contact support.  https://valloontrader.com", ConsoleColor.Green);
                            Logger.WriteWait("", 60);
                            continue;
                        }
#endif
                        decimal heightX = lastPrice * dropConfig.HeightRatio;
                        bool resetOrder = configUpdated || botLimitOrderList.Count != dropConfig.OrderCount;
                        if (!resetOrder)
                        {
                            if (dropConfig.BuyOrSell == 1)
                            {
                                decimal highest = 0;
                                foreach (var order in activeOrders)
                                {
                                    if (order.OrdType != "Limit" || !order.Text.Contains("<BOT>")) continue;
                                    if (order.Price != null && order.Price.Value > highest) highest = order.Price.Value;
                                }
                                if (lastPrice - highest > heightX * 1.5m) resetOrder = true;
                            }
                            else if (dropConfig.BuyOrSell == 2)
                            {
                                decimal lowest = int.MaxValue;
                                foreach (var order in activeOrders)
                                {
                                    if (order.OrdType != "Limit" || !order.Text.Contains("<BOT>")) continue;
                                    if (order.Price != null && order.Price.Value < lowest) lowest = order.Price.Value;
                                }
                                if (lowest - lastPrice > heightX * 1.5m) resetOrder = true;
                            }
                        }
                        if (resetOrder)
                        {
                            if (botLimitOrderList.Count > 0)
                            {
                                List<string> cancelOrderIdList = new List<string>();
                                foreach (var order in botLimitOrderList)
                                    cancelOrderIdList.Add(order.OrderID);
                                int canceledCount = apiHelper.CancelOrders(cancelOrderIdList).Count;
                                Logger.WriteLine($"{canceledCount} limit orders have been canceled.");
                            }
                            List<Order> newOrderList = new List<Order>();
                            int qtyStep = BitMEXApiHelper.FixQty((int)(margin.WalletBalance.Value * lastPrice * dropConfig.QtyRatio / 1000000));
                            for (int i = 0; i < dropConfig.OrderCount; i++)
                            {
                                int qty = BitMEXApiHelper.FixQty((int)(qtyStep * dropConfig.QtyStairs[i]));
                                int distance = (int)(lastPrice * dropConfig.HeightRatio * dropConfig.HeightStairs[i]);
                                if (dropConfig.BuyOrSell == 1)
                                {
                                    int price = (int)lastPrice - distance;
                                    if (price < 0) continue;
                                    newOrderList.Add(new Order
                                    {
                                        Symbol = BitMEXApiHelper.SYMBOL,
                                        OrdType = "Limit",
                                        Side = "Buy",
                                        OrderQty = qty,
                                        Price = price,
                                        Text = $"<BOT><LIMIT><BUY><{lastPrice:F0}><{i + 1}></BOT>"
                                    });
                                    Logger.WriteLine($"    <limit {i + 1}>\t  qty = {qty}  \tbuy_price = {price}  (-{distance})");
                                }
                                else if (dropConfig.BuyOrSell == 2)
                                {
                                    int price = (int)lastPrice + distance;
                                    newOrderList.Add(new Order
                                    {
                                        Symbol = BitMEXApiHelper.SYMBOL,
                                        OrdType = "Limit",
                                        Side = "Sell",
                                        OrderQty = qty,
                                        Price = price,
                                        Text = $"<BOT><LIMIT><SELL><{lastPrice:F0}><{i + 1}></BOT>"
                                    });
                                    Logger.WriteLine($"    <limit {i + 1}>\t  qty = {qty}  \tsell_price = {price}  (+{distance})");
                                }
                            }
                            if (newOrderList.Count > 0)
                            {
                                List<Order> resultOrderList = new List<Order>();
                                foreach (Order order in newOrderList)
                                    resultOrderList.Add(apiHelper.OrderNew(order.Side, order.OrderQty, order.Price, order.StopPx, order.OrdType, order.ExecInst, order.Text));
                                Logger.WriteLine($"    {resultOrderList.Count} limit orders have been created.");
                                Logger.WriteFile("--- " + JArray.FromObject(resultOrderList).ToString(Formatting.None));
                            }
                        }
                    }
                    else
                    {
                        string closeSide;
                        int closePrice;
                        if (positionQty < 0)
                        {
                            closeSide = "Buy";
                            closePrice = (int)(positionEntryPrice - positionEntryPrice * dropConfig.CloseHeightRatio);
                        }
                        else if (positionQty > 0)
                        {
                            closeSide = "Sell";
                            closePrice = (int)(positionEntryPrice + positionEntryPrice * dropConfig.CloseHeightRatio);
                        }
                        else
                        {
                            throw new Exception("positionQty = 0");
                        }
                        bool existCloseOrder = false;
                        List<string> cancelOrderList = new List<string>();
                        foreach (Order order in activeOrders)
                        {
                            if (order.OrdType == "Limit" && order.Side == closeSide)
                            {
                                if (order.OrderQty == null && order.ExecInst.Contains("Close") || order.OrderQty != null && order.OrderQty.Value == Math.Abs(positionQty))
                                {
                                    existCloseOrder = true;
                                }
                                else
                                {
                                    cancelOrderList.Add(order.OrderID);
                                }
                            }
                        }
                        if (cancelOrderList.Count > 0)
                        {
                            int canceledCount = apiHelper.CancelOrders(cancelOrderList).Count;
                            Logger.WriteLine($"{canceledCount} limit close orders have been canceled.");
                        }
                        if (!existCloseOrder)
                        {
                            Order closeOrder = apiHelper.OrderNewLimitClose(closeSide, closePrice, Math.Abs(positionQty), $"<BOT><LIMIT><{closeSide.ToUpper()}><CLOSE></BOT>");
                            Logger.WriteLine($"New position close order has been created. (qty = {closeOrder.OrderQty}, price = {closePrice})", ConsoleColor.Green);
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
                    int sleep = dropConfig.ConnectionInverval * 1000 - (int)(DateTime.Now - currentLoopTime).TotalMilliseconds;
                    Logger.WriteLine($"        sleeping {sleep:N0} ms ...", ConsoleColor.DarkGray);
                    if (sleep > 0) Thread.Sleep(sleep);
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