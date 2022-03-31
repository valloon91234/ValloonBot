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
    public class RSIStrategy
    {
        private class ParamConfig
        {
            public int BuyOrSell { get; set; }
            public int BinSize { get; set; }
            public int WindowBuy { get; set; }
            public double maxDiffBuy { get; set; }
            public double NeutralBuy { get; set; }
            public double OverBuy { get; set; }
            public int WindowSell { get; set; }
            public double maxDiffSell { get; set; }
            public double NeutralSell { get; set; }
            public double OverSell { get; set; }
        }

        public void Run(Config config = null)
        {
            int loopIndex = 0;
            DateTime? lastLoopTime = null;
            decimal lastWalletBalance = 0;
            ParamConfig param = null;
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
                    }
                    string symbol = config.Symbol.ToUpper();
                    if (param == null || BitMEXApiHelper.ServerTime.Minute % param.BinSize == 0 && BitMEXApiHelper.ServerTime.Second < 30)
                    {
                        string url = $"https://raw.githubusercontent.com/maksimg1002/_upload1002/main/sol_0322.txt";
                        string paramText = HttpClient2.HttpGet(url);
                        param = JsonConvert.DeserializeObject<ParamConfig>(paramText);
                        logger.WriteLine($"\r\n[{BitMEXApiHelper.ServerTime:yyyy-MM-dd  HH:mm:ss}]  ParamMap loaded.", ConsoleColor.Green);
                        logger.WriteLine(JObject.FromObject(param).ToString(Formatting.Indented));
                        logger.WriteLine();
                    }
                    else if (configUpdated)
                    {
                        logger.WriteLine();
                    }
                    Margin margin = apiHelper.GetMargin(BitMEXApiHelper.CURRENCY_XBt);
                    decimal walletBalance = margin.WalletBalance.Value / 100000000m;
                    List<Order> activeOrderList = apiHelper.GetActiveOrders(symbol);
                    List<TradeBin> binList = apiHelper.GetBinList("5m", false, symbol, 1000, null, true);
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
                    int orderQty = BitMEXApiHelper.FixQty((int)(margin.WalletBalance.Value * config.Leverage / 10000));
                    Position position = apiHelper.GetPosition(symbol);
                    decimal positionEntryPrice = 0;
                    int positionQty = 0;
                    {
                        string consoleTitle = $"$ {lastPrice:N2}  /  {walletBalance:N8} XBT  /  {orderQty:N0} Cont";
                        if (position != null && position.CurrentQty.Value != 0) consoleTitle += "  /  1 Position";
                        consoleTitle += $"    <{config.Username}>  |   {Config.APP_NAME}  v{fileModifiedTime:yyyy.MM.dd HH:mm:ss}   by  ValloonTrader.com";
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

                    List<TradeBin> reversedBinList = new List<TradeBin>(binList);
                    reversedBinList.Reverse();
                    List<TradeBin> list5 = BitMEXApiHelper.LoadBinListFrom5m($"{param.BinSize}m", reversedBinList);
                    var reversedBinArray = list5.ToArray();
                    double[] rsiBuyArray = RSI.CalculateRSIValues(reversedBinArray, param.WindowBuy);
                    double[] rsiSellArray = RSI.CalculateRSIValues(reversedBinArray, param.WindowSell);
                    logger.WriteLine($"        rsiBuy ({param.WindowBuy}) = {rsiBuyArray[rsiBuyArray.Length - 3]}  /  {rsiBuyArray[rsiBuyArray.Length - 2]}  /  {rsiBuyArray[rsiBuyArray.Length - 1]}");
                    logger.WriteLine($"        rsiSell ({param.WindowSell}) = {rsiSellArray[rsiSellArray.Length - 3]}  /  {rsiSellArray[rsiSellArray.Length - 2]}  /  {rsiSellArray[rsiSellArray.Length - 1]}");

                    bool buySignal = false, sellSignal = false, buyCloseSignal = false, sellCloseSignal = false;
                    if (BitMEXApiHelper.ServerTime.Minute % param.BinSize == 0)
                    {
                        buySignal = rsiBuyArray[rsiBuyArray.Length - 2] - rsiBuyArray[rsiBuyArray.Length - 3] < param.maxDiffBuy && rsiBuyArray[rsiBuyArray.Length - 3] < param.NeutralBuy && rsiBuyArray[rsiBuyArray.Length - 2] >= param.NeutralBuy;
                        sellSignal = rsiSellArray[rsiSellArray.Length - 3] - rsiSellArray[rsiSellArray.Length - 2] < param.maxDiffSell && rsiSellArray[rsiSellArray.Length - 3] >= param.NeutralSell && rsiSellArray[rsiSellArray.Length - 2] < param.NeutralSell;
                        buyCloseSignal = rsiBuyArray[rsiBuyArray.Length - 3] > param.NeutralBuy && rsiBuyArray[rsiBuyArray.Length - 2] < param.NeutralBuy || rsiBuyArray[rsiBuyArray.Length - 3] > param.OverBuy && rsiBuyArray[rsiBuyArray.Length - 2] < param.OverBuy;
                        sellCloseSignal = rsiSellArray[rsiSellArray.Length - 3] < param.NeutralSell && rsiSellArray[rsiSellArray.Length - 2] > param.NeutralSell || rsiSellArray[rsiSellArray.Length - 3] < param.OverSell && rsiSellArray[rsiSellArray.Length - 2] > param.OverSell;
                        if (buySignal) logger.WriteLine($"buySignal = {buySignal}", ConsoleColor.Green);
                        if (sellSignal) logger.WriteLine($"sellSignal = {sellSignal}", ConsoleColor.Green);
                        if (buyCloseSignal) logger.WriteLine($"buyCloseSignal = {buyCloseSignal}", ConsoleColor.Green);
                        if (sellCloseSignal) logger.WriteLine($"sellCloseSignal = {sellCloseSignal}", ConsoleColor.Green);
                    }

                    if (config.Exit == 1)
                    {
                        logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  No order. (exit = {config.Exit})", ConsoleColor.DarkGray);
                        goto endLoop;
                    }
                    if (positionQty == 0)
                    {
                        if (config.Leverage == 0)
                        {
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  No order. (qty = {config.Leverage})", ConsoleColor.DarkGray);
                            goto endLoop;
                        }
#if LICENSE_MODE
                        if (serverTime.Year != 2022 || serverTime.Month != 2)
                        {
                            logger.WriteLine($"This bot is too old. Please contact support.  https://valloontrader.com", ConsoleColor.Green);
                            goto endLoop;
                        }
#endif
                        if (config.Exit == 2)
                        {
                        }
                        else if ((param.BuyOrSell == 1 || param.BuyOrSell == 3) && buySignal)
                        {
                            Order newOrder = apiHelper.OrderNew(new Order
                            {
                                Symbol = symbol,
                                Side = "Buy",
                                OrderQty = orderQty,
                                OrdType = "Market",
                                Text = $"<BOT><OPEN></BOT>",
                            });
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New market buy order has been created: qty = {orderQty}");
                            logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                            positionQty = orderQty;
                        }
                        else if ((param.BuyOrSell == 2 || param.BuyOrSell == 3) && sellSignal)
                        {
                            Order newOrder = apiHelper.OrderNew(new Order
                            {
                                Symbol = symbol,
                                Side = "Sell",
                                OrderQty = orderQty,
                                OrdType = "Market",
                                Text = $"<BOT><OPEN></BOT>",
                            });
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New market sell order has been created: qty = {orderQty}");
                            logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                            positionQty = -orderQty;
                        }
                    }
                    else if (positionQty > 0)
                    {
                        if (buyCloseSignal)
                        {
                            Order newOrder = apiHelper.OrderNew(new Order
                            {
                                Symbol = symbol,
                                Side = "Sell",
                                OrderQty = positionQty,
                                OrdType = "Market",
                                ExecInst = "ReduceOnly",
                                Text = $"<BOT><CLOSE></BOT>",
                            });
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New market close sell order has been created: qty = {positionQty}");
                            logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                            positionQty = 0;
                        }
                    }
                    else if (positionQty < 0)
                    {
                        if (sellCloseSignal)
                        {
                            Order newOrder = apiHelper.OrderNew(new Order
                            {
                                Symbol = symbol,
                                Side = "Buy",
                                OrderQty = -positionQty,
                                OrdType = "Market",
                                ExecInst = "ReduceOnly",
                                Text = $"<BOT><CLOSE></BOT>",
                            });
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New market close buy order has been created: qty = {positionQty}");
                            logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                            positionQty = 0;
                        }
                    }
                    else
                    {
                        logger.WriteLine($"        Unknown position exists: qty = {positionQty}");
                    }

                    if (BitMEXApiHelper.ServerTime.Minute % param.BinSize == 0 && BitMEXApiHelper.ServerTime.Second < 30)
                    {
                        List<string> cancelOrderList = new List<string>();
                        foreach (Order order in botOrderList)
                        {
                            if (order.Text.Contains("<POST-ONLY>"))
                                cancelOrderList.Add(order.OrderID);
                        }
                        if (cancelOrderList.Count > 0)
                        {
                            apiHelper.CancelOrders(cancelOrderList);
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  {cancelOrderList.Count} POST-ONLY orders have been canceled.", ConsoleColor.DarkGray);
                        }
                        if (positionQty <= 0)
                        {
                            decimal price = BitMEXApiHelper.ServerTime.Hour + BitMEXApiHelper.ServerTime.Minute / 100m;
                            if (price == 0) price = 24;
                            Order newOrder = apiHelper.OrderNew(new Order
                            {
                                Symbol = symbol,
                                Side = "Buy",
                                OrderQty = 1,
                                Price = price,
                                OrdType = "Limit",
                                ExecInst = "ParticipateDoNotInitiate",
                                Text = $"<BOT><POST-ONLY></BOT>",
                            });
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New post-only buy order has been created: qty = {1}, price = {price}", ConsoleColor.DarkGray);
                        }
                        else
                        {
                            decimal price = BitMEXApiHelper.ServerTime.Hour + BitMEXApiHelper.ServerTime.Minute / 100m + 999000;
                            if (price == 0) price = 24;
                            Order newOrder = apiHelper.OrderNew(new Order
                            {
                                Symbol = symbol,
                                Side = "Sell",
                                OrderQty = 1,
                                Price = price,
                                OrdType = "Limit",
                                ExecInst = "ParticipateDoNotInitiate",
                                Text = $"<BOT><POST-ONLY></BOT>",
                            });
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New post-only sell order has been created: qty = {1}, price = {price}", ConsoleColor.DarkGray);
                        }
                    }

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
                            if (positionQty != 0) suffix = $"        position = {positionQty}";
                            logger2.WriteFile($"[{BitMEXApiHelper.ServerTime:yyyy-MM-dd  HH:mm:ss}  ({++loopIndex:D6})]    {walletBalance:F8}    {walletBalance - lastWalletBalance:F8}    price = {lastPrice:F1}  /  {markPrice:F3}        balance = {(walletBalance - lastWalletBalance) / lastWalletBalance * 100:N4}%{suffix}");
                        }
                        lastWalletBalance = walletBalance;
                    }

                    int waitMilliseconds = (int)(binList[0].Timestamp.Value - BitMEXApiHelper.ServerTime).TotalMilliseconds;
                    if (waitMilliseconds >= 0)
                    {
                        int waitSeconds = (waitMilliseconds / 1000) % 30;
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