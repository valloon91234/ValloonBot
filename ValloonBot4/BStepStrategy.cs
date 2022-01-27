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
    public class BStepStrategy
    {

        public void Run(Config config)
        {
            int loopIndex = 0;
            string[] timeMap = null;
            DateTime? lastLoopTime = null;
            decimal lastWalletBalance = 0;
            while (true)
            {
                BitMEXApiHelper apiHelper = null;
                try
                {
                    DateTime currentLoopTime = DateTime.Now;
                    config = Config.Load(out bool configUpdated, lastLoopTime == null || lastLoopTime.Value.Day != currentLoopTime.Day);
                    BStepConfig bStepConfig = config.BStep;
                    apiHelper = new BitMEXApiHelper(config.ApiKey, config.ApiSecret, config.TestnetMode);
                    if (lastLoopTime == null || lastLoopTime.Value.Hour != currentLoopTime.Hour)
                    {
                        try
                        {
                            TimeZoneInfo pacificZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
                            bool isDaylight = pacificZone.IsDaylightSavingTime(currentLoopTime);
                            String url = isDaylight ?
                                "https://raw.githubusercontent.com/maksimg1002/_upload1002/main/timemap-1m-d.txt" :
                                "https://raw.githubusercontent.com/maksimg1002/_upload1002/main/timemap-1m.txt";
                            string timeMapText = BackendClient.HttpGet(url);
                            timeMap = timeMapText.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                            Logger.WriteLine($"timemap loaded: {timeMap.Length} lines. (daylight = {isDaylight})");
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteLine("failed to load timamap. " + ex.Message, ConsoleColor.Red);
                        }
                    }
                    lastLoopTime = currentLoopTime;
                    if (configUpdated) loopIndex = 0;
                    Margin margin = apiHelper.GetMargin();
                    decimal walletBalance = margin.WalletBalance.Value / 100000000m;
                    List<Order> activeOrders = apiHelper.GetActiveOrders();
                    Instrument instrument = apiHelper.GetInstrument();
                    decimal lastPrice = instrument.LastPrice.Value;
                    decimal markPrice = instrument.MarkPrice.Value;
                    List<TradeBin> binList = apiHelper.GetRencentBinList(bStepConfig.BinSize, 1000, true);
                    string currentCandleTimestamp = binList[0].Timestamp.Value.ToString("yyyy-MM-ddTHH:mm:ss");
                    DateTime serverTime = BitMEXApiHelper.ServerTime.Value;
                    string timeText = serverTime.ToString("yyyy-MM-dd  HH:mm:ss");
                    int oldBotOrderCount = 0;
                    int hurryUp = 0;
                    foreach (Order order in activeOrders)
                        if (order.Text.Contains("<BOT>")) oldBotOrderCount++;
                    {
                        decimal unavailableMarginPercent = 100m * (margin.WalletBalance.Value - margin.AvailableMargin.Value) / margin.WalletBalance.Value;
                        Logger.WriteLine($"[{timeText}  ({++loopIndex})]    $ {lastPrice:F1}  /  $ {markPrice:F2}  /  {binList[1].Volume:N0}  /  {binList[0].Volume:N0}    {walletBalance:N8} XBT    {oldBotOrderCount} / {activeOrders.Count} / {unavailableMarginPercent:N2} %", ConsoleColor.White);
                    }
                    decimal standardDeviation, middleBand, upperBand, lowerBand, bandWidth, bandWidthRatio;
                    {
                        int candleCount = bStepConfig.BBLength;
                        double[] closeArray = new double[candleCount];
                        double[] sd2Array = new double[candleCount];
                        for (int i = 0; i < candleCount; i++)
                        {
                            decimal? close = binList[candleCount - 1 - i].Close;
                            if (close == null)
                            {
                                close = binList[candleCount - i].Close;
                                binList[candleCount - 1 - i].Close = close;
                            }
                            closeArray[i] = (double)close.Value;
                        }
                        double movingAverage = closeArray.Average();
                        for (int i = 0; i < candleCount; i++)
                        {
                            sd2Array[i] = Math.Pow(closeArray[i] - movingAverage, 2);
                        }
                        standardDeviation = (decimal)Math.Pow(sd2Array.Average(), 0.5d);
                        middleBand = (decimal)movingAverage;
                        upperBand = middleBand + standardDeviation * bStepConfig.BBUpperX;
                        lowerBand = middleBand - standardDeviation * bStepConfig.BBLowerX;
                        bandWidth = standardDeviation * bStepConfig.BBX * 2;
                        bandWidthRatio = bandWidth / middleBand;
                    }
                    decimal lastStandardDeviation, lastMiddleBand, lastUpperBand, lastLowerBand, lastBandWidth, lastBandWidthRatio;
                    {
                        int candleCount = bStepConfig.BBLength;
                        double[] closeArray = new double[candleCount];
                        double[] sd2Array = new double[candleCount];
                        for (int i = 0; i < candleCount; i++)
                        {
                            closeArray[i] = (double)binList[candleCount - i].Close.Value;
                        }
                        double movingAverage = closeArray.Average();
                        for (int i = 0; i < candleCount; i++)
                        {
                            sd2Array[i] = Math.Pow(closeArray[i] - movingAverage, 2);
                        }
                        lastStandardDeviation = (decimal)Math.Pow(sd2Array.Average(), 0.5d);
                        lastMiddleBand = (decimal)movingAverage;
                        lastUpperBand = lastMiddleBand + lastStandardDeviation * bStepConfig.BBUpperX;
                        lastLowerBand = lastMiddleBand - lastStandardDeviation * bStepConfig.BBLowerX;
                        lastBandWidth = lastStandardDeviation * bStepConfig.BBX * 2;
                        lastBandWidthRatio = lastBandWidth / lastMiddleBand;
                    }
                    decimal last2StandardDeviation, last2MiddleBand, last2UpperBand, last2LowerBand, last2BandWidth, last2BandWidthRatio;
                    {
                        int candleCount = bStepConfig.BBLength;
                        double[] closeArray = new double[candleCount];
                        double[] sd2Array = new double[candleCount];
                        for (int i = 0; i < candleCount; i++)
                        {
                            closeArray[i] = (double)binList[candleCount + 1 - i].Close.Value;
                        }
                        double movingAverage = closeArray.Average();
                        for (int i = 0; i < candleCount; i++)
                        {
                            sd2Array[i] = Math.Pow(closeArray[i] - movingAverage, 2);
                        }
                        last2StandardDeviation = (decimal)Math.Pow(sd2Array.Average(), 0.5d);
                        last2MiddleBand = (decimal)movingAverage;
                        last2UpperBand = last2MiddleBand + last2StandardDeviation * bStepConfig.BBUpperX;
                        last2LowerBand = last2MiddleBand - last2StandardDeviation * bStepConfig.BBLowerX;
                        last2BandWidth = last2StandardDeviation * bStepConfig.BBX * 2;
                        last2BandWidthRatio = last2BandWidth / last2MiddleBand;
                    }
                    double currentRSI, lastRSI;
                    try
                    {
                        List<TradeBin> reversedBinList = new List<TradeBin>(binList);
                        reversedBinList.Reverse();
                        double[] rsiArray = RSI.CalculateRSIValues(reversedBinList.ToArray(), bStepConfig.RSILength);
                        int rsiArrayLength = rsiArray.Length;
                        currentRSI = rsiArray[rsiArrayLength - 1];
                        lastRSI = rsiArray[rsiArrayLength - 2];
                    }
                    catch (Exception)
                    {
                        currentRSI = 0; lastRSI = 0;
                    }
                    //List<Order> lastFilledOrders = apiHelper.GetOrders("{\"ordStatus\":\"Filled\"}");
                    //Order lastFilledUpperOrder = null;
                    //Order lastFilledLowerOrder = null;
                    //bool lastUpperClosed = false;
                    //bool lastLowerClosed = false;
                    //foreach (Order order in lastFilledOrders)
                    //{
                    //    if (order.Text.Contains("<BOT>BAND-LIMIT<BOT>"))
                    //    {
                    //        if (order.Side == "Buy")
                    //            lastFilledLowerOrder = order;
                    //        else if (order.Side == "Sell")
                    //            lastFilledUpperOrder = order;
                    //        break;
                    //    }
                    //    else if (order.Text.Contains("<BOT>CLOSE-LIMIT<BOT>"))
                    //    {
                    //        if (order.Side == "Buy")
                    //            lastUpperClosed = true;
                    //        else if (order.Side == "Sell")
                    //            lastLowerClosed = true;
                    //    }
                    //}
#if !LICENSE_MODE
                    {
                        string printLine1 = $"        BB = {bandWidth:F1} / {middleBand:F1} / {bandWidthRatio:F4}    _BB = {lastBandWidth:F1} / {lastMiddleBand:F1} / {lastBandWidthRatio:F4}    _BB2 = {last2BandWidth:F1} / {last2MiddleBand:F1} {last2BandWidthRatio:F4}    rsi = {currentRSI:F1} / {lastRSI:F1}";
                        Logger.WriteLine(printLine1, ConsoleColor.DarkGray);
                    }
#endif
                    decimal upperX = bStepConfig.BBUpperX, lowerX = bStepConfig.BBLowerX;
                    {
                        //if (currentRSI > bbConfig.RSIUpper)
                        //    upperX = (decimal)((currentRSI - bbConfig.RSIUpper) / 20d + 2);
                        //else if (currentRSI < bbConfig.RSILower)
                        //    lowerX = (decimal)((bbConfig.RSILower - currentRSI) / 20d + 2);
                        int bbwx = (int)(lastBandWidthRatio * 10000);
                        //if (bbwx < 42)
                        //{
                        //    upperX = 6;
                        //    lowerX = 6;
                        //}
                        //else if (bbwx < 47)
                        //{
                        //    upperX = 5.5m;
                        //    lowerX = 5.5m;
                        //}
                        //else if (bbwx < 54)
                        //{
                        //    upperX = 5;
                        //    lowerX = 5;
                        //}
                        //else
                        if (bbwx < 73)
                        {
                            upperX = 4.5m;
                            lowerX = 4.5m;
                        }
                        else if (bbwx < 97)
                        {
                            upperX = 4;
                            lowerX = 4;
                        }
                        else if (bbwx < 123)
                        {
                            upperX = 3.5m;
                            lowerX = 3.5m;
                        }
                        else if (bbwx < 142)
                        {
                            upperX = 3;
                            lowerX = 3;
                        }
                        else
                        {
                            upperX = 2;
                            lowerX = 2;
                        }
                        decimal sdx = Math.Max(Math.Round(10 * (decimal)Math.Sqrt((double)(standardDeviation / last2StandardDeviation))) / 10m, 1);
#if !LICENSE_MODE
                        Logger.WriteLine($"        bbwx = {bbwx}    upper_x = {upperX:F1} * {sdx:F1} = {upperX * sdx:F2}    lower_x = {lowerX:F1} * {sdx:F1} = {lowerX * sdx:F2}", ConsoleColor.DarkGray);
#endif
                        upperX *= sdx;
                        lowerX *= sdx;
                        if (timeMap != null)
                        {
                            DateTime t = binList[0].Timestamp.Value;
                            foreach (string line in timeMap)
                            {
                                string[] ttt = line.Trim().Split('/');
                                string[] tt = ttt[0].Trim().Split(':');
                                int h = int.Parse(tt[0]);
                                int m = int.Parse(tt[1]);
                                int x = int.Parse(ttt[1]);
                                if (t.Hour == h && t.Minute == m)
                                {
                                    if (upperX < x) upperX = x;
                                    if (lowerX < x) lowerX = x;
#if !LICENSE_MODE
                                    Logger.WriteLine($"    special_time = {h:D2}:{m:D2}, x = {x:F2}", ConsoleColor.DarkYellow);
#endif
                                    break;
                                }
                            }
                        }
                        if (upperX < bStepConfig.BBUpperX)
                            upperX = bStepConfig.BBUpperX;
                        if (lowerX < bStepConfig.BBLowerX)
                            lowerX = bStepConfig.BBLowerX;
                    }
                    Position position = apiHelper.GetPosition();
                    decimal positionEntryPrice = 0;
                    int positionQty = 0;
                    {
                        string consoleTitle = $"$ {lastPrice:N0}  /  {walletBalance:N8} XBT";
                        if (position != null && position.CurrentQty.Value != 0) consoleTitle += "  /  1 Position";
                        consoleTitle += $"  <{config.Username}>  |   {Config.APP_NAME}  v{Config.APP_VERSION}   by  ValloonTrader.com";
                        Console.Title = consoleTitle;
                        //if (position != null && position.CurrentQty.Value != 0)
                        //{
                        //    decimal unrealisedPercent = 100m * position.UnrealisedPnl.Value / margin.WalletBalance.Value;
                        //    decimal nowLoss = 100m * (lastPrice - position.AvgEntryPrice.Value) / (position.LiquidationPrice.Value - position.AvgEntryPrice.Value);
                        //    Logger.WriteFile($"        wallet_balance = {margin.WalletBalance}    unrealised_pnl = {position.UnrealisedPnl}    {unrealisedPercent:N2} %    leverage = {position.Leverage}");
                        //    Logger.WriteLine($"        entry = {position.AvgEntryPrice.Value:F1}    qty = {position.CurrentQty.Value}    liq = {position.LiquidationPrice}    {unrealisedPercent:N2} % / {nowLoss:N2} %", ConsoleColor.Green);
                        //}
                    }
                    lowerBand = middleBand - standardDeviation * lowerX;
                    upperBand = middleBand + standardDeviation * upperX;
                    if (position == null || position.CurrentQty.Value == 0)
                    {
                        if (config.Exit > 0)
                        {
                            if (activeOrders.Count > 0) apiHelper.CancelAllOrders();
                            Logger.WriteLine($"No order. (exit = {config.Exit})", ConsoleColor.DarkGray);
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
                        List<Order> newOrderList = new List<Order>();
                        if (bandWidthRatio >= bStepConfig.MinBandRatio || lastBandWidthRatio >= bStepConfig.MinBandRatio) hurryUp = 1;
                        if (bandWidthRatio >= bStepConfig.MinBandRatio && lastBandWidthRatio >= bStepConfig.MinBandRatio)
                        {
                            int qtyStep = (int)(margin.WalletBalance.Value * lastPrice * bStepConfig.QtyRatio / 1000000);
                            int lowerQtyStep = (int)(bStepConfig.LowerQtyX * qtyStep);
                            int upperQtyStep = (int)(bStepConfig.UpperQtyX * qtyStep);
                            if (bStepConfig.BuyOrSell != 2 && lastPrice < middleBand - standardDeviation && lowerBand < binList[0].Low.Value)
                            {
                                hurryUp = 2;
                                int limitPrice = (int)Math.Floor(lowerBand);
                                int closeLimitPrice = limitPrice + (int)((middleBand - lowerBand) * bStepConfig.CloseHeightRatio);
                                //int limitPrice2 = limitPrice - (int)Math.Floor(standardDeviation * Math.Max(lowerX * 0.5m, 2));
                                //int stopMarketPrice2 = limitPrice2 - (limitPrice - limitPrice2);
                                int limitPrice2 = limitPrice - (int)(standardDeviation * 3);
                                int stopMarketPrice2 = limitPrice2 - (int)(standardDeviation * 2);
                                int closeLimitPrice2 = limitPrice;
                                int qty = BitMEXApiHelper.FixQty(lowerQtyStep);
                                newOrderList.Add(new Order
                                {
                                    Symbol = BitMEXApiHelper.SYMBOL,
                                    Side = "Buy",
                                    OrderQty = qty,
                                    Price = limitPrice,
                                    OrdType = "Limit",
                                    Text = $"<BOT><BUY-LIMIT><1><{currentCandleTimestamp}></BOT>"
                                });
                                newOrderList.Add(new Order
                                {
                                    Symbol = BitMEXApiHelper.SYMBOL,
                                    Side = "Sell",
                                    OrderQty = qty,
                                    Price = closeLimitPrice,
                                    StopPx = limitPrice,
                                    OrdType = "StopLimit",
                                    ExecInst = "LastPrice,ReduceOnly",
                                    Text = $"<BOT><BUY-CLOSE><1><{currentCandleTimestamp}></BOT>"
                                });
                                newOrderList.Add(new Order
                                {
                                    Symbol = BitMEXApiHelper.SYMBOL,
                                    Side = "Buy",
                                    OrderQty = qty,
                                    Price = limitPrice2,
                                    OrdType = "Limit",
                                    Text = $"<BOT><BUY-LIMIT><2><{currentCandleTimestamp}></BOT>"
                                });
                                newOrderList.Add(new Order
                                {
                                    Symbol = BitMEXApiHelper.SYMBOL,
                                    Side = "Sell",
                                    OrderQty = qty * 2,
                                    StopPx = stopMarketPrice2,
                                    OrdType = "Stop",
                                    ExecInst = "LastPrice,ReduceOnly",
                                    Text = $"<BOT><BUY-STOP><2><{currentCandleTimestamp}></BOT>"
                                });
                                newOrderList.Add(new Order
                                {
                                    Symbol = BitMEXApiHelper.SYMBOL,
                                    Side = "Sell",
                                    OrderQty = qty * 2,
                                    Price = closeLimitPrice2,
                                    StopPx = limitPrice2,
                                    OrdType = "StopLimit",
                                    ExecInst = "LastPrice,ReduceOnly",
                                    Text = $"<BOT><BUY-CLOSE><2><{currentCandleTimestamp}></BOT>"
                                });
                            }
                            if (bStepConfig.BuyOrSell != 1 && lastPrice > middleBand + standardDeviation && upperBand > binList[0].High.Value)
                            {
                                hurryUp = 2;
                                int limitPrice = (int)Math.Ceiling(upperBand);
                                int closeLimitPrice = limitPrice - (int)((upperBand - middleBand) * bStepConfig.CloseHeightRatio); ;
                                //int limitPrice2 = limitPrice + (int)Math.Floor(standardDeviation * Math.Max(upperX * 0.5m, 3));
                                int limitPrice2 = limitPrice + (int)(standardDeviation * 3);
                                int stopMarketPrice2 = limitPrice2 + (int)(standardDeviation * 2);
                                int closeLimitPrice2 = limitPrice;
                                int qty = BitMEXApiHelper.FixQty(upperQtyStep);
                                newOrderList.Add(new Order
                                {
                                    Symbol = BitMEXApiHelper.SYMBOL,
                                    Side = "Sell",
                                    OrderQty = qty,
                                    Price = limitPrice,
                                    OrdType = "Limit",
                                    Text = $"<BOT><SELL-LIMIT><1><{currentCandleTimestamp}></BOT>"
                                });
                                newOrderList.Add(new Order
                                {
                                    Symbol = BitMEXApiHelper.SYMBOL,
                                    Side = "Buy",
                                    OrderQty = qty,
                                    Price = closeLimitPrice,
                                    StopPx = limitPrice,
                                    OrdType = "StopLimit",
                                    ExecInst = "LastPrice,ReduceOnly",
                                    Text = $"<BOT><SELL-CLOSE><1><{currentCandleTimestamp}></BOT>"
                                });
                                newOrderList.Add(new Order
                                {
                                    Symbol = BitMEXApiHelper.SYMBOL,
                                    Side = "Sell",
                                    OrderQty = qty,
                                    Price = limitPrice2,
                                    OrdType = "Limit",
                                    Text = $"<BOT><SELL-LIMIT><2><{currentCandleTimestamp}></BOT>"
                                });
                                newOrderList.Add(new Order
                                {
                                    Symbol = BitMEXApiHelper.SYMBOL,
                                    Side = "Buy",
                                    OrderQty = qty * 2,
                                    StopPx = stopMarketPrice2,
                                    OrdType = "Stop",
                                    ExecInst = "LastPrice,ReduceOnly",
                                    Text = $"<BOT><SELL-STOP><2><{currentCandleTimestamp}></BOT>"
                                });
                                newOrderList.Add(new Order
                                {
                                    Symbol = BitMEXApiHelper.SYMBOL,
                                    Side = "Buy",
                                    OrderQty = qty * 2,
                                    Price = closeLimitPrice2,
                                    StopPx = limitPrice2,
                                    OrdType = "StopLimit",
                                    ExecInst = "LastPrice,ReduceOnly",
                                    Text = $"<BOT><SELL-CLOSE><2><{currentCandleTimestamp}></BOT>"
                                });
                            }
                        }
                        List<string> cancelOrderList = new List<string>();
                        List<Order> amendOrderList = new List<Order>();
                        int duplicatedOrderCount = 0, amendOrderCount = 0, newOrderCount = 0;
                        foreach (Order oldOrder in activeOrders)
                        {
                            if (oldOrder.Text.Contains("<BOT>"))
                            {
                                if (string.IsNullOrEmpty(oldOrder.Triggered))
                                    foreach (Order newOrder in newOrderList)
                                    {
                                        if (oldOrder.Symbol == newOrder.Symbol && oldOrder.OrdType == newOrder.OrdType && oldOrder.Side == newOrder.Side &&
                                            (oldOrder.Text.Contains("<1>") && newOrder.Text.Contains("<1>") || oldOrder.Text.Contains("<2>") && newOrder.Text.Contains("<2>")))
                                        {
                                            //newOrderList.Remove(newOrder);
                                            if (oldOrder.OrderQty == newOrder.OrderQty && oldOrder.Price == newOrder.Price && oldOrder.StopPx == newOrder.StopPx)
                                            {
                                                newOrderList.Remove(newOrder);
                                                duplicatedOrderCount++;
                                                goto loopActiveOrders;
                                            }
                                            //else
                                            //{
                                            //newOrder.OrderID = oldOrder.OrderID;
                                            //amendOrderList.Add(newOrder);
                                            //}
                                            //goto loopActiveOrders;
                                        }
                                    }
                                cancelOrderList.Add(oldOrder.OrderID);
                            }
                        loopActiveOrders:;
                        }
                        int cancelOrderCount = cancelOrderList.Count;
                        bool firstOrderAlreadyFilled = false;
                        if (cancelOrderCount > 0)
                        {
                            var canceledOrderCount = apiHelper.CancelOrders(cancelOrderList).Count;
                            if (cancelOrderCount > canceledOrderCount)
                            {
                                firstOrderAlreadyFilled = true;
                                Logger.WriteLine($"        first order already filled. request = {cancelOrderCount}, canceled = {canceledOrderCount}", ConsoleColor.Red);
                            }
                        }
                        if (amendOrderList.Count > 0)
                        {
                            List<Order> resultOrderList = new List<Order>();
                            foreach (Order order in amendOrderList)
                            {
                                resultOrderList.Add(apiHelper.OrderAmend(order));
                            }
                            amendOrderCount = resultOrderList.Count;
                            Logger.WriteFile("--- " + JArray.FromObject(resultOrderList).ToString(Formatting.None));
                        }
                        if (newOrderList.Count > 0)
                        {
                            List<Order> resultOrderList = new List<Order>();
                            foreach (Order order in newOrderList)
                            {
                                string text = order.Text;
                                if (firstOrderAlreadyFilled && text.Contains("LIMIT") && text.Contains("<1>")) continue;
                                resultOrderList.Add(apiHelper.OrderNew(order.Side, order.OrderQty, order.Price, order.StopPx, order.OrdType, order.ExecInst, order.Text));
                            }
                            newOrderCount = resultOrderList.Count;
                            Logger.WriteFile("--- " + JArray.FromObject(resultOrderList).ToString(Formatting.None));
                        }
                        if (cancelOrderCount + newOrderCount + amendOrderCount > 0)
                            Logger.WriteLine($"        canceled = {cancelOrderCount}, amended = {amendOrderCount}, created = {newOrderCount}, final = {newOrderCount + amendOrderCount + duplicatedOrderCount} orders in all.");
                    }
                    else
                    {
                        Logger.WriteLine($"    <Position>    entry = {position.AvgEntryPrice.Value:F1}    qty = {position.CurrentQty.Value}    liq = {position.LiquidationPrice:F1}    leverage = {position.Leverage:F1}", ConsoleColor.Green);
                        positionEntryPrice = position.AvgEntryPrice.Value;
                        positionQty = position.CurrentQty.Value;
                        hurryUp = 2;
                        List<Order> amendOrderList = new List<Order>();
                        if (positionQty < 0)
                        {
                            bool close1Exist = false;
                            foreach (Order oldOrder in activeOrders)
                            {
                                if (oldOrder.Text.Contains("<SELL-CLOSE>") && oldOrder.Text.Contains("<1>"))
                                {
                                    if (string.IsNullOrEmpty(oldOrder.Triggered))
                                    {
                                        var cancelOrderList = new List<string>
                                            {
                                                oldOrder.OrderID
                                            };
                                        apiHelper.CancelOrders(cancelOrderList);
                                        apiHelper.OrderNewLimitClose(oldOrder.Side, oldOrder.Price);
                                        Logger.WriteLine($"        <BUY> stop limit order replaced into limit close order.", ConsoleColor.DarkGreen);
                                        activeOrders.Remove(oldOrder);
                                    }
                                    else
                                        Logger.WriteLine($"        <BUY> stop limit order triggered.", ConsoleColor.DarkGreen);
                                    close1Exist = true;
                                    break;
                                }
                            }
                            bool limit2Exist = false;
                            foreach (Order oldOrder in activeOrders)
                            {
                                if (oldOrder.Text.Contains("<SELL-LIMIT>") && oldOrder.Text.Contains("<2>"))
                                {
                                    limit2Exist = true;
                                    break;
                                }
                            }
                            if (!limit2Exist)
                            {
                                foreach (Order oldOrder in activeOrders)
                                {
                                    if (oldOrder.Text.Contains("<SELL-CLOSE>") && oldOrder.Text.Contains("<2>"))
                                    {
                                        if (string.IsNullOrEmpty(oldOrder.Triggered))
                                        {
                                            var cancelOrderList = new List<string>
                                                {
                                                    oldOrder.OrderID
                                                };
                                            apiHelper.CancelOrders(cancelOrderList);
                                            apiHelper.OrderNewLimitClose(oldOrder.Side, oldOrder.Price);
                                            Logger.WriteLine($"        <BUY> 2nd stop limit order replaced into limit close order.", ConsoleColor.DarkGreen);
                                            activeOrders.Remove(oldOrder);
                                        }
                                        else
                                            Logger.WriteLine($"        <BUY> 2nd stop limit order triggered.", ConsoleColor.DarkGreen);
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                if (!close1Exist)
                                {
                                    apiHelper.OrderNewLimitClose("Buy", positionEntryPrice * 0.998m);
                                    Logger.WriteLine($"        <BUY> stop limit order does not exist, new close order created.", ConsoleColor.DarkRed);
                                }
                                foreach (Order oldOrder in activeOrders)
                                {
                                    string text = oldOrder.Text;
                                    if (!text.Contains($"<{currentCandleTimestamp}>")) continue;
                                    int limitPrice = (int)Math.Ceiling(upperBand);
                                    int closeLimitPrice = limitPrice - (int)((upperBand - middleBand) * bStepConfig.CloseHeightRatio); ;
                                    int limitPrice2 = limitPrice + (int)(standardDeviation * 3);
                                    int stopMarketPrice2 = limitPrice2 + (int)(standardDeviation * 2);
                                    int closeLimitPrice2 = limitPrice;
                                    if (text.Contains("<SELL-LIMIT>") && text.Contains("<2>"))
                                    {
                                        if (oldOrder.Price != limitPrice2)
                                        {
                                            Order order = new Order()
                                            {
                                                OrderID = oldOrder.OrderID,
                                                OrderQty = oldOrder.OrderQty,
                                                Price = limitPrice2,
                                                Text = $"<BOT><SELL-LIMIT><2><{currentCandleTimestamp}></BOT>"
                                            };
                                            amendOrderList.Add(order);
                                            Logger.WriteLine($"        <SELL-LIMIT><2> order will be amended.", ConsoleColor.DarkGreen);
                                        }
                                    }
                                    else if (text.Contains("<SELL-STOP>") && text.Contains("<2>"))
                                    {
                                        if (oldOrder.StopPx != stopMarketPrice2)
                                        {
                                            Order order = new Order()
                                            {
                                                OrderID = oldOrder.OrderID,
                                                OrderQty = oldOrder.OrderQty,
                                                StopPx = stopMarketPrice2,
                                                Text = $"<BOT><SELL-STOP><2><{currentCandleTimestamp}></BOT>",
                                            };
                                            amendOrderList.Add(order);
                                            Logger.WriteLine($"        <SELL-STOP><2> order will be amended.", ConsoleColor.DarkGreen);
                                        }
                                    }
                                    else if (text.Contains("<SELL-CLOSE>") && text.Contains("<2>"))
                                    {
                                        if (oldOrder.Price != closeLimitPrice2 || oldOrder.StopPx != limitPrice2)
                                        {
                                            Order order = new Order()
                                            {
                                                OrderID = oldOrder.OrderID,
                                                OrderQty = oldOrder.OrderQty,
                                                Price = closeLimitPrice2,
                                                StopPx = limitPrice2,
                                                Text = $"<BOT><SELL-CLOSE><2><{currentCandleTimestamp}></BOT>",
                                            };
                                            amendOrderList.Add(order);
                                            Logger.WriteLine($"        <SELL-CLOSE><2> order will be amended.", ConsoleColor.DarkGreen);
                                        }
                                    }
                                }
                            }
                            foreach (Order oldOrder in activeOrders)
                            {
                                if (oldOrder.OrdType == "Stop")
                                    goto stopExist;
                            }
                            if (bStepConfig.ForceClosePosition == 1)
                            {
                                apiHelper.OrderNewMarketClose("Buy");
                                Logger.WriteLine($"        no stop order. position forcibly closed.", ConsoleColor.Red);
                            }
                        stopExist:;
                        }
                        else if (positionQty > 0)
                        {
                            bool close1Exist = false;
                            foreach (Order oldOrder in activeOrders)
                            {
                                if (oldOrder.Text.Contains("<BUY-CLOSE>") && oldOrder.Text.Contains("<1>"))
                                {
                                    if (string.IsNullOrEmpty(oldOrder.Triggered))
                                    {
                                        var cancelOrderList = new List<string>
                                            {
                                                oldOrder.OrderID
                                            };
                                        apiHelper.CancelOrders(cancelOrderList);
                                        apiHelper.OrderNewLimitClose(oldOrder.Side, oldOrder.Price);
                                        Logger.WriteLine($"        <SELL> stop limit order replaced into limit close order.", ConsoleColor.DarkGreen);
                                        activeOrders.Remove(oldOrder);
                                    }
                                    else
                                        Logger.WriteLine($"        <SELL> stop limit order triggered.", ConsoleColor.DarkGreen);
                                    close1Exist = true;
                                    break;
                                }
                            }
                            bool limit2Exist = false;
                            foreach (Order oldOrder in activeOrders)
                            {
                                if (oldOrder.Text.Contains("<BUY-LIMIT>") && oldOrder.Text.Contains("<2>"))
                                {
                                    limit2Exist = true;
                                    break;
                                }
                            }
                            if (!limit2Exist)
                            {
                                foreach (Order oldOrder in activeOrders)
                                {
                                    if (oldOrder.Text.Contains("<BUY-CLOSE>") && oldOrder.Text.Contains("<2>"))
                                    {
                                        if (string.IsNullOrEmpty(oldOrder.Triggered))
                                        {
                                            var cancelOrderList = new List<string>
                                                {
                                                    oldOrder.OrderID
                                                };
                                            apiHelper.CancelOrders(cancelOrderList);
                                            apiHelper.OrderNewLimitClose(oldOrder.Side, oldOrder.Price);
                                            Logger.WriteLine($"        <SELL> 2nd stop limit order replaced into limit close order.", ConsoleColor.DarkGreen);
                                            activeOrders.Remove(oldOrder);
                                        }
                                        else
                                            Logger.WriteLine($"        <SELL> 2nd stop limit order triggered.", ConsoleColor.DarkGreen);
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                if (!close1Exist)
                                {
                                    apiHelper.OrderNewLimitClose("Sell", positionEntryPrice * 1.002m);
                                    Logger.WriteLine($"        <SELL> stop limit order does not exist, new close order created.", ConsoleColor.DarkRed);
                                }
                                foreach (Order oldOrder in activeOrders)
                                {
                                    string text = oldOrder.Text;
                                    if (!text.Contains($"<{currentCandleTimestamp}>")) continue;
                                    int limitPrice = (int)Math.Floor(lowerBand);
                                    int closeLimitPrice = limitPrice + (int)((middleBand - lowerBand) * bStepConfig.CloseHeightRatio);
                                    int limitPrice2 = limitPrice - (int)(standardDeviation * 3);
                                    int stopMarketPrice2 = limitPrice2 - (int)(standardDeviation * 2);
                                    int closeLimitPrice2 = limitPrice;
                                    if (text.Contains("<BUY-LIMIT>") && text.Contains("<2>"))
                                    {
                                        if (oldOrder.Price != limitPrice2)
                                        {
                                            Order order = new Order()
                                            {
                                                OrderID = oldOrder.OrderID,
                                                OrderQty = oldOrder.OrderQty,
                                                Price = limitPrice2,
                                                Text = $"<BOT><BUY-LIMIT><2><{currentCandleTimestamp}></BOT>"
                                            };
                                            amendOrderList.Add(order);
                                            Logger.WriteLine($"        <BUY-LIMIT><2> order will be amended.", ConsoleColor.DarkGreen);
                                        }
                                    }
                                    else if (text.Contains("<BUY-STOP>") && text.Contains("<2>"))
                                    {
                                        if (oldOrder.StopPx != stopMarketPrice2)
                                        {
                                            Order order = new Order()
                                            {
                                                OrderID = oldOrder.OrderID,
                                                OrderQty = oldOrder.OrderQty,
                                                StopPx = stopMarketPrice2,
                                                Text = $"<BOT><BUY-STOP><2><{currentCandleTimestamp}></BOT>",
                                            };
                                            amendOrderList.Add(order);
                                            Logger.WriteLine($"        <BUY-STOP><2> order will be amended.", ConsoleColor.DarkGreen);
                                        }
                                    }
                                    else if (text.Contains("<BUY-CLOSE>") && text.Contains("<2>"))
                                    {
                                        if (oldOrder.Price != closeLimitPrice2 || oldOrder.StopPx != limitPrice2)
                                        {
                                            Order order = new Order()
                                            {
                                                OrderID = oldOrder.OrderID,
                                                OrderQty = oldOrder.OrderQty,
                                                Price = closeLimitPrice2,
                                                StopPx = limitPrice2,
                                                Text = $"<BOT><BUY-CLOSE><2><{currentCandleTimestamp}></BOT>",
                                            };
                                            amendOrderList.Add(order);
                                            Logger.WriteLine($"        <BUY-CLOSE><2> order will be amended.", ConsoleColor.DarkGreen);
                                        }
                                    }
                                }
                            }
                            foreach (Order oldOrder in activeOrders)
                            {
                                if (oldOrder.OrdType == "Stop")
                                    goto stopExist;
                            }
                            if (bStepConfig.ForceClosePosition == 1)
                            {
                                apiHelper.OrderNewMarketClose("Buy");
                                Logger.WriteLine($"        no stop order. position forcibly closed.", ConsoleColor.Red);
                            }
                        stopExist:;
                        }
                        if (amendOrderList.Count > 0)
                        {
                            List<Order> resultOrderList = new List<Order>();
                            foreach (Order order in amendOrderList)
                            {
                                resultOrderList.Add(apiHelper.OrderAmend(order));
                            }
                            Logger.WriteFile("--- " + JArray.FromObject(resultOrderList).ToString(Formatting.None));
                        }
                    }
                    if (lastWalletBalance != walletBalance)
                    {
                        if (lastWalletBalance != 0)
                        {
                            string filenameSuffix = "-balance";
                            if (!Logger.ExistFile(filenameSuffix))
                                Logger.WriteFile($"[{timeText}  ({++loopIndex:D5})]    {lastWalletBalance:F8}", filenameSuffix);
                            decimal diff = walletBalance - lastWalletBalance;
                            string suffix = null;
                            if (positionQty != 0) suffix = $"{positionQty}";
                            if (diff > 0)
                                Logger.WriteFile($"[{timeText}  ({++loopIndex:D5})]    {walletBalance:F8}     {diff:F8}    {lastBandWidth:F1} / {bandWidth:F1}    {suffix}", filenameSuffix);
                            else
                                Logger.WriteFile($"[{timeText}  ({++loopIndex:D5})]    {walletBalance:F8}    {diff:F8}    {lastBandWidth:F1} / {bandWidth:F1}    {suffix}", filenameSuffix);
                        }
                        lastWalletBalance = walletBalance;
                    }
                    int sleep;
                    if (hurryUp == 0)
                        sleep = 20000 - (int)(DateTime.Now - currentLoopTime).TotalMilliseconds;
                    else if (hurryUp == 1)
                        sleep = apiHelper.RequestCount * 1000 - (int)(DateTime.Now - currentLoopTime).TotalMilliseconds;
                    else
                        sleep = apiHelper.RequestCount * 1000 / 2 - (int)(DateTime.Now - currentLoopTime).TotalMilliseconds + 1000;
                    Logger.WriteLine($"        hurry = {hurryUp}, sleeping {sleep:N0} ms ...", ConsoleColor.DarkGray);
                    if (sleep > 0) Thread.Sleep(sleep);
                    Thread.Sleep(bStepConfig.ConnectionInverval * 1000);
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