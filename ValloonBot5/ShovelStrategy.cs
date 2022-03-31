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
using Valloon.Utils;

/**
 * @author Valloon Present
 * @version 2022-02-10
 */
namespace Valloon.Trading
{
    public class ShovelStrategy
    {
        private class ShovelConfig
        {
            public decimal QtyX { get; set; }
            public int SMALength { get; set; }
            public int DelayLength { get; set; }
            public decimal LimitX { get; set; }
            public decimal CloseX { get; set; }
        }

        public void Run(Config config = null)
        {
            const string SYMBOL = BitMEXApiHelper.SYMBOL_XBTUSD;
            int loopIndex = 0;
            DateTime? lastLoopTime = null;
            TradeBin lastCandle = null;
            decimal lastWalletBalance = 0;
            ShovelConfig shovel = null;
            DateTime? lastParamMapTime = null;
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
                    if (shovel == null || lastParamMapTime == null || lastParamMapTime.Value.Hour != BitMEXApiHelper.ServerTime.Hour || (BitMEXApiHelper.ServerTime - lastParamMapTime.Value).TotalMinutes > 30)
                    {
                        string url = $"https://raw.githubusercontent.com/maksimg1002/_upload1002/main/shovel.txt";
                        string paramText = HttpClient2.HttpGet(url);
                        shovel = JsonConvert.DeserializeObject<ShovelConfig>(paramText);
                        logger.WriteLine($"\r\n[{BitMEXApiHelper.ServerTime:yyyy-MM-dd  HH:mm:ss}]  ParamMap loaded.", ConsoleColor.Green);
                        logger.WriteLine(JObject.FromObject(shovel).ToString(Formatting.Indented));
                        logger.WriteLine();
                        lastParamMapTime = BitMEXApiHelper.ServerTime;
                    }
                    Margin margin = apiHelper.GetMargin(BitMEXApiHelper.CURRENCY_XBt);
                    decimal walletBalance = margin.WalletBalance.Value / 100000000m;
                    List<Order> activeOrderList = apiHelper.GetActiveOrders(SYMBOL);
                    List<TradeBin> binList = apiHelper.GetBinList("1h", false, SYMBOL, 1000, null, true);
                    while (shovel.SMALength + shovel.DelayLength > binList.Count)
                        binList.AddRange(apiHelper.GetBinList("1h", false, SYMBOL, 1000, null, true, null, binList[binList.Count - 1].Timestamp.Value.AddHours(-1)));
                    decimal sma;
                    {
                        int candleCount = shovel.SMALength;
                        decimal[] closeArray = new decimal[candleCount];
                        for (int i = 0; i < candleCount; i++)
                        {
                            decimal? close = binList[candleCount - 1 - i + shovel.DelayLength].Close;
                            if (close == null)
                            {
                                close = binList[candleCount - i].Close;
                                binList[candleCount - 1 - i].Close = close;
                            }
                            closeArray[i] = close.Value;
                        }
                        sma = closeArray.Average();
                    }

                    Instrument instrument = apiHelper.GetInstrument(SYMBOL);
                    decimal lastPrice = instrument.LastPrice.Value;
                    decimal markPrice = instrument.MarkPrice.Value;
                    List<Order> botOrderList = new List<Order>();
                    foreach (Order order in activeOrderList)
                        if (order.Text.Contains("<BOT>")) botOrderList.Add(order);
                    {
                        decimal unavailableMarginPercent = 100m * (margin.WalletBalance.Value - margin.AvailableMargin.Value) / margin.WalletBalance.Value;
                        logger.WriteLine($"[{BitMEXApiHelper.ServerTime:yyyy-MM-dd  HH:mm:ss fff}  ({++loopIndex})]    $ {lastPrice:F2}  /  $ {markPrice:F3}  /  {binList[1].Volume:N0}  /  {binList[0].Volume:N0}    {walletBalance:N8} XBT    {botOrderList.Count} / {activeOrderList.Count} / {unavailableMarginPercent:N2} %", ConsoleColor.White);
                    }
                    Position position = apiHelper.GetPosition(SYMBOL);
                    decimal positionEntryPrice = 0;
                    int positionQty = 0;
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

                    int orderQty = BitMEXApiHelper.FixQty((int)(margin.WalletBalance.Value * btcPrice * config.Leverage * shovel.QtyX / 100000000));
                    if (positionQty == 0)
                    {
                        bool exist = false;
#if LICENSE_MODE
                        if (serverTime.Year != 2022 || serverTime.Month != 2)
                        {
                            logger.WriteLine($"This bot is too old. Please contact support.  https://valloontrader.com", ConsoleColor.Green);
                            exist = true;
                        } else 
#endif
                        if (config.Exit > 0)
                        {
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  No order. (exit = {config.Exit})", ConsoleColor.DarkGray);
                            exist = true;
                        }
                        else if (config.Leverage == 0)
                        {
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  No order. (qty = {config.Leverage})", ConsoleColor.DarkGray);
                            exist = true;
                        }
                        if (exist)
                        {
                            List<string> cancelOrderList = new List<string>();
                            foreach (Order order in botOrderList)
                            {
                                cancelOrderList.Add(order.OrderID);
                            }
                            if (cancelOrderList.Count > 0)
                            {
                                int canceledCount = apiHelper.CancelOrders(cancelOrderList).Count;
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  {canceledCount} orders have been canceled.");
                            }
                            Logger.WriteWait("", 60, 5);
                            continue;
                        }
                    }
                    else if (positionQty > 0)
                    {
                        List<string> cancelOrderList = new List<string>();
                        int closeQtySum = 0, manualCloseQtySum = 0;
                        foreach (Order order in activeOrderList)
                        {
                            if (order.Side == "Sell")
                            {
                                closeQtySum += order.OrderQty.Value;
                                if (order.Text.Contains("<BOT>"))
                                    cancelOrderList.Add(order.OrderID);
                                else
                                    manualCloseQtySum += order.OrderQty.Value;
                            }
                        }
                        if (closeQtySum != positionQty && cancelOrderList.Count > 0)
                        {
                            int canceledCount = apiHelper.CancelOrders(cancelOrderList).Count;
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  {canceledCount} close orders have been canceled.");
                        }
                        int closeQty = positionQty - manualCloseQtySum;
                        if (closeQtySum < positionQty && closeQty > 0)
                        {
                            decimal closePrice = (int)Math.Floor(positionEntryPrice * (1 + shovel.CloseX));
                            Order newOrder = apiHelper.OrderNew(new Order
                            {
                                Symbol = SYMBOL,
                                Side = "Sell",
                                OrderQty = closeQty,
                                Price = closePrice,
                                OrdType = "Limit",
                                ExecInst = "Close",
                                //ExecInst = "ReduceOnly",
                                Text = $"<BOT><CLOSE></BOT>",
                            });
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New close order has been created: qty = {orderQty}, sma = {sma:F2}, price = {closePrice}");
                            logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                        }
                    }
                    else
                    {
                        logger.WriteLine($"        Short position exists: qty = {positionQty}");
                    }
                    List<Order> botLimitOrders = new List<Order>();
                    foreach (Order order in botOrderList)
                        if (order.Side == "Buy") botLimitOrders.Add(order);
                    if ((lastCandle == null || lastCandle.Timestamp.Value.Hour != binList[0].Timestamp.Value.Hour || botLimitOrders.Count == 0) && (positionQty == 0 || positionEntryPrice > sma))
                    {
                        List<string> cancelOrderList = new List<string>();
                        foreach (Order order in botLimitOrders)
                            cancelOrderList.Add(order.OrderID);
                        if (cancelOrderList.Count > 0)
                        {
                            int canceledCount = apiHelper.CancelOrders(cancelOrderList).Count;
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  {canceledCount} old limit orders have been canceled.");
                        }
                        decimal limitPrice;
                        int deep = 0;
                        int n = 0;
                        while (true)
                        {
                            if (positionQty == 0)
                                limitPrice = (int)Math.Ceiling(sma - sma * shovel.LimitX * (2 + deep / 2m));
                            else
                                limitPrice = (int)Math.Ceiling(sma - sma * shovel.LimitX * (1 + deep / 2m));
                            if (lastPrice > limitPrice)
                            {
                                Order newOrder = apiHelper.OrderNew(new Order
                                {
                                    Symbol = SYMBOL,
                                    Side = "Buy",
                                    OrderQty = orderQty,
                                    Price = limitPrice,
                                    OrdType = "Limit",
                                    Text = $"<BOT><A={sma}></BOT>",
                                });
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New limit buy order: qty = {orderQty}, price = {limitPrice}, n = {n}, deep = {deep}, sma = {sma:F2}  /  {binList[shovel.DelayLength].Timestamp.Value:yyyy-MM-dd HH:mm:ss}");
                                logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                                break;
                            }
                            deep++;
                        }
                    }
                    lastCandle = binList[0];

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
                            if (positionQty == 0)
                                suffix = $"        profit = {(walletBalance - lastWalletBalance) / lastWalletBalance * 100:N2} %";
                            else
                                suffix = $"        position = {positionQty}";
                            logger2.WriteFile($"[{BitMEXApiHelper.ServerTime:yyyy-MM-dd  HH:mm:ss}  ({++loopIndex:D6})]    {walletBalance:F8}    {walletBalance - lastWalletBalance:F8}    last = {lastPrice:F2}    mark = {markPrice:F3}{suffix}");
                        }
                        lastWalletBalance = walletBalance;
                    }

                    int waitMilliseconds = (int)(binList[0].Timestamp.Value - BitMEXApiHelper.ServerTime).TotalMilliseconds;
                    if (waitMilliseconds >= 0)
                    {
                        int waitSeconds = (waitMilliseconds / 1000) % 60;
                        if (waitSeconds < 1) waitSeconds = 1;
                        Logger.WriteWait($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Sleeping {waitSeconds:N0} seconds ({apiHelper.RequestCount} requests) ", waitSeconds, 1);
                        Thread.Sleep(waitMilliseconds % 1000);
                    }
                    else
                    {
                        logger.WriteLine($"[{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  Error: waitMilliseconds = {waitMilliseconds} < 0", ConsoleColor.Red);
                    }
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