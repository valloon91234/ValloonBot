using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using IO.Swagger.Client;
using IO.Swagger.Model;
using Newtonsoft.Json.Linq;

/**
 * @author Valloon Project
 * @version 2.0 @2020-05-10
 */
namespace Valloon.BitMEX
{
    public class RobustStrategy2
    {
        private int StairLimitCount;
        private int[] StairLimitHeight;
        private int[] StairLimitQty;
        private int StairStopCount;
        private int[] StairStopHeight;
        private int[] StairStopQty;
        private decimal[] StairCloseRatio;

        public RobustStrategy2()
        {
            string text;
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream("Valloon.BitMEX.RobustStrategy2.txt"))
            using (StreamReader reader = new StreamReader(stream))
            {
                text = reader.ReadToEnd();
            }
            string[] textLines = text.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            int stairsLimitCount = 0;
            List<int> stairLimitHeight = new List<int>();
            List<int> stairLimitQty = new List<int>();
            int stairStopCount = 0;
            List<int> stairStopHeight = new List<int>();
            List<int> stairStopQty = new List<int>();
            List<decimal> stairCloseRatio = new List<decimal>();
            int lineIndex = 0;
            foreach (string line in textLines)
            {
                lineIndex++;
                try
                {
                    string[] words = line.Split('\t');
                    int height = int.Parse(words[0]);
                    int qty = int.Parse(words[2]);
                    if (qty > 0)
                    {
                        decimal closeRatio = decimal.Parse(words[6]);
                        stairLimitHeight.Add(height);
                        stairLimitQty.Add(qty);
                        stairCloseRatio.Add(closeRatio);
                        stairsLimitCount++;
                    }
                    else
                    {
                        stairStopHeight.Add(height);
                        stairStopQty.Add(Math.Abs(qty));
                        stairStopCount++;
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLine($"Error in reading config - line {lineIndex} : {ex.Message}", ConsoleColor.Red);
                    Logger.WriteFile(ex.ToString());
                    stairsLimitCount--;
                }
            }
            StairLimitCount = stairsLimitCount;
            StairLimitHeight = stairLimitHeight.ToArray();
            StairLimitQty = stairLimitQty.ToArray();
            StairStopCount = stairStopCount;
            StairStopHeight = stairStopHeight.ToArray();
            StairStopQty = stairStopQty.ToArray();
            StairCloseRatio = stairCloseRatio.ToArray();
        }

