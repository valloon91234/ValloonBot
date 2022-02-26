using IO.Swagger.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Valloon.Utils;

namespace Valloon.Trading.Backtest
{
    static class Btc
    {
        public static void Run()
        {
            {
                DateTime startTime = new DateTime(2017, 11, 1, 0, 0, 0, DateTimeKind.Utc);
                DateTime endTime = DateTime.UtcNow;
                Load("1h", startTime, endTime);
                return;
            }

            {
                Logger logger1 = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  Xbt Buy = 1");
                Logger logger2 = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  Xbt Sell = 2");
                DateTime startTime = new DateTime(2021, 10, 1, 0, 0, 0, DateTimeKind.Utc);
                List<BtcBin> list = BtcDao.SelectAll("1m");
                //int[] lengthArray = { 5, 10, 15, 20, 25, 30, 45, 50, 55, 60, 65, 70, 75, 80, 85, 90, 120, 150, 180, 240, 360, 420, 480, 520, 600, 660, 720, 780, 840, 900, 960 };
                int[] lengthArray = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 20, 25, 30, 45, 50, 55, 60, 65, 70, 75, 80, 85, 90, 120, 150, 180, 240, 360, 420, 480, 520, 600, 660, 720, 780, 840, 900, 960 };
                foreach (int length in lengthArray)
                {
                    float result1 = Test(list, length, 1, startTime);
                    logger1.WriteLine($"{startTime:yyyy-MM-dd}    length = {length}    result = {result1:F8}");

                    float result2 = Test(list, length, 2, startTime);
                    logger2.WriteLine($"{startTime:yyyy-MM-dd}    length = {length}    result = {result2:F8}");
                }
                //Test(list, 1, 1, startTime);
                //Test(list, 1, 2, startTime);
                //TestBuyOrSell(null, 1, 1, 0.0082f, 0.0076f, 0.0019f, startTime, null);
                //TestBuyOrSell(null, 1, 2, 0.0082f, 0.0076f, 0.0019f, startTime, null);
                return;
            }

            //{
            //    DateTime startTime = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            //    TestBuyOrSell(null, startTime, 1, 0, 660, 0.009m, 0.95m, 6.2m);
            //    goto end;
            //}



            //int[] lengthArray = { 5, 10, 15, 20, 25, 30, 40, 50, 60, 90, 120, 180, 240, 360, 420, 480, 520, 600, 660, 720, 780, 840, 900, 960 };
            //int[] lengthArray = { 660 };
            //decimal[] stopXArray = { 1m / 10, 1m / 8, 1m / 6, 1m / 5, 1m / 4, 1m / 3, 1m / 2, 2m / 3, 1, 3m / 2, 2, 3, 4, 5, 6, 8, 10 };
            //decimal[] stopXArray = { 1m / 5, 1m / 4, 1m / 3, 1m / 2, 2m / 3, 1, 3m / 2, 2, 3, 4, 5, 6, 8, 10 };
            //decimal[] stopXArray = { 6.5m, 6, 5.5m, 5 };
            //foreach (int length in lengthArray)
            //    foreach (decimal stopX in stopXArray)
            //    {
            //        //{
            //        //    DateTime startTime = new DateTime(2021, 4, 1, 0, 0, 0, DateTimeKind.Utc);
            //        //    Test2(startTime, 1, length, stopX);
            //        //}
            //        {
            //            DateTime startTime = new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc);
            //            Test2(startTime, 1, length, stopX);
            //        }
            //        {
            //            DateTime startTime = new DateTime(2021, 10, 1, 0, 0, 0, DateTimeKind.Utc);
            //            Test2(startTime, 1, length, stopX);
            //        }
            //        {
            //            DateTime startTime = new DateTime(2021, 12, 1, 0, 0, 0, DateTimeKind.Utc);
            //            Test2(startTime, 1, length, stopX);
            //        }
            //        {
            //            DateTime startTime = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            //            Test2(startTime, 1, length, stopX);
            //        }
            //    }

            List<BtcBin> getList(DateTime startTime, DateTime? endTime = null)
            {
                List<BtcBin> list = BtcDao.SelectAll("1m");
                {
                    int mLength = 660;
                    int countAll = list.Count;
                    for (int i = mLength; i < countAll; i++)
                    {
                        float[] closeArray = new float[mLength];
                        for (int j = 0; j < mLength; j++)
                            closeArray[j] = list[i - mLength + j].Close;
                        list[i].SMA = closeArray.Average();
                    }
                    //int removeCount = 0;
                    //for (int i = 0; i < countAll - 1; i++)
                    //{
                    //    if (list[i].Timestamp < startTime)
                    //        removeCount++;
                    //    else
                    //        break;
                    //}
                    //list.RemoveRange(0, removeCount);
                    list.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
                }
                Console.WriteLine($"----    Ready from {list[0].Date}    ----");
                return list;
            }

            void runHCS(DateTime startTime, DateTime? endTime = null)
            {
                List<BtcBin> list = getList(startTime, endTime);
                //TestHCS(list, 1);
                //Test(list, 2);
            }

            runHCS(new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc));
            runHCS(new DateTime(2021, 10, 1, 0, 0, 0, DateTimeKind.Utc));
            runHCS(new DateTime(2021, 12, 1, 0, 0, 0, DateTimeKind.Utc));
            runHCS(new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc));


            ThreadPool.QueueUserWorkItem(state => runHCS(new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc)));
            ThreadPool.QueueUserWorkItem(state => runHCS(new DateTime(2021, 8, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc)));
            ThreadPool.QueueUserWorkItem(state => runHCS(new DateTime(2021, 9, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc)));
            ThreadPool.QueueUserWorkItem(state => runHCS(new DateTime(2021, 10, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc)));
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
                    List<TradeBin> list = apiHelper.GetBinList(binSize, false, BitMEXApiHelper.SYMBOL_XBTUSD, 1000, null, startTime, nextTime);
                    int count = list.Count;
                    for (int i = 0; i < count - 1; i++)
                    {
                        TradeBin t = list[i];
                        try
                        {
                            BtcBin b = new BtcBin(t);
                            BtcDao.Insert(b, binSize);
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

        const float lossX = 1.5f;

        static float Test(List<BtcBin> listFull, int smaLength, int buyOrSell, DateTime startTime, DateTime? endTime = null)
        {
            List<BtcBin> list;
            if (listFull == null)
                list = BtcDao.SelectAll("1m");
            else
                list = new List<BtcBin>(listFull);
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
            int totalDays = count / 60 / 24;
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  smaLength = {smaLength}    buyOrSell = {buyOrSell}   ({list[0].Date} ~ {totalDays:N0} days)");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;

            List<Dictionary<string, float>> topList = new List<Dictionary<string, float>>();

            for (float heightX = 0.0050f; heightX <= 0.0150f; heightX += 0.0005f)
            {
                logger.WriteLine($"\n\n----    heightX = {heightX}    ----\n");
                for (float closeX = 0.0050f; closeX <= heightX; closeX += 0.0005f)
                {
                    for (float stopX = 0.0020f; stopX <= closeX * 3; stopX += 0.0005f)
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

        static float TestBuyOrSell(List<BtcBin> list, int smaLength, int buyOrSell, float heightX, float closeX, float stopX, DateTime startTime, DateTime? endTime)
        {
            int count;
            if (list == null)
            {
                list = BtcDao.SelectAll("1m");
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

    }
}
