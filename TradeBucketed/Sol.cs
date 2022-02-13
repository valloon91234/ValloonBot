using IO.Swagger.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Valloon.Utils;

namespace Valloon.Trading.Backtest
{
    static class Sol
    {
        public static void Run()
        {
            //DateTime startTime = new DateTime(2021, 10, 21, 0, 0, 0, DateTimeKind.Utc);
            //{
            //    DateTime startTime = new DateTime(2022, 2, 11, 0, 0, 0, DateTimeKind.Utc);
            //    DateTime endTime = DateTime.UtcNow;
            //    Load("5m", startTime, endTime);
            //    return;
            //}

            //{
            //    Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  Sol Test");
            //    DateTime startTime = new DateTime(2021, 12, 1, 0, 0, 0, DateTimeKind.Utc);
            //    List<SolBin> list = SolDao.SelectAll("5m");
            //    //int[] lengthArray = { 5, 10, 15, 20, 25, 30, 45, 60, 90, 120, 150, 180, 240, 360, 420, 480, 520, 600, 660, 720, 780, 840, 900, 960 };
            //    int[] lengthArray = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 };
            //    foreach (int length in lengthArray)
            //    {
            //        float result = Test(list, length, 1, startTime);
            //        logger.WriteLine($"{startTime:yyyy-MM-dd}    length = {length}    result = {result}");
            //    }
            //    return;
            //}

            {
                DateTime startTime = new DateTime(2022, 2, 1, 0, 0, 0, DateTimeKind.Utc);
                //{
                //    Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  buy = 1");
                //    for (float i = 35; i <= 36; i += 0.1f)
                //    {
                //        float result = Test2(1, 70, i, startTime);
                //        logger.WriteLine($"{startTime:yyyy-MM-dd}    {i}    result = {result}");
                //    }
                //}
                //{
                //    Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  Sell = 2");
                //    for (int i = 50; i <= 70; i += 1)
                //    {
                //        float result = Test2(2, i, 30, startTime);
                //        logger.WriteLine($"{startTime:yyyy-MM-dd}    {i}    result = {result}");
                //    }
                //}

                //{
                //    Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  Sell = 2");
                //    for (float i = 59; i <= 75; i += .5f)
                //    {
                //        float result = Test2(2, i, 30, startTime);
                //        logger.WriteLine($"{startTime:yyyy-MM-dd}    {i}    result = {result}");
                //    }
                //}

                //Test2(1, 35.8f, 35.8f, startTime);
                //Test2(2, 59.8f, 59.8f, startTime);

                //DateTime startTime = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                TestBuyOrSell2(1, 35.6f, 35.6f, 2.94f, 3.57f, 0.005f, .05f, startTime);
                TestBuyOrSell2(2, 60.5f, 60.5f, 0.71f, 1.54f, 0.008f, 0.08f, startTime);
                return;
            }

            //ThreadPool.QueueUserWorkItem(state => runHCS(new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc)));
        }

