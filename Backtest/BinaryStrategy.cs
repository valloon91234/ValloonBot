﻿using Newtonsoft.Json.Linq;
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
    static class BinaryStrategy
    {
        static readonly string SYMBOL = "LUNCUSDT";

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

            const float maxLoss = 0.05f;
            //const int leverage = 4;

            const float makerFee = 0.0003f;
            const float takerFee = 0.002f;

            //DateTime startTime = new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc);
            //DateTime startTime = new DateTime(2022, 2, 1, 0, 0, 0, DateTimeKind.Utc);
            //DateTime startTime = new DateTime(2022, 5, 15, 0, 0, 0, DateTimeKind.Utc);
            DateTime startTime = new DateTime(2022, 9, 27, 0, 0, 0, DateTimeKind.Utc);
            DateTime? endTime = null;
            //DateTime? endTime = new DateTime(2022, 7, 1, 0, 0, 0, DateTimeKind.Utc);

            const int binSize = 5;
            var list1h = Dao.SelectAll(SYMBOL, "1m");
            var list = Loader.LoadBinListFrom1m(binSize, list1h, false);

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
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}    {SYMBOL}-BS    buyOrSell = {buyOrSell}    bin = {binSize}    {startTime:yyyy-MM-dd} ~ {endTime:yyyy-MM-dd} ({totalDays:N0}) days");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;

            List<Dictionary<string, float>> topList = new List<Dictionary<string, float>>();
            //int[] smaLengthArray = { 3, 5, 10, 15, 20, 30, 45, 60, 90, 120, 180, 240, 360 };
            int[] smaLengthArray = { 6, 12, 18, 24, 30, 36, 48, 60, 72, 96 };
            //int[] smaLengthArray = { 3, 5, 10, 15, 20, 25, 30, 45, 60 };
            foreach (int smaLength in smaLengthArray)
            {
                var smaList = quoteList.GetSma(smaLength).ToList();
                smaList.RemoveAll(x => x.Date < startTime || endTime != null && x.Date > endTime.Value);

                for (float limitX = 0.002f; limitX < .03f; limitX += 0.001f)
                //float bbwOpen = 0.05f;
                {
                    for (float stopX = 0.001f; stopX < limitX; stopX += 0.0005f)
                    //float stopX = closeX;
                    {
                        for (float closeX = 0.001f; closeX < Math.Min(stopX * 2, limitX); closeX += 0.0005f)
                        //float closeX = 0;
                        {
                            float leverage = maxLoss / stopX;
                            int tryCount = 0;
                            int succeedCount = 0, failedCount = 0;
                            float finalPercent = 1, finalPercent2 = 1;
                            int position = 0;
                            int positionEntryPrice = 0;
                            for (int i = 1; i < count - 1; i++)
                            {
                                if (position == 0)
                                {
                                    if (buyOrSell == 1)
                                    {
                                        var limitPrice = (int)Math.Floor(smaList[i - 1].Sma.Value * (1 - limitX));
                                        if (list[i].Open > limitPrice && list[i].Low < limitPrice)
                                        {
                                            int closePrice = (int)Math.Floor(limitPrice * (1 + closeX));
                                            int stopPrice = (int)Math.Ceiling(limitPrice * (1 - stopX));
                                            if (list[i].Low < stopPrice)
                                            {
                                                tryCount++;
                                                failedCount++;
                                                float percent = (float)stopPrice / limitPrice - takerFee;
                                                finalPercent *= percent;
                                                finalPercent2 *= 1 + (percent - 1) * leverage;
                                            }
                                            else if (list[i].Close > closePrice)
                                            {
                                                tryCount++;
                                                succeedCount++;
                                                float percent = (float)closePrice / limitPrice - makerFee;
                                                finalPercent *= percent;
                                                finalPercent2 *= 1 + (percent - 1) * leverage;
                                            }
                                            else
                                            {
                                                position = 1;
                                                positionEntryPrice = limitPrice;
                                            }
                                        }
                                    }
                                    else if (buyOrSell == 2)
                                    {
                                        var limitPrice = (int)Math.Ceiling(smaList[i - 1].Sma.Value * (1 + limitX));
                                        if (list[i].Open < limitPrice && list[i].High > limitPrice)
                                        {
                                            int closePrice = (int)Math.Ceiling(limitPrice * (1 - closeX));
                                            int stopPrice = (int)Math.Floor(limitPrice * (1 + stopX));
                                            if (list[i].High > stopPrice)
                                            {
                                                tryCount++;
                                                failedCount++;
                                                float percent = 2 - (float)stopPrice / limitPrice - takerFee;
                                                finalPercent *= percent;
                                                finalPercent2 *= 1 + (percent - 1) * leverage;
                                            }
                                            else if (list[i].Close < closePrice)
                                            {
                                                tryCount++;
                                                succeedCount++;
                                                float percent = 2 - (float)closePrice / limitPrice - makerFee;
                                                finalPercent *= percent;
                                                finalPercent2 *= 1 + (percent - 1) * leverage;
                                            }
                                            else
                                            {
                                                position = -1;
                                                positionEntryPrice = limitPrice;
                                            }
                                        }
                                    }
                                }
                                else if (position == 1)
                                {
                                    int closePrice = (int)Math.Floor(positionEntryPrice * (1 + closeX));
                                    int stopPrice = (int)Math.Ceiling(positionEntryPrice * (1 - stopX));
                                    if (list[i].Low < stopPrice)
                                    {
                                        tryCount++;
                                        failedCount++;
                                        float percent = (float)stopPrice / positionEntryPrice - takerFee;
                                        finalPercent *= percent;
                                        finalPercent2 *= 1 + (percent - 1) * leverage;
                                        position = 0;
                                        positionEntryPrice = 0;
                                    }
                                    else if (list[i].High > closePrice)
                                    {
                                        tryCount++;
                                        succeedCount++;
                                        float percent = (float)closePrice / positionEntryPrice - makerFee;
                                        finalPercent *= percent;
                                        finalPercent2 *= 1 + (percent - 1) * leverage;
                                        position = 0;
                                        positionEntryPrice = 0;
                                    }
                                }
                                else if (position == -1)
                                {
                                    int closePrice = (int)Math.Ceiling(positionEntryPrice * (1 - closeX));
                                    int stopPrice = (int)Math.Floor(positionEntryPrice * (1 + stopX));
                                    if (list[i].High > stopPrice)
                                    {
                                        tryCount++;
                                        failedCount++;
                                        float percent = 2 - (float)stopPrice / positionEntryPrice - takerFee;
                                        finalPercent *= percent;
                                        finalPercent2 *= 1 + (percent - 1) * leverage;
                                        position = 0;
                                        positionEntryPrice = 0;
                                    }
                                    else if (list[i].Low < closePrice)
                                    {
                                        tryCount++;
                                        succeedCount++;
                                        float percent = 2 - (float)closePrice / positionEntryPrice - takerFee;
                                        finalPercent *= percent;
                                        finalPercent2 *= 1 + (percent - 1) * leverage;
                                        position = 0;
                                        positionEntryPrice = 0;
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
                                    { "smaLength", smaLength },
                                    { "limitX", limitX },
                                    { "closeX", closeX },
                                    { "stopX", stopX },
                                    { "tryCount", tryCount },
                                    { "succeedCount", succeedCount },
                                    { "failedCount", failedCount },
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
                                logger.WriteLine($"sma = {smaLength} \t limit = {limitX:F4} / {closeX:F4} / {stopX:F4} \t count = {tryCount} / {succeedCount} / {failedCount} / {successRate:F2} \t % = {finalPercent:F4} / {finalPercent2:F4} ({leverage:F2})");
                            }
                            //else if (finalPercent > .5f && succeedCount > 0)
                            //{
                            //    logger.WriteLine($"sma = {smaLength} \t limit = {limitX:F4} / {closeX:F4} / {stopX:F4} \t count = {tryCount} / {succeedCount} / {failedCount} / {successRate:F2} \t % = {finalPercent:F4} / {finalPercent2:F4} ({leverage:F2})", ConsoleColor.DarkGray, false);
                            //}
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
            result += Test(new DateTime(2022, 10, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2022, 9, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 10, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2022, 8, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 9, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
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
            int binSize = 4;
            int rsiLength = 14;

            int rsiValue1 = 69;
            int bbwLength1 = 8;
            float bbwOpen1 = 0.088f;
            float bbwClose1 = 0.085f;
            float closeX1 = 0.06f;
            float stopX1 = 0.015f;

            int rsiValue2 = 30;
            int bbwLength2 = 4;
            float bbwOpen2 = 0.058f;
            float bbwClose2 = 0;
            float closeX2 = 0.07f;
            float stopX2 = 0.015f;

            //const float makerFee = 0.0003f;
            const float takerFee = 0.002f;

            var list1h = Dao.SelectAll(SYMBOL, "1h");
            var list = Loader.LoadBinListFrom1h(binSize, list1h, false);

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
            var macdList = quoteList.GetMacd().ToList();
            macdList.RemoveAll(x => x.Date < startTime || endTime != null && x.Date > endTime.Value);

            var bbwList1 = quoteList.GetBollingerBands(bbwLength1, 2).ToList();
            bbwList1.RemoveAll(x => x.Date < startTime || endTime != null && x.Date > endTime.Value);
            var bbwList2 = quoteList.GetBollingerBands(bbwLength2, 2).ToList();
            bbwList2.RemoveAll(x => x.Date < startTime || endTime != null && x.Date > endTime.Value);

            //for (int i = 0; i < count; i++)
            //{
            //    Console.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t {rsiList[i].Rsi:F4}    {macdList[i].Macd:F4}  /  {macdList[i].Histogram:F4}  /  {macdList[i].Signal:F4}");
            //}

            int tryCount = 0;
            int succeedCount = 0, failedCount = 0;
            float finalPercent = 1, finalPercent2 = 1, finalPercent3 = 1, finalPercent4 = 1, finalPercent5 = 1, finalPercent10 = 1;
            int position = 0;
            int positionEntryPrice = 0;
            int lastPositionEntryPrice = 0;
            for (int i = 2; i < count - 1; i++)
            {
                if (position == 0)
                {
                    if (buyOrSell == 1 || buyOrSell == 3)
                    {
                        //if (macdList[i].Histogram > 0 && bbwList1[i - 1].Width < bbwOpen1 && bbwList1[i].Width >= bbwOpen1)
                        if (macdList[i].Histogram > 0 && macdList[i].Histogram > macdList[i - 1].Histogram && bbwList1[i - 1].Width < bbwOpen1 && bbwList1[i].Width >= bbwOpen1)
                        {
                            lastPositionEntryPrice = list[i].Close;
                            if (rsiList[i].Rsi < rsiValue1)
                            {
                                tryCount++;
                                position = 1;
                                positionEntryPrice = list[i].Close;
                                logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t {rsiList[i].Rsi:F4}    {macdList[i].Macd:F4}  /  {macdList[i].Histogram:F4}  /  {macdList[i].Signal:F4} \t Position = {position} \t Entry = {positionEntryPrice} \t <LONG>");
                            }
                        }
                    }
                    if (buyOrSell == 2 || buyOrSell == 3)
                    {
                        //if (macdList[i].Histogram < 0 && bbwList2[i - 1].Width < bbwOpen2 && bbwList2[i].Width >= bbwOpen2)
                        if (macdList[i].Histogram < 0 && macdList[i].Histogram < macdList[i - 1].Histogram && bbwList2[i - 1].Width < bbwOpen2 && bbwList2[i].Width >= bbwOpen2)
                        {
                            lastPositionEntryPrice = list[i].Close;
                            if (rsiList[i].Rsi > rsiValue2)
                            {
                                tryCount++;
                                position = -1;
                                positionEntryPrice = list[i].Close;
                                logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t {rsiList[i].Rsi:F4}    {macdList[i].Macd:F4}  /  {macdList[i].Histogram:F4}  /  {macdList[i].Signal:F4} \t Position = {position} \t Entry = {positionEntryPrice} \t <SHORT>");
                            }
                        }
                    }
                }
                else if (position == 1)
                {
                    int closePrice = (int)Math.Floor(positionEntryPrice * (1 + closeX1));
                    int stopPrice = (int)Math.Ceiling(positionEntryPrice * (1 - stopX1));
                    if (stopX1 > 0 && list[i].Low < stopPrice)
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
                    else if (closeX1 > 0 && list[i].High > closePrice)
                    {
                        succeedCount++;
                        float percent = (float)closePrice / positionEntryPrice - takerFee;
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
                    else if (macdList[i].Histogram < 0 || bbwList1[i - 1].Width >= bbwClose1 && bbwList1[i].Width < bbwClose1)
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
                        finalPercent10 *= 1 + (percent - 1) * 10;
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
                    if (stopX2 > 0 && list[i].High > stopPrice)
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
                    else if (closeX2 > 0 && list[i].Low < closePrice)
                    {
                        succeedCount++;
                        float percent = 2 - (float)closePrice / positionEntryPrice - takerFee;
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
                    else if (macdList[i].Histogram > 0 || bbwList2[i - 1].Width >= bbwClose2 && bbwList2[i].Width < bbwClose2)
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
                        finalPercent10 *= 1 + (percent - 1) * 10;
                        logger.WriteLine($"     {list[i].Timestamp:MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t Stopped \t <SHORT> \t Entry = {positionEntryPrice} \t Stop = {list[i].Close} \t % = {percent:F4}  /  {finalPercent}", ConsoleColor.DarkYellow);
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