using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using IO.Swagger.Client;
using IO.Swagger.Model;
using Newtonsoft.Json.Linq;

/**
 * @author Valloon Project
 * @version 1.0 @2020-03-03
 */
namespace Valloon.Trading
{
    public class InertiaStrategy
    {
        public static void Run(Main mainForm = null)
        {
            int loopCount = 0;
            DateTime? lastLoopTime = null;
            int lastVolume = 0;
            bool positionCreatedInThisVolume = false;
            bool positionExistInThisVolume = false;
            Wallet wallet = null;
            List<Transaction> walletHistory = null;
            Config config = null;
            while (true)
            {
                try
                {
                    if (Program.TEST_MODE)
                    {
                        config = Config.Load(out bool clearOrder);
                        if (clearOrder) loopCount = 0;
                    }
                    else
                    {
                        BackendClient.CheckPing(ref config, wallet, walletHistory);
                        if (config == null || !Config.Active)
                        {
                            mainForm.SetMessage(Config.Message);
                            Logger.WriteLine(Config.Message, ConsoleColor.Red);
                            Logger.WriteWait($"Waiting for {Config.BackendConnectionInterval} seconds", Config.BackendConnectionInterval);
                            continue;
                        }
                        mainForm.SetCaption(config.Strategy.ToUpper());
                        mainForm.SetMessage($"Your license expire in {Config.RemainingDays} days");
                    }
                    DateTime currentLoopTime = DateTime.Now;
                    BitMEXApiHelper apiHelper = new BitMEXApiHelper(config.ApiKey, config.ApiSecret, config.TestnetMode);
                    TradeBin tradeBin = apiHelper.GetVolume(config.VolumeBinSize, config.VolumeCount, out decimal ema);
                    if (tradeBin == null) throw new Exception("failed to get volume.");
                    if (lastVolume > tradeBin.Volume.Value)
                    {
                        positionCreatedInThisVolume = false;
                        positionExistInThisVolume = false;
                    }
                    lastVolume = (int)tradeBin.Volume.Value;
                    Margin margin = apiHelper.GetMargin();
                    List<Order> activeOrders = apiHelper.GetActiveOrders();
                    int activeOrdersCount = activeOrders.Count;
                    Position position = apiHelper.GetPosition();
                    {
                        decimal walletBalance = margin.WalletBalance.Value / 100000000m;
                        //string consoleTitle = $"$ {tradeBin.Close:N0}  /  {lastVolume:N0}  /  {walletBalance:N8} XBT  /  {activeOrdersCount} Orders";
                        //if (position != null && position.CurrentQty.Value != 0) consoleTitle += "  /  1 Position";
                        //consoleTitle += $"  <{config.Username}>  |  {Config.APP_NAME}  v{Config.APP_VERSION}";
                        //try
                        //{
                        //    Console.Title = consoleTitle;
                        //}
                        //catch { }
                        string timeText = DateTime.UtcNow.ToString("yyyy-MM-dd  HH:mm:ss");
                        decimal unavailableMarginPercent = 100m * (margin.WalletBalance.Value - margin.AvailableMargin.Value) / margin.WalletBalance.Value;
                        Logger.WriteLine($"[{timeText}  ({++loopCount})]    $ {tradeBin.Close:F1}  /  $ {ema:F1}  /  $ {lastVolume:N0}    {walletBalance:N8} XBT    {activeOrdersCount} orders    {unavailableMarginPercent:N2} %");
                    }
                    if (position == null || position.CurrentQty.Value == 0)
                    {
                        List<string> cancelOrderList = new List<string>();
                        foreach (Order order in activeOrders)
                        {
                            if (order.ExecInst.Contains("Close") || order.Text.Contains("BOT")) cancelOrderList.Add(order.OrderID);
                        }
                        if (cancelOrderList.Count > 0) apiHelper.CancelOrders(cancelOrderList);
                        activeOrders = apiHelper.GetActiveOrders();
                        activeOrdersCount = activeOrders.Count;
                        //if (apiHelper.ServerTime.Year > 2020 || apiHelper.ServerTime.Month > 5 || apiHelper.ServerTime.Day > 28)
                        //{
                        //    Logger.WriteLine($"Activation expired. Please contact support.  https://join.skype.com/invite/EFAu4X0DwOAx", ConsoleColor.Green);
                        //    Logger.WriteWait("", 60);
                        //}
                        //else 
                        if (activeOrdersCount > 0)
                        {
                            Logger.WriteLine($"Manual orders exist. (manual_order_count = {activeOrdersCount})", ConsoleColor.DarkGray);
                        }
                        else if (lastVolume < config.VolumeMin)
                        {
                            Logger.WriteLine($"Volume is too low. ({lastVolume:N0} < {config.VolumeMin:N0})", ConsoleColor.DarkGray);
                        }
                        else if (positionCreatedInThisVolume || positionExistInThisVolume)
                        {
                            Logger.WriteLine($"A trade has already done in this bin.", ConsoleColor.DarkGray);
                        }
                        else if (tradeBin.Close < ema && tradeBin.Open <= tradeBin.Close)
                        {
                            Logger.WriteLine($"Now price is lower than EMA, but higher than open price.", ConsoleColor.DarkGray);
                        }
                        else if (tradeBin.Close > ema && tradeBin.Open >= tradeBin.Close)
                        {
                            Logger.WriteLine($"Now price is higher than EMA, but lower than open price.", ConsoleColor.DarkGray);
                        }
                        else if (tradeBin.Close < ema && tradeBin.Open > tradeBin.Close && tradeBin.High > ema)
                        {
                            Logger.WriteLine($"Now price is lower than EMA and open price, but high price is higher than EMA.", ConsoleColor.DarkGray);
                        }
                        else if (tradeBin.Close > ema && tradeBin.Open < tradeBin.Close && tradeBin.Low < ema)
                        {
                            Logger.WriteLine($"Now price is higher than EMA and open price, but low price is lower than EMA.", ConsoleColor.DarkGray);
                        }
                        else
                        {
                            int qty = BitMEXApiHelper.FixQty((int)(margin.WalletBalance.Value * tradeBin.Close.Value * config.QtyRate / 1000000));
                            List<Order> newOrderList = new List<Order>();
                            int limitClosePrice = 0;
                            int takeClosePrice = 0;
                            if ((tradeBin.Close < ema && !config.InverseMode || tradeBin.Close > ema && config.InverseMode) && config.BuySell != 1)
                            {
                                Order newPositionOrder = apiHelper.OrderNewMarket("Sell", qty);
                                int tryCount = 0;
                                bool positionCreated = false;
                                while (tryCount < 10)
                                {
                                    position = apiHelper.GetPosition();
                                    if (position != null && position.CurrentQty.Value != 0)
                                    {
                                        positionCreated = true;
                                        break;
                                    }
                                    Thread.Sleep(1000);
                                    tryCount++;
                                }
                                if (!positionCreated)
                                {
                                    Logger.WriteLine($"Position did not created. sell_qty = {qty}", ConsoleColor.DarkYellow);
                                    Logger.WriteFile(JObject.FromObject(newPositionOrder).ToString());
                                }
                                else
                                {
                                    takeClosePrice = (int)Math.Floor(position.AvgEntryPrice.Value - config.TakeProfit);
                                    limitClosePrice = (int)Math.Floor(position.AvgEntryPrice.Value - config.LimitProfit);
                                    if (config.TakeProfit > 0)
                                        apiHelper.OrderNewTakeProfitMarketClose("Buy", takeClosePrice);
                                    if (config.LimitProfit > 0)
                                        apiHelper.OrderNewLimitClose("Buy", limitClosePrice, Math.Abs(position.CurrentQty.Value));
                                    if (config.StopLoss > 0)
                                        apiHelper.OrderNewStopMarketClose("Buy", (int)position.AvgEntryPrice.Value + config.StopLoss);
                                    for (int i = 0; i < config.OrderCount; i++)
                                    {
                                        int orderQty = BitMEXApiHelper.FixQty((int)(qty * Math.Pow(config.Martingale, i + 1)));
                                        int distance = (i + 1) * config.OrderDistance;
                                        if (distance >= config.StopLoss) break;
                                        int price = (int)position.AvgEntryPrice.Value + distance;
                                        newOrderList.Add(new Order
                                        {
                                            Symbol = BitMEXApiHelper.SYMBOL,
                                            OrdType = "Limit",
                                            Side = "Sell",
                                            OrderQty = orderQty,
                                            Price = price,
                                            Text = "BOT"
                                        });
                                    }
                                }
                            }
                            else if ((tradeBin.Close > ema && !config.InverseMode || tradeBin.Close < ema && config.InverseMode) && config.BuySell != 2)
                            {
                                Order newPositionOrder = apiHelper.OrderNewMarket("Buy", qty);
                                int tryCount = 0;
                                bool positionCreated = false;
                                while (tryCount < 10)
                                {
                                    position = apiHelper.GetPosition();
                                    if (position != null && position.CurrentQty.Value != 0)
                                    {
                                        positionCreated = true;
                                        break;
                                    }
                                    Thread.Sleep(1000);
                                    tryCount++;
                                }
                                if (!positionCreated)
                                {
                                    Logger.WriteLine($"Position did not created. buy_qty = {qty}", ConsoleColor.DarkYellow);
                                    Logger.WriteFile(JObject.FromObject(newPositionOrder).ToString());
                                }
                                else
                                {
                                    takeClosePrice = (int)Math.Ceiling(position.AvgEntryPrice.Value + config.TakeProfit);
                                    limitClosePrice = (int)Math.Ceiling(position.AvgEntryPrice.Value + config.LimitProfit);
                                    if (config.TakeProfit > 0)
                                        apiHelper.OrderNewTakeProfitMarketClose("Sell", takeClosePrice);
                                    if (config.LimitProfit > 0)
                                        apiHelper.OrderNewLimitClose("Sell", limitClosePrice, Math.Abs(position.CurrentQty.Value));
                                    if (config.StopLoss > 0)
                                        apiHelper.OrderNewStopMarketClose("Sell", (int)position.AvgEntryPrice.Value - config.StopLoss);
                                    for (int i = 0; i < config.OrderCount; i++)
                                    {
                                        int orderQty = BitMEXApiHelper.FixQty((int)(qty * Math.Pow(config.Martingale, i + 1)));
                                        int distance = (i + 1) * config.OrderDistance;
                                        if (distance >= config.StopLoss) break;
                                        int price = (int)position.AvgEntryPrice.Value - distance;
                                        if (price <= 0) continue;
                                        newOrderList.Add(new Order
                                        {
                                            Symbol = BitMEXApiHelper.SYMBOL,
                                            OrdType = "Limit",
                                            Side = "Buy",
                                            OrderQty = orderQty,
                                            Price = price,
                                            Text = "BOT"
                                        });
                                    }
                                }
                            }
                            else
                            {
                                Logger.WriteLine($"Strange error : close = {tradeBin.Close}, ema = {ema}, inverse_mode = {config.InverseMode}", ConsoleColor.Red);
                            }
                            List<Order> newLimitOrderList = new List<Order>();
                            if (newOrderList.Count > 0)
                            {
                                foreach (Order order in newOrderList)
                                {
                                    newLimitOrderList.Add(apiHelper.OrderNew(order.Side, order.OrderQty, order.Price, order.StopPx, order.OrdType, order.ExecInst, order.Text));
                                }
                                Logger.WriteFile(JArray.FromObject(newLimitOrderList).ToString());
                            }
                            Logger.WriteLine($"Position and {newLimitOrderList.Count} orders have been created.", ConsoleColor.Green);
                            {
                                decimal unrealisedPercent = 100m * position.UnrealisedPnl.Value / margin.WalletBalance.Value;
                                Logger.WriteFile($"wallet_balance = {margin.WalletBalance}    unrealised_pnl = {position.UnrealisedPnl}    {unrealisedPercent:N2} %    leverage = {position.Leverage}");
                                Logger.WriteLine($"    <Position>    $ {(position.AvgEntryPrice == null ? 0 : position.AvgEntryPrice.Value):F1}  /  {limitClosePrice:F1}    qty = {(position.CurrentQty == null ? 0 : position.CurrentQty.Value)}    liq = {(position.LiquidationPrice == null ? 0 : position.LiquidationPrice.Value)}    {unrealisedPercent:N2} %", ConsoleColor.Green);
                            }
                            positionCreatedInThisVolume = true;
                        }
                    }
                    else
                    {
                        positionExistInThisVolume = true;
                        string side = null;
                        if (position.CurrentQty.Value < 0)
                        {
                            side = "Buy";
                            if (config.ForceStopLoss && !positionCreatedInThisVolume && lastVolume > config.VolumeMin && tradeBin.Open.Value < tradeBin.Close.Value)
                            {
                                apiHelper.OrderNewMarketClose(side);
                                apiHelper.CancelAllOrders();
                                int qty = BitMEXApiHelper.FixQty((int)(margin.WalletBalance * tradeBin.Close.Value * config.QtyRate / 1000000));
                                apiHelper.OrderNewMarket(side, qty);
                                Logger.WriteLine($"Original position was forcely closed and new opposite position has been created.", ConsoleColor.Green);
                                activeOrders = apiHelper.GetActiveOrders();
                                activeOrdersCount = activeOrders.Count;
                                int tryCount = 0;
                                while (tryCount < 10)
                                {
                                    position = apiHelper.GetPosition();
                                    if (position != null && position.CurrentQty.Value != 0)
                                    {
                                        break;
                                    }
                                    Thread.Sleep(1000);
                                    tryCount++;
                                }
                                side = "Sell";
                            }
                        }
                        else if (position.CurrentQty.Value > 0)
                        {
                            side = "Sell";
                            if (config.ForceStopLoss && !positionCreatedInThisVolume && lastVolume > config.VolumeMin && tradeBin.Open.Value > tradeBin.Close.Value)
                            {
                                apiHelper.OrderNewMarketClose(side);
                                apiHelper.CancelAllOrders();
                                int qty = BitMEXApiHelper.FixQty((int)(margin.WalletBalance * tradeBin.Close.Value * config.QtyRate / 1000000));
                                apiHelper.OrderNewMarket(side, qty);
                                Logger.WriteLine($"Original position was forcely closed and new opposite position has been created.", ConsoleColor.Green);
                                activeOrders = apiHelper.GetActiveOrders();
                                activeOrdersCount = activeOrders.Count;
                                int tryCount = 0;
                                while (tryCount < 10)
                                {
                                    position = apiHelper.GetPosition();
                                    if (position != null && position.CurrentQty.Value != 0)
                                    {
                                        break;
                                    }
                                    Thread.Sleep(1000);
                                    tryCount++;
                                }
                                side = "Buy";
                            }
                        }
                        bool existTakeClose = false;
                        bool existLimitClose = false;
                        decimal takeClosePrice = 0;
                        decimal limitClosePrice = 0;
                        int limitOrderCount = 0;
                        int stopOrderCount = 0;
                        List<string> cancelOrderList = new List<string>();
                        for (int i = 0; i < activeOrdersCount; i++)
                        {
                            Order order = activeOrders[i];
                            if (order.Side == side)
                            {
                                if ((order.OrderQty == null || order.OrderQty.Value == Math.Abs(position.CurrentQty.Value)) && order.OrdType == "MarketIfTouched")
                                {
                                    existTakeClose = true;
                                    takeClosePrice = order.StopPx.Value;
                                }
                                else if ((order.OrderQty == null || order.OrderQty.Value == Math.Abs(position.CurrentQty.Value)) && order.OrdType == "Limit" && order.ExecInst.Contains("Close"))
                                {
                                    existLimitClose = true;
                                    limitClosePrice = order.Price.Value;
                                }
                                else if (order.OrdType == "Stop")
                                {
                                    stopOrderCount++;
                                }
                                else
                                {
                                    cancelOrderList.Add(order.OrderID);
                                }
                            }
                            else if (order.OrdType == "Limit")
                            {
                                limitOrderCount++;
                            }
                        }
                        if (cancelOrderList.Count > 0) apiHelper.CancelOrders(cancelOrderList);
                        position = apiHelper.GetPosition();
                        if (position.AvgEntryPrice.Value == 0) throw new Exception("failed to get position price.");
                        if (!existTakeClose && config.TakeProfit > 0)
                        {
                            int price;
                            if (position.CurrentQty.Value < 0)
                            {
                                side = "Buy";
                                price = (int)Math.Floor(position.AvgEntryPrice.Value - config.TakeProfit);
                            }
                            else
                            {
                                side = "Sell";
                                price = (int)Math.Ceiling(position.AvgEntryPrice.Value + config.TakeProfit);
                            }
                            Order order = apiHelper.OrderNewTakeProfitMarketClose(side, price);
                            Logger.WriteLine($"New take-close order has been created. (price = {price:N2})", ConsoleColor.Green);
                            takeClosePrice = price;
                        }
                        if (!existLimitClose && config.LimitProfit > 0)
                        {
                            int price;
                            if (position.CurrentQty.Value < 0)
                            {
                                side = "Buy";
                                price = (int)Math.Floor(position.AvgEntryPrice.Value - config.LimitProfit);
                            }
                            else
                            {
                                side = "Sell";
                                price = (int)Math.Ceiling(position.AvgEntryPrice.Value + config.LimitProfit);
                            }
                            Order order = apiHelper.OrderNewLimitClose(side, price, Math.Abs(position.CurrentQty.Value));
                            Logger.WriteLine($"New limit-close order has been created. (price = {price:N2})", ConsoleColor.Green);
                            limitClosePrice = price;
                        }
                        if (limitOrderCount < 1 && config.OrderCount > 0 && Math.Abs(position.CurrentQty.Value) < margin.WalletBalance.Value * tradeBin.Close.Value * config.QtyRate * 2 / 1000000)
                        {
                            int qty = Math.Abs(position.CurrentQty.Value);
                            List<Order> newOrderList = new List<Order>();
                            if (position.CurrentQty.Value < 0)
                            {
                                for (int i = 0; i < config.OrderCount; i++)
                                {
                                    int orderQty = BitMEXApiHelper.FixQty((int)(qty * Math.Pow(config.Martingale, i + 1)));
                                    int distance = (i + 1) * config.OrderDistance;
                                    if (distance >= config.StopLoss)
                                        break;
                                    int price = (int)position.AvgEntryPrice.Value + distance;
                                    newOrderList.Add(new Order
                                    {
                                        Symbol = BitMEXApiHelper.SYMBOL,
                                        OrdType = "Limit",
                                        Side = "Sell",
                                        OrderQty = orderQty,
                                        Price = price,
                                        Text = "BOT"
                                    });
                                }
                            }
                            else
                            {
                                for (int i = 0; i < config.OrderCount; i++)
                                {
                                    int orderQty = BitMEXApiHelper.FixQty((int)(qty * Math.Pow(config.Martingale, i + 1)));
                                    int distance = (i + 1) * config.OrderDistance;
                                    if (distance >= config.StopLoss)
                                        break;
                                    int price = (int)position.AvgEntryPrice.Value - distance;
                                    if (price <= 0)
                                        continue;
                                    newOrderList.Add(new Order
                                    {
                                        Symbol = BitMEXApiHelper.SYMBOL,
                                        OrdType = "Limit",
                                        Side = "Buy",
                                        OrderQty = orderQty,
                                        Price = price,
                                        Text = "BOT"
                                    });
                                }
                            }
                            List<Order> newLimitOrderList = new List<Order>();
                            foreach (Order order in newOrderList)
                            {
                                newLimitOrderList.Add(apiHelper.OrderNew(order.Side, order.OrderQty, order.Price, order.StopPx, order.OrdType, order.ExecInst, order.Text));
                            }
                            Logger.WriteFile(JArray.FromObject(newLimitOrderList).ToString());
                            Logger.WriteLine($"{newLimitOrderList.Count} orders have been created.", ConsoleColor.Green);
                        }
                        if (stopOrderCount < 1)
                        {
                            if (position.CurrentQty.Value < 0)
                            {
                                if (config.StopLoss > 0)
                                    apiHelper.OrderNewStopMarketClose("Buy", (int)position.AvgEntryPrice.Value + config.StopLoss);
                            }
                            else
                            {
                                if (config.StopLoss > 0)
                                    apiHelper.OrderNewStopMarketClose("Sell", (int)position.AvgEntryPrice.Value - config.StopLoss);
                            }
                            Logger.WriteLine($"Stop-loss has been created.", ConsoleColor.Green);
                        }
                        {
                            decimal unrealisedPercent = 100m * position.UnrealisedPnl.Value / margin.WalletBalance.Value;
                            Logger.WriteFile($"wallet_balance = {margin.WalletBalance}    unrealised_pnl = {position.UnrealisedPnl}    {unrealisedPercent:N2} %    leverage = {position.Leverage}");
                            Logger.WriteLine($"    <Position>    $ {position.AvgEntryPrice.Value:F1}  /  {limitClosePrice:F1}    qty = {position.CurrentQty.Value}    liq = {position.LiquidationPrice}    {unrealisedPercent:N2} %", ConsoleColor.Green);
                        }
                    }
                    if (!Program.TEST_MODE)
                    {
                        if (lastLoopTime == null || (currentLoopTime - lastLoopTime.Value).TotalSeconds > 60)
                        {
                            wallet = apiHelper.GetWallet();
                            walletHistory = apiHelper.GetWalletHistory();
                            mainForm.SetBalance(wallet.Amount.Value / 100000000m);
                            foreach (Transaction t in walletHistory)
                            {
                                if (t.TransactType == "RealisedPNL")
                                {
                                    decimal percent = 100m * t.Amount.Value / margin.WalletBalance.Value;
                                    mainForm.SetProfitToday(t.Amount.Value / 100000000m, percent);
                                    break;
                                }
                            }
                            decimal unrealisedPercent = 100m * position.UnrealisedPnl.Value / margin.WalletBalance.Value;
                            mainForm.SetOpened(position.UnrealisedPnl.Value / 100000000m, unrealisedPercent);
                            if (position.LiquidationPrice == null) mainForm.SetLiqPrice(0);
                            else mainForm.SetLiqPrice(position.LiquidationPrice.Value);
                            lastLoopTime = DateTime.Now;
                        }
                    }
                    int sleep = apiHelper.RequestCount * 1000 / 2 - (int)(DateTime.Now - currentLoopTime).TotalMilliseconds + 1000;
                    if (sleep > 0) Thread.Sleep(sleep);
                    Thread.Sleep(config.ConnectionInverval * 1000);
                }
                catch (Exception ex)
                {
                    if (ex is WebException || ex is ApiException)
                    {
                        Logger.WriteLine("<API Error>  " + ex.Message, ConsoleColor.Red, false);
                    }
                    else
                    {
                        Logger.WriteLine(ex.Message, ConsoleColor.Red, false);
                    }
                    Logger.WriteFile(ex.ToString());
                    Thread.Sleep(4000);
                }
            }
        }

    }
}
