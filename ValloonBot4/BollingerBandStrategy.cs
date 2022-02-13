using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using IO.Swagger.Client;
using IO.Swagger.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Valloon.Trading.Utils;

/**
 * @author Valloon Project
 * @version 1.0 @2020-05-29
 * @version 1.1 @2020-06-02
 * @version 1.2 @2020-06-18
 */
namespace Valloon.Trading
{
    public class BollingerBandStrategy
    {

        public void Run()
        {
            int loopIndex = 0;
            int upperQtyStep = 0;
            int lowerQtyStep = 0;
            string[] timeMap = null;
            //bool retryOrder = false;
            DateTime? lastLoopTime = null;
            while (true)
            {
                try
                {
                    DateTime currentLoopTime = DateTime.Now;
                    BollingerBandConfig config = BollingerBandConfig.Load(out bool configUpdated, lastLoopTime == null || lastLoopTime.Value.Day != currentLoopTime.Day);
                    BitMEXApiHelper apiHelper = new BitMEXApiHelper(config.ApiKey, config.ApiSecret, config.TestnetMode);
                    if (lastLoopTime == null || lastLoopTime.Value.Hour != currentLoopTime.Hour)
                    {
                        try
                        {
                            string timeMapText = BackendClient.HttpGet("https://raw.githubusercontent.com/anonymous-bye/node/master/BOT/timemap.txt");
                            timeMap = timeMapText.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteLine("failed to load timamap. " + ex.Message, ConsoleColor.Red);
                        }
                    }
                    lastLoopTime = currentLoopTime;
                    if (configUpdated) loopIndex = 0;
                    Instrument instrument = apiHelper.GetInstrument();
                    decimal lastPrice = instrument.LastPrice.Value;
                    decimal markPrice = instrument.MarkPrice.Value;
                    Margin margin = apiHelper.GetMargin();
                    List<TradeBin> binList = apiHelper.GetRencentBinList(config.BinSize, 1000, true);
                    List<Order> activeOrders = apiHelper.GetActiveOrders();
                    int oldBotOrderCount = 0;
                    foreach (Order order in activeOrders)
                        if (order.Text.Contains("<BOT>")) oldBotOrderCount++;
                    Position position = apiHelper.GetPosition();
                    {
                        decimal walletBalance = margin.WalletBalance.Value / 100000000m;
                        string consoleTitle = $"$ {lastPrice:N0}  /  {walletBalance:N8} XBT";
                        if (position != null && position.CurrentQty.Value != 0) consoleTitle += "  /  1 Position";
                        consoleTitle += $"  <{config.Username}>  |  {BollingerBandConfig.APP_NAME}  v{BollingerBandConfig.APP_VERSION}";
                        Console.Title = consoleTitle;
                        string timeText = BitMEXApiHelper.ServerTime.Value.ToString("yyyy-MM-dd  HH:mm:ss");
                        decimal unavailableMarginPercent = 100m * (margin.WalletBalance.Value - margin.AvailableMargin.Value) / margin.WalletBalance.Value;
                        Logger.WriteLine($"[{timeText}  ({++loopIndex})]    $ {lastPrice:F1}  /  $ {markPrice:F2}  /  {binList[1].Volume:N0}  /  {binList[0].Volume:N0}    {walletBalance:N8} XBT    {oldBotOrderCount} / {activeOrders.Count} / {unavailableMarginPercent:N2} %", ConsoleColor.White);
                        if (position != null && position.CurrentQty.Value != 0)
                        {
                            decimal unrealisedPercent = 100m * position.UnrealisedPnl.Value / margin.WalletBalance.Value;
                            decimal nowLoss = 100m * (lastPrice - position.AvgEntryPrice.Value) / (position.LiquidationPrice.Value - position.AvgEntryPrice.Value);
                            Logger.WriteFile($"    wallet_balance = {margin.WalletBalance}    unrealised_pnl = {position.UnrealisedPnl}    {unrealisedPercent:N2} %    leverage = {position.Leverage}");
                            Logger.WriteLine($"    entry = {position.AvgEntryPrice.Value:F1}    qty = {position.CurrentQty.Value}    liq = {position.LiquidationPrice}    {unrealisedPercent:N2} % / {nowLoss:N2} %", ConsoleColor.Green);
                        }
                    }
                    {
                        int candleCount1 = config.BBLength1, candleCount2 = config.BBLength2, candleCount3 = config.BBLength3;
                        double upperX1 = config.BBUpperX1, upperX2 = config.BBUpperX2, upperX3 = config.BBUpperX3;
                        double lowerX1 = config.BBLowerX1, lowerX2 = config.BBLowerX2, lowerX3 = config.BBLowerX3;
                        double standardDeviation1, standardDeviation2, standardDeviation3;
                        decimal middleBand1, middleBand2, middleBand3, upperBand1, lowerBand1, upperBand2, lowerBand2, upperBand3, lowerBand3;
                        {
                            int candleCount = candleCount1;
                            double[] closeArray = new double[candleCount];
                            double[] sd2Array = new double[candleCount];
                            for (int i = 0; i < candleCount; i++)
                            {
                                closeArray[i] = (double)binList[candleCount - 1 - i].Close.Value;
                            }
                            double movingAverage = closeArray.Average();
                            for (int i = 0; i < candleCount; i++)
                            {
                                sd2Array[i] = Math.Pow(closeArray[i] - movingAverage, 2);
                            }
                            standardDeviation1 = Math.Pow(sd2Array.Average(), 0.5d);
                            middleBand1 = (decimal)movingAverage;
                            upperBand1 = middleBand1 + (decimal)(standardDeviation1 * upperX1);
                            lowerBand1 = middleBand1 - (decimal)(standardDeviation1 * lowerX1);
                        }
                        {
                            int candleCount = candleCount2;
                            double[] closeArray = new double[candleCount];
                            double[] sd2Array = new double[candleCount];
                            for (int i = 0; i < candleCount; i++)
                            {
                                closeArray[i] = (double)binList[candleCount - 1 - i].Close.Value;
                            }
                            double movingAverage = closeArray.Average();
                            for (int i = 0; i < candleCount; i++)
                            {
                                sd2Array[i] = Math.Pow(closeArray[i] - movingAverage, 2);
                            }
                            standardDeviation2 = Math.Pow(sd2Array.Average(), 0.5d);
                            middleBand2 = (decimal)movingAverage;
                            upperBand2 = middleBand2 + (decimal)(standardDeviation2 * upperX2);
                            lowerBand2 = middleBand2 - (decimal)(standardDeviation2 * lowerX2);
                        }
                        {
                            int candleCount = candleCount3;
                            double[] closeArray = new double[candleCount];
                            double[] sd2Array = new double[candleCount];
                            for (int i = 0; i < candleCount; i++)
                            {
                                closeArray[i] = (double)binList[candleCount - 1 - i].Close.Value;
                            }
                            double movingAverage = closeArray.Average();
                            for (int i = 0; i < candleCount; i++)
                            {
                                sd2Array[i] = Math.Pow(closeArray[i] - movingAverage, 2);
                            }
                            standardDeviation3 = Math.Pow(sd2Array.Average(), 0.5d);
                            middleBand3 = (decimal)movingAverage;
                            upperBand3 = middleBand3 + (decimal)(standardDeviation3 * upperX3);
                            lowerBand3 = middleBand3 - (decimal)(standardDeviation3 * lowerX3);
                        }
                        double lastStandardDeviation;
                        decimal lastMiddleBand, lastUpperBand, lastLowerBand;
                        {
                            int candleCount = candleCount1;
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
                            lastStandardDeviation = Math.Pow(sd2Array.Average(), 0.5d);
                            lastMiddleBand = (decimal)movingAverage;
                            decimal distance2 = (decimal)(lastStandardDeviation * 3);
                            lastUpperBand = lastMiddleBand + distance2;
                            lastLowerBand = lastMiddleBand - distance2;
                        }
                        double currentRSI, lastRSI;
                        {
                            List<TradeBin> reversedBinList = new List<TradeBin>(binList);
                            reversedBinList.Reverse();
                            double[] rsiArray = RSI.CalculateRSIValues(reversedBinList.ToArray(), config.RSILength);
                            int rsiArrayLength = rsiArray.Length;
                            currentRSI = rsiArray[rsiArrayLength - 1];
                            lastRSI = rsiArray[rsiArrayLength - 2];
                        }
                        int lowerPositionQtySize = 0, upperPositionQtySize = 0;
                        int lowerHold = 0, upperHold = 0;
                        List<Order> lastFilledOrders = apiHelper.GetOrders("{\"ordStatus\":\"Filled\"}");
                        Order lastFilledUpperOrder = null;
                        Order lastFilledLowerOrder = null;
                        bool lastUpperClosed = false;
                        bool lastLowerClosed = false;
                        foreach (Order order in lastFilledOrders)
                        {
                            if (order.Text.Contains("<BOT>BAND-LIMIT<BOT>"))
                            {
                                if (order.Side == "Buy")
                                    lastFilledLowerOrder = order;
                                else if (order.Side == "Sell")
                                    lastFilledUpperOrder = order;
                                break;
                            }
                            else if (order.Text.Contains("<BOT>CLOSE-LIMIT<BOT>"))
                            {
                                if (order.Side == "Buy")
                                    lastUpperClosed = true;
                                else if (order.Side == "Sell")
                                    lastLowerClosed = true;
                            }
                        }
                        position = apiHelper.GetPosition();
                        decimal positionEntryPrice = 0;
                        int positionQty = 0;
                        if (position == null || position.CurrentQty.Value == 0)
                        {
                            if (config.Exit > 0)
                            {
                                if (activeOrders.Count > 0) apiHelper.CancelAllOrders();
                                Logger.WriteLine($"No order. (exit = {config.Exit})", ConsoleColor.DarkGray);
                                Logger.WriteWait("", 60);
                                continue;
                            }
                            int qtyStep = (int)((double)(margin.WalletBalance.Value * lastPrice) * config.QtyRatio / 1000000);
                            lowerQtyStep = (int)(config.LowerQtyX * qtyStep);
                            upperQtyStep = (int)(config.UpperQtyX * qtyStep);
                        }
                        else
                        {
                            positionEntryPrice = position.AvgEntryPrice.Value;
                            positionQty = position.CurrentQty.Value;
                            int qtyStep = (int)(positionQty / Math.Round(positionQty / ((double)(margin.WalletBalance.Value * lastPrice) * config.QtyRatio / 1000000)));
                            if (lowerQtyStep == 0) lowerQtyStep = (int)(config.LowerQtyX * qtyStep);
                            if (upperQtyStep == 0) upperQtyStep = (int)(config.UpperQtyX * qtyStep);
                            if (positionQty < 0)
                            {
                                upperPositionQtySize = (int)Math.Round((double)Math.Abs(positionQty) / lowerQtyStep);
                                if (upperPositionQtySize < 1) upperPositionQtySize = 1;
                                if (config.Exponential && lastFilledUpperOrder != null)
                                {
                                    upperHold = (int)(lastFilledUpperOrder.AvgPx.Value + (decimal)Math.Max(Math.Log(upperPositionQtySize, 2), 1) * lastPrice * config.MinOrderDistanceRatio);
                                }
                                else
                                {
                                    upperHold = (int)(positionEntryPrice + upperPositionQtySize * lastPrice * config.MinOrderDistanceRatio);
                                }
                                //if (lastFilledUpperOrder == null)
                                //{
                                //    upperHold = (int)(positionEntryPrice + upperPositionQtySize * config.MinOrderDistance);
                                //    Logger.WriteLine("warning! position_qty < 0, but lastFilledUpperOrder = null", ConsoleColor.Red);
                                //}
                                //else
                                //{
                                //    upperHold = (int)(lastFilledUpperOrder.AvgPx.Value + (decimal)((Math.Log(upperPositionQtySize, 2) + 1) * config.MinOrderDistance));
                                //    Logger.WriteLine($"UH = {lastFilledUpperOrder.AvgPx} + {Math.Log(upperPositionQtySize, 2) + 1:F2} * {config.MinOrderDistance}", ConsoleColor.DarkYellow);
                                //}
                                if (upperHold - middleBand1 < lastPrice * config.MinUpperHeightRatio) upperHold = (int)(Math.Ceiling(middleBand1) + lastPrice * config.MinUpperHeightRatio);
                            }
                            else if (positionQty > 0)
                            {
                                lowerPositionQtySize = (int)Math.Round((double)Math.Abs(positionQty) / upperQtyStep);
                                if (lowerPositionQtySize < 1) lowerPositionQtySize = 1;
                                if (config.Exponential && lastFilledLowerOrder != null)
                                {
                                    lowerHold = (int)(lastFilledLowerOrder.AvgPx.Value - (decimal)Math.Max(Math.Log(upperPositionQtySize, 2), 1) * lastPrice * config.MinOrderDistanceRatio);
                                }
                                else
                                {
                                    lowerHold = (int)(positionEntryPrice - lowerPositionQtySize * lastPrice * config.MinOrderDistanceRatio);
                                }
                                //if (lastFilledLowerOrder == null)
                                //{
                                //    lowerHold = (int)(positionEntryPrice - lowerPositionQtySize * config.MinOrderDistance);
                                //    Logger.WriteLine("warning! position_qty > 0, but lastFilledLowerOrder = null", ConsoleColor.Red);
                                //}
                                //else
                                //{
                                //    lowerHold = (int)(lastFilledLowerOrder.AvgPx.Value - (decimal)((Math.Log(upperPositionQtySize, 2) + 1) * config.MinOrderDistance));
                                //    Logger.WriteLine($"LH = {lastFilledLowerOrder.AvgPx} - {Math.Log(upperPositionQtySize, 2) + 1:F2} * {config.MinOrderDistance}", ConsoleColor.DarkYellow);
                                //}
                                if (middleBand1 - lowerHold < lastPrice * config.MinLowerHeightRatio) lowerHold = (int)(Math.Floor(middleBand1) - lastPrice * config.MinLowerHeightRatio);
                            }
                        }
                        {
                            string printLine1 = $"        BB = {standardDeviation1 * 2:F1} / {middleBand1:F1} / {upperBand1:F1} / {lowerBand1:F1}    _BB = {lastMiddleBand:F1} / {lastUpperBand:F1} / {lastLowerBand:F1}    rsi = {currentRSI:F4}  /  {lastRSI:F4}";
                            string printLine2 = $"        QS = {upperPositionQtySize} / {lowerPositionQtySize}    HOLD = {upperHold} / {lowerHold}    FILLED = {(lastFilledUpperOrder == null ? 0 : lastFilledUpperOrder.AvgPx)} / {(lastFilledLowerOrder == null ? 0 : lastFilledLowerOrder.AvgPx)}    CLOSED = {(lastUpperClosed ? 1 : 0)} / {(lastLowerClosed ? 1 : 0)}";
                            Logger.WriteLine(printLine1, ConsoleColor.DarkGray);
                            Logger.WriteLine(printLine2, ConsoleColor.DarkGray);
                        }
                        {
                            double upperX = 0, lowerX = 0;
                            if (currentRSI > config.RSIUpper)
                                upperX = (currentRSI - config.RSIUpper) / 20d + 2;
                            else if (currentRSI < config.RSILower)
                                lowerX = (config.RSILower - currentRSI) / 20d + 2;
                            //if (binList[1].High.Value > lastMiddleBand + (decimal)(lastStandardDeviation * 4))
                            //    upperX = 6;
                            //else if (binList[1].High.Value > lastMiddleBand + (decimal)(lastStandardDeviation * 3))
                            //    upperX = 5;
                            //else 
                            //if (binList[1].High.Value > lastMiddleBand + (decimal)(lastStandardDeviation * 2.5))
                            //    upperX = 4;
                            //else if (binList[1].High.Value > lastUpperBand)
                            //    upperX = 3.5;
                            //if (binList[1].Low.Value < lastMiddleBand - (decimal)(lastStandardDeviation * 4))
                            //    lowerX = 6;
                            //else if (binList[1].Low.Value < lastMiddleBand - (decimal)(lastStandardDeviation * 3))
                            //    lowerX = 5;
                            //else
                            //if (binList[1].Low.Value < lastMiddleBand - (decimal)(lastStandardDeviation * 2.5))
                            //    lowerX = 4;
                            //else if (binList[1].Low.Value < lastLowerBand)
                            //    lowerX = 3.5;
                            if (timeMap != null)
                            {
                                DateTime t = binList[0].Timestamp.Value;
                                foreach (string line in timeMap)
                                {
                                    string[] t3 = line.Trim().Split('/');
                                    string[] t2 = t3[0].Trim().Split(':');
                                    int h = int.Parse(t2[0]);
                                    int m = int.Parse(t2[1]);
                                    int x = int.Parse(t3[1]);
                                    if (t.Hour == h && t.Minute == m)
                                    {
                                        upperBand1 = Math.Max(upperBand1, middleBand1 + Math.Max((decimal)(standardDeviation1 * x), lastPrice * config.MinUpperHeightRatio));
                                        lowerBand1 = Math.Min(lowerBand1, middleBand1 - Math.Max((decimal)(standardDeviation1 * x), lastPrice * config.MinLowerHeightRatio));
                                        Logger.WriteLine($"    special_time = {h:D2}:{m:D2}, x = {x}", ConsoleColor.DarkYellow);
                                        break;
                                    }
                                }
                            }
                            if (positionQty < 0)
                            {
                                upperX = Math.Max(upperX, (Math.Ceiling(Math.Log(upperPositionQtySize, 2)) - 1) / 2d + 2);
                            }
                            else if (positionQty > 0)
                            {
                                lowerX = Math.Max(lowerX, (Math.Ceiling(Math.Log(upperPositionQtySize, 2)) - 1) / 2d + 2);
                            }
                            if (upperX >= upperX1)
                            {
                                upperBand1 = Math.Max(upperBand1, middleBand1 + Math.Max((decimal)(standardDeviation1 * upperX), lastPrice * config.MinUpperHeightRatio));
                                Logger.WriteLine($"    upper_band was increased by x = {upperX:F2}", ConsoleColor.DarkYellow);
                            }
                            if (lowerX >= lowerX1)
                            {
                                lowerBand1 = Math.Min(lowerBand1, middleBand1 - Math.Max((decimal)(standardDeviation1 * lowerX), lastPrice * config.MinLowerHeightRatio));
                                Logger.WriteLine($"    lower_band was increased by x = {lowerX:F2}", ConsoleColor.DarkYellow);
                            }

                        }
                        List<Order> newOrderList = new List<Order>();
                        if (positionQty == 0)
                        {
                            if (config.BuyOrSell != 2 && lowerBand1 < binList[0].Low.Value && middleBand1 - lowerBand1 >= lastPrice * config.MinLowerHeightRatio)
                            {
                                int price = (int)Math.Floor(lowerBand1);
                                int qty = BitMEXApiHelper.FixQty(lowerQtyStep);
                                newOrderList.Add(new Order
                                {
                                    Symbol = BitMEXApiHelper.SYMBOL,
                                    OrdType = "Limit",
                                    Side = "Buy",
                                    OrderQty = qty,
                                    Price = price,
                                    Text = $"<BOT>BAND-LIMIT<BOT>"
                                });
                            }
                            if (config.BuyOrSell != 1 && upperBand1 > binList[0].High.Value && upperBand1 - middleBand1 >= lastPrice * config.MinUpperHeightRatio)
                            {
                                int price = (int)Math.Ceiling(upperBand1);
                                int qty = BitMEXApiHelper.FixQty(upperQtyStep);
                                newOrderList.Add(new Order
                                {
                                    Symbol = BitMEXApiHelper.SYMBOL,
                                    OrdType = "Limit",
                                    Side = "Sell",
                                    OrderQty = qty,
                                    Price = price,
                                    Text = $"<BOT>BAND-LIMIT<BOT>"
                                });
                            }
                        }
                        else if (positionQty < 0)
                        {
                            int price;
                            int qty;
                            decimal upperCloseHeight;
                            if (config.Exponential)
                            {
                                upperCloseHeight = lastPrice * config.MinUpperCloseHeightRatio * (decimal)(1 + Math.Ceiling(Math.Log(upperPositionQtySize, 2)) / 4d);
                                if (lastFilledUpperOrder != null && !lastUpperClosed && config.StopQtyX > 0 && upperPositionQtySize >= config.StopQtyX)
                                {
                                    price = (int)Math.Floor(lastFilledUpperOrder.AvgPx.Value - lastPrice * config.MinUpperCloseHeightRatio);
                                    qty = Math.Abs(positionQty) >> 1;
                                }
                                else
                                {
                                    price = (int)Math.Floor(positionEntryPrice - upperCloseHeight);
                                    qty = Math.Abs(positionQty);
                                }
                            }
                            else
                            {
                                upperCloseHeight = lastPrice * config.MinUpperCloseHeightRatio * (1 + Math.Max((upperPositionQtySize - config.RaiseQtyX) / 4m, 0));
                                if (lastFilledUpperOrder != null && !lastUpperClosed && config.StopQtyX > 0 && upperPositionQtySize >= config.StopQtyX)
                                {
                                    price = (int)Math.Floor(lastFilledUpperOrder.AvgPx.Value - lastPrice * config.MinUpperCloseHeightRatio);
                                    qty = lowerQtyStep;
                                }
                                else
                                {
                                    price = (int)Math.Floor(positionEntryPrice - upperCloseHeight);
                                    qty = Math.Abs(positionQty);
                                }
                            }
                            //if (Math.Abs(positionQty) - qty == 0 && price > positionEntryPrice - config.MinUpperCloseHeight && price <= positionEntryPrice + config.MinOrderDistance) qty = 0;
                            if (qty > 0)
                                newOrderList.Add(new Order
                                {
                                    Symbol = BitMEXApiHelper.SYMBOL,
                                    OrdType = "Limit",
                                    Side = "Buy",
                                    OrderQty = BitMEXApiHelper.FixQty(qty),
                                    Price = price,
                                    ExecInst = "ReduceOnly",
                                    Text = $"<BOT>CLOSE-LIMIT<BOT>"
                                });
                            if (Math.Abs(positionQty) - qty > 0)
                            {
                                if (positionEntryPrice - lowerBand1 > upperCloseHeight) price = (int)Math.Floor(lowerBand1);
                                else if (positionEntryPrice - lowerBand2 > upperCloseHeight) price = (int)Math.Floor(lowerBand2);
                                else price = (int)Math.Floor(Math.Min(lowerBand1, positionEntryPrice - upperCloseHeight));
                                qty = BitMEXApiHelper.FixQty(Math.Abs(positionQty) - qty);
                                newOrderList.Add(new Order
                                {
                                    Symbol = BitMEXApiHelper.SYMBOL,
                                    OrdType = "Limit",
                                    Side = "Buy",
                                    OrderQty = qty,
                                    Price = price,
                                    ExecInst = "ReduceOnly",
                                    Text = $"<BOT>BAND-LIMIT<BOT>"
                                });
                            }
                            if (upperPositionQtySize < config.MaxQtyX && (lastFilledUpperOrder == null || (BitMEXApiHelper.ServerTime.Value - lastFilledUpperOrder.Timestamp.Value).TotalSeconds > 60) && upperBand1 - middleBand1 >= lastPrice * config.MinUpperHeightRatio)
                            {
                                if (upperHold > upperBand1) upperBand1 = middleBand1 + (decimal)(standardDeviation1 * 6);
                                price = (int)Math.Ceiling(Math.Max(upperHold, upperBand1));
                                qty = BitMEXApiHelper.FixQty(config.Exponential ? Math.Max(Math.Abs(positionQty) - upperQtyStep, upperQtyStep) : upperQtyStep);
                                newOrderList.Add(new Order
                                {
                                    Symbol = BitMEXApiHelper.SYMBOL,
                                    OrdType = "Limit",
                                    Side = "Sell",
                                    OrderQty = qty,
                                    Price = price,
                                    Text = $"<BOT>BAND-LIMIT<BOT>"
                                });
                            }
                        }
                        else if (positionQty > 0)
                        {
                            int price;
                            int qty;
                            decimal lowerCloseHeight;
                            if (config.Exponential)
                            {
                                lowerCloseHeight = lastPrice * config.MinLowerCloseHeightRatio * (decimal)(1 + Math.Ceiling(Math.Log(lowerPositionQtySize, 2)) / 4d);
                                if (lastFilledLowerOrder != null && !lastLowerClosed && config.StopQtyX > 0 && lowerPositionQtySize >= config.StopQtyX)
                                {
                                    price = (int)Math.Ceiling(lastFilledLowerOrder.AvgPx.Value + lastPrice * config.MinLowerCloseHeightRatio);
                                    qty = Math.Abs(positionQty) >> 1;
                                }
                                else
                                {
                                    price = (int)Math.Ceiling(positionEntryPrice + lowerCloseHeight);
                                    qty = Math.Abs(positionQty);
                                }
                            }
                            else
                            {
                                lowerCloseHeight = lastPrice * config.MinLowerCloseHeightRatio * (1 + Math.Max((lowerPositionQtySize - config.RaiseQtyX) / 4m, 0));
                                if (lastFilledLowerOrder != null && !lastLowerClosed && config.StopQtyX > 0 && lowerPositionQtySize >= config.StopQtyX)
                                {
                                    price = (int)Math.Ceiling(lastFilledLowerOrder.AvgPx.Value + lastPrice * config.MinLowerCloseHeightRatio);
                                    qty = upperQtyStep;
                                }
                                else
                                {
                                    price = (int)Math.Ceiling(positionEntryPrice + lowerCloseHeight);
                                    qty = Math.Abs(positionQty);
                                }
                            }
                            //if (Math.Abs(positionQty) - qty == 0 && price < positionEntryPrice + config.MinLowerCloseHeight && price >= positionEntryPrice - config.MinOrderDistance) qty = 0;
                            if (qty > 0)
                                newOrderList.Add(new Order
                                {
                                    Symbol = BitMEXApiHelper.SYMBOL,
                                    OrdType = "Limit",
                                    Side = "Sell",
                                    OrderQty = BitMEXApiHelper.FixQty(qty),
                                    Price = price,
                                    ExecInst = "ReduceOnly",
                                    Text = $"<BOT>CLOSE-LIMIT<BOT>"
                                });
                            if (Math.Abs(positionQty) - qty > 0)
                            {
                                if (upperBand1 - positionEntryPrice > lowerCloseHeight) price = (int)Math.Ceiling(upperBand1);
                                else if (upperBand2 - positionEntryPrice > lowerCloseHeight) price = (int)Math.Ceiling(upperBand2);
                                else price = (int)Math.Ceiling(Math.Max(upperBand1, positionEntryPrice + lowerCloseHeight));
                                qty = BitMEXApiHelper.FixQty(Math.Abs(positionQty) - qty);
                                newOrderList.Add(new Order
                                {
                                    Symbol = BitMEXApiHelper.SYMBOL,
                                    OrdType = "Limit",
                                    Side = "Sell",
                                    OrderQty = qty,
                                    Price = price,
                                    ExecInst = "ReduceOnly",
                                    Text = $"<BOT>BAND-LIMIT<BOT>"
                                });
                            }
                            if (lowerPositionQtySize < config.MaxQtyX && (lastFilledLowerOrder == null || (BitMEXApiHelper.ServerTime.Value - lastFilledLowerOrder.Timestamp.Value).TotalSeconds > 60) && middleBand1 - lowerBand1 >= lastPrice * config.MinLowerHeightRatio)
                            {
                                if (lowerHold < lowerBand1) lowerBand1 = middleBand1 - (decimal)(standardDeviation1 * 6);
                                price = (int)Math.Floor(Math.Min(lowerHold, lowerBand1));
                                qty = BitMEXApiHelper.FixQty(config.Exponential ? Math.Max(Math.Abs(positionQty) - lowerQtyStep, lowerQtyStep) : lowerQtyStep);
                                newOrderList.Add(new Order
                                {
                                    Symbol = BitMEXApiHelper.SYMBOL,
                                    OrdType = "Limit",
                                    Side = "Buy",
                                    OrderQty = qty,
                                    Price = price,
                                    Text = $"<BOT>BAND-LIMIT<BOT>"
                                });
                            }
                        }
                        else
                        {
                            throw new Exception($"strange error : position_qty = {positionQty}");
                        }
                        List<string> cancelOrderList = new List<string>();
                        List<Order> amendOrderList = new List<Order>();
                        int duplicatedOrderCount = 0, amendOrderCount = 0, newOrderCount = 0;
                        foreach (Order oldOrder in activeOrders)
                        {
                            if (oldOrder.Text.Contains("<BOT>"))
                            {
                                foreach (Order newOrder in newOrderList)
                                {
                                    if (oldOrder.Symbol == newOrder.Symbol && oldOrder.OrdType == newOrder.OrdType && oldOrder.Side == newOrder.Side)
                                    {
                                        newOrderList.Remove(newOrder);
                                        if (oldOrder.OrderQty == newOrder.OrderQty && oldOrder.Price == newOrder.Price/*  && (string.IsNullOrEmpty(oldOrder.ExecInst) && string.IsNullOrEmpty(newOrder.ExecInst) || oldOrder.ExecInst == newOrder.ExecInst) && oldOrder.Text == newOrder.Text*/)
                                        {
                                            duplicatedOrderCount++;
                                        }
                                        else
                                        {
                                            newOrder.OrderID = oldOrder.OrderID;
                                            amendOrderList.Add(newOrder);
                                        }
                                        goto loopActiveOrders;
                                    }
                                }
                                cancelOrderList.Add(oldOrder.OrderID);
                            }
                        loopActiveOrders:;
                        }
                        //retryOrder = true;
                        int cancelOrderCount = cancelOrderList.Count;
                        if (cancelOrderCount > 0) apiHelper.CancelOrders(cancelOrderList);
                        position = apiHelper.GetPosition();
                        if (position == null || position.AvgEntryPrice == null || position.CurrentQty.Value == positionQty && position.AvgEntryPrice.Value == positionEntryPrice)
                        {
                            if (amendOrderList.Count > 0)
                            {
                                List<Order> resultOrderList = new List<Order>();
                                foreach (Order order in amendOrderList)
                                {
                                    resultOrderList.Add(apiHelper.OrderAmend(order));
                                }
                                amendOrderCount = resultOrderList.Count;
                                Logger.WriteFile(JArray.FromObject(resultOrderList).ToString(Formatting.None));
                            }
                            if (newOrderList.Count > 0)
                            {
                                List<Order> resultOrderList = new List<Order>();
                                foreach (Order order in newOrderList)
                                {
                                    resultOrderList.Add(apiHelper.OrderNew(order.Side, order.OrderQty, order.Price, null, order.OrdType, null, order.Text));
                                }
                                newOrderCount = resultOrderList.Count;
                                Logger.WriteFile(JArray.FromObject(resultOrderList).ToString(Formatting.None));
                            }
                            if (cancelOrderCount + newOrderCount + amendOrderCount > 0)
                                Logger.WriteLine($"    canceled = {cancelOrderCount}, amended = {amendOrderCount}, created = {newOrderCount}, final = {newOrderCount + amendOrderCount + duplicatedOrderCount} orders in all.");
                            //retryOrder = false;
                        }
                        else
                        {
                            Logger.WriteLine($"    position has changed. (old_qty = {positionQty}, now_qty = {position.CurrentQty.Value}, old_entry = {positionEntryPrice}, new_entry = {position.AvgEntryPrice.Value}");
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
                if (BollingerBandConfig.Warning != null) Logger.WriteLine("<Warning>  " + BollingerBandConfig.Warning, ConsoleColor.Green);
            }
        }
    }
}