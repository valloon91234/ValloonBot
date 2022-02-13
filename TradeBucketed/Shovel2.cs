using IO.Swagger.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Valloon.Trading.Backtest
{
    static class Shovel2
    {
        public static void Run()
        {
            List<TradeBinModel> getListWithRSI()
            {
                const int rsiLength = 14;
                List<TradeBinModel> list = MainDao.SelectAll("1m");
                int countAll = list.Count;
                List<TradeBin> binList = new List<TradeBin>();
                foreach (TradeBinModel m in list)
                {
                    binList.Add(new TradeBin(m.Timestamp, "XBTUSD", m.Open, m.High, m.Low, m.Close));
                }
                double[] rsiArray = RSI.CalculateRSIValues(binList.ToArray(), rsiLength);
                int count = rsiArray.Length;
                for (int i = 0; i < count; i++)
                {
                    TradeBinModel m = list[i];
                    m.RSI = rsiArray[i];
                    m.RSI_M = (decimal)m.RSI;
                }
                return list;
            }

            {
                List<TradeBinModel> list = getListWithRSI();
                Console.WriteLine($"List loaded with RSI: {list.Count}");
                DateTime startTime = new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc);
                ThreadPool.QueueUserWorkItem(state => Test(list, 2, 5, startTime));
                ThreadPool.QueueUserWorkItem(state => Test(list, 2, 10, startTime));
                ThreadPool.QueueUserWorkItem(state => Test(list, 2, 15, startTime));
                ThreadPool.QueueUserWorkItem(state => Test(list, 2, 20, startTime));
                ThreadPool.QueueUserWorkItem(state => Test(list, 2, 25, startTime));
                ThreadPool.QueueUserWorkItem(state => Test(list, 2, 30, startTime));
                Test(list, 2, 35, startTime);
                ThreadPool.QueueUserWorkItem(state => Test(list, 2, 40, startTime));
                ThreadPool.QueueUserWorkItem(state => Test(list, 2, 45, startTime));
                ThreadPool.QueueUserWorkItem(state => Test(list, 2, 50, startTime));
                ThreadPool.QueueUserWorkItem(state => Test(list, 2, 55, startTime));
                ThreadPool.QueueUserWorkItem(state => Test(list, 2, 60, startTime));
                ThreadPool.QueueUserWorkItem(state => Test(list, 2, 70, startTime));
                Test(list, 2, 80, startTime);
                ThreadPool.QueueUserWorkItem(state => Test(list, 2, 90, startTime));
                ThreadPool.QueueUserWorkItem(state => Test(list, 2, 100, startTime));
                ThreadPool.QueueUserWorkItem(state => Test(list, 2, 110, startTime));
                ThreadPool.QueueUserWorkItem(state => Test(list, 2, 120, startTime));
                ThreadPool.QueueUserWorkItem(state => Test(list, 2, 150, startTime));
                ThreadPool.QueueUserWorkItem(state => Test(list, 2, 180, startTime));
                Test(list, 2, 210, startTime);
                ThreadPool.QueueUserWorkItem(state => Test(list, 2, 240, startTime));
                ThreadPool.QueueUserWorkItem(state => Test(list, 2, 300, startTime));
                ThreadPool.QueueUserWorkItem(state => Test(list, 2, 360, startTime));
                ThreadPool.QueueUserWorkItem(state => Test(list, 2, 420, startTime));
                ThreadPool.QueueUserWorkItem(state => Test(list, 2, 480, startTime));
                ThreadPool.QueueUserWorkItem(state => Test(list, 2, 540, startTime));
                Test(list, 2, 600, startTime);
                ThreadPool.QueueUserWorkItem(state => Test(list, 2, 660, startTime));
                ThreadPool.QueueUserWorkItem(state => Test(list, 2, 720, startTime));
                ThreadPool.QueueUserWorkItem(state => Test(list, 2, 780, startTime));
                ThreadPool.QueueUserWorkItem(state => Test(list, 2, 840, startTime));
                ThreadPool.QueueUserWorkItem(state => Test(list, 2, 900, startTime));
                ThreadPool.QueueUserWorkItem(state => Test(list, 2, 960, startTime));
                Test(list, 2, 600, startTime);

                return;
            }

            List<TradeBinModel> getList(DateTime startTime, DateTime? endTime = null, int mLength = 660)
            {
                List<TradeBinModel> list = MainDao.SelectAll("1m");
                {
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
                List<TradeBinModel> list = getList(startTime, endTime);
                //TestHCS(list, 1);
                //TestHCS(list, 2);
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

        static void WriteRSI()
        {
            const int rsiLength = 14;
            List<TradeBinModel> list = MainDao.SelectAll("1m");
            int countAll = list.Count;
            List<TradeBin> binList = new List<TradeBin>();
            foreach (TradeBinModel m in list)
            {
                binList.Add(new TradeBin(m.Timestamp, "XBTUSD", m.Open, m.High, m.Low, m.Close));
            }
            double[] rsiArray = RSI.CalculateRSIValues(binList.ToArray(), rsiLength);
            int count = rsiArray.Length;
            for (int i = 0; i < count; i++)
            {
                TradeBinModel m = list[i];
                m.RSI = rsiArray[i];
                MainDao.UpdateRSI(m, "1m", rsiLength);
                if (i % 100 == 0)
                    Console.WriteLine($"{i} / {count}    {list[i].Timestamp}");
            }
            return;
        }

        static decimal Test(List<TradeBinModel> listFull, int buyOrSell, int smaLength, DateTime startTime, DateTime? endTime = null)
        {
            const decimal lossX = 1.2m;
            List<TradeBinModel> list = new List<TradeBinModel>(listFull);
            {
                int countAll = list.Count;
                for (int i = smaLength; i < countAll; i++)
                {
                    decimal[] closeArray = new decimal[smaLength];
                    for (int j = 0; j < smaLength; j++)
                        closeArray[j] = list[i - smaLength + j].Close;
                    list[i].SMA_M = closeArray.Average();
                }
                list.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
            }
            int count = list.Count;
            int totalDays = count / 60 / 24;
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  buyOrSell = {buyOrSell}   smaLength = {smaLength}    ({list[0].Date} ~ {totalDays:N0} days)");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;

            List<Dictionary<string, decimal>> topList = new List<Dictionary<string, decimal>>();

            for (decimal heightX = 0.00m; heightX <= 1.50m; heightX += 0.01m)
            //decimal heightX = 0.0246m;
            {
                logger.WriteLine($"\n\n----    heightX = {heightX}    ----\n");
                for (decimal closeX = 0.0005m; closeX <= 0.0250m; closeX += 0.0005m)
                //for (decimal closeX = 0.9m; closeX <= 1.1m; closeX += 0.01m)
                {
                    for (decimal stopX = 0.0005m; stopX <= 0.0250m; stopX += 0.0005m)
                    //for (decimal stopX = 2.5m; stopX <= 3.5m; stopX += 0.1m)
                    {
                        int succeedCount = 0, failedCount = 0;
                        decimal positionCloseHeight = 0, positionStopHeight = 0, closePrice = 0, stopPrice = 0;
                        decimal totalProfit = 0;
                        for (int i = 1; i < count - 1; i++)
                        {
                            decimal sma = list[i - 1].SMA_M;
                            decimal rsi = list[i - 1].RSI_M;
                            if (positionCloseHeight == 0)
                            {
                                if (buyOrSell == 1)
                                {
                                    decimal height = sma * heightX / (100 - rsi);
                                    decimal closeHeight = sma * closeX;
                                    decimal stopHeight = sma * stopX;
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
                                    decimal height = sma * heightX / rsi;
                                    decimal closeHeight = sma * closeX;
                                    decimal stopHeight = sma * stopX;
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
                        if (avgProfit > 0)
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
                            logger.WriteLine($"{heightX:F4}    {closeX:F4}    {stopX:F4} \t succeed = {succeedCount} \t failed = {failedCount} \t score = {score:F2} \t total = {totalProfit:N0} \t avg = {avgProfit:F2}");
                        }
                        else
                        {
                            Console.WriteLine($"{heightX:F4}    {closeX:F4}    {stopX:F4} \t succeed = {succeedCount} \t failed = {failedCount} \t score = {score:F2} \t total = {totalProfit:N0} \t avg = {avgProfit:F2}");
                        }
                    }
                }
            }
            logger.WriteLine(JArray.FromObject(topList).ToString());
            logger.WriteLine();
            if (topList.Count > 0)
                return topList[topList.Count - 1]["avgProfit"];
            return 0;
        }

        static void WriteSMA(string binSize, int length)
        {
            List<TradeBinModel> list = MainDao.SelectAll(binSize);
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
        static decimal TestBuyOrSell(List<TradeBinModel> list, DateTime startTime, int buyOrSell, int hLength, int mLength, decimal heightX, decimal closeX, decimal stopX)
        {
            const decimal lossX = 1.2m;

            List<TradeBinModel> hList = MainDao.SelectAll("1h");
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

            List<TradeBinModel> hList = MainDao.SelectAll("1h");
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

            List<TradeBinModel> list = MainDao.SelectAll("1m");
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
