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
    public class RSIStrategy2
    {
        private class ParamConfig
        {
            public int BuyOrSell { get; set; }
            public int BinSize { get; set; }
            public int WindowBuy { get; set; }
            public double MinDiffBuy { get; set; }
            public double MaxDiffBuy { get; set; }
            public double MinValueBuy { get; set; }
            public double MaxValueBuy { get; set; }
            public decimal MinPriceBuy { get; set; }
            public decimal MaxPriceBuy { get; set; }
            public double CloseBuy { get; set; }
            public decimal StopBuy { get; set; }
            public double CloseValueBuy { get; set; }
            public int WindowSell { get; set; }
            public double MinDiffSell { get; set; }
            public double MaxDiffSell { get; set; }
            public double MinValueSell { get; set; }
            public double MaxValueSell { get; set; }
            public decimal MinPriceSell { get; set; }
            public decimal MaxPriceSell { get; set; }
            public double CloseSell { get; set; }
            public decimal StopSell { get; set; }
            public double CloseValueSell { get; set; }
        }

        public void Run(Config config = null)
        {
            int loopIndex = 0;
            DateTime? lastLoopTime = null;
            decimal lastWalletBalance = 0;
            ParamConfig param = null;
            Logger logger = null;
            DateTime fileModifiedTime = File.GetLastWriteTimeUtc(Assembly.GetExecutingAssembly().Location);
            double buyCloseRSI = 0, sellCloseRSI = 0;
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
                        string url = $"https://raw.githubusercontent.com/maksimg1002/_upload1002/main/sol_0328.txt";
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
                    List<TradeBin> binListConverted = BitMEXApiHelper.LoadBinListFrom5m($"{param.BinSize}m", reversedBinList);
                    var reversedBinArray = binListConverted.ToArray();
                    double[] rsiBuyArray = RSI.CalculateRSIValues(reversedBinArray, param.WindowBuy);
                    double[] rsiSellArray = RSI.CalculateRSIValues(reversedBinArray, param.WindowSell);
                    logger.WriteLine($"        rsiBuy ({param.WindowBuy}) = {rsiBuyArray[rsiBuyArray.Length - 3]:F4}  /  {rsiBuyArray[rsiBuyArray.Length - 2]:F4}  /  {rsiBuyArray[rsiBuyArray.Length - 1]:F4} \t\t buyClose = {buyCloseRSI:F4}");
                    logger.WriteLine($"        rsiSell ({param.WindowSell}) = {rsiSellArray[rsiSellArray.Length - 3]:F4}  /  {rsiSellArray[rsiSellArray.Length - 2]:F4}  /  {rsiSellArray[rsiSellArray.Length - 1]:F4} \t\t sellClose = {sellCloseRSI:F4}");

                    bool buySignal = false, sellSignal = false, buyCloseSignal = false, sellCloseSignal = false;
                    if (BitMEXApiHelper.ServerTime.Minute % param.BinSize == 0)
                    {
                        buySignal = rsiBuyArray[rsiBuyArray.Length - 2] - rsiBuyArray[rsiBuyArray.Length - 3] >= param.MinDiffBuy && rsiBuyArray[rsiBuyArray.Length - 2] - rsiBuyArray[rsiBuyArray.Length - 3] < param.MaxDiffBuy && rsiBuyArray[rsiBuyArray.Length - 2] >= param.MinValueBuy && rsiBuyArray[rsiBuyArray.Length - 2] < param.MaxValueBuy;
                        sellSignal = rsiSellArray[rsiSellArray.Length - 3] - rsiSellArray[rsiSellArray.Length - 2] >= param.MinDiffSell && rsiSellArray[rsiSellArray.Length - 3] - rsiSellArray[rsiSellArray.Length - 2] < param.MaxDiffSell && rsiSellArray[rsiSellArray.Length - 2] >= param.MinValueSell && rsiSellArray[rsiSellArray.Length - 2] < param.MaxValueSell;
                        if (buySignal) logger.WriteLine($"buySignal = {buySignal}", ConsoleColor.Green);
                        if (sellSignal) logger.WriteLine($"sellSignal = {sellSignal}", ConsoleColor.Green);
                    }
                    {
                        buyCloseSignal = buyCloseRSI > 0 && rsiBuyArray[rsiBuyArray.Length - 2] >= buyCloseRSI - param.CloseBuy && rsiBuyArray[rsiBuyArray.Length - 1] < buyCloseRSI - param.CloseBuy;
                        sellCloseSignal = sellCloseRSI > 0 && rsiSellArray[rsiSellArray.Length - 2] <= sellCloseRSI + param.CloseSell && rsiSellArray[rsiSellArray.Length - 1] > sellCloseRSI + param.CloseSell;
                        if (buyCloseSignal) logger.WriteLine($"buyCloseSignal = {buyCloseSignal}", ConsoleColor.Green);
                        if (sellCloseSignal) logger.WriteLine($"sellCloseSignal = {sellCloseSignal}", ConsoleColor.Green);
                    }

                    if (config.Exit == 2)
                    {
                        logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  No order. (exit = {config.Exit})", ConsoleColor.DarkGray);
                        goto endLoop;
                    }
                    if (positionQty == 0)
                    {
                        List<string> cancelOrderList = new List<string>();
                        foreach (Order order in botOrderList)
                        {
                            if (order.Text.Contains("<STOP>"))
                                cancelOrderList.Add(order.OrderID);
                        }
                        if (cancelOrderList.Count > 0)
                        {
                            var canceledOrderList = apiHelper.CancelOrders(cancelOrderList);
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  {canceledOrderList.Count} STOP orders have been canceled.", ConsoleColor.DarkGray);
                        }
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
                        buyCloseRSI = 0;
                        sellCloseRSI = 0;
                        if (config.Exit == 1)
                        {
                        }
                        else if ((param.BuyOrSell == 1 || param.BuyOrSell == 3) && buySignal)
                        {
                            if (lastPrice >= param.MinPriceBuy && lastPrice < param.MaxPriceBuy)
                            {
                                {
                                    Order newOrder = apiHelper.OrderNew(new Order
                                    {
                                        Symbol = symbol,
                                        Side = "Buy",
                                        OrderQty = orderQty,
                                        OrdType = "Market",
                                        Text = $"<BOT><OPEN></BOT>",
                                    });
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New MARKET buy order: qty = {orderQty}");
                                    logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                                }
                                {
                                    decimal stopPrice = Math.Round(binListConverted[binListConverted.Count - 1].Open.Value * (1 - param.StopBuy) * 100) / 100m;
                                    Order stopOrder = apiHelper.OrderNew(new Order
                                    {
                                        Symbol = symbol,
                                        Side = "Sell",
                                        OrderQty = orderQty,
                                        StopPx = stopPrice,
                                        OrdType = "Stop",
                                        ExecInst = "ReduceOnly",
                                        Text = $"<BOT><STOP></BOT>",
                                    });
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New STOP close order: qty = {orderQty}, stopX = {param.StopBuy}, price = {stopPrice}");
                                    logger.WriteFile("--- " + JObject.FromObject(stopOrder).ToString(Formatting.None));
                                }
                                positionQty = orderQty;
                                buyCloseRSI = rsiBuyArray[rsiBuyArray.Length - 2];
                            }
                            else
                            {
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  buySignal but lastPrice overflow. min = {param.MinPriceBuy}, max = {param.MaxPriceBuy}");
                            }
                        }
                        else if ((param.BuyOrSell == 2 || param.BuyOrSell == 3) && sellSignal)
                        {
                            if (lastPrice >= param.MinPriceSell && lastPrice < param.MaxPriceSell)
                            {
                                {
                                    Order newOrder = apiHelper.OrderNew(new Order
                                    {
                                        Symbol = symbol,
                                        Side = "Sell",
                                        OrderQty = orderQty,
                                        OrdType = "Market",
                                        Text = $"<BOT><OPEN></BOT>",
                                    });
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New MARKET sell order: qty = {orderQty}");
                                    logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                                }
                                {
                                    decimal stopPrice = Math.Round(binListConverted[binListConverted.Count - 1].Open.Value * (1 + param.StopSell) * 100) / 100m;
                                    Order stopOrder = apiHelper.OrderNew(new Order
                                    {
                                        Symbol = symbol,
                                        Side = "Buy",
                                        OrderQty = orderQty,
                                        StopPx = stopPrice,
                                        OrdType = "Stop",
                                        ExecInst = "ReduceOnly",
                                        Text = $"<BOT><STOP></BOT>",
                                    });
                                    logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New STOP close order: qty = {orderQty}, stopX = {param.StopSell}, price = {stopPrice}");
                                    logger.WriteFile("--- " + JObject.FromObject(stopOrder).ToString(Formatting.None));
                                }
                                positionQty = -orderQty;
                                sellCloseRSI = rsiSellArray[rsiSellArray.Length - 2];
                            }
                            else
                            {
                                logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  sellSignal but lastPrice overflow. min = {param.MinPriceSell}, max = {param.MaxPriceSell}");
                            }
                        }
                    }
                    else if (positionQty > 0)
                    {
                        if (rsiBuyArray[rsiBuyArray.Length - 1] >= param.CloseValueBuy || buyCloseSignal && lastPrice / positionEntryPrice >= 1.05m)
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
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New MARKET sell close order: qty = {positionQty}");
                            logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                            positionQty = 0;
                            buyCloseRSI = 0;
                        }
                        else if (buyCloseRSI < rsiBuyArray[rsiBuyArray.Length - 1])
                        {
                            buyCloseRSI = rsiBuyArray[rsiBuyArray.Length - 1];
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  buyClose = {buyCloseRSI}");
                        }
                    }
                    else if (positionQty < 0)
                    {
                        if (rsiSellArray[rsiSellArray.Length - 1] <= param.CloseValueSell || sellCloseSignal && positionEntryPrice / lastPrice >= 1.05m)
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
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New MARKET buy close order has been created: qty = {positionQty}");
                            logger.WriteFile("--- " + JObject.FromObject(newOrder).ToString(Formatting.None));
                            positionQty = 0;
                            sellCloseRSI = 0;
                        }
                        else if (sellCloseRSI == 0 || sellCloseRSI > rsiSellArray[rsiSellArray.Length - 1])
                        {
                            sellCloseRSI = rsiSellArray[rsiSellArray.Length - 1];
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  sellClose = {sellCloseRSI}");
                        }
                    }
                    else
                    {
                        logger.WriteLine($"        Unknown position exists: qty = {positionQty}");
                    }

                    if (BitMEXApiHelper.ServerTime.Minute % param.BinSize == 0 && BitMEXApiHelper.ServerTime.Second < 15)
                    {
                        List<string> cancelOrderList = new List<string>();
                        foreach (Order order in botOrderList)
                        {
                            if (order.Text.Contains("<POST-ONLY>"))
                                cancelOrderList.Add(order.OrderID);
                        }
                        if (cancelOrderList.Count > 0)
                        {
                            var canceledOrderList = apiHelper.CancelOrders(cancelOrderList);
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  {canceledOrderList.Count} POST-ONLY orders have been canceled.", ConsoleColor.DarkGray);
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
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New post-only buy order has been created: qty = {1}, price = {price:F2}", ConsoleColor.DarkGray);
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
                            logger.WriteLine($"        [{BitMEXApiHelper.ServerTime:HH:mm:ss fff}]  New post-only sell order has been created: qty = {1}, price = {price:F2}", ConsoleColor.DarkGray);

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
                        int waitSeconds = (waitMilliseconds / 1000) % 15;
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