        static void Load(string binSize, DateTime startTime, DateTime endTime)
        {
            BitMEXApiHelper apiHelper = new BitMEXApiHelper();
            while (true)
            {
                try
                {
                    DateTime nextTime;
                    switch (binSize)
                    {
                        case "1m":
                            nextTime = startTime.AddHours(12);
                            break;
                        case "5m":
                            nextTime = startTime.AddDays(3);
                            break;
                        case "1h":
                            nextTime = startTime.AddDays(40);
                            break;
                        default:
                            Console.WriteLine($"Invalid bin_size: {binSize}");
                            return;
                    }
                    if (startTime > endTime)
                    {
                        Console.WriteLine($"end: nextTime = {startTime:yyyy-MM-dd HH:mm:ss} > {endTime:yyyy-MM-dd HH:mm:ss}");
                        break;
                    }
                    List<TradeBin> list = apiHelper.GetBinList(binSize, false, BitMEXApiHelper.SYMBOL_SOLUSD, 1000, null, startTime, nextTime);
                    int count = list.Count;
                    for (int i = 0; i < count - 1; i++)
                    {
                        TradeBin t = list[i];
                        try
                        {
                            SolBin b = new SolBin(t);
                            SolDao.Insert(b, binSize);
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.ContainsIgnoreCase("UNIQUE constraint failed:"))
                                Console.WriteLine($"Failed: {t.Timestamp:yyyy-MM-dd HH:mm:ss} - Already exists.");
                            else
                                Console.WriteLine($"Failed: {t.Timestamp:yyyy-MM-dd HH:mm:ss}\r\n{ex.StackTrace}");
                        }
                    }
                    Console.WriteLine($"Inserted: {startTime:yyyy-MM-dd HH:mm:ss}");
                    startTime = nextTime;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex.Message}");
                    Thread.Sleep(5000);
                }
            }
        }

        const float lossX = 1.2f;

        static float Test(List<SolBin> listFull, int smaLength, int buyOrSell, DateTime startTime, DateTime? endTime = null)
        {
            List<SolBin> list;
            if (listFull == null)
                list = SolDao.SelectAll("1m");
            else
                list = new List<SolBin>(listFull);
            {
                int countAll = list.Count;
                for (int i = smaLength; i < countAll; i++)
                {
                    float[] closeArray = new float[smaLength];
                    for (int j = 0; j < smaLength; j++)
                        closeArray[j] = list[i - smaLength + j].Close;
                    list[i].SMA = closeArray.Average();
                }
                list.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
            }
            int count = list.Count;
            int totalDays = (int)(list[count - 1].Timestamp - list[0].Timestamp).TotalDays;
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  smaLength = {smaLength}    buyOrSell = {buyOrSell}   ({list[0].Date} ~ {totalDays:N0} days)");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;

            List<Dictionary<string, float>> topList = new List<Dictionary<string, float>>();

            for (float heightX = 0.0050f; heightX <= 0.0300f; heightX += 0.0010f)
            {
                logger.WriteLine($"\n\n----    heightX = {heightX}    ----\n");
                for (float closeX = 0.0050f; closeX <= heightX * 1.5; closeX += 0.0010f)
                {
                    for (float stopX = 0.0050f; stopX <= closeX * 3; stopX += 0.0010f)
                    {
                        int succeedCount = 0, failedCount = 0;
                        float positionEntryPrice = 0, positionCloseHeight = 0, positionStopHeight = 0, closePrice = 0, stopPrice = 0;
                        float totalProfit = 0;
                        for (int i = 0; i < count - 1; i++)
                        {
                            float sma = list[i].SMA;
                            float height = sma * heightX;
                            float closeHeight = sma * closeX;
                            float stopHeight = sma * stopX;
                            if (positionCloseHeight == 0)
                            {
                                if (buyOrSell == 1)
                                {
                                    if (list[i].Open > sma - height && list[i].Low < sma - height)
                                    {
                                        if (list[i].Low < sma - height - stopHeight)
                                        {
                                            failedCount++;
                                            totalProfit -= stopHeight * lossX / (sma - height);
                                        }
                                        else if (list[i].Close > sma - height + closeHeight)
                                        {
                                            succeedCount++;
                                            totalProfit += closeHeight / (sma - height);
                                        }
                                        else
                                        {
                                            positionEntryPrice = sma - height;
                                            positionCloseHeight = closeHeight;
                                            positionStopHeight = stopHeight;
                                            closePrice = sma - height + closeHeight;
                                            stopPrice = sma - height - stopHeight;
                                        }
                                    }
                                }
                                else
                                {
                                    if (list[i].Open < sma + height && list[i].High > sma + height)
                                    {
                                        if (list[i].High > sma + height + stopHeight)
                                        {
                                            failedCount++;
                                            totalProfit -= stopHeight * lossX / (sma + height);
                                        }
                                        else if (list[i].Close < sma + height - closeHeight)
                                        {
                                            succeedCount++;
                                            totalProfit += closeHeight / (sma + height);
                                        }
                                        else
                                        {
                                            positionEntryPrice = sma + height;
                                            positionCloseHeight = closeHeight;
                                            positionStopHeight = stopHeight;
                                            closePrice = sma + height - closeHeight;
                                            stopPrice = sma + height + stopHeight;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (buyOrSell == 1)
                                {
                                    if (list[i].Low < stopPrice)
                                    {
                                        failedCount++;
                                        totalProfit -= positionStopHeight * lossX / positionEntryPrice;
                                        positionEntryPrice = 0;
                                        positionCloseHeight = 0;
                                        closePrice = 0;
                                        stopPrice = 0;
                                    }
                                    else if (list[i].High > closePrice)
                                    {
                                        succeedCount++;
                                        totalProfit += positionCloseHeight / positionEntryPrice;
                                        positionEntryPrice = 0;
                                        positionCloseHeight = 0;
                                        closePrice = 0;
                                        stopPrice = 0;
                                    }
                                }
                                else
                                {
                                    if (list[i].High > stopPrice)
                                    {
                                        failedCount++;
                                        totalProfit -= positionStopHeight * lossX / positionEntryPrice;
                                        positionEntryPrice = 0;
                                        positionCloseHeight = 0;
                                        closePrice = 0;
                                        stopPrice = 0;
                                    }
                                    else if (list[i].Low < closePrice)
                                    {
                                        succeedCount++;
                                        totalProfit += positionCloseHeight / positionEntryPrice;
                                        positionEntryPrice = 0;
                                        positionCloseHeight = 0;
                                        closePrice = 0;
                                        stopPrice = 0;
                                    }
                                }
                            }
                        }
                        float score = succeedCount - failedCount * stopX / closeX * lossX;
                        //if (failedCount == 0)
                        //    score = succeedCount;
                        //else
                        //    score = (float)succeedCount / failedCount;
                        float avgProfit = totalProfit / totalDays;
                        if (avgProfit > 0)
                        {
                            Dictionary<string, float> dic = new Dictionary<string, float>
                            {
                                { "heightX", heightX },
                                { "closeX", closeX },
                                { "stopX", stopX },
                                { "succeedCount", succeedCount },
                                { "failedCount", failedCount },
                                { "score", score },
                                { "totalProfit", totalProfit },
                                { "avgProfit", avgProfit },
                            };
                            int topListCount = topList.Count;
                            if (topListCount > 0)
                            {
                                while (topListCount > 100)
                                {
                                    topList.RemoveAt(0);
                                    topListCount--;
                                }
                                for (int i = 0; i < topListCount; i++)
                                {
                                    if (topList[i]["score"] > score)
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
                            logger.WriteLine($"{smaLength}    {heightX:F4}    {closeX:F4}    {stopX:F4} \t succeed = {succeedCount} \t failed = {failedCount} \t score = {score:F4} \t total = {totalProfit:F8} \t avg = {avgProfit:F8}");
                        }
                        else
                        {
                            Console.WriteLine($"{smaLength}    {heightX:F4}    {closeX:F4}    {stopX:F4} \t succeed = {succeedCount} \t failed = {failedCount} \t score = {score:F4} \t total = {totalProfit:F8} \t avg = {avgProfit:F8}");
                        }
                    }
                }
            }
            logger.WriteLine(JArray.FromObject(topList).ToString());
            logger.WriteLine();
            if (topList.Count > 0)
                return topList[topList.Count - 1]["score"];
            return 0;
        }

        static float Test2(int buyOrSell, float upperRSI, float lowerRSI, DateTime startTime, DateTime? endTime = null)
        {
            List<SolBin> list = SolDao.SelectAll("5m");
            int count = list.Count;
            {
                const int rsiLength = 14;
                List<TradeBin> binList = new List<TradeBin>();
                foreach (SolBin m in list)
                {
                    binList.Add(new TradeBin(m.Timestamp, BitMEXApiHelper.SYMBOL_SOLUSD, m.Open, m.High, m.Low, m.Close));
                }
                double[] rsiArray = RSI.CalculateRSIValues(binList.ToArray(), rsiLength);
                for (int i = 0; i < count; i++)
                {
                    SolBin m = list[i];
                    m.RSI = (float)rsiArray[i];
                }
                list.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
                count = list.Count;
            }
            int totalDays = (int)(list[count - 1].Timestamp - list[0].Timestamp).TotalDays;
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  buyOrSell = {buyOrSell}    {upperRSI} - {lowerRSI}   ({list[0].Date} ~ {totalDays:N0} days)");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;

            List<Dictionary<string, float>> topList = new List<Dictionary<string, float>>();

            for (float minDiff = 2.9f; minDiff <= 3.0f; minDiff += 0.01f)
            {
                for (float maxDiff = 3.5f; maxDiff <= 3.6; maxDiff += 0.01f)
                //int maxDiff = 12;
                {
                    logger.WriteLine($"\n\n----    minDiff = {minDiff}    maxDiff = {maxDiff}    ----\n");
                    for (float closeX = 0.0050f; closeX <= 0.0070f; closeX += 0.0010f)
                    //float closeX = 0.005f;
                    {
                        for (float stopX = 0.0400f; stopX <= closeX * 10; stopX += 0.0050f)
                        //float stopX = 0.08f;
                        {
                            int succeedCount = 0, failedCount = 0;
                            int position = 0, positionEntryPrice = 0;
                            float totalProfit = 0;
                            for (int i = 3; i < count - 1; i++)
                            {
                                if (position == 0)
                                {
                                    if (buyOrSell == 1 && list[i - 3].RSI > list[i - 2].RSI && list[i - 2].RSI < lowerRSI && list[i - 1].RSI - list[i - 2].RSI >= minDiff && (list[i - 1].RSI - list[i - 2].RSI) < maxDiff)
                                    {
                                        position = 1;
                                        positionEntryPrice = list[i].Open;
                                        i--;
                                    }
                                    else if (buyOrSell == 2 && list[i - 3].RSI < list[i - 2].RSI && list[i - 2].RSI > upperRSI && list[i - 2].RSI - list[i - 1].RSI >= minDiff && (list[i - 2].RSI - list[i - 1].RSI) < maxDiff)
                                    {
                                        position = -1;
                                        positionEntryPrice = list[i].Open;
                                        i--;
                                    }
                                }
                                else if (position > 0)
                                {
                                    if (list[i].Low < positionEntryPrice - positionEntryPrice * stopX)
                                    {
                                        failedCount++;
                                        totalProfit -= (stopX + 0.001f) * lossX;
                                        position = 0;
                                        positionEntryPrice = 0;
                                    }
                                    else if (list[i].High > positionEntryPrice + positionEntryPrice * closeX)
                                    {
                                        succeedCount++;
                                        totalProfit += closeX - 0.0006f;
                                        position = 0;
                                        positionEntryPrice = 0;
                                    }
                                }
                                else if (position < 0)
                                {
                                    if (list[i].High > positionEntryPrice + positionEntryPrice * stopX)
                                    {
                                        failedCount++;
                                        totalProfit -= (stopX + 0.001f) * lossX;
                                        position = 0;
                                        positionEntryPrice = 0;
                                    }
                                    else if (list[i].Low < positionEntryPrice - positionEntryPrice * closeX)
                                    {
                                        succeedCount++;
                                        totalProfit += closeX - 0.0006f;
                                        position = 0;
                                        positionEntryPrice = 0;
                                    }
                                }
                            }
                            //float score = succeedCount - failedCount * stopX / closeX * lossX;
                            float score;
                            if (failedCount > 0) score = (float)succeedCount / failedCount;
                            else score = succeedCount;
                            float avgProfit = totalProfit / totalDays / closeX;
                            if (avgProfit > 0 && failedCount == 0)
                            {
                                Dictionary<string, float> dic = new Dictionary<string, float>
                                {
                                    { "minDiff", minDiff },
                                    { "maxDiff", maxDiff },
                                    { "closeX", closeX },
                                    { "stopX", stopX },
                                    { "succeedCount", succeedCount },
                                    { "failedCount", failedCount },
                                    { "score", score },
                                    { "totalProfit", totalProfit },
                                    { "avgProfit", avgProfit },
                                };
                                int topListCount = topList.Count;
                                if (topListCount > 0)
                                {
                                    while (topListCount > 100)
                                    {
                                        topList.RemoveAt(0);
                                        topListCount--;
                                    }
                                    for (int i = 0; i < topListCount; i++)
                                    {
                                        if (topList[i]["avgProfit"] > avgProfit)
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
                                logger.WriteLine($"{upperRSI} / {lowerRSI}    {minDiff} / {maxDiff}    {closeX:F4}    {stopX:F4} \t succeed = {succeedCount} \t failed = {failedCount} \t score = {score:F4} \t total = {totalProfit:F8} \t avg = {avgProfit:F8}");
                            }
                            else
                            {
                                Console.WriteLine($"{upperRSI} / {lowerRSI}    {minDiff} / {maxDiff}    {closeX:F4}    {stopX:F4} \t succeed = {succeedCount} \t failed = {failedCount} \t score = {score:F4} \t total = {totalProfit:F8} \t avg = {avgProfit:F8}");
                            }
                        }
                    }
                }
            }
            logger.WriteLine("\r\n" + JArray.FromObject(topList).ToString());
            if (topList.Count > 0)
                return topList[topList.Count - 1]["avgProfit"];
            return 0;
        }

        static float TestBuyOrSell(List<SolBin> list, int smaLength, int buyOrSell, float heightX, float closeX, float stopX, DateTime startTime, DateTime? endTime = null)
        {
            int count;
            if (list == null)
            {
                list = SolDao.SelectAll("1m");
                count = list.Count;
                for (int i = smaLength; i < count; i++)
                {
                    float[] closeArray = new float[smaLength];
                    for (int j = 0; j < smaLength; j++)
                        closeArray[j] = list[i - smaLength + j].Close;
                    list[i].SMA = closeArray.Average();
                }
                list.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
                count = list.Count;
            }
            else
            {
                count = list.Count;
            }

            int totalDays = count / 60 / 24;
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  smaLength = {smaLength}    buyOrSell = {buyOrSell}   ({list[0].Date} ~ {totalDays:N0} days)");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;

            int succeedCount = 0, failedCount = 0;
            float positionEntryPrice = 0, positionCloseHeight = 0, positionStopHeight = 0, closePrice = 0, stopPrice = 0;
            float totalProfit = 0;
            for (int i = 1; i < count - 1; i++)
            {
                float sma = list[i].SMA;
                float height = sma * heightX;
                float closeHeight = sma * closeX;
                float stopHeight = sma * stopX;
                if (positionCloseHeight == 0)
                {
                    if (buyOrSell == 1)
                    {
                        if (list[i].Open > sma - height && list[i].Low < sma - height)
                        {
                            if (list[i].Low < sma - height - stopHeight)
                            {
                                failedCount++;
                                totalProfit -= stopHeight * lossX / (sma - height);
                                logger.WriteLine($"{list[i].Timestamp} \t - failed: open = {list[i].Open}    high = {list[i].High}    low = {list[i].Low}    close = {list[i].Close}");
                            }
                            else if (list[i].Close > sma - height + closeHeight)
                            {
                                succeedCount++;
                                totalProfit += closeHeight / (sma - height);
                                logger.WriteLine($"{list[i].Timestamp} \t succeed: open = {list[i].Open}    high = {list[i].High}    low = {list[i].Low}    close = {list[i].Close}");
                            }
                            else
                            {
                                positionEntryPrice = sma - height;
                                positionCloseHeight = closeHeight;
                                positionStopHeight = stopHeight;
                                closePrice = sma - height + closeHeight;
                                stopPrice = sma - height - stopHeight;
                                logger.WriteLine($"{list[i].Timestamp} \t position created: open = {list[i].Open}    high = {list[i].High}    low = {list[i].Low}    close = {list[i].Close}");
                            }
                        }
                    }
                    else
                    {
                        if (list[i].Open < sma + height && list[i].High > sma + height)
                        {
                            if (list[i].High > sma + height + stopHeight)
                            {
                                failedCount++;
                                totalProfit -= stopHeight * lossX / (sma + height);
                                logger.WriteLine($"{list[i].Timestamp} \t - failed: open = {list[i].Open}    high = {list[i].High}    low = {list[i].Low}    close = {list[i].Close}");
                            }
                            else if (list[i].Close < sma + height - closeHeight)
                            {
                                succeedCount++;
                                totalProfit += closeHeight / (sma + height);
                                logger.WriteLine($"{list[i].Timestamp} \t succeed: open = {list[i].Open}    high = {list[i].High}    low = {list[i].Low}    close = {list[i].Close}");
                            }
                            else
                            {
                                positionEntryPrice = sma + height;
                                positionCloseHeight = closeHeight;
                                positionStopHeight = stopHeight;
                                closePrice = sma + height - closeHeight;
                                stopPrice = sma + height + stopHeight;
                                logger.WriteLine($"{list[i].Timestamp} \t position created: open = {list[i].Open}    high = {list[i].High}    low = {list[i].Low}    close = {list[i].Close}");
                            }
                        }
                    }
                }
                else
                {
                    if (buyOrSell == 1)
                    {
                        if (list[i].Low < stopPrice)
                        {
                            failedCount++;
                            totalProfit -= positionStopHeight * lossX / positionEntryPrice;
                            positionCloseHeight = 0;
                            closePrice = 0;
                            stopPrice = 0;
                            logger.WriteLine($"{list[i].Timestamp} \t - failed");
                        }
                        else if (list[i].High > closePrice)
                        {
                            succeedCount++;
                            totalProfit += positionCloseHeight / positionEntryPrice;
                            positionCloseHeight = 0;
                            closePrice = 0;
                            stopPrice = 0;
                            logger.WriteLine($"{list[i].Timestamp} \t succeed");
                        }
                    }
                    else
                    {
                        if (list[i].High > stopPrice)
                        {
                            failedCount++;
                            totalProfit -= positionStopHeight * lossX / positionEntryPrice;
                            positionCloseHeight = 0;
                            closePrice = 0;
                            stopPrice = 0;
                            logger.WriteLine($"{list[i].Timestamp} \t - failed");
                        }
                        else if (list[i].Low < closePrice)
                        {
                            succeedCount++;
                            totalProfit += positionCloseHeight / positionEntryPrice;
                            positionCloseHeight = 0;
                            closePrice = 0;
                            stopPrice = 0;
                            logger.WriteLine($"{list[i].Timestamp} \t succeed");
                        }
                    }
                }
            }
            float score = succeedCount - failedCount * stopX / closeX * lossX;
            float avgProfit = totalProfit / totalDays;
            logger.WriteLine($"{smaLength}    {heightX:F4}    {closeX:F4}    {stopX:F4} \t succeed = {succeedCount} \t failed = {failedCount} \t score = {score:F4} \t total = {totalProfit:F8} \t avg = {avgProfit:F8}");
            return score;
        }

        static float TestBuyOrSell2(int buyOrSell, float upperRSI, float lowerRSI, float minDiff, float maxDiff, float closeX, float stopX, DateTime startTime, DateTime? endTime = null)
        {
            List<SolBin> list = SolDao.SelectAll("5m");
            int count = list.Count;
            {
                const int rsiLength = 14;
                List<TradeBin> binList = new List<TradeBin>();
                foreach (SolBin m in list)
                {
                    binList.Add(new TradeBin(m.Timestamp, BitMEXApiHelper.SYMBOL_SOLUSD, m.Open, m.High, m.Low, m.Close));
                }
                double[] rsiArray = RSI.CalculateRSIValues(binList.ToArray(), rsiLength);
                for (int i = 0; i < count; i++)
                {
                    SolBin m = list[i];
                    m.RSI = (float)rsiArray[i];
                }
                list.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
                count = list.Count;
            }
            int totalDays = (int)(list[count - 1].Timestamp - list[0].Timestamp).TotalDays;
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  buyOrSell = {buyOrSell}   ({list[0].Date} ~ {totalDays:N0} days)");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;

            int succeedCount = 0, failedCount = 0;
            int position = 0, positionEntryPrice = 0;
            DateTime? positionTime = null;
            float totalProfit = 0;
            for (int i = 3; i < count - 1; i++)
            {
                if (position == 0)
                {
                    if (buyOrSell == 1 && list[i - 3].RSI > list[i - 2].RSI && list[i - 2].RSI < lowerRSI && list[i - 1].RSI - list[i - 2].RSI >= minDiff && (list[i - 1].RSI - list[i - 2].RSI) < maxDiff)
                    {
                        position = 1;
                        positionEntryPrice = list[i].Open;
                        positionTime = list[i].Timestamp;
                        logger.WriteLine($"{list[i].Timestamp} \t position created: open = {list[i].Open}    high = {list[i].High}    low = {list[i].Low}    close = {list[i].Close}    rsi = {list[i - 2].RSI}    diff = {list[i - 1].RSI - list[i - 2].RSI}");
                        i--;
                    }
                    else if (buyOrSell == 2 && list[i - 3].RSI < list[i - 2].RSI && list[i - 2].RSI > upperRSI && list[i - 2].RSI - list[i - 1].RSI >= minDiff && (list[i - 2].RSI - list[i - 1].RSI) < maxDiff)
                    {
                        position = -1;
                        positionEntryPrice = list[i].Open;
                        positionTime = list[i].Timestamp;
                        logger.WriteLine($"{list[i].Timestamp} \t position created: open = {list[i].Open}    high = {list[i].High}    low = {list[i].Low}    close = {list[i].Close}    rsi = {list[i - 2].RSI}    diff = {list[i - 1].RSI - list[i - 2].RSI}");
                        i--;
                    }
                }
                else if (position > 0)
                {
                    if (list[i].Low < positionEntryPrice - positionEntryPrice * stopX)
                    {
                        failedCount++;
                        totalProfit -= (stopX + 0.001f) * lossX;
                        position = 0;
                        positionEntryPrice = 0;
                        logger.WriteLine($"{list[i].Timestamp} \t - failed ({(list[i].Timestamp - positionTime.Value).TotalMinutes})\r\n");
                        positionTime = null;
                    }
                    else if (list[i].High > positionEntryPrice + positionEntryPrice * closeX)
                    {
                        succeedCount++;
                        totalProfit += closeX - 0.0006f;
                        position = 0;
                        positionEntryPrice = 0;
                        logger.WriteLine($"{list[i].Timestamp} \t succeed ({(list[i].Timestamp - positionTime.Value).TotalMinutes})\r\n");
                        positionTime = null;
                    }
                }
                else if (position < 0)
                {
                    if (list[i].High > positionEntryPrice + positionEntryPrice * stopX)
                    {
                        failedCount++;
                        totalProfit -= (stopX + 0.001f) * lossX;
                        position = 0;
                        positionEntryPrice = 0;
                        logger.WriteLine($"{list[i].Timestamp} \t - failed ({(list[i].Timestamp - positionTime.Value).TotalMinutes})\r\n");
                        positionTime = null;
                    }
                    else if (list[i].Low < positionEntryPrice - positionEntryPrice * closeX)
                    {
                        succeedCount++;
                        totalProfit += closeX - 0.0006f;
                        position = 0;
                        positionEntryPrice = 0;
                        logger.WriteLine($"{list[i].Timestamp} \t succeed ({(list[i].Timestamp - positionTime.Value).TotalMinutes})\r\n");
                        positionTime = null;
                    }
                }
            }
            float score = succeedCount - failedCount * stopX / closeX * lossX;
            float avgProfit = totalProfit / totalDays;
            logger.WriteLine($"\r\n{upperRSI} / {lowerRSI}    {maxDiff}    {closeX:F4}    {stopX:F4} \t succeed = {succeedCount} \t failed = {failedCount} \t score = {score:F4} \t total = {totalProfit:F8} \t avg = {avgProfit:F8}\r\n");
            return score;
        }

    }
}
