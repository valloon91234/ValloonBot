using Newtonsoft.Json.Linq;
using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Valloon.Indicators;
using Valloon.Trading;
using Valloon.Utils;

namespace Valloon.BitMEX.Backtest
{
    static class RSI_BBW
    {
        static readonly string SYMBOL = "SOLUSDT";

        public static void Run()
        {
            Program.MoveWindow(20, 0, 1600, 200);
            //Loader.LoadCSV(SYMBOL, "1m", new DateTime(2022, 6, 16, 0, 0, 0, DateTimeKind.Utc)); return;
            //Loader.Load(SYMBOL, "1h", new DateTime(2022, 8, 3, 0, 0, 0, DateTimeKind.Utc)); return;
            {
                //Benchmark();
                Test();
                return;
            }
            //ThreadPool.QueueUserWorkItem(state => runHCS(new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc)));
        }

        static float Benchmark()
        {
            const int buyOrSell = 1;

            //const float maxLoss = 0.1f;
            //const int leverage = 4;

            const float makerFee = 0.001f;
            const float takerFee = 0.002f;

            //DateTime startTime = new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime startTime = new DateTime(2022, 2, 1, 0, 0, 0, DateTimeKind.Utc);
            //DateTime startTime = new DateTime(2022, 5, 15, 0, 0, 0, DateTimeKind.Utc);
            //DateTime startTime = new DateTime(2022, 6, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime? endTime = null;
            //DateTime? endTime = new DateTime(2022, 7, 1, 0, 0, 0, DateTimeKind.Utc);

            const int binSize = 5;
            var list1m = Dao.SelectAll(SYMBOL, "1m");
            var list = Loader.LoadBinListFrom1m(binSize, list1m, false);

            var quoteList = new List<Quote>();
            foreach (var t in list)
            {
                quoteList.Add(new Quote
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
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}    {SYMBOL}-MACD-BBW    buyOrSell = {buyOrSell}    bin = {binSize}    {startTime:yyyy-MM-dd} ~ {endTime:yyyy-MM-dd} ({totalDays:N0}) days");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;

            List<Dictionary<string, float>> topList = new List<Dictionary<string, float>>();
            //for (int rsiLength = 6; rsiLength <= 8; rsiLength += 1)
            int rsiLength = 6;
            {
                var rsiList = quoteList.GetRsi(rsiLength).ToList();
                rsiList.RemoveAll(x => x.Date < startTime || endTime != null && x.Date > endTime.Value);

                //for (int bbwLength = 6; bbwLength <= 16; bbwLength += 2)
                int bbwLength = 8;
                {
                    var bbwList = quoteList.GetBollingerBands(bbwLength, 2).ToList();
                    bbwList.RemoveAll(x => x.Date < startTime || endTime != null && x.Date > endTime.Value);

                    //for (int i = 0; i < count; i++)
                    //{
                    //    Console.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t {rsiList[i].Rsi:F4}    {macdList[i].Macd:F4}  /  {macdList[i].Histogram:F4}  /  {macdList[i].Signal:F4}    {bbwList[i].Width:F4}");
                    //}

                    for (float rsiValue = 50; rsiValue < 95; rsiValue += .5f)
                    //int rsiValue = 100;
                    {
                        float rsi;
                        if (buyOrSell == 1)
                            rsi = rsiValue;
                        else if (buyOrSell == 2)
                            rsi = 100 - rsiValue;
                        else
                            throw new Exception($"buyOrSell = {buyOrSell}");
                        for (float bbwOpen = 0; bbwOpen < .05f; bbwOpen += 0.005f)
                        //float bbwOpen = 0.05f;
                        {
                            for (float closeX = 0.005f; closeX <= 0.03f; closeX += 0.005f)
                            //float closeX = 0;
                            {
                                for (float stopX = 0.01f; stopX <= closeX; stopX += 0.005f)
                                //float stopX = .01f;
                                {
                                    float leverage = stopX == 0 ? 1 : 0.1f / stopX;

                                    int tryCount = 0;
                                    int succeedCount = 0, failedCount = 0;
                                    int closeCount = 0, stopCount = 0, flipCount = 0, skipCount = 0;
                                    float maxLoss = 1, maxProfit = 0, finalPercent = 1, finalPercent2 = 1;
                                    int position = 0;
                                    int positionEntryPrice = 0;
                                    for (int i = 4; i < count - 1; i++)
                                    {
                                        if (position == 0)
                                        {
                                            if (buyOrSell == 1)
                                            {
                                                if (bbwList[i].Width > bbwOpen && rsiList[i - 3].Rsi < rsiList[i - 2].Rsi && rsiList[i - 2].Rsi < rsiList[i - 1].Rsi && rsiList[i - 1].Rsi > rsiList[i].Rsi && rsiList[i].Rsi > rsi)
                                                {
                                                    position = 1;
                                                    positionEntryPrice = list[i].Close;
                                                }
                                            }
                                            else if (buyOrSell == 2)
                                            {
                                                if (bbwList[i].Width > bbwOpen && rsiList[i - 3].Rsi > rsiList[i - 2].Rsi && rsiList[i - 2].Rsi > rsiList[i - 1].Rsi && rsiList[i - 1].Rsi < rsiList[i].Rsi && rsiList[i].Rsi < rsi)
                                                {
                                                    position = -1;
                                                    positionEntryPrice = list[i].Close;
                                                }
                                            }
                                        }
                                        else if (position == 1)
                                        {
                                            int closePrice = (int)Math.Floor(positionEntryPrice * (1 + closeX));
                                            int stopPrice = (int)Math.Ceiling(positionEntryPrice * (1 - stopX));
                                            if (stopX > 0 && list[i].Low < stopPrice)
                                            {
                                                tryCount++;
                                                stopCount++;
                                                failedCount++;
                                                float percent = (float)stopPrice / positionEntryPrice - takerFee;
                                                finalPercent *= percent;
                                                finalPercent2 *= 1 + (percent - 1) * leverage;
                                                maxProfit = Math.Max(maxProfit, percent);
                                                maxLoss = Math.Min(maxLoss, percent);
                                                position = 0;
                                                positionEntryPrice = 0;
                                                i--;
                                            }
                                            else if (closeX > 0 && list[i].High > closePrice)
                                            {
                                                tryCount++;
                                                closeCount++;
                                                succeedCount++;
                                                float percent = (float)closePrice / positionEntryPrice - makerFee;
                                                finalPercent *= percent;
                                                finalPercent2 *= 1 + (percent - 1) * leverage;
                                                maxProfit = Math.Max(maxProfit, percent);
                                                maxLoss = Math.Min(maxLoss, percent);
                                                position = 0;
                                                positionEntryPrice = 0;
                                                i--;
                                            }
                                        }
                                        else if (position == -1)
                                        {
                                            int closePrice = (int)Math.Ceiling(positionEntryPrice * (1 - closeX));
                                            int stopPrice = (int)Math.Floor(positionEntryPrice * (1 + stopX));
                                            if (stopX > 0 && list[i].High > stopPrice)
                                            {
                                                tryCount++;
                                                stopCount++;
                                                failedCount++;
                                                float percent = 2 - (float)stopPrice / positionEntryPrice - takerFee;
                                                finalPercent *= percent;
                                                finalPercent2 *= 1 + (percent - 1) * leverage;
                                                maxProfit = Math.Max(maxProfit, percent);
                                                maxLoss = Math.Min(maxLoss, percent);
                                                position = 0;
                                                positionEntryPrice = 0;
                                                i--;
                                            }
                                            else if (closeX > 0 && list[i].Low < closePrice)
                                            {
                                                tryCount++;
                                                closeCount++;
                                                succeedCount++;
                                                float percent = 2 - (float)closePrice / positionEntryPrice - makerFee;
                                                finalPercent *= percent;
                                                finalPercent2 *= 1 + (percent - 1) * leverage;
                                                maxProfit = Math.Max(maxProfit, percent);
                                                maxLoss = Math.Min(maxLoss, percent);
                                                position = 0;
                                                positionEntryPrice = 0;
                                                i--;
                                            }
                                        }
                                    }
                                    float successRate = failedCount > 0 ? (float)succeedCount / failedCount : succeedCount;
                                    if (finalPercent > 1f && succeedCount > 0)
                                    {
                                        Dictionary<string, float> dic = new Dictionary<string, float>
                                            {
                                                { "buyOrSell", buyOrSell },
                                                { "binSize", binSize },
                                                { "fee", takerFee },
                                                { "leverage", leverage },
                                                { "rsiLength", rsiLength },
                                                { "rsiValue", rsi },
                                                { "bbwLength", bbwLength },
                                                { "bbwOpen", bbwOpen },
                                                { "closeX", closeX },
                                                { "stopX", stopX },
                                                { "tryCount", tryCount },
                                                { "skipCount", skipCount },
                                                { "succeedCount", succeedCount },
                                                { "failedCount", failedCount },
                                                { "closeCount", closeCount },
                                                { "stopCount", stopCount },
                                                { "flipCount", flipCount },
                                                { "maxProfit", maxProfit },
                                                { "maxLoss", maxLoss },
                                                { "finalPercent", finalPercent },
                                                { "finalPercent2", finalPercent2 },
                                            };
                                        int topListCount = topList.Count;
                                        if (topListCount > 0)
                                        {
                                            while (topListCount > 1000)
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
                                        logger.WriteLine($"rsi = {rsiLength} / {rsi} \t bbw = {bbwLength} / {bbwOpen:F4} \t limit = {closeX:F4} / {stopX:F4} \t count = {tryCount} / {skipCount} / {succeedCount} / {failedCount} / {successRate:F2} \t % = {finalPercent:F4} \t %% = {finalPercent2:F4} ({leverage:F2})");
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
            result += Test(new DateTime(2022, 6, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
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
            int binSize = 5;
            int rsiLength = 6;
            int bbwLength = 8;

            const float makerFee = 0.001f;
            const float takerFee = 0.002f;

            var list1m = Dao.SelectAll(SYMBOL, "1m");
            var list = Loader.LoadBinListFrom1m(binSize, list1m, false);

            var quoteList = new List<Quote>();
            foreach (var t in list)
            {
                quoteList.Add(new Quote
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

            var bbwList = quoteList.GetBollingerBands(bbwLength, 2).ToList();
            bbwList.RemoveAll(x => x.Date < startTime || endTime != null && x.Date > endTime.Value);

            //for (int i = 0; i < count; i++)
            //{
            //    Console.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t {rsiList[i].Rsi:F4}    {macdList[i].Macd:F4}  /  {macdList[i].Histogram:F4}  /  {macdList[i].Signal:F4}");
            //}

            int tryCount = 0;
            int succeedCount = 0, failedCount = 0;
            float finalPercent = 1, finalPercent2 = 1, finalPercent3 = 1, finalPercent4 = 1, finalPercent5 = 1, finalPercent10 = 1;
            int position = 0;
            int positionEntryPrice = 0;
            int closePrice = 0, stopPrice = 0;
            for (int i = 4; i < count - 1; i++)
            {
                if (position == 0)
                {
                    if (buyOrSell == 1 || buyOrSell == 3)
                    {
                        if (bbwList[i].Width > 0.04f && rsiList[i - 3].Rsi < rsiList[i - 2].Rsi && rsiList[i - 2].Rsi < rsiList[i - 1].Rsi && rsiList[i - 1].Rsi > rsiList[i].Rsi && rsiList[i].Rsi > 60)
                        {
                            tryCount++;
                            position = 1;
                            positionEntryPrice = list[i].Close;
                            logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t {rsiList[i].Rsi:F4}    {bbwList[i].Width:F4} \t Position = {position} \t Entry = {positionEntryPrice} \t <LONG>");
                            closePrice = (int)Math.Floor(positionEntryPrice * (1 + 0.03f));
                            stopPrice = (int)Math.Ceiling(positionEntryPrice * (1 - 0.015f));
                        }
                    }
                    if (buyOrSell == 2 || buyOrSell == 3)
                    {
                        if (bbwList[i].Width < 0.01f && rsiList[i - 3].Rsi > rsiList[i - 2].Rsi && rsiList[i - 2].Rsi > rsiList[i - 1].Rsi && rsiList[i - 1].Rsi < rsiList[i].Rsi && rsiList[i].Rsi < 27)
                        {
                            tryCount++;
                            position = -1;
                            positionEntryPrice = list[i].Close;
                            logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t {rsiList[i].Rsi:F4}    {bbwList[i].Width:F4} \t Position = {position} \t Entry = {positionEntryPrice} \t <SHORT>");
                            closePrice = (int)Math.Ceiling(positionEntryPrice * (1 - 0.025f));
                            stopPrice = (int)Math.Floor(positionEntryPrice * (1 + 0.01f));
                        }
                        else
                        if (bbwList[i].Width >= 0.01f && rsiList[i - 3].Rsi > rsiList[i - 2].Rsi && rsiList[i - 2].Rsi > rsiList[i - 1].Rsi && rsiList[i - 1].Rsi < rsiList[i].Rsi && rsiList[i].Rsi < 13)
                        {
                            tryCount++;
                            position = -1;
                            positionEntryPrice = list[i].Close;
                            logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t {rsiList[i].Rsi:F4}    {bbwList[i].Width:F4} \t Position = {position} \t Entry = {positionEntryPrice} \t <SHORT>");
                            closePrice = (int)Math.Ceiling(positionEntryPrice * (1 - 0.03f));
                            stopPrice = (int)Math.Floor(positionEntryPrice * (1 + 0.01f));
                        }
                    }
                }
                else if (position == 1)
                {
                    if (list[i].Low < stopPrice)
                    {
                        failedCount++;
                        float percent = (float)stopPrice / positionEntryPrice - takerFee;
                        finalPercent *= percent;
                        finalPercent2 *= 1 + (percent - 1) * 2;
                        finalPercent3 *= 1 + (percent - 1) * 3;
                        finalPercent4 *= 1 + (percent - 1) * 4;
                        finalPercent5 *= 1 + (percent - 1) * 5;
                        finalPercent10 *= 1 + (percent - 1) * 10;
                        logger.WriteLine($"     {list[i].Timestamp:MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t Failed \t <LONG> \t Entry = {positionEntryPrice} \t Stop = {stopPrice} \t % = {percent:F4}  /  {finalPercent}", ConsoleColor.Red);
                        position = 0;
                        positionEntryPrice = 0;
                        i--;
                    }
                    else if (list[i].High > closePrice)
                    {
                        succeedCount++;
                        float percent = (float)closePrice / positionEntryPrice - makerFee;
                        finalPercent *= percent;
                        finalPercent2 *= 1 + (percent - 1) * 2;
                        finalPercent3 *= 1 + (percent - 1) * 3;
                        finalPercent4 *= 1 + (percent - 1) * 4;
                        finalPercent5 *= 1 + (percent - 1) * 5;
                        finalPercent10 *= 1 + (percent - 1) * 10;
                        logger.WriteLine($"     {list[i].Timestamp:MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t Succeed \t <LONG> \t Entry = {positionEntryPrice} \t Close = {closePrice} \t % = {percent:F4}  /  {finalPercent}", ConsoleColor.Green);
                        position = 0;
                        positionEntryPrice = 0;
                        i--;
                    }
                }
                else if (position == -1)
                {
                    if (list[i].High > stopPrice)
                    {
                        failedCount++;
                        float percent = 2 - (float)stopPrice / positionEntryPrice - takerFee;
                        finalPercent *= percent;
                        finalPercent2 *= 1 + (percent - 1) * 2;
                        finalPercent3 *= 1 + (percent - 1) * 3;
                        finalPercent4 *= 1 + (percent - 1) * 4;
                        finalPercent5 *= 1 + (percent - 1) * 5;
                        finalPercent10 *= 1 + (percent - 1) * 10;
                        logger.WriteLine($"     {list[i].Timestamp:MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t Failed \t <SHORT> \t Entry = {positionEntryPrice} \t Stop = {stopPrice} \t % = {percent:F4}  /  {finalPercent}", ConsoleColor.Red);
                        position = 0;
                        positionEntryPrice = 0;
                        i--;
                    }
                    else if (list[i].Low < closePrice)
                    {
                        succeedCount++;
                        float percent = 2 - (float)closePrice / positionEntryPrice - makerFee;
                        finalPercent *= percent;
                        finalPercent2 *= 1 + (percent - 1) * 2;
                        finalPercent3 *= 1 + (percent - 1) * 3;
                        finalPercent4 *= 1 + (percent - 1) * 4;
                        finalPercent5 *= 1 + (percent - 1) * 5;
                        finalPercent10 *= 1 + (percent - 1) * 10;
                        logger.WriteLine($"     {list[i].Timestamp:MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t Succeed \t <SHORT> \t Entry = {positionEntryPrice} \t Close = {closePrice} \t % = {percent:F4}  /  {finalPercent}", ConsoleColor.Green);
                        position = 0;
                        positionEntryPrice = 0;
                        i--;
                    }
                }
            }
            float successRate = failedCount > 0 ? (float)succeedCount / failedCount : succeedCount;
            string result = $"{startTime} ~ {list.Last().Timestamp} ({totalDays} days) \t count = {tryCount} / {succeedCount} / {failedCount} / {successRate:F2} \t % = {finalPercent:F4} / {finalPercent2:F4} / {finalPercent3:F4} / {finalPercent4:F4} / {finalPercent5:F4} / {finalPercent10:F4}";
            logger.WriteLine($"\r\n{result}\r\n");
            return result;
        }

    }

}
