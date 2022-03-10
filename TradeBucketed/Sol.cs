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
            //    DateTime startTime = new DateTime(2022, 2, 18, 0, 0, 0, DateTimeKind.Utc);
            //    DateTime endTime = DateTime.UtcNow;
            //    Load("1m", startTime, endTime);
            //    return;
            //}

            //{
            //    Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  Sol Test");
            //    DateTime startTime = new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc);
            //    List<SolBin> list = SolDao.SelectAll("1m");
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
                DateTime startTime = new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc);
                {
                    //int[] lengthArray = { 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 30, 35, 40, 45, 50, 55, 60, 70, 80, 90, 120, 150, 180, 240, 360, 420, 480, 520, 600, 660, 720, 780, 840, 900, 960 };
                    //foreach (int length in lengthArray)
                    //{
                    //    Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  buy = 1    length = {length}");
                    //    for (float i = 55; i <= 80; i += 1f)
                    //    {
                    //        float result = Test2(length, 2, i, 100, startTime);
                    //        logger.WriteLine($"{startTime:yyyy-MM-dd}    {length} / {i}    result = {result}");
                    //    }
                    //}

                    //int[] lengthArray = { 58 };
                    //foreach (int length in lengthArray)
                    //{
                    //    Logger logger = new Logger($"length = {length}");
                    //    for (float i = 57.3f; i <= 57.5; i += .1f)
                    //    {
                    //        string result = Test2(length, 2, i, 100, startTime);
                    //        logger.WriteLine($"{startTime:yyyy-MM-dd}    length = {length}    {i} ~ 100        {result}");
                    //    }
                    //}

                    //int[] lengthArray = { 58 };
                    //foreach (int length in lengthArray)
                    //{
                    //    Logger logger = new Logger($"length = {length}");
                    //    for (float i = 38.05f; i <= 38.2f; i += .05f)
                    //    {
                    //        string result = Test2(length, 1, 0, i, startTime);
                    //        logger.WriteLine($"{startTime:yyyy-MM-dd}    length = {length}    {i} ~ 100        {result}");
                    //    }
                    //}
                }
                //Test2(58, 2, 60.5f, 63.5f, startTime);
                Test2(58, 1, 26.5f, 29.5f, startTime);
                //TestBuyOrSell3(58, 2, 57.2f, 100, 0.61f, 0.83f, 0.006f, 0.066f, startTime);

                //TestBuyOrSell4(startTime);
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
                    List<TradeBin> list = apiHelper.GetBinList(binSize, false, BitMEXApiHelper.SYMBOL_SOLUSD, 1000, null, null, startTime, nextTime);
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

        const float lossX = 1.5f;

        static string Test2(int rsiLength, int buyOrSell, float minRSI, float maxRSI, DateTime startTime, DateTime? endTime = null)
        {
            List<SolBin> list = SolDao.SelectAll("1m");
            int count = list.Count;
            {
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
            Logger logger = new Logger($"rsiLength = {rsiLength}    buyOrSell = {buyOrSell}    {minRSI} ~ {maxRSI}   ({list[0].Date} ~ {totalDays:N0} days)");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;

            List<Dictionary<string, float>> topList = new List<Dictionary<string, float>>();
            List<Dictionary<string, float>> top0List = new List<Dictionary<string, float>>();

            for (float minDiff = 0; minDiff < 3.5f; minDiff += 0.1f)
            //int minDiff = 0;
            {
                for (float maxDiff = 0.6f; maxDiff < 0.61f; maxDiff += .1f)
                //int maxDiff = 20;
                {
                    logger.WriteLine($"\n\n----    minDiff = {minDiff}    maxDiff = {maxDiff}    ----\n");
                    for (float closeX = 0.0030f; closeX <= 0.0070f; closeX += 0.0010f)
                    //float closeX = 0.005f;
                    {
                        for (float stopX = closeX; stopX <= closeX * 10; stopX += closeX / 2)
                        //float stopX = 0.08f;
                        {
                            int succeedCount = 0, failedCount = 0;
                            float totalProfit = 0;
                            for (int i = 3; i < count - 1; i++)
                            {
                                if (buyOrSell == 1 && list[i - 3].RSI > list[i - 2].RSI && list[i - 2].RSI >= minRSI && list[i - 2].RSI < maxRSI && list[i - 1].RSI - list[i - 2].RSI >= minDiff && (list[i - 1].RSI - list[i - 2].RSI) < maxDiff)
                                {
                                    int positionEntryPrice = list[i].Open;
                                    for (int j = i; j < count; j++)
                                    {
                                        if (list[j].Low < positionEntryPrice - positionEntryPrice * stopX)
                                        {
                                            failedCount++;
                                            totalProfit -= (stopX + 0.001f) * lossX;
                                            positionEntryPrice = 0;
                                            break;
                                        }
                                        else if (list[j].High > positionEntryPrice + positionEntryPrice * closeX)
                                        {
                                            succeedCount++;
                                            totalProfit += closeX - 0.0006f;
                                            positionEntryPrice = 0;
                                            break;
                                        }
                                        //i = j + 1;
                                    }
                                }
                                else if (buyOrSell == 2 && list[i - 3].RSI < list[i - 2].RSI && list[i - 2].RSI >= minRSI && list[i - 2].RSI < maxRSI && list[i - 2].RSI - list[i - 1].RSI >= minDiff && (list[i - 2].RSI - list[i - 1].RSI) < maxDiff)
                                {
                                    int positionEntryPrice = list[i].Open;
                                    for (int j = i; j < count; j++)
                                    {
                                        if (list[j].High > positionEntryPrice + positionEntryPrice * stopX)
                                        {
                                            failedCount++;
                                            totalProfit -= (stopX + 0.001f) * lossX;
                                            positionEntryPrice = 0;
                                            break;
                                        }
                                        else if (list[j].Low < positionEntryPrice - positionEntryPrice * closeX)
                                        {
                                            succeedCount++;
                                            totalProfit += closeX - 0.0006f;
                                            positionEntryPrice = 0;
                                            break;
                                        }
                                        //i = j + 1;
                                    }
                                }
                            }
                            float score;
                            if (failedCount > 0) score = (float)succeedCount / failedCount;
                            else score = succeedCount;
                            float avgProfit = totalProfit / totalDays / closeX;
                            if (avgProfit > 0)
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
                                    while (topListCount > 1000)
                                    {
                                        topList.RemoveAt(0);
                                        topListCount--;
                                    }
                                    for (int i = 0; i < topListCount; i++)
                                    {
                                        if (topList[i]["avgProfit"] > avgProfit ||
                                                topList[i]["avgProfit"] == avgProfit && topList[i]["minDiff"] < minDiff ||
                                                topList[i]["avgProfit"] == avgProfit && topList[i]["minDiff"] == minDiff && topList[i]["maxDiff"] > maxDiff ||
                                                topList[i]["avgProfit"] == avgProfit && topList[i]["minDiff"] == minDiff && topList[i]["maxDiff"] == maxDiff && topList[i]["closeX"] > closeX ||
                                                topList[i]["avgProfit"] == avgProfit && topList[i]["minDiff"] == minDiff && topList[i]["maxDiff"] == maxDiff && topList[i]["closeX"] == closeX && topList[i]["stopX"] < stopX)
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
                                if (failedCount == 0)
                                {
                                    int top0ListCount = top0List.Count;
                                    if (top0ListCount > 0)
                                    {
                                        while (top0ListCount > 1000)
                                        {
                                            top0List.RemoveAt(0);
                                            top0ListCount--;
                                        }
                                        for (int i = 0; i < top0ListCount; i++)
                                        {
                                            if (top0List[i]["avgProfit"] > avgProfit ||
                                                top0List[i]["avgProfit"] == avgProfit && top0List[i]["minDiff"] < minDiff ||
                                                top0List[i]["avgProfit"] == avgProfit && top0List[i]["minDiff"] == minDiff && top0List[i]["maxDiff"] > maxDiff ||
                                                top0List[i]["avgProfit"] == avgProfit && top0List[i]["minDiff"] == minDiff && top0List[i]["maxDiff"] == maxDiff && top0List[i]["closeX"] > closeX ||
                                                top0List[i]["avgProfit"] == avgProfit && top0List[i]["minDiff"] == minDiff && top0List[i]["maxDiff"] == maxDiff && top0List[i]["closeX"] == closeX && top0List[i]["stopX"] < stopX)
                                            {
                                                top0List.Insert(i, dic);
                                                goto top0ListEnd;
                                            }
                                        }
                                        top0List.Add(dic);
                                    top0ListEnd:;
                                    }
                                    else
                                    {
                                        top0List.Add(dic);
                                    }
                                }
                                logger.WriteLine($"{rsiLength}    {minRSI} ~ {maxRSI}    {minDiff} / {maxDiff}    {closeX:F4}    {stopX:F4} \t succeed = {succeedCount} \t failed = {failedCount} \t score = {score:F4} \t total = {totalProfit:F8} \t avg = {avgProfit:F8}");
                            }
                            else
                            {
                                Console.WriteLine($"{rsiLength}    {minRSI} ~ {maxRSI}    {minDiff} / {maxDiff}    {closeX:F4}    {stopX:F4} \t succeed = {succeedCount} \t failed = {failedCount} \t score = {score:F4} \t total = {totalProfit:F8} \t avg = {avgProfit:F8}");
                            }
                        }
                    }
                }
            }
            logger.WriteLine($"\r\n\r\n\r\n\r\ntopList={topList.Count}\r\n" + JArray.FromObject(topList).ToString());
            logger.WriteLine($"\r\n\r\n\r\n\r\ntop0List={top0List.Count}\r\n" + JArray.FromObject(top0List).ToString());
            if (topList.Count > 0)
            {
                var x = topList[topList.Count - 1];
                return $"{x["minDiff"]} / {x["maxDiff"]}    {x["closeX"]:F4}    {x["stopX"]:F4} \t succeed = {x["succeedCount"]} \t failed = {x["failedCount"]} \t score = {x["score"]:F4} \t total = {x["totalProfit"]:F8} \t avg = {x["avgProfit"]:F8}";
            }
            return null;
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
            List<SolBin> list = SolDao.SelectAll("1m");
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

        static float TestBuyOrSell3(int rsiLength, int buyOrSell, float minRSI, float maxRSI, float minDiff, float maxDiff, float closeX, float stopX, DateTime startTime, DateTime? endTime = null)
        {
            List<SolBin> list = SolDao.SelectAll("1m");
            int count = list.Count;
            {
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
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  buyOrSell = {buyOrSell}    minRSI = {minRSI}    maxRSI = {maxRSI}    ({list[0].Date} ~ {totalDays:N0} days)");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;

            int succeedCount = 0, failedCount = 0;
            float totalProfit = 0;
            for (int i = 3; i < count - 1; i++)
            {
                if (buyOrSell == 1 && list[i - 3].RSI > list[i - 2].RSI && list[i - 2].RSI >= minRSI && list[i - 2].RSI < maxRSI && list[i - 1].RSI - list[i - 2].RSI >= minDiff && (list[i - 1].RSI - list[i - 2].RSI) < maxDiff)
                {
                    int positionEntryPrice = list[i].Open;
                    DateTime positionTime = list[i].Timestamp;
                    logger.WriteLine($"{list[i].Timestamp} \t position created: open = {list[i].Open}    high = {list[i].High}    low = {list[i].Low}    close = {list[i].Close}    rsi = {list[i - 2].RSI}    diff = {list[i - 1].RSI - list[i - 2].RSI}");
                    for (int j = i; j < count; j++)
                    {
                        if (list[j].Low < positionEntryPrice - positionEntryPrice * stopX)
                        {
                            failedCount++;
                            totalProfit -= (stopX + 0.001f) * lossX;
                            positionEntryPrice = 0;
                            logger.WriteLine($"{list[j].Timestamp} \t - failed ({(list[j].Timestamp - positionTime).TotalMinutes})\r\n");
                            break;
                        }
                        else if (list[j].High > positionEntryPrice + positionEntryPrice * closeX)
                        {
                            succeedCount++;
                            totalProfit += closeX - 0.0006f;
                            positionEntryPrice = 0;
                            logger.WriteLine($"{list[j].Timestamp} \t succeed ({(list[j].Timestamp - positionTime).TotalMinutes})\r\n");
                            break;
                        }
                    }
                }
                else if (buyOrSell == 2 && list[i - 3].RSI < list[i - 2].RSI && list[i - 2].RSI >= minRSI && list[i - 2].RSI < maxRSI && list[i - 2].RSI - list[i - 1].RSI >= minDiff && (list[i - 2].RSI - list[i - 1].RSI) < maxDiff)
                {
                    int positionEntryPrice = list[i].Open;
                    DateTime positionTime = list[i].Timestamp;
                    logger.WriteLine($"{list[i].Timestamp} \t position created: open = {list[i].Open}    high = {list[i].High}    low = {list[i].Low}    close = {list[i].Close}    rsi = {list[i - 2].RSI}    diff = {list[i - 2].RSI - list[i - 1].RSI}");
                    for (int j = i; j < count; j++)
                    {
                        if (list[j].High > positionEntryPrice + positionEntryPrice * stopX)
                        {
                            failedCount++;
                            totalProfit -= (stopX + 0.001f) * lossX;
                            positionEntryPrice = 0;
                            logger.WriteLine($"{list[j].Timestamp} \t - failed ({(list[j].Timestamp - positionTime).TotalMinutes})\r\n");
                            break;
                        }
                        else if (list[j].Low < positionEntryPrice - positionEntryPrice * closeX)
                        {
                            succeedCount++;
                            totalProfit += closeX - 0.0006f;
                            positionEntryPrice = 0;
                            logger.WriteLine($"{list[j].Timestamp} \t succeed ({(list[j].Timestamp - positionTime).TotalMinutes})\r\n");
                            break;
                        }
                    }
                }
            }
            float score = succeedCount - failedCount * stopX / closeX * lossX;
            float avgProfit = totalProfit / totalDays;
            logger.WriteLine($"\r\n{minRSI} / {maxRSI}    {maxDiff}    {closeX:F4}    {stopX:F4} \t succeed = {succeedCount} \t failed = {failedCount} \t score = {score:F4} \t total = {totalProfit:F8} \t avg = {avgProfit:F8}\r\n");
            return score;
        }

        public class ParamMap
        {
            public int BuyOrSell { get; set; }
            public decimal MinRSI { get; set; }
            public decimal MaxRSI { get; set; }
            public decimal MinDiff { get; set; }
            public decimal MaxDiff { get; set; }
            public decimal CloseX { get; set; }
            public decimal StopX { get; set; }
            public decimal QtyX { get; set; }
        }

        static float TestBuyOrSell4(DateTime startTime, DateTime? endTime = null)
        {
            ParamMap[] paramMapArray;
            {
                string paramText = @"2	85	100	0	10	0.004	0.016	1.5
2	80	85	0	2.8	0.008	0.04	0.5
2	75	80	0	2.8	0.006	0.06	0.4
2	70	75	0.2	2.8	0.008	0.08	0.3
2	67	70	0.4	2.8	0.006	0.06	0.4
1	0	15	0	10	0.008	0.01	1.5
1	15	20	0	4.6	0.008	0.06	0.4
1	20	25	2.9	3.5	0.005	0.05	0.5
1	25	30	2.9	3.5	0.005	0.05	0.5
1	30	35	3	3.5	0.005	0.05	0.5
";
                string[] paramLines = paramText.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                List<ParamMap> paramMapList = new List<ParamMap>();
                foreach (string paramLine in paramLines)
                {
                    string[] paramValues = paramLine.Split(new string[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
                    paramMapList.Add(new ParamMap
                    {
                        BuyOrSell = Int32.Parse(paramValues[0]),
                        MinRSI = decimal.Parse(paramValues[1]),
                        MaxRSI = decimal.Parse(paramValues[2]),
                        MinDiff = decimal.Parse(paramValues[3]),
                        MaxDiff = decimal.Parse(paramValues[4]),
                        CloseX = decimal.Parse(paramValues[5]),
                        StopX = decimal.Parse(paramValues[6]),
                        QtyX = decimal.Parse(paramValues[7]),
                    });
                }
                paramMapArray = paramMapList.ToArray();
            }

            List<SolBin> list = SolDao.SelectAll("1m");
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
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  ({list[0].Date} ~ {totalDays:N0} days)");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;

            int succeedCount = 0, failedCount = 0;
            int position = 0, positionEntryPrice = 0;
            float stopX = 0, closeX = 0;
            DateTime? positionTime = null;
            float totalProfit = 0;
            for (int i = 3; i < count - 1; i++)
            {
                if (position == 0)
                {
                    foreach (ParamMap paramMap in paramMapArray)
                    {
                        if (paramMap.BuyOrSell == 1 && paramMap.QtyX > 0 && list[i - 3].RSI > list[i - 2].RSI && list[i - 2].RSI >= (float)paramMap.MinRSI && list[i - 2].RSI < (float)paramMap.MaxRSI && list[i - 1].RSI - list[i - 2].RSI >= (float)paramMap.MinDiff && (list[i - 1].RSI - list[i - 2].RSI) < (float)paramMap.MaxDiff)
                        {
                            position = 1;
                            positionEntryPrice = list[i].Open;
                            positionTime = list[i].Timestamp;
                            closeX = (float)paramMap.CloseX;
                            stopX = (float)paramMap.StopX;
                            logger.WriteLine($"{list[i].Timestamp} \t position created: open = {list[i].Open}    high = {list[i].High}    low = {list[i].Low}    close = {list[i].Close}    rsi = {list[i - 2].RSI}    diff = {list[i - 1].RSI - list[i - 2].RSI}");
                            i--;
                            break;
                        }
                        if (paramMap.BuyOrSell == 2 && paramMap.QtyX > 0 && list[i - 3].RSI < list[i - 2].RSI && list[i - 2].RSI >= (float)paramMap.MinRSI && list[i - 2].RSI < (float)paramMap.MaxRSI && list[i - 2].RSI - list[i - 1].RSI >= (float)paramMap.MinDiff && (list[i - 2].RSI - list[i - 1].RSI) < (float)paramMap.MaxDiff)
                        {
                            position = -1;
                            positionEntryPrice = list[i].Open;
                            positionTime = list[i].Timestamp;
                            closeX = (float)paramMap.CloseX;
                            stopX = (float)paramMap.StopX;
                            logger.WriteLine($"{list[i].Timestamp} \t position created: open = {list[i].Open}    high = {list[i].High}    low = {list[i].Low}    close = {list[i].Close}    rsi = {list[i - 2].RSI}    diff = {list[i - 1].RSI - list[i - 2].RSI}");
                            i--;
                            break;
                        }
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
            float avgProfit = totalProfit / totalDays;
            logger.WriteLine($"\r\nsucceed = {succeedCount} \t failed = {failedCount} \t total = {totalProfit:F8} \t avg = {avgProfit:F8}\r\n");
            return avgProfit;
        }

    }
}
