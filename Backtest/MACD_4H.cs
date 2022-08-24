using Newtonsoft.Json.Linq;
using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Valloon.Trading;
using Valloon.Utils;

namespace Valloon.BitMEX.Backtest
{
    static class MACD_4H
    {
        //static readonly string SYMBOL = BitMEXApiHelper.SYMBOL_ETHUSD;
        //static readonly string SYMBOL = BitMEXApiHelper.SYMBOL_SOLUSD;
        static readonly string SYMBOL = "SOLUSDT";

        public static void Run()
        {
            Program.MoveWindow(20, 0, 1600, 200);

            //Loader.LoadCSV(SYMBOL, "1m", new DateTime(2022, 6, 16, 0, 0, 0, DateTimeKind.Utc)); return;
            //Loader.Load(SYMBOL, "1h", new DateTime(2022, 8, 3, 0, 0, 0, DateTimeKind.Utc)); return;

            {
                Benchmark();
                //Test();
                return;
            }

            //ThreadPool.QueueUserWorkItem(state => runHCS(new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc)));
        }

        static float Benchmark()
        {
            const int buyOrSell = 2;

            //const float maxLoss = 0.1f;
            const int leverage = 4;

            const float makerFee = 0.0003f;
            const float takerFee = 0.001f;

            //DateTime startTime = new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc);
            //DateTime startTime = new DateTime(2022, 2, 1, 0, 0, 0, DateTimeKind.Utc);
            //DateTime startTime = new DateTime(2022, 5, 15, 0, 0, 0, DateTimeKind.Utc);
            DateTime startTime = new DateTime(2022, 6, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime? endTime = null;
            //DateTime? endTime = new DateTime(2022, 8, 14, 0, 0, 0, DateTimeKind.Utc);

            //int rsiLength = 6;
            //const int binSize = 4;
            //var list1h = Dao.SelectAll(SYMBOL, "1h");
            //var list = Loader.LoadBinListFrom1h(binSize, list1h, false, 0);

            int rsiLength = 16;
            const int binSize = 15;
            var list1m = Dao.SelectAll(SYMBOL, "1m");
            var list = Loader.LoadBinListFrom1m(binSize, list1m, false);

            var quoteList = new List<Skender.Stock.Indicators.Quote>();
            foreach (var t in list)
            {
                quoteList.Add(new Skender.Stock.Indicators.Quote
                {
                    Date = t.Timestamp,
                    Open = t.Open,
                    High = t.High,
                    Low = t.Low,
                    Close = t.Close,
                    Volume = t.Volume,
                });
            }
            list.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
            int count = list.Count;
            int totalDays = (int)(list[count - 1].Timestamp - list[0].Timestamp).TotalDays;
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}    {SYMBOL}-MACD    buyOrSell = {buyOrSell}    bin = {binSize}    fee = {makerFee} - {takerFee}    {startTime:yyyy-MM-dd} ~ {endTime:yyyy-MM-dd} ({totalDays:N0}) days");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;

            var macdList = quoteList.GetMacd().ToList();
            macdList.RemoveAll(x => x.Date < startTime || endTime != null && x.Date > endTime.Value);

            List<Dictionary<string, float>> topList = new List<Dictionary<string, float>>();
            {
                var rsiList = quoteList.GetRsi(rsiLength).ToList();
                rsiList.RemoveAll(x => x.Date < startTime || endTime != null && x.Date > endTime.Value);

                for (int rsiValue = 50; rsiValue <= 90; rsiValue++)
                //int rsiValue = 80;
                {
                    //for (int macdDeep = -20; macdDeep <= 20; macdDeep += 2)
                    int macdDeep = 0;
                    {
                        //for (float skipX = 0.02f; skipX <= 0.1f; skipX += 0.005f)
                        float skipX = 0;
                        {
                            for (float closeX = 0; closeX <= 0.4f; closeX += 0.005f)
                            //float closeX = 0;
                            {
                                for (float stopX = 0.02f; stopX < 0.1f; stopX += 0.005f)
                                //float stopX = .03f;
                                {
                                    //float leverage = maxLoss / stopX;
                                    //for (float tailMin = 0.02f; tailMin <= 0.3f; tailMin += 0.01f)
                                    float tailMin = 0;
                                    {
                                        //for (float tailX = 0.1f; tailX < 1f; tailX += 0.01f)
                                        float tailX = 0;
                                        {
                                            int tryCount = 0;
                                            int succeedCount = 0, failedCount = 0;
                                            int closeCount = 0, stopCount = 0, tailCount = 0, flipCount = 0, skipCount = 0;
                                            float maxProfit = 0, finalPercent = 1, finalPercent2 = 1;
                                            int position = 0;
                                            int positionEntryPrice = 0;
                                            int topPrice = 0;
                                            int lastPositionEntryPrice = 0;
                                            for (int i = 2; i < count - 1; i++)
                                            {
                                                if (position == 0)
                                                {
                                                    //if (buyOrSell == 1 && macdList[i - 2].Histogram < macdDeep && macdList[i - 1].Histogram >= macdDeep && macdList[i].Histogram > macdDeep && (skipX == 0 || list[i].Close < list[i - 1].Open * (1 + skipX)))
                                                    if (buyOrSell == 1)
                                                    {
                                                        if (macdList[i - 1].Histogram < macdDeep && macdList[i].Histogram >= macdDeep && (skipX == 0 || list[i].Close < list[i - 1].Open * (1 + skipX)))
                                                        {
                                                            lastPositionEntryPrice = list[i].Close;
                                                            if (rsiList[i].Rsi < rsiValue)
                                                            {
                                                                position = 1;
                                                                positionEntryPrice = list[i].Close;
                                                                topPrice = positionEntryPrice;
                                                            }
                                                            else
                                                            {
                                                                skipCount++;
                                                            }
                                                        }
                                                        //else if (lastPositionEntryPrice > 0 && macdList[i - 1].Histogram >= macdDeep && macdList[i].Histogram > macdDeep && list[i].Close >= lastPositionEntryPrice)
                                                        //{
                                                        //    if (rsiList[i].Rsi < rsiValue)
                                                        //    {
                                                        //        position = 1;
                                                        //        positionEntryPrice = list[i].Close;
                                                        //        topPrice = positionEntryPrice;
                                                        //    }
                                                        //    else
                                                        //    {
                                                        //        skipCount++;
                                                        //    }
                                                        //}
                                                    }
                                                    //else if (buyOrSell == 2 && macdList[i - 2].Histogram > macdDeep && macdList[i - 1].Histogram <= macdDeep && macdList[i].Histogram < macdDeep && (skipX == 0 || list[i].Close > list[i - 1].Open * (1 - skipX)))
                                                    else if (buyOrSell == 2)
                                                    {
                                                        if (macdList[i - 1].Histogram > macdDeep && macdList[i].Histogram <= macdDeep && (skipX == 0 || list[i].Close > list[i - 1].Open * (1 - skipX)))
                                                        {
                                                            lastPositionEntryPrice = list[i].Close;
                                                            if (rsiList[i].Rsi > 100 - rsiValue)
                                                            {
                                                                position = -1;
                                                                positionEntryPrice = list[i].Close;
                                                                topPrice = positionEntryPrice;
                                                            }
                                                            else
                                                            {
                                                                skipCount++;
                                                            }
                                                        }
                                                        //else if (lastPositionEntryPrice > 0 && macdList[i - 1].Histogram <= macdDeep && macdList[i].Histogram < macdDeep && list[i].Close <= lastPositionEntryPrice)
                                                        //{
                                                        //    if (rsiList[i].Rsi > 100 - rsiValue)
                                                        //    {
                                                        //        position = -1;
                                                        //        positionEntryPrice = list[i].Close;
                                                        //        topPrice = positionEntryPrice;
                                                        //    }
                                                        //    else
                                                        //    {
                                                        //        skipCount++;
                                                        //    }
                                                        //}
                                                    }
                                                }
                                                else if (position == 1)
                                                {
                                                    int closePrice = (int)Math.Floor(positionEntryPrice * (1 + closeX));
                                                    int stopPrice = (int)Math.Ceiling(positionEntryPrice * (1 - stopX));
                                                    int tailPrice = (int)Math.Ceiling(positionEntryPrice + (topPrice - positionEntryPrice) * tailX);
                                                    //int tailPrice = (int)Math.Ceiling(positionEntryPrice * (1 + tailMin) + (topPrice - positionEntryPrice * (1 + tailMin)) * tailX);
                                                    if (list[i].Low < stopPrice)
                                                    {
                                                        tryCount++;
                                                        stopCount++;
                                                        failedCount++;
                                                        float percent = (float)stopPrice / positionEntryPrice - takerFee;
                                                        finalPercent *= percent;
                                                        finalPercent2 *= 1 + (percent - 1) * leverage;
                                                        maxProfit = Math.Max(maxProfit, percent);
                                                        position = 0;
                                                        positionEntryPrice = 0;
                                                        i--;
                                                    }
                                                    else if (topPrice > positionEntryPrice * (1 + tailMin) && tailX > 0 && list[i].Low <= tailPrice)
                                                    {
                                                        tryCount++;
                                                        tailCount++;
                                                        if (list[i].Open < tailPrice) tailPrice = list[i].Open;
                                                        float percent = (float)tailPrice / positionEntryPrice - takerFee;
                                                        if (percent > 1)
                                                            succeedCount++;
                                                        else
                                                            failedCount++;
                                                        finalPercent *= percent;
                                                        finalPercent2 *= 1 + (percent - 1) * leverage;
                                                        maxProfit = Math.Max(maxProfit, percent);
                                                        position = 0;
                                                        positionEntryPrice = 0;
                                                        i--;
                                                    }
                                                    else if (closeX > 0 && list[i].High > closePrice)
                                                    {
                                                        tryCount++;
                                                        closeCount++;
                                                        succeedCount++;
                                                        float percent = (float)closePrice / positionEntryPrice - takerFee;
                                                        finalPercent *= percent;
                                                        finalPercent2 *= 1 + (percent - 1) * leverage;
                                                        maxProfit = Math.Max(maxProfit, percent);
                                                        position = 0;
                                                        positionEntryPrice = 0;
                                                        i--;
                                                    }
                                                    else if (macdList[i].Histogram < 0)
                                                    {
                                                        tryCount++;
                                                        flipCount++;
                                                        float percent = (float)list[i].Close / positionEntryPrice - takerFee;
                                                        if (percent > 1)
                                                            succeedCount++;
                                                        else
                                                            failedCount++;
                                                        finalPercent *= percent;
                                                        finalPercent2 *= 1 + (percent - 1) * leverage;
                                                        maxProfit = Math.Max(maxProfit, percent);
                                                        position = 0;
                                                        positionEntryPrice = 0;
                                                        i--;
                                                    }
                                                    topPrice = Math.Max(topPrice, list[i].High);
                                                }
                                                else if (position == -1)
                                                {
                                                    int closePrice = (int)Math.Ceiling(positionEntryPrice * (1 - closeX));
                                                    int stopPrice = (int)Math.Floor(positionEntryPrice * (1 + stopX));
                                                    //int tailPrice = (int)Math.Ceiling(positionEntryPrice - (positionEntryPrice - topPrice) * tailX);
                                                    int tailPrice = (int)Math.Ceiling(positionEntryPrice * (1 - tailMin) - (positionEntryPrice * (1 - tailMin) - topPrice) * tailX);
                                                    if (list[i].High > stopPrice)
                                                    {
                                                        tryCount++;
                                                        stopCount++;
                                                        failedCount++;
                                                        float percent = 2 - (float)stopPrice / positionEntryPrice - takerFee;
                                                        finalPercent *= percent;
                                                        finalPercent2 *= 1 + (percent - 1) * leverage;
                                                        maxProfit = Math.Max(maxProfit, percent);
                                                        position = 0;
                                                        positionEntryPrice = 0;
                                                        i--;
                                                    }
                                                    else if (topPrice < positionEntryPrice * (1 - tailMin) && tailX > 0 && list[i].High >= tailPrice)
                                                    {
                                                        tryCount++;
                                                        tailCount++;
                                                        if (list[i].Open > tailPrice) tailPrice = list[i].Open;
                                                        float percent = 2 - (float)tailPrice / positionEntryPrice - takerFee;
                                                        if (percent > 1)
                                                            succeedCount++;
                                                        else
                                                            failedCount++;
                                                        finalPercent *= percent;
                                                        finalPercent2 *= 1 + (percent - 1) * leverage;
                                                        maxProfit = Math.Max(maxProfit, percent);
                                                        position = 0;
                                                        positionEntryPrice = 0;
                                                        i--;
                                                    }
                                                    else if (closeX > 0 && list[i].Low < closePrice)
                                                    {
                                                        tryCount++;
                                                        closeCount++;
                                                        succeedCount++;
                                                        float percent = 2 - (float)closePrice / positionEntryPrice - takerFee;
                                                        finalPercent *= percent;
                                                        finalPercent2 *= 1 + (percent - 1) * leverage;
                                                        maxProfit = Math.Max(maxProfit, percent);
                                                        position = 0;
                                                        positionEntryPrice = 0;
                                                        i--;
                                                    }
                                                    else if (macdList[i].Histogram > 0)
                                                    {
                                                        tryCount++;
                                                        flipCount++;
                                                        float percent = 2 - (float)list[i].Close / positionEntryPrice - takerFee;
                                                        if (percent > 1)
                                                            succeedCount++;
                                                        else
                                                            failedCount++;
                                                        finalPercent *= percent;
                                                        finalPercent2 *= 1 + (percent - 1) * leverage;
                                                        maxProfit = Math.Max(maxProfit, percent);
                                                        position = 0;
                                                        positionEntryPrice = 0;
                                                        i--;
                                                    }
                                                    topPrice = Math.Min(topPrice, list[i].Low);
                                                }
                                            }
                                            float successRate = failedCount > 0 ? (float)succeedCount / failedCount : succeedCount;
                                            if (finalPercent > 1f && succeedCount > 0)
                                            {
                                                Dictionary<string, float> dic = new Dictionary<string, float>
                                                {
                                                    { "buyOrSell", buyOrSell },
                                                    { "binSize", binSize },
                                                    { "makerFee", makerFee },
                                                    { "takerFee", takerFee },
                                                    { "leverage", leverage },
                                                    { "rsiLength", rsiLength },
                                                    { "rsiValue", rsiValue },
                                                    { "macdDeep", macdDeep },
                                                    { "skipX", skipX },
                                                    { "closeX", closeX },
                                                    { "stopX", stopX },
                                                    { "tailMin", tailMin },
                                                    { "tailX", tailX },
                                                    { "tryCount", tryCount },
                                                    { "skipCount", skipCount },
                                                    { "succeedCount", succeedCount },
                                                    { "failedCount", failedCount },
                                                    { "closeCount", closeCount },
                                                    { "stopCount", stopCount },
                                                    { "tailCount", tailCount },
                                                    { "flipCount", flipCount },
                                                    { "maxProfit", maxProfit },
                                                    { "finalPercent", finalPercent },
                                                    { "finalPercent2", finalPercent2 },
                                                };
                                                int topListCount = topList.Count;
                                                if (topListCount > 0)
                                                {
                                                    while (topListCount > 10000)
                                                    {
                                                        topList.RemoveAt(0);
                                                        topListCount--;
                                                    }
                                                    for (int i = 0; i < topListCount; i++)
                                                    {
                                                        if (topList[i]["finalPercent2"] > finalPercent2)
                                                        {
                                                            topList.Insert(i, dic);
                                                            goto topListEnd;
                                                        }
                                                    }
                                                    topList.Add(dic);
                                                topListEnd:;
                                                }
                                                else
                                                {
                                                    topList.Add(dic);
                                                }
                                                logger.WriteLine($"rsi = {rsiLength} / {rsiValue} \t limit = {closeX:F4} / {stopX:F4} / {tailMin:F4} / {tailX:F4} \t count = {tryCount} / {succeedCount} / {failedCount} / {successRate:F2} \t % = {finalPercent:F4} / {finalPercent2:F4}");
                                            }
                                            //else if (finalPercent > .5f && succeedCount > 0)
                                            //{
                                            //    logger.WriteLine($"rsi = {rsiLength} / {rsiValue} \t limit = {closeX:F4} / {stopX:F4} / {tailX:F4} \t count = {tryCount} / {succeedCount} / {failedCount} / {successRate:F2} \t % = {finalPercent:F4} / {finalPercent2:F4}", ConsoleColor.DarkGray, false);
                                            //}
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            logger.WriteLine($"\r\n\r\n\r\n\r\ntopList={topList.Count}\r\n" + JArray.FromObject(topList).ToString());
            if (topList.Count > 0)
                return topList[topList.Count - 1]["finalPercent"];
            return 0;
        }

        static string Test()
        {
            //Test(new DateTime(2022, 3, 16, 0, 0, 0, DateTimeKind.Utc)); return null;

            string result = "";
            result += Test(new DateTime(2022, 8, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2022, 7, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 8, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2022, 6, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 7, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2022, 5, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 6, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2022, 4, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 5, 2, 8, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 4, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2022, 2, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 2, 2, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2021, 12, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 12, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2021, 10, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2021, 9, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 10, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2021, 8, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 9, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 8, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2022, 5, 15, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2022, 2, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            Console.WriteLine("\r\n\r\n================================\r\n");
            Console.WriteLine(result);
            return result;
        }

        static string Test(DateTime startTime, DateTime? endTime = null)
        {
            int buyOrSell = 3;

            //int rsiLength = 6;
            //int rsiValue1 = 70;
            //int rsiValue2 = 28;
            //int macdDeep = 0;
            //float skipX1 = 0;
            //float closeX1 = 0.15f;
            //float stopX1 = 0.04f;
            //float skipX2 = 0;
            //float closeX2 = 0.25f;
            //float stopX2 = 0.03f;

            int rsiLength = 6;
            int rsiValue1 = 80;
            int rsiValue2 = 26;
            int macdDeep = 0;
            float skipX1 = 0;
            float closeX1 = 0.08f;
            float stopX1 = 0.04f;
            float skipX2 = 0;
            float closeX2 = 0.25f;
            float stopX2 = 0.03f;

            const int binSize = 4;

            const float makerFee = 0.0003f;
            const float takerFee = 0.001f;

            var list1h = Dao.SelectAll(SYMBOL, "1h");
            var list = Loader.LoadBinListFrom1h(binSize, list1h, false, 0);

            var quoteList = new List<Skender.Stock.Indicators.Quote>();
            foreach (var t in list)
            {
                quoteList.Add(new Skender.Stock.Indicators.Quote
                {
                    Date = t.Timestamp,
                    Open = t.Open,
                    High = t.High,
                    Low = t.Low,
                    Close = t.Close,
                    Volume = t.Volume,
                });
            }

            list.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
            int count = list.Count;
            int totalDays = (int)(list.Last().Timestamp - startTime).TotalDays;
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}    TestSAR2    bin = {binSize}    takerFee = {takerFee}    ({startTime:yyyy-MM-dd HH.mm.ss} ~ {totalDays:N0} days)");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;

            var rsiList = quoteList.GetRsi(rsiLength).ToList();
            rsiList.RemoveAll(x => x.Date < startTime || endTime != null && x.Date > endTime.Value);
            var macdList = quoteList.GetMacd().ToList();
            macdList.RemoveAll(x => x.Date < startTime || endTime != null && x.Date > endTime.Value);

            //for (int i = 0; i < count; i++)
            //{
            //    Console.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t {rsiList[i].Rsi:F4}    {macdList[i].Macd:F4}  /  {macdList[i].Histogram:F4}  /  {macdList[i].Signal:F4}");
            //}

            int tryCount = 0;
            int succeedCount = 0, failedCount = 0;
            float finalPercent = 1, finalPercent2 = 1, finalPercent3 = 1, finalPercent4 = 1, finalPercent5 = 1;
            int position = 0;
            int positionEntryPrice = 0;
            int lastPositionEntryPrice = 0;
            for (int i = 2; i < count - 1; i++)
            {
                if (position == 0)
                {
                    if (buyOrSell == 1 || buyOrSell == 3)
                    {
                        if (macdList[i - 1].Histogram < macdDeep && macdList[i].Histogram >= macdDeep && (skipX1 == 0 || list[i].Close < list[i - 1].Open * (1 + skipX1)))
                        {
                            lastPositionEntryPrice = list[i].Close;
                            if (rsiList[i].Rsi < rsiValue1)
                            {
                                tryCount++;
                                position = 1;
                                positionEntryPrice = list[i].Close;
                                //topPrice = positionEntryPrice;
                                logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t {rsiList[i].Rsi:F4}    {macdList[i].Macd:F4}  /  {macdList[i].Histogram:F4}  /  {macdList[i].Signal:F4} \t Position = {position} \t Entry = {positionEntryPrice} \t <LONG>");
                            }
                            else
                            {
                                //skipCount++;
                            }
                        }
                        //else if (lastPositionEntryPrice > 0 && macdList[i - 1].Histogram >= macdDeep && macdList[i].Histogram > macdDeep && list[i].Close >= lastPositionEntryPrice)
                        //{
                        //    if (rsiList[i].Rsi < rsiValue1)
                        //    {
                        //        tryCount++;
                        //        position = 1;
                        //        positionEntryPrice = list[i].Close;
                        //        //topPrice = positionEntryPrice;
                        //        logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t {rsiList[i].Rsi:F4}    {macdList[i].Macd:F4}  /  {macdList[i].Histogram:F4}  /  {macdList[i].Signal:F4} \t Position = {position} \t Entry = {positionEntryPrice} \t <LONG>");
                        //    }
                        //    else
                        //    {
                        //        //skipCount++;
                        //    }
                        //}
                    }
                    //else if (buyOrSell == 2 && macdList[i - 2].Histogram > macdDeep && macdList[i - 1].Histogram <= macdDeep && macdList[i].Histogram < macdDeep && (skipX == 0 || list[i].Close > list[i - 1].Open * (1 - skipX)))
                    if (buyOrSell == 2 || buyOrSell == 3)
                    {
                        if (macdList[i - 1].Histogram > macdDeep && macdList[i].Histogram <= macdDeep && (skipX2 == 0 || list[i].Close > list[i - 1].Open * (1 - skipX2)))
                        {
                            lastPositionEntryPrice = list[i].Close;
                            if (rsiList[i].Rsi > rsiValue2)
                            {
                                tryCount++;
                                position = -1;
                                positionEntryPrice = list[i].Close;
                                //topPrice = positionEntryPrice;
                                logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t {rsiList[i].Rsi:F4}    {macdList[i].Macd:F4}  /  {macdList[i].Histogram:F4}  /  {macdList[i].Signal:F4} \t Position = {position} \t Entry = {positionEntryPrice} \t <SHORT>");
                            }
                            else
                            {
                                //skipCount++;
                            }
                        }
                        //else if (lastPositionEntryPrice > 0 && macdList[i - 1].Histogram <= macdDeep && macdList[i].Histogram < macdDeep && list[i].Close <= lastPositionEntryPrice)
                        //{
                        //    if (rsiList[i].Rsi > rsiValue2)
                        //    {
                        //        tryCount++;
                        //        position = -1;
                        //        positionEntryPrice = list[i].Close;
                        //        //topPrice = positionEntryPrice;
                        //        logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t {rsiList[i].Rsi:F4}    {macdList[i].Macd:F4}  /  {macdList[i].Histogram:F4}  /  {macdList[i].Signal:F4} \t Position = {position} \t Entry = {positionEntryPrice} \t <SHORT>");
                        //    }
                        //    else
                        //    {
                        //        //skipCount++;
                        //    }
                        //}
                    }
                }
                else if (position == 1)
                {
                    int closePrice = (int)Math.Floor(positionEntryPrice * (1 + closeX1));
                    int stopPrice = (int)Math.Ceiling(positionEntryPrice * (1 - stopX1));
                    if (list[i].Low < stopPrice)
                    {
                        failedCount++;
                        float percent = (float)stopPrice / positionEntryPrice - takerFee;
                        finalPercent *= percent;
                        finalPercent2 *= 1 + (percent - 1) * 2;
                        finalPercent3 *= 1 + (percent - 1) * 3;
                        finalPercent4 *= 1 + (percent - 1) * 4;
                        finalPercent5 *= 1 + (percent - 1) * 5;
                        logger.WriteLine($"     {list[i].Timestamp:MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t Failed \t <LONG> \t Entry = {positionEntryPrice} \t Stop = {stopPrice} \t % = {percent:F4}  /  {finalPercent}", ConsoleColor.Red);
                        position = 0;
                        positionEntryPrice = 0;
                        i--;
                    }
                    else if (closeX1 > 0 && list[i].High > closePrice)
                    {
                        succeedCount++;
                        float percent = (float)closePrice / positionEntryPrice - takerFee;
                        finalPercent *= percent;
                        finalPercent2 *= 1 + (percent - 1) * 2;
                        finalPercent3 *= 1 + (percent - 1) * 3;
                        finalPercent4 *= 1 + (percent - 1) * 4;
                        finalPercent5 *= 1 + (percent - 1) * 5;
                        logger.WriteLine($"     {list[i].Timestamp:MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t Succeed \t <LONG> \t Entry = {positionEntryPrice} \t Close = {closePrice} \t % = {percent:F4}  /  {finalPercent}", ConsoleColor.Green);
                        position = 0;
                        positionEntryPrice = 0;
                        i--;
                    }
                    else if (macdList[i].Histogram < 0)
                    {
                        float percent = (float)list[i].Close / positionEntryPrice - takerFee;
                        if (percent > 1)
                            succeedCount++;
                        else
                            failedCount++;
                        finalPercent *= percent;
                        finalPercent2 *= 1 + (percent - 1) * 2;
                        finalPercent3 *= 1 + (percent - 1) * 3;
                        finalPercent4 *= 1 + (percent - 1) * 4;
                        finalPercent5 *= 1 + (percent - 1) * 5;
                        logger.WriteLine($"     {list[i].Timestamp:MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t Stopped \t <LONG> \t Entry = {positionEntryPrice} \t Stop = {list[i].Close} \t % = {percent:F4}  /  {finalPercent}", ConsoleColor.DarkYellow);
                        position = 0;
                        positionEntryPrice = 0;
                        i--;
                    }

                }
                else if (position == -1)
                {
                    int closePrice = (int)Math.Ceiling(positionEntryPrice * (1 - closeX2));
                    int stopPrice = (int)Math.Floor(positionEntryPrice * (1 + stopX2));
                    if (list[i].High > stopPrice)
                    {
                        failedCount++;
                        float percent = 2 - (float)stopPrice / positionEntryPrice - takerFee;
                        finalPercent *= percent;
                        finalPercent2 *= 1 + (percent - 1) * 2;
                        finalPercent3 *= 1 + (percent - 1) * 3;
                        finalPercent4 *= 1 + (percent - 1) * 4;
                        finalPercent5 *= 1 + (percent - 1) * 5;
                        logger.WriteLine($"     {list[i].Timestamp:MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t Failed \t <SHORT> \t Entry = {positionEntryPrice} \t Stop = {stopPrice} \t % = {percent:F4}  /  {finalPercent}", ConsoleColor.Red);
                        position = 0;
                        positionEntryPrice = 0;
                        i--;
                    }
                    else if (closeX2 > 0 && list[i].Low < closePrice)
                    {
                        succeedCount++;
                        float percent = 2 - (float)closePrice / positionEntryPrice - takerFee;
                        finalPercent *= percent;
                        finalPercent2 *= 1 + (percent - 1) * 2;
                        finalPercent3 *= 1 + (percent - 1) * 3;
                        finalPercent4 *= 1 + (percent - 1) * 4;
                        finalPercent5 *= 1 + (percent - 1) * 5;
                        logger.WriteLine($"     {list[i].Timestamp:MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t Succeed \t <SHORT> \t Entry = {positionEntryPrice} \t Close = {closePrice} \t % = {percent:F4}  /  {finalPercent}", ConsoleColor.Green);
                        position = 0;
                        positionEntryPrice = 0;
                        i--;
                    }
                    else if (macdList[i].Histogram > 0)
                    {
                        float percent = 2 - (float)list[i].Close / positionEntryPrice - takerFee;
                        if (percent > 1)
                            succeedCount++;
                        else
                            failedCount++;
                        finalPercent *= percent;
                        finalPercent2 *= 1 + (percent - 1) * 2;
                        finalPercent3 *= 1 + (percent - 1) * 3;
                        finalPercent4 *= 1 + (percent - 1) * 4;
                        finalPercent5 *= 1 + (percent - 1) * 5;
                        logger.WriteLine($"     {list[i].Timestamp:MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t Stopped \t <SHORT> \t Entry = {positionEntryPrice} \t Stop = {list[i].Close} \t % = {percent:F4}  /  {finalPercent}", ConsoleColor.DarkYellow);
                        position = 0;
                        positionEntryPrice = 0;
                        i--;
                    }
                }
            }
            float successRate = failedCount > 0 ? (float)succeedCount / failedCount : succeedCount;
            string result = $"{startTime} ~ {list.Last().Timestamp} ({totalDays} days) \t count = {tryCount} / {succeedCount} / {failedCount} / {successRate:F2} \t % = {finalPercent:F4} / {finalPercent2:F4} / {finalPercent3:F4} / {finalPercent4:F4} / {finalPercent5:F4}";
            logger.WriteLine($"\r\n{result}\r\n");
            return result;
        }

    }

}