        public void Run()
        {
            int loopCount = 0;
            //DateTime? lastLoopTime = null;
            while (true)
            {
                try
                {
                    Config config = Config.Load(out bool clearOrder);
                    //{
                    //    DateTime currentLoopTime = DateTime.Now;
                    //    if (lastLoopTime == null || lastLoopTime.Value.Hour != currentLoopTime.Hour)
                    //    {
                    //        BackendClient.Ping(ref config);
                    //    }
                    //    lastLoopTime = DateTime.Now;
                    //}
                    if (clearOrder) loopCount = 0;
                    BitMEXApiHelper apiHelper = new BitMEXApiHelper(config.ApiKey, config.ApiSecret, config.TestnetMode);
                    int volume = apiHelper.GetRecentVolume();
                    Instrument instrument = apiHelper.GetInstrument();
                    decimal lastPrice = instrument.LastPrice.Value;
                    decimal markPrice = instrument.MarkPrice.Value;
                    Margin margin = apiHelper.GetMargin();
                    List<Order> activeOrders = apiHelper.GetActiveOrdersAll();
                    int activeOrdersCount = activeOrders.Count;
                    Position position = apiHelper.GetPosition();
                    {
                        decimal walletBalance = margin.WalletBalance.Value / 100000000m;
                        string consoleTitle = $"$ {lastPrice:N0}  /  {walletBalance:N8} XBT  /  {activeOrdersCount} Orders";
                        if (position != null && position.CurrentQty.Value != 0) consoleTitle += "  /  1 Position";
                        consoleTitle += $"  <{config.Username}>  |  {Config.APP_NAME}  v{Config.APP_VERSION}";
                        Console.Title = consoleTitle;
                        string timeText = DateTime.UtcNow.ToString("yyyy-MM-dd  HH:mm:ss");
                        decimal unavailableMarginPercent = 100m * (margin.WalletBalance.Value - margin.AvailableMargin.Value) / margin.WalletBalance.Value;
                        Logger.WriteLine($"[{timeText}  ({++loopCount})]    $ {lastPrice:N2}  /  $ {markPrice:N2}  /  $ {volume:N0}    {walletBalance:N8} XBT    {activeOrdersCount} orders    {unavailableMarginPercent:N2} %");
                    }
                    decimal markPriceDistance = Math.Abs(markPrice - lastPrice);
                    if (position == null || position.CurrentQty.Value == 0)
                    {
                        int activeOrdersLimitCount = 0;
                        foreach (var order in activeOrders)
                        {
                            if (order.OrdType == "Limit") activeOrdersLimitCount++;
                        }
                        bool resetOrder = (config.StairsDirection == 1 || config.StairsDirection == 2) && activeOrdersLimitCount != StairLimitCount;
                        if (!resetOrder)
                        {
                            if (config.StairsDirection == 1)
                            {
                                decimal highest = 0;
                                foreach (var order in activeOrders)
                                {
                                    if (order.OrdType != "Limit") continue;
                                    if (order.Price != null && order.Price.Value > highest) highest = order.Price.Value;
                                }
                                if (lastPrice - highest > StairLimitHeight[0] + config.BuyHeightDistance + config.StairsResetDistance) resetOrder = true;
                            }
                            else if (config.StairsDirection == 2)
                            {
                                decimal lowest = 0;
                                foreach (var order in activeOrders)
                                {
                                    if (order.OrdType != "Limit") continue;
                                    if (lowest == 0 || order.Price != null && order.Price.Value < lowest) lowest = order.Price.Value;
                                }
                                if (lowest - lastPrice > StairLimitHeight[0] + config.SellHeightDistance + config.StairsResetDistance) resetOrder = true;
                            }
                        }
                        if (config.StairsDirection != 1 && config.StairsDirection != 2 && activeOrdersLimitCount != StairLimitCount * 2 || resetOrder || clearOrder)
                        {
                            if (apiHelper.CancelAllOrders().Count > 0)
                            {
                                Logger.WriteLine($"All orders have been canceled.");
                            }
                            if (!config.Active)
                            {
                                Logger.WriteLine(Config.Alert ?? "This version is too old. Please contact support.  https://join.skype.com/invite/EFAu4X0DwOAx", ConsoleColor.Green);
                                Logger.WriteWait("", 60);
                            }
                            else if (config.ExpireDateTime < apiHelper.ServerTime)
                            {
                                Logger.WriteLine(Config.Alert ?? "This version is too old. Please contact support.  https://join.skype.com/invite/EFAu4X0DwOAx", ConsoleColor.Green);
                                Logger.WriteWait("", 60);
                            }
                            else if (config.Exit > 0)
                            {
                                Logger.WriteLine($"No order. (exit = {config.Exit})", ConsoleColor.DarkGray);
                                Logger.WriteWait("", 60);
                            }
                            else
                            {
                                int qtyStep = (int)(margin.WalletBalance * lastPrice * config.FirstQtyRatio / 1000000);
                                if (qtyStep * 100000000m / lastPrice < 250000) Logger.WriteLine($"Warning! Wallet balance is too low. (first_order_qty = {qtyStep})", ConsoleColor.DarkYellow);
                                List<Order> newOrderList = new List<Order>();
                                for (int i = 0; i < StairLimitCount; i++)
                                {
                                    int qty = qtyStep * StairLimitQty[i];
                                    int buyMargin = (int)(StairLimitHeight[i] + config.BuyHeightDistance);
                                    int sellMargin = (int)(StairLimitHeight[i] + config.SellHeightDistance);
                                    int buyPrice = (int)lastPrice - buyMargin;
                                    int sellPrice = (int)lastPrice + sellMargin;
                                    int lastClosePriceBuy = 0;
                                    int lastClosePriceSell = 0;
                                    if (i > 0)
                                    {
                                        lastClosePriceBuy = (int)(lastPrice - config.BuyHeightDistance - (1m - StairCloseRatio[i - 1]) * StairLimitHeight[i - 1]);
                                        lastClosePriceSell = (int)(lastPrice + config.SellHeightDistance + (1m - StairCloseRatio[i - 1]) * StairLimitHeight[i - 1]);
                                    }
                                    if (config.StairsDirection == 1 && buyPrice > 0 || config.StairsDirection != 1 && config.StairsDirection != 2 && buyPrice <= 0)
                                    {
                                        newOrderList.Add(new Order
                                        {
                                            Symbol = BitMEXApiHelper.SYMBOL,
                                            OrdType = "Limit",
                                            Side = "Buy",
                                            OrderQty = qty,
                                            Price = buyPrice,
                                            Text = $"BOT:P={lastPrice}:Q={qtyStep}:C={lastClosePriceBuy}:BOT"
                                        });
                                        Logger.WriteLine($"    <limit {i + 1}>\t  qty = {qty}  ({StairLimitQty[i]})  \tbuy_price = {buyPrice}  (-{buyMargin})");
                                    }
                                    else if (config.StairsDirection == 2)
                                    {
                                        newOrderList.Add(new Order
                                        {
                                            Symbol = BitMEXApiHelper.SYMBOL,
                                            OrdType = "Limit",
                                            Side = "Sell",
                                            OrderQty = qty,
                                            Price = sellPrice,
                                            Text = $"BOT:P={lastPrice}:Q={qtyStep}:C={lastClosePriceSell}:BOT"
                                        });
                                        Logger.WriteLine($"    <limit {i + 1}>\t  qty = {qty}  ({StairLimitQty[i]})  \tsell_price = {sellPrice}  (+{sellMargin})");
                                    }
                                    else if (config.StairsDirection != 1 && config.StairsDirection != 2)
                                    {
                                        newOrderList.Add(new Order
                                        {
                                            Symbol = BitMEXApiHelper.SYMBOL,
                                            OrdType = "Limit",
                                            Side = "Buy",
                                            OrderQty = qty,
                                            Price = buyPrice,
                                            Text = $"BOT:P={lastPrice}:Q={qtyStep}:C={lastClosePriceBuy}:BOT"
                                        });
                                        newOrderList.Add(new Order
                                        {
                                            Symbol = BitMEXApiHelper.SYMBOL,
                                            OrdType = "Limit",
                                            Side = "Sell",
                                            OrderQty = qty,
                                            Price = sellPrice,
                                            Text = $"BOT:P={lastPrice}:Q={qtyStep}:C={lastClosePriceSell}:BOT"
                                        });
                                        Logger.WriteLine($"    <limit {i + 1}>\t  qty = {qty}  ({StairLimitQty[i]})  \tbuy_price = {buyPrice}  (-{buyMargin})  \tsell_price = {sellPrice}  (+{sellMargin})");
                                    }
                                }
                                List<Order> newLimitOrderList = apiHelper.OrderNewLimitBulk(newOrderList);
                                Logger.WriteLine($"    {newLimitOrderList.Count} limit orders have been created.");
                                Logger.WriteFile(JArray.FromObject(newLimitOrderList).ToString());
                            }
                        }
                        else
                        {
                            //if (markPriceDistance > config.LimitMarkCancel)
                            //{
                            //    apiHelper.CancelAllOrders();
                            //    Logger.WriteLine($"All of {activeOrdersCount} active orders have been canceled. (mark_distance = {markPriceDistance} > {config.LimitMarkCancel})");
                            //}
                        }
                    }
                    else
                    {
                        decimal startPrice = 0;
                        int qtyStep = 0;
                        decimal positionClosePrice = 0;
                        string side = null;
                        if (position.CurrentQty.Value < 0)
                        {
                            side = "Buy";
                        }
                        else if (position.CurrentQty.Value > 0)
                        {
                            side = "Sell";
                        }
                        bool existCloseOrder = false;
                        int stopMarketCount = 0;
                        List<string> cancelOrderList = new List<string>();
                        for (int i = 0; i < activeOrdersCount; i++)
                        {
                            Order order = activeOrders[i];
                            if (order.Side == side)
                            {
                                if ((order.OrderQty == null || order.OrderQty.Value == Math.Abs(position.CurrentQty.Value)) && order.OrdType == "MarketIfTouched")
                                {
                                    existCloseOrder = true;
                                    positionClosePrice = order.StopPx.Value;
                                }
                                else if ((order.OrderQty == null || order.OrderQty.Value == Math.Abs(position.CurrentQty.Value)) && order.OrdType == "Limit" && order.ExecInst == "Close")
                                {
                                    existCloseOrder = true;
                                    positionClosePrice = order.Price.Value;
                                }
                                else if (order.OrdType == "Stop")
                                {
                                    stopMarketCount++;
                                }
                                else
                                {
                                    cancelOrderList.Add(order.OrderID);
                                }
                            }
                            else if (order.OrdType == "Stop")
                            {
                                cancelOrderList.Add(order.OrderID);
                            }
                            if (startPrice == 0 && !string.IsNullOrWhiteSpace(order.Text))
                            {
                                string[] array = order.Text.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (string s in array)
                                {
                                    if (s.StartsWith("P="))
                                    {
                                        string s2 = s.Substring("P=".Length);
                                        bool result = decimal.TryParse(s2, out decimal dStartPrice);
                                        if (result)
                                        {
                                            startPrice = dStartPrice;
                                        }
                                        else
                                        {
                                            Logger.WriteLine($"Error in parsing start price. (order_text = {order.Text})", ConsoleColor.Red);
                                        }
                                        break;
                                    }
                                }
                            }
                            if (qtyStep == 0 && !string.IsNullOrWhiteSpace(order.Text))
                            {
                                string[] array = order.Text.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (string s in array)
                                {
                                    if (s.StartsWith("Q="))
                                    {
                                        string s2 = s.Substring("Q=".Length);
                                        bool result = int.TryParse(s2, out int iQtyStep);
                                        if (result)
                                        {
                                            qtyStep = iQtyStep;
                                        }
                                        else
                                        {
                                            Logger.WriteLine($"Error in parsing start price. (order_text = {order.Text})", ConsoleColor.Red);
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                        if (cancelOrderList.Count > 0) apiHelper.CancelOrders(cancelOrderList);
                        if (startPrice == 0 || qtyStep == 0)
                            Logger.WriteLine($"    Warning : start_price or qty_step not found. start_price = {startPrice}, qty_step = {qtyStep}", ConsoleColor.DarkYellow);
                        if (stopMarketCount < 1 && startPrice > 0)
                        {
                            List<Order> newOrderList = new List<Order>();
                            if (position.CurrentQty.Value < 0)
                            {
                                for (int i = 0; i < StairStopCount; i++)
                                {
                                    int stopBuyPrice = (int)(startPrice + config.SellHeightDistance + StairStopHeight[i]);
                                    if (stopBuyPrice < lastPrice) continue;
                                    int qty = qtyStep * Math.Abs(StairStopQty[i]);
                                    if (qty > 0)
                                        newOrderList.Add(new Order
                                        {
                                            Symbol = BitMEXApiHelper.SYMBOL,
                                            OrdType = "Stop",
                                            Side = "Buy",
                                            OrderQty = qty,
                                            StopPx = stopBuyPrice,
                                            ExecInst = "Close,LastPrice",
                                            Text = $"BOT:P={startPrice}:Q={qtyStep}:BOT"
                                        });
                                    else
                                        newOrderList.Add(new Order
                                        {
                                            Symbol = BitMEXApiHelper.SYMBOL,
                                            OrdType = "Stop",
                                            Side = "Buy",
                                            StopPx = stopBuyPrice,
                                            ExecInst = "Close,LastPrice",
                                            Text = $"BOT:P={startPrice}:Q={qtyStep}:BOT"
                                        });
                                    Logger.WriteLine($"    <stop {i + 1}>\t  qty = {qty}  ({StairStopQty[i]})  \tbuy_price = {stopBuyPrice}  (+{StairStopHeight[i]})");
                                }
                            }
                            else
                            {
                                for (int i = 0; i < StairStopCount; i++)
                                {
                                    int stopSellPrice = (int)(startPrice - config.BuyHeightDistance - StairStopHeight[i]);
                                    if (stopSellPrice > lastPrice || stopSellPrice <= 0) continue;
                                    int qty = qtyStep * Math.Abs(StairStopQty[i]);
                                    if (qty > 0)
                                        newOrderList.Add(new Order
                                        {
                                            Symbol = BitMEXApiHelper.SYMBOL,
                                            OrdType = "Stop",
                                            Side = "Sell",
                                            OrderQty = qty,
                                            StopPx = stopSellPrice,
                                            ExecInst = "Close,LastPrice",
                                            Text = $"BOT:P={startPrice}:Q={qtyStep}:BOT"
                                        });
                                    else
                                        newOrderList.Add(new Order
                                        {
                                            Symbol = BitMEXApiHelper.SYMBOL,
                                            OrdType = "Stop",
                                            Side = "Sell",
                                            StopPx = stopSellPrice,
                                            ExecInst = "Close,LastPrice",
                                            Text = $"BOT:P={startPrice}:Q={qtyStep}:BOT"
                                        });
                                    Logger.WriteLine($"    <stop {i + 1}>\t  qty = {qty}  ({StairStopQty[i]})  \tbuy_price = {stopSellPrice}  (+{StairStopHeight[i]})");
                                }
                            }
                            List<Order> newLimitOrderList = apiHelper.OrderNewLimitBulk(newOrderList);
                            Logger.WriteLine($"    {newLimitOrderList.Count} stop-market orders have been created.");
                            Logger.WriteFile(JArray.FromObject(newLimitOrderList).ToString());
                        }
                        if (!existCloseOrder)
                        {
                            activeOrders = apiHelper.GetActiveOrdersAll();
                            activeOrdersCount = activeOrders.Count;
                            position = apiHelper.GetPosition();
                            if (position.AvgEntryPrice.Value == 0) throw new Exception("failed to get position price.");
                            int closePrice = 0;
                            if (position.CurrentQty.Value < 0)
                            {
                                side = "Buy";
                                decimal lowest = 0;
                                foreach (var order in activeOrders)
                                {
                                    if (order.OrdType == "Limit" && order.Price != null && order.Side == "Sell" && !order.ExecInst.Contains("Close") && (order.Price.Value < lowest || lowest == 0))
                                    {
                                        lowest = order.Price.Value;
                                        string[] array = order.Text.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                                        foreach (string s in array)
                                        {
                                            if (s.StartsWith("C="))
                                            {
                                                string s2 = s.Substring("C=".Length);
                                                bool result = int.TryParse(s2, out closePrice);
                                                if (!result)
                                                {
                                                    Logger.WriteLine($"Error in parsing close price. (order_text = {order.Text})", ConsoleColor.Red);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                                if (closePrice == 0)
                                {
                                    if (startPrice > 0)
                                        closePrice = (int)Math.Floor(position.AvgEntryPrice.Value - Math.Abs(startPrice - position.AvgEntryPrice.Value) / 2);
                                    else
                                        closePrice = (int)Math.Floor(position.AvgEntryPrice.Value - 100);
                                }
                            }
                            else if (position.CurrentQty.Value > 0)
                            {
                                side = "Sell";
                                decimal highest = 0;
                                foreach (var order in activeOrders)
                                {
                                    if (order.OrdType == "Limit" && order.Price != null && order.Side == "Buy" && !order.ExecInst.Contains("Close") && order.Price.Value > highest)
                                    {
                                        highest = order.Price.Value;
                                        string[] array = order.Text.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                                        foreach (string s in array)
                                        {
                                            if (s.StartsWith("C="))
                                            {
                                                string s2 = s.Substring("C=".Length);
                                                bool result = int.TryParse(s2, out closePrice);
                                                if (!result)
                                                {
                                                    Logger.WriteLine($"Error in parsing close price. (order_text = {order.Text})", ConsoleColor.Red);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                                if (closePrice == 0)
                                {
                                    if (startPrice > 0)
                                        closePrice = (int)Math.Floor(position.AvgEntryPrice.Value + Math.Abs(startPrice - position.AvgEntryPrice.Value) / 2);
                                    else
                                        closePrice = (int)Math.Floor(position.AvgEntryPrice.Value + 500);
                                }
                            }
                            if (closePrice > 0)
                            {
                                //Order closeOrder = apiHelper.OrderNewTakeProfitMarketClose(side, closePrice);
                                Order closeOrder = apiHelper.OrderNewLimitClose(side, closePrice, Math.Abs(position.CurrentQty.Value), $"BOT:P={startPrice}:Q={qtyStep}:BOT");
                                Logger.WriteLine($"New order for position has been created. (qty = {closeOrder.OrderQty}, price = {closePrice:N2})", ConsoleColor.Green);
                                positionClosePrice = closePrice;
                            }
                        }
                        {
                            decimal unrealisedPercent = 100m * position.UnrealisedPnl.Value / margin.WalletBalance.Value;
                            decimal nowLoss = 100m * (lastPrice - position.AvgEntryPrice.Value) / (position.LiquidationPrice.Value - position.AvgEntryPrice.Value);
                            Logger.WriteFile($"wallet_balance = {margin.WalletBalance}    unrealised_pnl = {position.UnrealisedPnl}    {unrealisedPercent:N2} %    leverage = {position.Leverage}");
                            Logger.WriteLine($"    $ {position.AvgEntryPrice.Value:F1} / {positionClosePrice:F1}    qty = {position.CurrentQty.Value}    liq = {position.LiquidationPrice}    {unrealisedPercent:N2} % / {nowLoss:N2} %", ConsoleColor.Green);
                        }
                    }
                    Thread.Sleep(config.ConnectionInverval * 1000);
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
                    Thread.Sleep(4000);
                }
                if (Config.Warning != null) Logger.WriteLine("<Message>  " + Config.Warning, ConsoleColor.Green);
            }
        }
    }
}