using IO.Swagger.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Valloon.BitMEX.dao;

namespace Valloon.BitMEX
{
    class Program
    {
        static void Main(string[] args)
        {
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            //BB("1m", 30, 40);
            //BB("1m", 30, 0.004d / 3);

            //{
            //    DateTime startTime = new DateTime(2022, 1, 27, 0, 0, 0, DateTimeKind.Utc);
            //    DateTime endTime = DateTime.UtcNow;
            //    Load_1m(startTime, endTime);
            //    goto end;
            //}

            //{
            //    DateTime startTime = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            //    TestBuyOrSell(null, startTime, 1, 0, 660, 0.009m, 0.95m, 6.2m);
            //    goto end;
            //}

            //BB("1m", 20, 0.004d / 3);
            //Test1("1m", 30);

            //WriteSMA("1m", 660);
            //goto end;


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

            List<TradeBinBB> getList(DateTime startTime, DateTime? endTime = null)
            {
                List<TradeBinBB> list = MainDao.SelectAll("1m");
                {
                    int mLength = 660;
                    int countAll = list.Count;
                    for (int i = mLength; i < countAll; i++)
                    {
                        double[] closeArray = new double[mLength];
                        for (int j = 0; j < mLength; j++)
                            closeArray[j] = (double)list[i - mLength + j].Close;
                        list[i].BB_SMA = closeArray.Average();
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
                List<TradeBinBB> list = getList(startTime, endTime);
                //TestHCS(list, 1);
                TestHCS(list, 2);
            }

            runHCS(new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc));
            runHCS(new DateTime(2021, 10, 1, 0, 0, 0, DateTimeKind.Utc));
            runHCS(new DateTime(2021, 12, 1, 0, 0, 0, DateTimeKind.Utc));
            runHCS(new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            goto end;


            ThreadPool.QueueUserWorkItem(state => runHCS(new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc)));
            ThreadPool.QueueUserWorkItem(state => runHCS(new DateTime(2021, 8, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc)));
            ThreadPool.QueueUserWorkItem(state => runHCS(new DateTime(2021, 9, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc)));
            ThreadPool.QueueUserWorkItem(state => runHCS(new DateTime(2021, 10, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc)));
            //ThreadPool.QueueUserWorkItem(state => runHCS(new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 12, 1, 0, 0, 0, DateTimeKind.Utc)));

            //ThreadPool.QueueUserWorkItem(state => runHCS(new DateTime(2021, 12, 1, 0, 0, 0, DateTimeKind.Utc)));
            //ThreadPool.QueueUserWorkItem(state => runHCS(new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc)));

            while (true) Console.ReadKey(false);
            goto end;


            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  hLength-StopX2 Test");
            int hLength = 0;
            while (hLength <= 48)
            {
                //decimal result = TestBuyAndSell(startTime, hLength, 660, 0.01m, 1.6m, 5, 0.009m, 0.8m, 7);

                //for (decimal stopX2 = -3; stopX2 <= 1; stopX2 += .5m)
                //{
                //    decimal result = TestBuyOrSell(list, startTime, 2, hLength, 660, 0.01m, 1.6m, 5, stopX2);
                //    logger.WriteLine($"{startTime:yyyy-MM-dd}    hLength = {hLength}    stopX2 = {stopX2}    result = {result}");
                //}
                //hLength += 2;

                //if (hLength < 24) hLength += 6;
                //else if (hLength < 48) hLength += 12;
                //else if (hLength < 360) hLength += 24;
                //else if (hLength < 720) hLength += 72;
                //else hLength += 144;
            }

            {
                //DateTime startTime = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                //TestBuyOrSell(startTime, 1, 660, 0.009m, 0.8m, 7, false);
                //TestBuyOrSell(startTime, 1, 660, 0.009m, 0.8m, 7, true);
                //TestBuyOrSell(startTime, 2, 660, 0.01m, 1.6m, 5, false);
                //TestBuyOrSell(startTime, 2, 660, 0.01m, 1.6m, 5, true);
            }

        end:;
            Console.WriteLine($"\nCompleted. Press any key to exit... ");
            Console.ReadKey(false);
        }

        static void Load_1m(DateTime startTime, DateTime endTime)
        {
            const string binSize = "1m";
            //DateTime? startTime = MainDao.SelectLastTimestamp(binSize);
            //if (startTime == null) startTime = new DateTime(2020, 12, 1, 0, 0, 0, DateTimeKind.Utc);
            //DateTime endTime = DateTime.UtcNow;
            BitMEXApiHelper apiHelper = new BitMEXApiHelper();
            while (true)
            {
                try
                {
                    DateTime nextTime = startTime.AddHours(12);
                    if (startTime >= endTime)
                    {
                        Console.WriteLine($"End.");
                        break;
                    }
                    List<TradeBin> list = apiHelper.GetBinList(binSize, false, 1000, null, startTime, nextTime.AddMinutes(-1));
                    foreach (TradeBin t in list)
                    {
                        try
                        {
                            TradeBinBB tb = new TradeBinBB(t);
                            MainDao.Insert(tb, binSize);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed: {t.Timestamp} - {ex.Message}");
                        }
                    }
                    Console.WriteLine($"Inserted: {startTime}");
                    startTime = nextTime;
                    //Thread.Sleep(3000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex.Message}");
                    Thread.Sleep(30000);
                }
            }
        }

        static void Load_5m(DateTime startTime, DateTime endTime)
        {
            const string binSize = "5m";
            BitMEXApiHelper apiHelper = new BitMEXApiHelper();
            while (true)
            {
                try
                {
                    DateTime nextTime = startTime.AddDays(1);
                    if (nextTime > endTime)
                    {
                        Console.WriteLine($"end: {startTime} > {endTime}");
                        break;
                    }
                    List<TradeBin> list = apiHelper.GetBinList(binSize, false, 1000, null, startTime, nextTime.AddMinutes(-5));
                    foreach (TradeBin t in list)
                    {
                        try
                        {
                            TradeBinBB tb = new TradeBinBB(t);
                            MainDao.Insert(tb, binSize);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed: {t.Timestamp} - {ex.Message}");
                        }
                    }
                    Console.WriteLine($"Inserted: {startTime}");
                    startTime = nextTime;
                    Thread.Sleep(3000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex.Message}");
                    Thread.Sleep(30000);
                }
            }
        }

        static void Load_1h(DateTime startTime, DateTime endTime)
        {
            const string binSize = "1h";
            BitMEXApiHelper apiHelper = new BitMEXApiHelper();
            while (true)
            {
                try
                {
                    DateTime nextTime = startTime.AddHours(960);
                    if (startTime >= endTime)
                    {
                        Console.WriteLine($"End.");
                        break;
                    }
                    List<TradeBin> list = apiHelper.GetBinList(binSize, false, 1000, null, startTime, nextTime.AddHours(-1));
                    foreach (TradeBin t in list)
                    {
                        try
                        {
                            TradeBinBB tb = new TradeBinBB(t);
                            MainDao.Insert(tb, binSize);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed: {t.Timestamp} - {ex}");
                        }
                    }
                    Console.WriteLine($"Inserted: {startTime}");
                    startTime = nextTime;
                    //Thread.Sleep(3000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex}");
                    Thread.Sleep(30000);
                }
            }
        }

        static void BB(string binSize, int bbLength, double hold)
        {
            List<TradeBinBB> list = MainDao.SelectAll(binSize, bbLength);
            int count = list.Count;
            Console.WriteLine($"{count} loaded.");
            for (int i = bbLength; i < count; i++)
            {
                double[] closeArray = new double[bbLength];
                double[] sd2Array = new double[bbLength];
                for (int j = 0; j < bbLength; j++)
                {
                    closeArray[j] = (double)list[i - bbLength + j].Close;
                }
                double movingAverage = closeArray.Average();
                for (int j = 0; j < bbLength; j++)
                {
                    sd2Array[j] = Math.Pow(closeArray[j] - movingAverage, 2);
                }
                double standardDeviation = Math.Pow(sd2Array.Average(), 0.5d);
                list[i].BB_SMA = movingAverage;
                list[i].BB_SD = standardDeviation;
                list[i].BB_Value = Math.Max(((double)list[i].High - movingAverage) / standardDeviation, (movingAverage - (double)list[i].Low) / standardDeviation);
                if (standardDeviation > movingAverage * hold && list[i - 1].BB_SD > list[i].BB_SMA * hold)
                {
                    if (list[i].BB_Value >= 6) list[i].BB_Level = 6;
                    else if (list[i].BB_Value >= 4) list[i].BB_Level = 4;
                }
                MainDao.UpdateBB(list[i], bbLength, binSize);
                if (i % 100 == 0)
                    Console.WriteLine($"{i} / {count}    {list[i].Timestamp}\t\t{movingAverage:F1}\t\t{standardDeviation:F1}\t{list[i].BB_Value:F1}\t{list[i].BB_Level}");
            }
        }

        static void Test1(string binSize = "1m", int bbLength = 30, int bbMultiplier = 2, double hold = 0.04)
        {
            Logger2.logFilename = DateTime.Now.ToString("yyyy-MM-dd  HH.mm.ss");
            DateTime startTime = new DateTime(2021, 10, 1, 0, 0, 0, DateTimeKind.Utc);
            List<TradeBinBB> list = MainDao.SelectAll("1m", 30);
            int count = list.Count;
            Logger2.WriteLine($"{count} loaded.");
            for (int i = bbLength; i < count; i++)
            {
                double[] closeArray = new double[bbLength];
                double[] sd2Array = new double[bbLength];
                for (int j = 0; j < bbLength; j++)
                {
                    closeArray[j] = (double)list[i - bbLength + j].Close;
                }
                double movingAverage = closeArray.Average();
                for (int j = 0; j < bbLength; j++)
                {
                    sd2Array[j] = Math.Pow(closeArray[j] - movingAverage, 2);
                }
                double standardDeviation = Math.Pow(sd2Array.Average(), 0.5d);
                list[i].BB_SMA = movingAverage;
                list[i].BB_SD = standardDeviation;
            }

            for (int bbw = 30; bbw < 200; bbw++)
            {
                Logger2.WriteLine($"\n--------    bbw = {bbw}    --------");
                for (double x = 2.0d; x < 10.0d; x += 0.5d)
                {
                    int upperSucceed = 0, upperFailed = 0, lowerSucceed = 0, lowerFailed = 0;
                    for (int i = bbLength + 2; i < count - 1; i++)
                    {
                        if (list[i].Timestamp < startTime) continue;
                        double prevBand = list[i - 1].BB_SD * bbMultiplier * 2;
                        int prevBBW = (int)(prevBand / list[i - 1].BB_SMA * 10000);
                        if (prevBBW != bbw) continue;
                        double sdx = Math.Max(Math.Round(10 * Math.Sqrt(list[i - 1].BB_SD / list[i - 2].BB_SD)) / 10, 1);
                        double prevUpper = list[i - 1].BB_SMA + list[i - 1].BB_SD * x * sdx;
                        double prevLower = list[i - 1].BB_SMA - list[i - 1].BB_SD * x * sdx;
                        double upper = list[i].BB_SMA + list[i].BB_SD * x;
                        double lower = list[i].BB_SMA - list[i].BB_SD * x;
                        if ((double)list[i - 1].High < prevUpper && (double)list[i].High > upper)
                        {
                            double stop = upper + list[i].BB_SD * 2;
                            double close = upper - list[i].BB_SD * x * .2;
                            if ((double)list[i].High > stop) upperFailed++;
                            else if ((double)list[i].Close < close) upperSucceed++;
                            else
                                for (int j = i + 1; j < count - 1; j++)
                                {
                                    if ((double)list[j].High > stop)
                                    {
                                        upperFailed++;
                                        break;
                                    }
                                    else if ((double)list[j].Low < close)
                                    {
                                        upperSucceed++;
                                        break;
                                    }
                                }
                            //else if ((double)list[i + 1].High > stop) upperFailed++;
                            //else if ((double)list[i + 1].Low < close) upperSucceed++;
                            //else upperFailed++;
                        }
                        else if ((double)list[i - 1].Low > prevLower && (double)list[i].Low < lower)
                        {
                            double stop = lower - list[i].BB_SD * 2;
                            double close = lower + list[i].BB_SD * x * .2;
                            if ((double)list[i].Low < stop) lowerFailed++;
                            else if ((double)list[i].Close > close) lowerSucceed++;
                            else
                                for (int j = i + 1; j < count - 1; j++)
                                {
                                    if ((double)list[j].Low < stop)
                                    {
                                        lowerFailed++;
                                        break;
                                    }
                                    else if ((double)list[j].High > close)
                                    {
                                        lowerSucceed++;
                                        break;
                                    }
                                }
                            //else if ((double)list[i + 1].Low < stop) lowerFailed++;
                            //else if ((double)list[i + 1].High > close) lowerSucceed++;
                            //else lowerFailed++;
                        }
                    }
                    double upperScore, lowerScore;
                    if (upperFailed == 0)
                        upperScore = upperSucceed;
                    else
                        upperScore = (double)upperSucceed / upperFailed;
                    if (lowerFailed == 0)
                        lowerScore = lowerSucceed;
                    else
                        lowerScore = (double)lowerSucceed / lowerFailed;
                    if (upperSucceed != 0 || upperFailed != 0 || lowerSucceed != 0 || lowerFailed != 0)
                        Logger2.WriteLine($"{x:F1} \t upperSucceed = {upperSucceed} \t upperFailed = {upperFailed} \t {upperScore:F1} \t lowerSucceed = {lowerSucceed} \t lowerFailed = {lowerFailed} \t {lowerScore:F1}");
                }
            }

            //if (i % 100 == 0)
            //    Console.WriteLine($"{i} / {count}    {list[i].Timestamp}\t\t{movingAverage:F1}\t\t{standardDeviation:F1}\t{list[i].BB_Value:F1}\t{list[i].BB_Level}");
        }

        static void TestHCS(List<TradeBinBB> list, int buyOrSell)
        {
            const decimal lossX = 1.2m;
            int count = list.Count;
            int totalDays = count / 60 / 24;
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  buyOrSell = {buyOrSell}   ({list[0].Date} ~ {totalDays:N0} days)");
            logger.WriteLine("\n" + logger.logFilename + "\n");
            logger.WriteLine($"{count} loaded. ({totalDays:N0} days)");
            Console.Title = logger.logFilename;

            List<Dictionary<string, decimal>> topList = new List<Dictionary<string, decimal>>();

            for (decimal heightX = 0.0200m; heightX <= 0.0350m; heightX += 0.0001m)
            //decimal heightX = 0.0246m;
            {
                logger.WriteLine($"\n\n----    heightX = {heightX}    ----\n");
                for (decimal closeX = 0.75m; closeX <= 1.5m; closeX += 0.05m)
                //for (decimal closeX = 0.9m; closeX <= 1.1m; closeX += 0.01m)
                {
                    for (decimal stopX = 2m; stopX <= 8; stopX += 0.5m)
                    //for (decimal stopX = 2.5m; stopX <= 3.5m; stopX += 0.1m)
                    {
                        int succeedCount = 0, failedCount = 0;
                        decimal positionCloseHeight = 0, positionStopHeight = 0, closePrice = 0, stopPrice = 0;
                        decimal totalProfit = 0;
                        for (int i = 0; i < count - 1; i++)
                        {
                            decimal sma = (decimal)list[i].BB_SMA;
                            decimal height = sma * heightX;
                            decimal closeHeight = height * closeX;
                            decimal stopHeight = closeHeight * stopX;
                            if (positionCloseHeight == 0)
                            {
                                if (buyOrSell == 1)
                                {
                                    if (list[i].Open > sma - height && list[i].Low < sma - height)
                                    {
                                        if (list[i].Low < sma - height - stopHeight)
                                        {
                                            failedCount++;
                                            totalProfit -= stopHeight * lossX;
                                        }
                                        else if (list[i].Close > sma - height + closeHeight)
                                        {
                                            succeedCount++;
                                            totalProfit += closeHeight;
                                        }
                                        else
                                        {
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
                                            totalProfit -= stopHeight * lossX;
                                        }
                                        else if (list[i].Close < sma + height - closeHeight)
                                        {
                                            succeedCount++;
                                            totalProfit += closeHeight;
                                        }
                                        else
                                        {
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
                                        totalProfit -= positionStopHeight * lossX;
                                        positionCloseHeight = 0;
                                        closePrice = 0;
                                        stopPrice = 0;
                                    }
                                    else if (list[i].High > closePrice)
                                    {
                                        succeedCount++;
                                        totalProfit += positionCloseHeight;
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
                                        totalProfit -= positionStopHeight * lossX;
                                        positionCloseHeight = 0;
                                        closePrice = 0;
                                        stopPrice = 0;
                                    }
                                    else if (list[i].Low < closePrice)
                                    {
                                        succeedCount++;
                                        totalProfit += positionCloseHeight;
                                        positionCloseHeight = 0;
                                        closePrice = 0;
                                        stopPrice = 0;
                                    }
                                }
                            }
                        }
                        decimal score;
                        if (failedCount == 0)
                            score = succeedCount;
                        else
                            score = (decimal)succeedCount / failedCount;
                        decimal avgProfit = totalProfit / totalDays;
                        if (avgProfit > 20)
                        {
                            Dictionary<string, decimal> dic = new Dictionary<string, decimal>
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
                            logger.WriteLine($"{heightX:F4} \t {closeX:F3} \t {stopX:F1} \t succeed = {succeedCount} \t failed = {failedCount} * {stopX} \t score = {score:F2} \t totalProfit = {totalProfit:N0} \t avgProfit = {avgProfit:F2}");
                        }
                    }
                }
            }
            logger.WriteLine(JArray.FromObject(topList).ToString());
            logger.WriteLine();
        }

        static void WriteSMA(string binSize, int length)
        {
            List<TradeBinBB> list = MainDao.SelectAll(binSize);
            int count = list.Count;
            for (int i = length; i < count; i++)
            {
                double[] closeArray = new double[length];
                for (int j = 0; j < length; j++)
                    closeArray[j] = (double)list[i - length + j].Close;
                list[i].BB_SMA = closeArray.Average();
                MainDao.UpdateSMA(list[i], binSize, length);
                if (i % 100 == 0)
                    Console.WriteLine($"{i} / {count}    {list[i].Timestamp}\t\t{list[i].BB_SMA}");
            }
        }
        static decimal TestBuyOrSell(List<TradeBinBB> list, DateTime startTime, int buyOrSell, int hLength, int mLength, decimal heightX, decimal closeX, decimal stopX)
        {
            const decimal lossX = 1.2m;

            List<TradeBinBB> hList = MainDao.SelectAll("1h");
            int hCountAll = hList.Count;
            if (hLength > 0)
            {
                for (int i = hLength; i < hCountAll; i++)
                {
                    double[] closeArray = new double[hLength];
                    for (int j = 0; j < hLength; j++)
                        closeArray[j] = (double)hList[i - hLength + j].Close;
                    hList[i].BB_SMA = closeArray.Average();
                }
            }

            int count;
            if (list == null)
            {
                list = MainDao.SelectAll("1m");
                count = list.Count;
                int hNext = 1;
                for (int i = mLength; i < count; i++)
                {
                    double[] closeArray = new double[mLength];
                    for (int j = 0; j < mLength; j++)
                        closeArray[j] = (double)list[i - mLength + j].Close;
                    list[i].BB_SMA = closeArray.Average();

                    if (hLength > 0)
                    {
                        for (int j = hNext; j < hCountAll; j++)
                        {
                            if (hList[j].Timestamp > list[i].Timestamp)
                            {
                                list[i].BB_SMA_H = hList[j - 1].BB_SMA;
                                hNext = j;
                                //Console.WriteLine($"{hList[j - 1].Timestamp} / {list[i].Timestamp}");
                                break;
                            }
                        }
                    }
                }
                int removeCount = 0;
                for (int i = 0; i < count - 1; i++)
                {
                    if (list[i].Timestamp < startTime)
                        removeCount++;
                    else
                        break;
                }
                list.RemoveRange(0, removeCount);
                count = list.Count;
            }
            else
            {
                count = list.Count;
                int hNext = 1;
                if (hLength > 0)
                    for (int i = 0; i < count; i++)
                    {
                        for (int j = hNext; j < hCountAll; j++)
                        {
                            if (hList[j].Timestamp > list[i].Timestamp)
                            {
                                list[i].BB_SMA_H = hList[j - 1].BB_SMA;
                                hNext = j;
                                //Console.WriteLine($"{hList[j - 1].Timestamp} / {list[i].Timestamp}");
                                break;
                            }
                        }
                    }
            }

            int totalDays = count / 60 / 24;
            Logger2.logFilename = DateTime.Now.ToString("yyyy-MM-dd  HH.mm.ss") + $"  -  startTime = {startTime:yyyy-MM-dd}    hLength = {hLength}    ({totalDays:N0} days)";
            Logger2.WriteLine("\n" + Logger2.logFilename + "\n");
            Logger2.WriteLine($"{count} loaded. ({totalDays:N0} days)");
            Console.Title = Logger2.logFilename;

            int succeedCount = 0, failedCount = 0;
            decimal lastHeight = 0;
            decimal positionEntryPrice = 0, positionCloseHeight = 0, positionStopHeight = 0, closePrice = 0, stopPrice = 0;
            decimal totalProfit = 0;
            for (int i = 0; i < count - 1; i++)
            {
                decimal sma = (decimal)list[i].BB_SMA;
                decimal height = sma * heightX;
                decimal closeHeight = height * closeX;
                decimal stopHeight = closeHeight * stopX;
                if (positionCloseHeight == 0)
                {
                    if (buyOrSell == 1)
                    {
                        if (list[i].Open > sma - height && list[i].Low < sma - height)
                        {
                            if (list[i].Low < sma - height - stopHeight)
                            {
                                failedCount++;
                                totalProfit -= stopHeight * lossX;
                                Logger2.WriteLine($"{list[i].Timestamp} \t - failed");
                            }
                            else if (list[i].Close > sma - height + closeHeight)
                            {
                                succeedCount++;
                                totalProfit += closeHeight;
                                Logger2.WriteLine($"{list[i].Timestamp} \t succeed");
                            }
                            else
                            {
                                lastHeight = height;
                                positionEntryPrice = sma - height;
                                positionCloseHeight = closeHeight;
                                positionStopHeight = stopHeight;
                                closePrice = sma - height + closeHeight;
                                stopPrice = sma - height - stopHeight;
                                Logger2.WriteLine($"{list[i].Timestamp} \t position created");
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
                                totalProfit -= stopHeight * lossX;
                                Logger2.WriteLine($"{list[i].Timestamp} \t - failed");
                            }
                            else if (list[i].Close < sma + height - closeHeight)
                            {
                                succeedCount++;
                                totalProfit += closeHeight;
                                Logger2.WriteLine($"{list[i].Timestamp} \t succeed");
                            }
                            else
                            {
                                lastHeight = height;
                                positionEntryPrice = sma + height;
                                positionCloseHeight = closeHeight;
                                positionStopHeight = stopHeight;
                                closePrice = sma + height - closeHeight;
                                stopPrice = sma + height + stopHeight;
                                Logger2.WriteLine($"{list[i].Timestamp} \t position created");
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
                            totalProfit -= positionStopHeight * lossX;
                            positionCloseHeight = 0;
                            closePrice = 0;
                            stopPrice = 0;
                            Logger2.WriteLine($"{list[i].Timestamp} \t - failed");
                        }
                        else if (list[i].High > closePrice)
                        {
                            succeedCount++;
                            totalProfit += positionCloseHeight;
                            positionCloseHeight = 0;
                            closePrice = 0;
                            stopPrice = 0;
                            Logger2.WriteLine($"{list[i].Timestamp} \t succeed");
                        }
                    }
                    else
                    {
                        if (list[i].High > stopPrice)
                        {
                            failedCount++;
                            totalProfit -= positionStopHeight * lossX;
                            positionCloseHeight = 0;
                            closePrice = 0;
                            stopPrice = 0;
                            Logger2.WriteLine($"{list[i].Timestamp} \t - failed");
                        }
                        else if (list[i].Low < closePrice)
                        {
                            succeedCount++;
                            totalProfit += positionCloseHeight;
                            positionCloseHeight = 0;
                            closePrice = 0;
                            stopPrice = 0;
                            Logger2.WriteLine($"{list[i].Timestamp} \t succeed");
                        }
                    }
                }
            }
            decimal score;
            if (failedCount == 0)
                score = succeedCount;
            else
                score = (decimal)succeedCount / failedCount;
            decimal avgProfit = totalProfit / totalDays;
            Logger2.WriteLine($"{heightX:F4} \t {closeX:F3} \t succeed = {succeedCount} \t failed = {failedCount} * {stopX} \t score = {score:F2} \t profit = {totalProfit:N0} \t x = {avgProfit:F2}");
            return avgProfit;
        }

        static decimal TestBuyAndSell(DateTime startTime, int hLength, int mLength, decimal upperHeightX, decimal upperCloseX, decimal upperStopX, decimal lowerHeightX, decimal lowerCloseX, decimal lowerStopX)
        {
            const decimal lossX = 1.2m;

            List<TradeBinBB> hList = MainDao.SelectAll("1h");
            int hCountAll = hList.Count;
            if (hLength > 0)
            {
                for (int i = hLength; i < hCountAll; i++)
                {
                    double[] closeArray = new double[hLength];
                    for (int j = 0; j < hLength; j++)
                        closeArray[j] = (double)hList[i - hLength + j].Close;
                    hList[i].BB_SMA = closeArray.Average();
                }
            }

            List<TradeBinBB> list = MainDao.SelectAll("1m");
            int countAll = list.Count;
            int hNext = 1;
            for (int i = mLength; i < countAll; i++)
            {
                double[] closeArray = new double[mLength];
                for (int j = 0; j < mLength; j++)
                    closeArray[j] = (double)list[i - mLength + j].Close;
                list[i].BB_SMA = closeArray.Average();

                if (hLength > 0)
                {
                    for (int j = hNext; j < hCountAll; j++)
                    {
                        if (hList[j].Timestamp > list[i].Timestamp)
                        {
                            list[i].BB_SMA_H = hList[j - 1].BB_SMA;
                            hNext = j;
                            //Logger.WriteLine($"{hList[j - 1].Timestamp} / {list[i].Timestamp}");
                            break;
                        }
                    }
                }
            }

            int count = countAll;
            for (int i = 0; i < count - 1; i++)
            {
                if (list[i].Timestamp < startTime)
                {
                    list.RemoveAt(i--);
                    count--;
                }
            }

            int totalDays = count / 60 / 24;

            Logger2.logFilename = DateTime.Now.ToString("yyyy-MM-dd  HH.mm.ss") + $"  -  startTime = {startTime:yyyy-MM-dd}    hLength = {hLength}    ({totalDays:N0} days)";
            Logger2.WriteLine("\n" + Logger2.logFilename + "\n");
            Logger2.WriteLine($"{count} / {countAll} loaded. ({totalDays:N0} days)");
            Console.Title = Logger2.logFilename;

            int upperSucceedCount = 0, upperFailedCount = 0, lowerSucceedCount = 0, lowerFailedCount = 0;
            int positionQty = 0;
            decimal positionCloseHeight = 0, positionStopHeight = 0, closePrice = 0, stopPrice = 0;
            decimal totalProfit = 0;
            for (int i = 0; i < count - 1; i++)
            {
                decimal sma = (decimal)list[i].BB_SMA;
                decimal upperHeight = sma * upperHeightX;
                decimal upperClose = upperHeight * upperCloseX;
                decimal upperStop = upperHeight * upperStopX;
                decimal lowerHeight = sma * lowerHeightX;
                decimal lowerClose = lowerHeight * lowerCloseX;
                decimal lowerStop = lowerHeight * lowerStopX;
                if (positionQty == 0)
                {
                    if (list[i].Open > sma - lowerHeight && list[i].Low < sma - lowerHeight && (hLength == 0 || list[i].Open < (decimal)list[i].BB_SMA_H))
                    {
                        if (list[i].Low < sma - lowerHeight - lowerStop)
                        {
                            lowerFailedCount++;
                            totalProfit -= lowerStop * lossX;
                            Logger2.WriteLine($"{list[i].Timestamp} \t - lower failed");
                        }
                        else if (list[i].Close > sma - lowerHeight + lowerClose)
                        {
                            lowerSucceedCount++;
                            totalProfit += lowerClose;
                            Logger2.WriteLine($"{list[i].Timestamp} \t lower succeed");
                        }
                        else
                        {
                            positionQty = 1;
                            positionCloseHeight = lowerClose;
                            positionStopHeight = lowerStop;
                            closePrice = sma - lowerHeight + lowerClose;
                            stopPrice = sma - lowerHeight - lowerStop;
                            Logger2.WriteLine($"{list[i].Timestamp} \t lower position created");
                        }
                    }
                    else if (list[i].Open < sma + upperHeight && list[i].High > sma + upperHeight && (hLength == 0 || list[i].Open > (decimal)list[i].BB_SMA_H))
                    {
                        if (list[i].High > sma + upperHeight + upperStop)
                        {
                            upperFailedCount++;
                            totalProfit -= upperStop * lossX;
                            Logger2.WriteLine($"{list[i].Timestamp} \t - upper failed");
                        }
                        else if (list[i].Close < sma + upperHeight - upperClose)
                        {
                            upperSucceedCount++;
                            totalProfit += upperClose;
                            Logger2.WriteLine($"{list[i].Timestamp} \t upper succeed");
                        }
                        else
                        {
                            positionQty = -1;
                            positionCloseHeight = upperClose;
                            positionStopHeight = upperStop;
                            closePrice = sma + upperHeight - upperClose;
                            stopPrice = sma + upperHeight + upperStop;
                            Logger2.WriteLine($"{list[i].Timestamp} \t upper position created");
                        }
                    }
                }
                else
                {
                    if (positionQty > 0)
                    {
                        if (list[i].Low < stopPrice)
                        {
                            lowerFailedCount++;
                            totalProfit -= positionStopHeight * lossX;
                            positionQty = 0;
                            positionCloseHeight = 0;
                            closePrice = 0;
                            stopPrice = 0;
                            Logger2.WriteLine($"{list[i].Timestamp} \t - lower failed");
                        }
                        else if (list[i].High > closePrice)
                        {
                            lowerSucceedCount++;
                            totalProfit += positionCloseHeight;
                            positionQty = 0;
                            positionCloseHeight = 0;
                            closePrice = 0;
                            stopPrice = 0;
                            Logger2.WriteLine($"{list[i].Timestamp} \t lower succeed");
                        }
                    }
                    else if (positionQty < 0)
                    {
                        if (list[i].High > stopPrice)
                        {
                            upperFailedCount++;
                            totalProfit -= positionStopHeight * lossX;
                            positionQty = 0;
                            positionCloseHeight = 0;
                            closePrice = 0;
                            stopPrice = 0;
                            Logger2.WriteLine($"{list[i].Timestamp} \t - upper failed");
                        }
                        else if (list[i].Low < closePrice)
                        {
                            upperSucceedCount++;
                            totalProfit += positionCloseHeight;
                            positionQty = 0;
                            positionCloseHeight = 0;
                            closePrice = 0;
                            stopPrice = 0;
                            Logger2.WriteLine($"{list[i].Timestamp} \t upper succeed");
                        }
                    }
                }
            }
            decimal avgDailyProfit = totalProfit / totalDays;
            Logger2.WriteLine("\n");
            Logger2.WriteLine($"upperSucceedCount = {upperSucceedCount} \t upperFailedCount = {upperFailedCount} * {upperStopX}");
            Logger2.WriteLine($"lowerSucceedCount = {lowerSucceedCount} \t lowerFailedCount = {lowerFailedCount} * {lowerStopX}");
            Logger2.WriteLine($"totalProfit = {totalProfit:N0} \t avgDailyProfit = {avgDailyProfit:F2}");
            Logger2.WriteLine();
            return avgDailyProfit;
        }


    }
}
