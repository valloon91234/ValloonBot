using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using IO.Swagger.Client;
using IO.Swagger.Model;

/**
 * @author Valloon Project
 * @version 1.0 @2020-03-03
 */
namespace Valloon.BitMEX
{
    public class RobustStrategy
    {
        public static int GetStopMarketPrice(Position position, decimal lastPrice, decimal stopLoss, bool printValue = true)
        {
            int point = (int)(position.AvgEntryPrice.Value + (position.LiquidationPrice.Value - position.AvgEntryPrice.Value) * stopLoss);
            decimal nowLoss = 100m * (lastPrice - position.AvgEntryPrice.Value) / (position.LiquidationPrice.Value - position.AvgEntryPrice.Value);
            if (printValue) Logger.WriteLine($"    stop_loss = {stopLoss * 100:N0} %    stop_price = {point}    now_loss = {nowLoss:N2} %", ConsoleColor.Yellow);
            return point;
        }

        public static void Run()
        {
            using (var valloonClient = new ValloonClient())
            {
                int loopCount = 0;
                decimal positionClosePrice = 0;
                while (true)
                {
                    try
                    {
                        Config config = Config.Load(out bool clearOrder);
                        if (clearOrder) loopCount = 0;
                        BitMEXApiHelper apiHelper = new BitMEXApiHelper(config.ApiKey, config.ApiSecret, config.TestnetMode);
                        decimal investUnit = 0;
                        {
                            decimal investSum = 0;
                            for (int i = 0; i < config.StairsCount; i++)
                                investSum += config.StairsInvest[i];
                            investUnit = config.InvestRatio / investSum;
                        }
                        int volume = apiHelper.GetRecentVolume();
                        Instrument instrument = apiHelper.GetInstrument();
                        decimal lastPrice = instrument.LastPrice.Value;
                        decimal markPrice = instrument.MarkPrice.Value;
                        Margin margin = apiHelper.GetMargin();
                        List<Order> activeOrders = apiHelper.GetActiveOrders();
                        int activeOrdersCount = activeOrders.Count;
                        Position position = apiHelper.GetPosition();
                        {
                            decimal walletBalance = margin.WalletBalance.Value / 100000000m;
                            string consoleTitle = $"$ {lastPrice:N0}  /  {walletBalance:N8} XBT  /  {activeOrdersCount} Orders";
                            if (position != null && position.CurrentQty.Value != 0) consoleTitle += "  /  1 Position";
                            consoleTitle += $"  <{config.Username}>  |  {GlobalParam.APP_NAME}  v{GlobalParam.APP_VERSION}";
                            Console.Title = consoleTitle;
                            string timeText = DateTime.UtcNow.ToString("yyyy-MM-dd  HH:mm:ss");
                            decimal unavailableMarginPercent = 100m * (margin.WalletBalance.Value - margin.AvailableMargin.Value) / margin.WalletBalance.Value;
                            Logger.WriteLine($"[{timeText}  ({++loopCount})]    $ {lastPrice:N2}  /  $ {markPrice:N2}  /  $ {volume:N0}    {walletBalance:N8} XBT    {activeOrdersCount} orders    {unavailableMarginPercent:N2} %");
                        }
                        valloonClient.CheckPing(config, lastPrice, markPrice, volume, activeOrdersCount, position, margin);
                        decimal markPriceDistance = Math.Abs(markPrice - lastPrice);
                        if (position == null || position.CurrentQty.Value == 0)
                        {
                            bool resetOrder = (config.StairsDirection == 1 || config.StairsDirection == 2) && activeOrdersCount != config.StairsCount;
                            if (!resetOrder)
                            {
                                if (config.StairsDirection == 1)
                                {
                                    decimal highest = 0;
                                    foreach (var order in activeOrders)
                                    {
                                        if (order.Price != null && order.Price.Value > highest) highest = order.Price.Value;
                                    }
                                    if (lastPrice - highest > (config.StairsHeight[0] + config.BuyHeightDistance) * config.BuyHeightRate + config.StairsResetDistance) resetOrder = true;
                                }
                                else if (config.StairsDirection == 2)
                                {
                                    decimal lowest = 0;
                                    foreach (var order in activeOrders)
                                    {
                                        if (lowest == 0 || order.Price != null && order.Price.Value < lowest) lowest = order.Price.Value;
                                    }
                                    if (lowest - lastPrice > (config.StairsHeight[0] + config.SellHeightDistance) * config.SellHeightRate + config.StairsResetDistance) resetOrder = true;
                                }
                            }
                            if (config.StairsDirection != 1 && config.StairsDirection != 2 && activeOrdersCount != config.StairsCount * 2 || resetOrder || clearOrder)
                            {
                                apiHelper.CancelAllOrders();
                                if (activeOrdersCount > 0)
                                {
                                    Logger.WriteLine($"All orders have been canceled.");
                                }
                                if (volume < config.LimitLower)
                                {
                                    Logger.WriteLine($"No order. (volume < {config.LimitLower})", ConsoleColor.DarkGray);
                                    Thread.Sleep(5000);
                                }
                                else if (volume > config.LimitHigher)
                                {
                                    Logger.WriteLine($"No order. (volume > {config.LimitHigher})", ConsoleColor.DarkGray);
                                    Thread.Sleep(5000);
                                }
                                else if (markPriceDistance > config.LimitMark)
                                {
                                    Logger.WriteLine($"No order. (mark_distance = {markPriceDistance} > {config.LimitMark})", ConsoleColor.DarkGray);
                                    Thread.Sleep(5000);
                                }
                                else if (GlobalParam.Paused)
                                {
                                    Logger.WriteLine($"No order. (Temperary paused.)", ConsoleColor.DarkGray);
                                    Thread.Sleep(5000);
                                }
                                else if (!config.Activated)
                                {
                                    Logger.WriteLine($"Activation failed. Please contact support.  https://join.skype.com/invite/EFAu4X0DwOAx", ConsoleColor.Green);
                                    Logger.WriteWait("", 60);
                                }
                                else if (config.ExpireDateTime < apiHelper.ServerTime)
                                {
                                    Logger.WriteLine($"Activation expired. Please contact support.  https://join.skype.com/invite/EFAu4X0DwOAx", ConsoleColor.Green);
                                    Logger.WriteWait("", 60);
                                }
                                else if (config.StairsCount == 0)
                                {
                                    Logger.WriteLine($"No order. (stairs = 0)", ConsoleColor.DarkGray);
                                    Thread.Sleep(5000);
                                }
                                else if (config.Exit > 0)
                                {
                                    Logger.WriteLine($"No order. (exit = {config.Exit})", ConsoleColor.DarkGray);
                                    Thread.Sleep(5000);
                                }
                                else
                                {
                                    int qtyStep = (int)(margin.WalletBalance * investUnit / 100);
                                    if (qtyStep * config.StairsInvest[0] < 25)
                                    {
                                        Logger.WriteLine($"Warning! Wallet balance is too low. (first_order_qty = {qtyStep})", ConsoleColor.DarkYellow);
                                    }
                                    List<Order> newOrderList = new List<Order>();
                                    for (int i = 0; i < config.StairsCount; i++)
                                    {
                                        int qty = (int)(config.StairsInvest[i] * qtyStep);
                                        int marginHeight = config.StairsHeight[i];
                                        int buyMargin = (int)((marginHeight + config.BuyHeightDistance) * config.BuyHeightRate);
                                        int sellMargin = (int)((marginHeight + config.SellHeightDistance) * config.SellHeightRate);
                                        int buyPrice = (int)lastPrice - buyMargin;
                                        int sellPrice = (int)lastPrice + sellMargin;
                                        if (config.StairsDirection == 1)
                                        {
                                            newOrderList.Add(new Order
                                            {
                                                Side = "Buy",
                                                OrderQty = qty,
                                                Price = buyPrice
                                            });
                                            Logger.WriteLine($"    <order {i + 1}>\t  qty = {qty}  ({config.StairsInvest[i]})  \tbuy_price = {buyPrice}  (-{buyMargin})");
                                        }
                                        else if (config.StairsDirection == 2)
                                        {
                                            newOrderList.Add(new Order
                                            {
                                                Side = "Sell",
                                                OrderQty = qty,
                                                Price = sellPrice
                                            });
                                            Logger.WriteLine($"    <order {i + 1}>\t  qty = {qty}  ({config.StairsInvest[i]})  \tsell_price = {sellPrice}  (+{sellMargin})");
                                        }
                                        else
                                        {
                                            newOrderList.Add(new Order
                                            {
                                                Side = "Buy",
                                                OrderQty = qty,
                                                Price = buyPrice
                                            });
                                            newOrderList.Add(new Order
                                            {
                                                Side = "Sell",
                                                OrderQty = qty,
                                                Price = sellPrice
                                            });
                                            Logger.WriteLine($"    <order {i + 1}>\t  qty = {qty}  ({config.StairsInvest[i]})  \tbuy_price = {buyPrice}  (-{buyMargin})  \tsell_price = {sellPrice}  (+{sellMargin})");
                                        }
                                    }
                                    List<Order> newLimitOrderList = apiHelper.OrderNewLimitBulk(newOrderList);
                                    Logger.WriteLine($"    {newLimitOrderList.Count} orders have been created.");
                                }
                            }
                            else
                            {
                                if (volume < config.LimitLowerCancel)
                                {
                                    apiHelper.CancelAllOrders();
                                    Logger.WriteLine($"All of {activeOrdersCount} active orders have been canceled. (volume < {config.LimitLowerCancel})");
                                }
                                else if (volume > config.LimitHigherCancel)
                                {
                                    apiHelper.CancelAllOrders();
                                    Logger.WriteLine($"All of {activeOrdersCount} active orders have been canceled. (volume > {config.LimitHigherCancel})");
                                }
                                else if (markPriceDistance > config.LimitMarkCancel)
                                {
                                    apiHelper.CancelAllOrders();
                                    Logger.WriteLine($"All of {activeOrdersCount} active orders have been canceled. (mark_distance = {markPriceDistance} > {config.LimitMarkCancel})");
                                }
                            }
                        }
                        else
                        {
                            {
                                decimal unrealisedPercent = 100m * position.UnrealisedPnl.Value / margin.WalletBalance.Value;
                                Logger.WriteFile($"wallet_balance = {margin.WalletBalance}    unrealised_pnl = {position.UnrealisedPnl}    {unrealisedPercent:N2} %    leverage = {position.Leverage}");
                                Logger.WriteLine($"    <Position>    $ {position.AvgEntryPrice.Value:F1} / {positionClosePrice:F1}    qty = {position.CurrentQty.Value}    liq = {position.LiquidationPrice}    {unrealisedPercent:N2} %", ConsoleColor.Green);
                            }
                            string side = null;
                            if (position.CurrentQty.Value < 0)
                            {
                                side = "Buy";
                            }
                            else if (position.CurrentQty.Value > 0)
                            {
                                side = "Sell";
                            }
                            int stopPrice = GetStopMarketPrice(position, lastPrice, config.StopLoss);
                            {
                                bool stopLoss = false;
                                if (position.CurrentQty.Value < 0)
                                {
                                    if (lastPrice > stopPrice) stopLoss = true;
                                }
                                else if (position.CurrentQty.Value > 0)
                                {
                                    if (lastPrice < stopPrice) stopLoss = true;
                                }
                                if (stopLoss)
                                {
                                    if (activeOrdersCount <= 1)
                                    {
                                        apiHelper.CancelAllOrders();
                                        apiHelper.OrderNewMarket(side, position.CurrentQty.Value);
                                        Logger.WriteLine($"Liquidation Alert! Active position has been filled by market.", ConsoleColor.DarkYellow);
                                        goto line_end_loop;
                                    }
                                    else if (activeOrdersCount >= 2)
                                    {
                                        apiHelper.CancelAllOrders();
                                        Logger.WriteLine($"Warning! Near Liquidation. All orders have been canceled.", ConsoleColor.DarkYellow);
                                        position = apiHelper.GetPosition();
                                    }
                                }
                            }
                            bool isActivePossionCorrect = false;
                            List<string> cancelOrderList = new List<string>();
                            for (int i = 0; i < activeOrdersCount; i++)
                            {
                                Order order = activeOrders[i];
                                if (order.Side == side)
                                {
                                    if (/*!clearOrder && */order.OrderQty.Value == Math.Abs(position.CurrentQty.Value) && order.ExecInst == "Close")
                                    {
                                        isActivePossionCorrect = true;
                                        positionClosePrice = order.Price.Value;
                                    }
                                    else
                                    {
                                        cancelOrderList.Add(order.OrderID);
                                    }
                                }
                            }
                            if (cancelOrderList.Count > 0) apiHelper.CancelOrders(cancelOrderList);
                            if (!isActivePossionCorrect)
                            {
                                position = apiHelper.GetPosition();
                                if (position.AvgEntryPrice.Value == 0) throw new Exception("failed to get position price.");
                                int price = 0;
                                if (position.CurrentQty.Value < 0)
                                {
                                    side = "Buy";
                                    price = (int)Math.Floor(position.AvgEntryPrice.Value - config.ProfitTarget);
                                }
                                else if (position.CurrentQty.Value > 0)
                                {
                                    side = "Sell";
                                    price = (int)Math.Ceiling(position.AvgEntryPrice.Value + config.ProfitTarget);
                                }
                                Order closeOrder = apiHelper.OrderNewLimitClose(side, Math.Abs(position.CurrentQty.Value), price);
                                Logger.WriteLine($"New order for position has been created. (qty = {closeOrder.OrderQty}, price = {closeOrder.Price:N2})", ConsoleColor.Green);
                                positionClosePrice = price;
                                activeOrders = apiHelper.GetActiveOrders();
                                activeOrdersCount = activeOrders.Count;
                                if (activeOrdersCount == 1)
                                {
                                    stopPrice = GetStopMarketPrice(position, lastPrice, config.StopLoss, false);
                                    Order stopMarketOrder = apiHelper.OrderNewStopMarket(side, Math.Abs(position.CurrentQty.Value), stopPrice);
                                    Logger.WriteLine($"Stop Market has been created. (qty = {stopMarketOrder.OrderQty}, stop_price = {stopMarketOrder.StopPx})", ConsoleColor.Green);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex is WebException || ex is ApiException)
                        {
                            Logger.WriteLine("<Server Connection Error>  " + ex.Message, ConsoleColor.Red, false);
                        }
                        else
                        {
                            Logger.WriteLine(ex.Message, ConsoleColor.Red, false);
                        }
                        Logger.WriteFile(ex.ToString());
                    }
                line_end_loop:
                    Thread.Sleep(4000);
                    valloonClient.ParsePingResult();
                    if (GlobalParam.Message != null) Logger.WriteLine("<Message>  " + GlobalParam.Message, ConsoleColor.Green);
                }
            }
        }
    }
}