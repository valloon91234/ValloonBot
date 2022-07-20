using IO.Swagger.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Valloon.Indicators;
using Valloon.Utils;

namespace Valloon.Trading.Backtest
{
    static class Sol5
    {
        const string binSize = "5m";

        public static void Run()
        {
            Program.MoveWindow(20, 0, 1600, 140);
            //Load2("15m"); return;
            //DateTime startTime = new DateTime(2021, 10, 21, 0, 0, 0, DateTimeKind.Utc);
            //{
            //    DateTime startTime = new DateTime(2022, 5, 6, 0, 0, 0, DateTimeKind.Utc);
            //    DateTime endTime = DateTime.UtcNow;
            //    Load("1m", startTime, endTime);
            //    //LoadCSV("1m", startTime, endTime);
            //    return;
            //}

            {
                BenchmarkRSILimit();
                //BenchmarkSAR3();
                //BenchmarkSAR2();
                return;
            }

            //ThreadPool.QueueUserWorkItem(state => runHCS(new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc)));
        }

        static void LoadCSV(string binSize, DateTime startTime, DateTime endTime)
        {
            using (var writer = new StreamWriter("data.csv", false, Encoding.UTF8))
            {
                writer.WriteLine($"timestamp,date,time,open,high,low,close,volume");
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
                                writer.WriteLine($"{t.Timestamp.Value:yyyy-MM-dd HH:mm:ss},{t.Timestamp.Value:yyyy-MM-dd},{t.Timestamp.Value:HH:mm},{t.Open.Value * 100},{t.High.Value * 100},{t.Low.Value * 100},{t.Close.Value * 100},{t.Volume.Value}");
                                writer.Flush();
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
                        Thread.Sleep(500);
                        startTime = nextTime;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception: {ex.Message}");
                        Thread.Sleep(5000);
                    }
                }
            }
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

        static void Load2(string binSize, DateTime? startTime = null)
        {
            List<SolBin> list;
            int batchLength;
            switch (binSize)
            {
                case "15m":
                    list = SolDao.SelectAll("5m");
                    batchLength = 3;
                    break;
                case "30m":
                    list = SolDao.SelectAll("5m");
                    batchLength = 6;
                    break;
                default:
                    Console.WriteLine($"Invalid bin_size: {binSize}");
                    return;
            }
            if (startTime != null) list.RemoveAll(x => x.Timestamp < startTime.Value);
            int count = list.Count;
            int i = 0;
            string filename = $"data-{binSize}.csv";
            File.Delete(filename);
            using (var writer = new StreamWriter(filename, true, Encoding.UTF8))
            {
                writer.WriteLine($"timestamp,date,time,open,high,low,close,volume");
                while (i < count - batchLength)
                {
                    int high = list[i + 1].High;
                    int low = list[i + 1].Low;
                    int volume = list[i + 1].Volume;
                    for (int j = i + 2; j <= i + batchLength; j++)
                    {
                        if (high < list[j].High) high = list[j].High;
                        if (low > list[j].Low) low = list[j].Low;
                        volume += list[j].Volume;
                    }
                    try
                    {
                        SolBin t = new SolBin
                        {
                            Timestamp = list[i].Timestamp,
                            Date = list[i].Date,
                            Time = list[i].Time,
                            Open = list[i + 1].Open,
                            High = high,
                            Low = low,
                            Close = list[i + batchLength].Close,
                            Volume = volume,
                        };
                        try
                        {
                            writer.WriteLine($"{t.Timestamp:yyyy-MM-dd HH:mm:ss},{t.Timestamp:yyyy-MM-dd},{t.Timestamp:HH:mm},{t.Open},{t.High},{t.Low},{t.Close},{t.Volume}");
                            writer.Flush();
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.ContainsIgnoreCase("UNIQUE constraint failed:"))
                                Console.WriteLine($"Failed: {t.Timestamp:yyyy-MM-dd HH:mm:ss} - Already exists.");
                            else
                                Console.WriteLine($"Failed: {t.Timestamp:yyyy-MM-dd HH:mm:ss}\r\n{ex.StackTrace}");
                        }
                        Console.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss}");
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.ContainsIgnoreCase("UNIQUE constraint failed:"))
                            Console.WriteLine($"Failed: {list[i].Timestamp:yyyy-MM-dd HH:mm:ss} - Already exists.");
                        else
                            Console.WriteLine($"Failed: {list[i].Timestamp:yyyy-MM-dd HH:mm:ss}\r\n{ex.StackTrace}");
                    }
                    i += batchLength;
                }
            }
        }

        static float BenchmarkRSI()
        {
            //TestRSI(); return 0;

            //const string binSize = "5m";
            const string binSize = "15m";
            //const string binSize = "30m";
            //const string binSize = "1h";
            const int buyOrSell = 2;

            DateTime startTime = new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime? endTime = null;// new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            List<SolBin> listAll = SolDao.SelectAll("5m");
            if (binSize != "5m") listAll = LoadBinListFrom5m(binSize, listAll);
            int countAll = listAll.Count;
            int totalDays = (int)(listAll[countAll - 1].Timestamp - listAll[0].Timestamp).TotalDays;
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}    buyOrSell = {buyOrSell}    bin = {binSize}    ({startTime:yyyy-MM-dd HH.mm.ss} ~ {totalDays:N0} days)");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{countAll} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;
            List<Dictionary<string, float>> topList = new List<Dictionary<string, float>>();

            //for (int window = 10; window <= 40; window++)
            int window = 26;
            {
                List<SolBin> list = new List<SolBin>(listAll);
                int count = list.Count;
                List<TradeBin> binList = new List<TradeBin>();
                foreach (SolBin m in list)
                {
                    binList.Add(new TradeBin(m.Timestamp, BitMEXApiHelper.SYMBOL_SOLUSD, m.Open, m.High, m.Low, m.Close));
                }
                double[] rsiArray = RSI.CalculateRSIValues(binList.ToArray(), window);
                for (int i = 0; i < count; i++)
                {
                    SolBin m = list[i];
                    m.RSI = (float)rsiArray[i];
                }
                list.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
                count = list.Count;

                for (float over = 10; over < 30; over += .1f)
                {
                    float overBuy = 50 + over;
                    float overSell = 50 - over;
                    for (float maxDiff = 1; maxDiff < 15; maxDiff += .1f)
                    {
                        for (float neutralOpen = -20; neutralOpen < 10; neutralOpen += .1f)
                        //int neutral =5;
                        {
                            //for (float neutralClose = 0; neutralClose < 10; neutralClose += .5f)
                            float neutralClose = neutralOpen;
                            {
                                int tryCount = 0;
                                float minHeight = 0, maxHeight = 0;
                                float totalProfit = 0;
                                float finalPercent = 1;
                                int positionEntryPrice = 0;
                                for (int i = 1; i < count - 1; i++)
                                {
                                    if (buyOrSell == 1)
                                    {
                                        if (positionEntryPrice == 0 && list[i].RSI - list[i - 1].RSI < maxDiff && list[i - 1].RSI < 50 + neutralOpen && list[i].RSI > 50 + neutralOpen)
                                        {
                                            tryCount++;
                                            positionEntryPrice = list[i].Close;
                                        }
                                        else if (positionEntryPrice > 0 && (list[i - 1].RSI > 50 + neutralClose && list[i].RSI < 50 + neutralClose || list[i - 1].RSI > overBuy && list[i].RSI < overBuy))
                                        {
                                            int profit = list[i].Close - positionEntryPrice;
                                            totalProfit += profit - positionEntryPrice * .001f;
                                            finalPercent *= (float)list[i].Close / positionEntryPrice - 0.001f;
                                            if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
                                            if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
                                            positionEntryPrice = 0;
                                        }
                                    }
                                    else if (buyOrSell == 2)
                                    {
                                        if (positionEntryPrice == 0 && list[i - 1].RSI - list[i].RSI < maxDiff && list[i - 1].RSI > 50 - neutralOpen && list[i].RSI < 50 - neutralOpen)
                                        {
                                            tryCount++;
                                            positionEntryPrice = list[i].Close;
                                        }
                                        else if (positionEntryPrice > 0 && (list[i - 1].RSI < 50 - neutralClose && list[i].RSI > 50 - neutralClose || list[i - 1].RSI < overSell && list[i].RSI > overSell))
                                        {
                                            int profit = positionEntryPrice - list[i].Close;
                                            totalProfit += profit - positionEntryPrice * .001f;
                                            finalPercent *= (float)positionEntryPrice / list[i].Close - 0.001f;
                                            if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
                                            if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
                                            positionEntryPrice = 0;
                                        }
                                    }
                                }
                                float avgProfit = totalProfit / tryCount;
                                if (totalProfit > 0)
                                {
                                    Dictionary<string, float> dic = new Dictionary<string, float>
                                    {
                                        { "buyOrSell", buyOrSell },
                                        { "window", window },
                                        { "over", over },
                                        { "maxDiff", maxDiff },
                                        { "neutralOpen", neutralOpen },
                                        { "neutralClose", neutralClose },
                                        { "tryCount", tryCount },
                                        { "minHeight", minHeight },
                                        { "maxHeight", maxHeight },
                                        { "totalProfit", totalProfit },
                                        { "avgProfit", avgProfit },
                                        { "finalPercent", finalPercent },
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
                                            if (topList[i]["finalPercent"] > finalPercent ||
                                                    topList[i]["finalPercent"] == finalPercent && topList[i]["totalProfit"] > totalProfit)
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
                                    logger.WriteLine($"<{buyOrSell}> \t w = {window} \t over = {over:F2} \t maxD = {maxDiff:F2} \t neutral = {neutralOpen:F2} / {neutralClose:F2} \t try = {tryCount} \t min = {minHeight:F4} \t max = {maxHeight:F4} \t profit = {totalProfit:F2} \t avg = {avgProfit} \t % = {finalPercent:F4}");
                                }
                                //else
                                //{
                                //    Console.WriteLine($"{minRSI} ~ {maxRSI}    {minDiff} / {maxDiff}    {closeX:F4}    {stopX:F4} \t succeed = {succeedCount} \t failed = {failedCount} \t unknown = {unknownCount} \t score = {score:F2} \t total = {totalProfit:F4} \t avg = {avgProfit:F4} \t final = {finalPercent:F4}");
                                //}
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

        static float BenchmarkRSI2(int binSize, bool mode)
        {
            //TestRSI(); return 0;

            DateTime startTime = new DateTime(2022, 3, 23, 0, 0, 0, DateTimeKind.Utc);
            DateTime? endTime = null;// new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            List<SolBin> list = SolDao.SelectAll("1m");
            list.RemoveAll(x => x.Timestamp < startTime.AddDays(-1));
            int count = list.Count;
            int totalDays = (int)(list[count - 1].Timestamp - startTime).TotalDays;
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}    bin = {binSize}    mode = {mode}    ({startTime:yyyy-MM-dd HH.mm.ss} ~ {totalDays:N0} days)");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;
            List<Dictionary<string, float>> topList = new List<Dictionary<string, float>>();

            for (int window = 10; window <= 30; window++)
            //int window = 16;
            {
                Console.Write($"\r\nWindow = {window} ");
                if (binSize == 1)
                {
                    List<TradeBin> binList = new List<TradeBin>();
                    foreach (SolBin m in list)
                    {
                        binList.Add(new TradeBin(m.Timestamp, BitMEXApiHelper.SYMBOL_SOLUSD, m.Open, m.High, m.Low, m.Close));
                    }
                    double[] rsiArray = RSI.CalculateRSIValues(binList.ToArray(), window);
                    for (int i = 0; i < count; i++)
                    {
                        SolBin m = list[i];
                        m.RSI = (float)rsiArray[i];
                    }
                }
                else
                {
                    for (int i = window * binSize; i < count; i++)
                    {
                        List<SolBin> list4BinSize = LoadBinListFrom1m(binSize, list.GetRange(0, i + 1));
                        List<TradeBin> binList = new List<TradeBin>();
                        foreach (SolBin m in list4BinSize)
                        {
                            binList.Add(new TradeBin(m.Timestamp, BitMEXApiHelper.SYMBOL_SOLUSD, m.Open, m.High, m.Low, m.Close));
                        }
                        double[] rsiArray = RSI.CalculateRSIValues(binList.ToArray(), window);
                        list[i].RSI = (float)rsiArray[rsiArray.Length - 1];
                    }
                }
                list.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
                count = list.Count;
                Console.WriteLine($"\t count = {count}");

                for (float upperDiff = 10; upperDiff <= 40; upperDiff += 1)
                {
                    //Console.WriteLine($"upperDiff = {upperDiff}");
                    for (float lowerDiff = 10; lowerDiff <= 40; lowerDiff += 1)
                    {
                        //for (float upperLimit = 75; upperLimit <= 85; upperLimit += 1)
                        float upperLimit = 100;
                        {
                            //for (float lowerLimit = 20; lowerLimit <= 30; lowerLimit += 1)
                            float lowerLimit = 0;
                            {
                                int tryCount = 0;
                                float minHeight = 0, maxHeight = 0;
                                float totalProfit = 0;
                                float finalPercent = 1;
                                int position = 1;
                                int positionEntryPrice = list[0].Close;
                                float topRSI = list[0].RSI;
                                for (int i = 1; i < count - 1; i++)
                                {
                                    if (position == 1)
                                    {
                                        if (list[i].RSI >= upperLimit || list[i].RSI > lowerLimit && list[i].RSI < (mode ? topRSI - upperDiff * (topRSI / 100f) : topRSI - upperDiff))
                                        {
                                            tryCount++;
                                            int profit = list[i].Close - positionEntryPrice;
                                            totalProfit += profit - positionEntryPrice * .001f;
                                            finalPercent *= (float)list[i].Close / positionEntryPrice - 0.001f;
                                            if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
                                            if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
                                            position = -1;
                                            positionEntryPrice = list[i].Close;
                                            topRSI = list[i].RSI;
                                        }
                                        else if (topRSI < list[i].RSI)
                                        {
                                            topRSI = list[i].RSI;
                                        }
                                    }
                                    else if (position == -1)
                                    {
                                        if (list[i].RSI <= lowerLimit || list[i].RSI < upperLimit && list[i].RSI > (mode ? topRSI + lowerDiff * (1 - topRSI / 100f) : topRSI + lowerDiff))
                                        {
                                            tryCount++;
                                            int profit = positionEntryPrice - list[i].Close;
                                            totalProfit += profit - positionEntryPrice * .001f;
                                            finalPercent *= (float)positionEntryPrice / list[i].Close - 0.001f;
                                            if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
                                            if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
                                            position = 1;
                                            positionEntryPrice = list[i].Close;
                                            topRSI = list[i].RSI;
                                        }
                                        else if (topRSI > list[i].RSI)
                                        {
                                            topRSI = list[i].RSI;
                                        }
                                    }
                                }
                                float avgProfit = totalProfit / tryCount;
                                if (totalProfit > 0 && finalPercent > 1 && tryCount >= totalDays / 2)
                                {
                                    Dictionary<string, float> dic = new Dictionary<string, float>
                                    {
                                        { "window", window },
                                        { "upperDiff", upperDiff },
                                        { "lowerDiff", lowerDiff },
                                        { "upperLimit", upperLimit },
                                        { "lowerLimit", lowerLimit },
                                        { "tryCount", tryCount },
                                        { "minHeight", minHeight },
                                        { "maxHeight", maxHeight },
                                        { "totalProfit", totalProfit },
                                        { "avgProfit", avgProfit },
                                        { "finalPercent", finalPercent },
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
                                            if (topList[i]["finalPercent"] > finalPercent ||
                                                    topList[i]["finalPercent"] == finalPercent && topList[i]["totalProfit"] > totalProfit)
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
                                    logger.WriteLine($"<{window}> \t diff = {upperDiff:F1} / {lowerDiff:F1} \t limit = {upperLimit:F1} / {lowerLimit:F1} \t try = {tryCount} \t h = {minHeight:F4} / {maxHeight:F4} \t p = {totalProfit:F2} \t avg = {avgProfit:F2} \t % = {finalPercent:F4}");
                                }
                                //else
                                //{
                                //    Console.WriteLine($"{minRSI} ~ {maxRSI}    {minDiff} / {maxDiff}    {closeX:F4}    {stopX:F4} \t succeed = {succeedCount} \t failed = {failedCount} \t unknown = {unknownCount} \t score = {score:F2} \t total = {totalProfit:F4} \t avg = {avgProfit:F4} \t final = {finalPercent:F4}");
                                //}
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

        static float TestRSI()
        {
            //const string binSize = "5m";
            const string binSize = "30m";
            //const string binSize = "1h";
            float overBuy = 70f;
            float overSell = 23f;
            const int buyOrSell = 1;
            int window = 14;
            float maxDiff = 17.5f;
            float neutralOpen = 5f;
            //const int buyOrSell = 2;
            //int window = 15;
            //float maxDiff = 11.5f;
            //float neutralOpen = -8f;

            float neutralClose = neutralOpen;

            DateTime startTime = new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime? endTime = null;// new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            List<SolBin> list5m = SolDao.SelectAll("5m");
            List<SolBin> listAll = LoadBinListFrom5m(binSize, list5m);
            int countAll = listAll.Count;
            int totalDays = (int)(listAll[countAll - 1].Timestamp - listAll[0].Timestamp).TotalDays;
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}    buyOrSell = {buyOrSell}    bin = {binSize}    ({startTime:yyyy-MM-dd HH.mm.ss} ~ {totalDays:N0} days)");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{countAll} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;
            List<Dictionary<string, float>> topList = new List<Dictionary<string, float>>();

            List<SolBin> list = new List<SolBin>(listAll);
            int count = list.Count;
            List<TradeBin> binList = new List<TradeBin>();
            foreach (SolBin m in list)
            {
                binList.Add(new TradeBin(m.Timestamp, BitMEXApiHelper.SYMBOL_SOLUSD, m.Open, m.High, m.Low, m.Close));
            }
            double[] rsiArray = RSI.CalculateRSIValues(binList.ToArray(), window);
            for (int i = 0; i < count; i++)
            {
                SolBin m = list[i];
                m.RSI = (float)rsiArray[i];
            }
            list.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
            count = list.Count;

            int tryCount = 0;
            float minHeight = 0, maxHeight = 0;
            float totalProfit = 0;
            float finalPercent = 1;
            int positionEntryPrice = 0;
            for (int i = 1; i < count - 1; i++)
            {
                if (buyOrSell == 1)
                {
                    if (positionEntryPrice == 0 && list[i].RSI - list[i - 1].RSI < maxDiff && list[i - 1].RSI < 50 + neutralOpen && list[i].RSI > 50 + neutralOpen)
                    {
                        tryCount++;
                        positionEntryPrice = list[i].Close;
                        logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t entry = {positionEntryPrice}");
                    }
                    else if (positionEntryPrice > 0 && (list[i - 1].RSI > 50 + neutralClose && list[i].RSI < 50 + neutralClose || list[i - 1].RSI > overBuy && list[i].RSI < overBuy))
                    {
                        int profit = list[i].Close - positionEntryPrice;
                        totalProfit += profit - positionEntryPrice * .001f;
                        finalPercent *= (float)list[i].Close / positionEntryPrice - 0.001f;
                        logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t entry = {positionEntryPrice} \t close = {list[i].Close} \t profit = {profit:F2} \t total = {totalProfit:F2}");
                        if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
                        if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
                        positionEntryPrice = 0;
                    }
                }
                else if (buyOrSell == 2)
                {
                    if (positionEntryPrice == 0 && list[i - 1].RSI - list[i].RSI < maxDiff && list[i - 1].RSI > 50 - neutralOpen && list[i].RSI < 50 - neutralOpen)
                    {
                        tryCount++;
                        positionEntryPrice = list[i].Close;
                        logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t entry = {positionEntryPrice}");
                    }
                    else if (positionEntryPrice > 0 && (list[i - 1].RSI < 50 - neutralClose && list[i].RSI > 50 - neutralClose || list[i - 1].RSI < overSell && list[i].RSI > overSell))
                    {
                        int profit = positionEntryPrice - list[i].Close;
                        totalProfit += profit - positionEntryPrice * .001f;
                        finalPercent *= (float)positionEntryPrice / list[i].Close - 0.001f;
                        logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t entry = {positionEntryPrice} \t close = {list[i].Close} \t profit = {profit:F2} \t total = {totalProfit:F2}");
                        if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
                        if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
                        positionEntryPrice = 0;
                    }
                }
            }
            logger.WriteLine($"\r\n<{buyOrSell}> \t window = {window} \t neutral = {neutralOpen} / {neutralClose} \t try = {tryCount} \t min = {minHeight} \t max = {maxHeight} \t profit = {totalProfit:F2} \t % = {finalPercent}");
            return totalProfit;
        }

        static float BenchmarkSAR()
        {
            TestSAR(); return 0;

            const float takerFee = 0.002f;
            const int binSize = 5;

            DateTime startTime = new DateTime(2022, 3, 23, 0, 0, 0, DateTimeKind.Utc);
            DateTime? endTime = null;// new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            List<SolBin> list = SolDao.SelectAll("1m");
            list = LoadBinListFrom1m(binSize, list);
            list.RemoveAll(x => x.Timestamp < startTime.AddDays(-1));
            int count = list.Count;
            int totalDays = (int)(list[count - 1].Timestamp - startTime).TotalDays;
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}    BenchmarkSAR    bin = {binSize}    fee = {takerFee}    ({startTime:yyyy-MM-dd HH.mm.ss} ~ {totalDays:N0} days)");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;
            List<Dictionary<string, float>> topList = new List<Dictionary<string, float>>();

            var quoteList = new List<CandleQuote>();
            foreach (var t in list)
            {
                quoteList.Add(new CandleQuote
                {
                    Timestamp = t.Timestamp,
                    Open = t.Open,
                    High = t.High,
                    Low = t.Low,
                    Close = t.Close,
                    Volume = t.Volume,
                });
            }

            for (float step = 0.001f; step <= 0.002f; step += .0001f)
            //decimal step = .102m;
            {
                for (float start = 0.001f; start <= 0.002f; start += .0001f)
                //float start = step;
                {
                    for (float max = .01f; max <= 0.04; max += .001f)
                    //float max = .0329f;
                    {
                        if (step >= max) continue;
                        List<ParabolicSarResult> parabolicSarList = ParabolicSar.GetParabolicSar(quoteList, step, max, start).ToList();
                        list.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
                        parabolicSarList.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
                        count = list.Count;
                        //Console.WriteLine($"\r\n start = {start} \t step = {step} \t max = {max} \t count = {count} / {smaList.Count} / {parabolicSarList.Count}\r\n");

                        int tryCount = 0;
                        float minHeight = 0, maxHeight = 0;
                        float totalProfit = 0;
                        float finalPercent = 1;
                        int position = 0;
                        int positionEntryPrice = 0;
                        for (int i = 1; i < count - 1; i++)
                        {
                            if (position == 0)
                            {
                                if (parabolicSarList[i].IsReversal.Value)
                                {
                                    if (parabolicSarList[i].OriginalSar.Value < parabolicSarList[i].Sar.Value)
                                    {
                                        position = -1;
                                        positionEntryPrice = (int)parabolicSarList[i].OriginalSar.Value;
                                    }
                                    else if (parabolicSarList[i].OriginalSar.Value > parabolicSarList[i].Sar.Value)
                                    {
                                        position = 1;
                                        positionEntryPrice = (int)parabolicSarList[i].OriginalSar.Value;
                                    }
                                }
                            }
                            else if (position == 1)
                            {
                                if (parabolicSarList[i].IsReversal.Value)
                                {
                                    if (parabolicSarList[i].OriginalSar.Value < parabolicSarList[i].Sar.Value)
                                    {
                                        tryCount++;
                                        int close = (int)parabolicSarList[i].OriginalSar.Value;
                                        int profit = close - positionEntryPrice;
                                        totalProfit += profit - positionEntryPrice * takerFee;
                                        finalPercent *= (float)close / positionEntryPrice - takerFee;
                                        if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
                                        if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
                                        position = -1;
                                        positionEntryPrice = close;
                                        //position = 0;
                                    }
                                    else
                                    {
                                        Console.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t {parabolicSarList[i].Sar} / {parabolicSarList[i].IsReversal} / {parabolicSarList[i].OriginalSar:F0} \t \t ERROR", ConsoleColor.Red);
                                    }
                                }
                            }
                            else if (position == -1)
                            {
                                if (parabolicSarList[i].IsReversal.Value)
                                {
                                    if (parabolicSarList[i].OriginalSar.Value > parabolicSarList[i].Sar.Value)
                                    {
                                        tryCount++;
                                        int close = (int)parabolicSarList[i].OriginalSar.Value;
                                        int profit = positionEntryPrice - close;
                                        totalProfit += profit - positionEntryPrice * takerFee;
                                        finalPercent *= (float)positionEntryPrice / close - takerFee;
                                        if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
                                        if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
                                        position = 1;
                                        positionEntryPrice = close;
                                        //position = 0;
                                    }
                                    else
                                    {
                                        Console.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t {parabolicSarList[i].Sar} / {parabolicSarList[i].IsReversal} / {parabolicSarList[i].OriginalSar:F0} \t \t ERROR", ConsoleColor.Red);
                                    }
                                }
                            }
                        }
                        float avgProfit = totalProfit / tryCount;
                        if (totalProfit > 0 && finalPercent > 1 && tryCount >= totalDays / 2)
                        {
                            Dictionary<string, float> dic = new Dictionary<string, float>
                            {
                                { "binSize", binSize },
                                { "start", (float)start },
                                { "step", (float)step },
                                { "max", (float)max },
                                { "tryCount", tryCount },
                                { "minHeight", minHeight },
                                { "maxHeight", maxHeight },
                                { "totalProfit", totalProfit },
                                { "avgProfit", avgProfit },
                                { "finalPercent", finalPercent },
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
                                    if (topList[i]["finalPercent"] > finalPercent ||
                                            topList[i]["finalPercent"] == finalPercent && topList[i]["totalProfit"] > totalProfit)
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
                            logger.WriteLine($"{start:F8} / {step:F8} / {max:F8} \t count = {count} / {parabolicSarList.Count} \t try = {tryCount} \t h = {minHeight:F4} / {maxHeight:F4} \t p = {totalProfit:F2} \t avg = {avgProfit:F2} \t % = {finalPercent:F4}");
                        }
                        //else
                        //{
                        //    Console.WriteLine($"{minRSI} ~ {maxRSI}    {minDiff} / {maxDiff}    {closeX:F4}    {stopX:F4} \t succeed = {succeedCount} \t failed = {failedCount} \t unknown = {unknownCount} \t score = {score:F2} \t total = {totalProfit:F4} \t avg = {avgProfit:F4} \t final = {finalPercent:F4}");
                        //}
                    }
                }
            }
            logger.WriteLine($"\r\n\r\n\r\n\r\ntopList={topList.Count}\r\n" + JArray.FromObject(topList).ToString());
            if (topList.Count > 0)
                return topList[topList.Count - 1]["finalPercent"];
            return 0;
        }

        static float TestSAR()
        {
            const float takerFee = 0.002f;
            const int binSize = 5;
            float start = 0.014f;
            float step = 0.014f;
            float max = 0.14f;

            //const int binSize = 15;
            //int sma = 48;
            //decimal diff = -0.014m;
            //decimal start = 0.057m;
            //decimal step = start;
            //decimal max = 0.21m;

            DateTime startTime = new DateTime(2022, 3, 23, 0, 0, 0, DateTimeKind.Utc);
            DateTime? endTime = null;// new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            List<SolBin> list = SolDao.SelectAll("1m");
            list = LoadBinListFrom1m(binSize, list);
            list.RemoveAll(x => x.Timestamp < startTime.AddDays(-1));
            int count = list.Count;
            int totalDays = (int)(list[count - 1].Timestamp - startTime).TotalDays;
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}    TestSAR    bin = {binSize}    fee = {takerFee}    ({startTime:yyyy-MM-dd HH.mm.ss} ~ {totalDays:N0} days)");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;

            var quoteList = new List<CandleQuote>();
            foreach (var t in list)
            {
                quoteList.Add(new CandleQuote
                {
                    Timestamp = t.Timestamp,
                    Open = t.Open,
                    High = t.High,
                    Low = t.Low,
                    Close = t.Close,
                    Volume = t.Volume,
                });
            }
            List<ParabolicSarResult> parabolicSarList = ParabolicSar.GetParabolicSar(quoteList, step, max, start).ToList();
            list.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
            parabolicSarList.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
            count = list.Count;

            //for (int i = 1; i < count - 1; i++)
            //{
            //    Console.WriteLine($"{parabolicSarList[i].Date:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t sma = {smaList[i].Sma:F2} \t {parabolicSarList[i].Sar:F2} / {parabolicSarList[i].IsReversal}");
            //}
            //return 0;

            int tryCount = 0;
            float minHeight = 0, maxHeight = 0;
            float totalProfit = 0;
            float finalPercent = 1;
            float finalPercent2 = 1;
            float finalPercent3 = 1;
            int position = 0;
            int positionEntryPrice = 0;
            int countSucceed = 0, countFail = 0, countSucceed2 = 0, countFail2 = 0;
            bool newPositionCreated = false;
            for (int i = 1; i < count - 1; i++)
            {
                if (position == 0)
                {
                    if (parabolicSarList[i].IsReversal.Value)
                    {
                        if (parabolicSarList[i].OriginalSar.Value < parabolicSarList[i].Sar.Value)
                        {
                            position = -1;
                            positionEntryPrice = (int)parabolicSarList[i].OriginalSar.Value;
                            logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t {parabolicSarList[i].Sar:F0} / {parabolicSarList[i].IsReversal} / {parabolicSarList[i].OriginalSar:F0} \t <SHORT>");
                        }
                        else if (parabolicSarList[i].OriginalSar.Value > parabolicSarList[i].Sar.Value)
                        {
                            position = 1;
                            positionEntryPrice = (int)parabolicSarList[i].OriginalSar.Value;
                            logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t {parabolicSarList[i].Sar:F0} / {parabolicSarList[i].IsReversal} / {parabolicSarList[i].OriginalSar:F0} \t <LONG>");
                        }
                    }
                }
                else if (position == 1)
                {
                    if (newPositionCreated && list[i].High - positionEntryPrice > .5)
                    {
                        countSucceed2++;
                        newPositionCreated = false;
                    }
                    if (parabolicSarList[i].IsReversal.Value)
                    {
                        if (parabolicSarList[i].OriginalSar.Value < parabolicSarList[i].Sar.Value)
                        {
                            tryCount++;
                            int close = (int)parabolicSarList[i].OriginalSar.Value;
                            int profit = close - positionEntryPrice;
                            totalProfit += profit - positionEntryPrice * takerFee;
                            finalPercent *= (float)close / positionEntryPrice - takerFee;
                            finalPercent2 *= 1 + ((float)close / positionEntryPrice - takerFee - 1) * 2;
                            finalPercent3 *= 1 + ((float)close / positionEntryPrice - takerFee - 1) * 3;
                            if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
                            if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
                            logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t {parabolicSarList[i].Sar:F0} / {parabolicSarList[i].IsReversal} / {parabolicSarList[i].OriginalSar:F0} \t entry = {positionEntryPrice} \t profit = {profit} \t total = {totalProfit} \t % = {finalPercent:F4} / {finalPercent2:F4}");
                            position = -1;
                            positionEntryPrice = close;
                            //position = 0;
                            newPositionCreated = true;
                            if (profit > 0) countSucceed++;
                            else countFail++;
                        }
                        else
                        {
                            Console.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t {parabolicSarList[i].Sar} / {parabolicSarList[i].IsReversal} / {parabolicSarList[i].OriginalSar:F0} \t \t ERROR", ConsoleColor.Red);
                        }
                    }
                }
                else if (position == -1)
                {
                    if (newPositionCreated && positionEntryPrice - list[i].Low > .5)
                    {
                        countSucceed2++;
                        newPositionCreated = false;
                    }
                    if (parabolicSarList[i].IsReversal.Value)
                    {
                        if (parabolicSarList[i].OriginalSar.Value > parabolicSarList[i].Sar.Value)
                        {
                            tryCount++;
                            int close = (int)parabolicSarList[i].OriginalSar.Value;
                            int profit = positionEntryPrice - close;
                            totalProfit += profit - positionEntryPrice * takerFee;
                            finalPercent *= (float)positionEntryPrice / close - takerFee;
                            finalPercent2 *= 1 + ((float)positionEntryPrice / close - takerFee - 1) * 2;
                            finalPercent3 *= 1 + ((float)positionEntryPrice / close - takerFee - 1) * 3;
                            if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
                            if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
                            logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t {parabolicSarList[i].Sar:F0} / {parabolicSarList[i].IsReversal} / {parabolicSarList[i].OriginalSar:F0} \t entry = {positionEntryPrice} \t profit = {profit} \t total = {totalProfit} \t % = {finalPercent:F4} / {finalPercent2:F4}");
                            position = 1;
                            positionEntryPrice = close;
                            //position = 0;
                            newPositionCreated = true;
                            if (profit > 0) countSucceed++;
                            else countFail++;
                        }
                        else
                        {
                            Console.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t {parabolicSarList[i].Sar} / {parabolicSarList[i].IsReversal} / {parabolicSarList[i].OriginalSar:F0} \t \t ERROR", ConsoleColor.Red);
                        }
                    }
                }
            }
            float avgProfit = totalProfit / tryCount;
            logger.WriteLine($"\r\n{start:F4} / {step:F4} / {max:F4} \t count = {count} / {parabolicSarList.Count} \t try = {tryCount} \t h = {minHeight:F4} / {maxHeight:F4} \t p = {totalProfit:F2} \t avg = {avgProfit:F2} \t % = {finalPercent:F4} / {finalPercent2:F4} / {finalPercent3:F4}");
            logger.WriteLine($"countSucceed = {countSucceed}, countFail = {countFail}, countSucceed2 = {countSucceed2}, countFail2 = {countFail2}");
            return finalPercent;
        }

        //static float BenchmarkSAR_SMA(int binSize)
        //{
        //    //TestSAR2(); return 0;

        //    const float takerFee = 0.003f;
        //    binSize = 5;

        //    DateTime startTime = new DateTime(2022, 3, 23, 0, 0, 0, DateTimeKind.Utc);
        //    DateTime? endTime = null;// new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        //    List<SolBin> list = SolDao.SelectAll("1m");
        //    list = LoadBinListFrom1m(binSize, list);
        //    list.RemoveAll(x => x.Timestamp < startTime.AddDays(-1));
        //    int count = list.Count;
        //    int totalDays = (int)(list[count - 1].Timestamp - startTime).TotalDays;
        //    Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}    bin = {binSize}    ({startTime:yyyy-MM-dd HH.mm.ss} ~ {totalDays:N0} days)");
        //    logger.WriteLine("\n" + logger.LogFilename + "\n");
        //    logger.WriteLine($"{count} loaded. ({totalDays:N0} days)");
        //    Console.Title = logger.LogFilename;
        //    List<Dictionary<string, float>> topList = new List<Dictionary<string, float>>();

        //    var quoteList = new List<ParabolicSarQuote>();
        //    foreach (var t in list)
        //    {
        //        quoteList.Add(new ParabolicSarQuote
        //        {
        //            Date = t.Timestamp,
        //            Open = t.Open,
        //            High = t.High,
        //            Low = t.Low,
        //            Close = t.Close,
        //            Volume = t.Volume,
        //        });
        //    }

        //    //for (int sma = 36; sma < 432; sma += 36)
        //    //for (int sma = 12; sma < 144; sma += 12)
        //    //for (int sma = 360; sma <= 720; sma += 360)
        //    //int sma = 144;
        //    int sma = 12 * 60 / binSize;
        //    {
        //        var smaList = quoteList.GetSma(sma).ToList();
        //        //for (float diff = -0.01m; diff <= -0.004f; diff += .001f)
        //        float diff = -.008f;
        //        {
        //            for (float max = .01f; max <= 0.6f; max += .01f)
        //            //decimal max = .17m;
        //            {
        //                for (float start = 0.001f; start <= 0.12f && start < max; start += .001f)
        //                //decimal start = .102m;
        //                {
        //                    //for (decimal step = 0.001m; step <= 0.03m && step < max; step += .001m)
        //                    float step = start;
        //                    {
        //                        List<ParabolicSarResult> parabolicSarList = ParabolicSar.GetParabolicSar(quoteList, step, max, start).ToList();
        //                        list.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
        //                        smaList.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
        //                        parabolicSarList.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
        //                        count = list.Count;
        //                        //Console.WriteLine($"\r\n start = {start} \t step = {step} \t max = {max} \t count = {count} / {smaList.Count} / {parabolicSarList.Count}\r\n");

        //                        int tryCount = 0;
        //                        float minHeight = 0, maxHeight = 0;
        //                        float totalProfit = 0;
        //                        float finalPercent = 1;
        //                        int position = 0;
        //                        int positionEntryPrice = 0;
        //                        for (int i = 1; i < count - 1; i++)
        //                        {
        //                            if (position == 0)
        //                            {
        //                                if (parabolicSarList[i].IsReversal.Value && parabolicSarList[i - 1].Sar < list[i].Open && parabolicSarList[i].Sar >= list[i].Open /* && parabolicSarList[i].Sar < smaList[i].Sma * (1 - diff)*/)
        //                                {
        //                                    position = -1;
        //                                    positionEntryPrice = list[i].Open;
        //                                }
        //                                else if (parabolicSarList[i].IsReversal.Value && parabolicSarList[i - 1].Sar > list[i].Open && parabolicSarList[i].Sar <= list[i].Open /* && parabolicSarList[i].Sar > smaList[i].Sma * (1 + diff)*/)
        //                                {
        //                                    position = 1;
        //                                    positionEntryPrice = list[i].Open;
        //                                }
        //                            }
        //                            else if (position == 1)
        //                            {
        //                                if (parabolicSarList[i].IsReversal.Value)
        //                                {
        //                                    tryCount++;
        //                                    int profit = list[i].Open - positionEntryPrice;
        //                                    totalProfit += profit - positionEntryPrice * takerFee;
        //                                    finalPercent *= (float)list[i].Open / positionEntryPrice - takerFee;
        //                                    if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
        //                                    if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
        //                                    //Console.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t {parabolicSarList[i].Sar} / {parabolicSarList[i].IsReversal} \t entry = {positionEntryPrice} \t profit = {profit} \t total = {totalProfit} \t % = {finalPercent}");
        //                                    //position = -1;
        //                                    //positionEntryPrice = (int)parabolicSarList[i].Sar.Value;
        //                                    position = 0;
        //                                }
        //                                else if (list[i].Low <= parabolicSarList[i].Sar)
        //                                {
        //                                    tryCount++;
        //                                    int profit = (int)parabolicSarList[i].Sar.Value - positionEntryPrice;
        //                                    totalProfit += profit - positionEntryPrice * takerFee;
        //                                    finalPercent *= (float)parabolicSarList[i].Sar.Value / positionEntryPrice - takerFee;
        //                                    if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
        //                                    if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
        //                                    //Console.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t {parabolicSarList[i].Sar} / {parabolicSarList[i].IsReversal} \t entry = {positionEntryPrice} \t profit = {profit} \t total = {totalProfit} \t % = {finalPercent}");
        //                                    //position = -1;
        //                                    //positionEntryPrice = (int)parabolicSarList[i].Sar.Value;
        //                                    position = 0;
        //                                }
        //                            }
        //                            else if (position == -1)
        //                            {
        //                                if (parabolicSarList[i].IsReversal.Value)
        //                                {
        //                                    tryCount++;
        //                                    int profit = positionEntryPrice - list[i].Open;
        //                                    totalProfit += profit - positionEntryPrice * takerFee;
        //                                    finalPercent *= positionEntryPrice / (float)list[i].Open - takerFee;
        //                                    if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
        //                                    if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
        //                                    //Console.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t {parabolicSarList[i].Sar} / {parabolicSarList[i].IsReversal} \t entry = {positionEntryPrice} \t profit = {profit} \t total = {totalProfit} \t % = {finalPercent}");
        //                                    //position = 1;
        //                                    //positionEntryPrice = (int)parabolicSarList[i].Sar.Value;
        //                                    position = 0;
        //                                }
        //                                else if (list[i].High >= parabolicSarList[i].Sar)
        //                                {
        //                                    tryCount++;
        //                                    int profit = positionEntryPrice - (int)parabolicSarList[i].Sar.Value;
        //                                    totalProfit += profit - positionEntryPrice * takerFee;
        //                                    finalPercent *= positionEntryPrice / (float)parabolicSarList[i].Sar.Value - takerFee;
        //                                    if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
        //                                    if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
        //                                    //Console.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t {parabolicSarList[i].Sar} / {parabolicSarList[i].IsReversal} \t entry = {positionEntryPrice} \t profit = {profit} \t total = {totalProfit} \t % = {finalPercent}");
        //                                    //position = 1;
        //                                    //positionEntryPrice = (int)parabolicSarList[i].Sar.Value;
        //                                    position = 0;
        //                                }
        //                            }
        //                        }
        //                        float avgProfit = totalProfit / tryCount;
        //                        if (totalProfit > 0 && finalPercent > 1 && tryCount >= totalDays / 2)
        //                        {
        //                            Dictionary<string, float> dic = new Dictionary<string, float>
        //                            {
        //                                { "binSize", binSize },
        //                                { "sma", sma },
        //                                { "diff", (float)diff },
        //                                { "start", (float)start },
        //                                { "step", (float)step },
        //                                { "max", (float)max },
        //                                { "tryCount", tryCount },
        //                                { "minHeight", minHeight },
        //                                { "maxHeight", maxHeight },
        //                                { "totalProfit", totalProfit },
        //                                { "avgProfit", avgProfit },
        //                                { "finalPercent", finalPercent },
        //                            };
        //                            int topListCount = topList.Count;
        //                            if (topListCount > 0)
        //                            {
        //                                while (topListCount > 1000)
        //                                {
        //                                    topList.RemoveAt(0);
        //                                    topListCount--;
        //                                }
        //                                for (int i = 0; i < topListCount; i++)
        //                                {
        //                                    if (topList[i]["finalPercent"] > finalPercent ||
        //                                            topList[i]["finalPercent"] == finalPercent && topList[i]["totalProfit"] > totalProfit)
        //                                    {
        //                                        topList.Insert(i, dic);
        //                                        goto topListEnd;
        //                                    }
        //                                }
        //                                topList.Add(dic);
        //                            topListEnd:;
        //                            }
        //                            else
        //                            {
        //                                topList.Add(dic);
        //                            }
        //                            logger.WriteLine($"<{sma} / {diff}> \t {start:F4} / {step:F4} / {max:F4} \t count = {count} / {smaList.Count} / {parabolicSarList.Count} \t try = {tryCount} \t h = {minHeight:F4} / {maxHeight:F4} \t p = {totalProfit:F2} \t avg = {avgProfit:F2} \t % = {finalPercent:F4}");
        //                        }
        //                        //else
        //                        //{
        //                        //    Console.WriteLine($"{minRSI} ~ {maxRSI}    {minDiff} / {maxDiff}    {closeX:F4}    {stopX:F4} \t succeed = {succeedCount} \t failed = {failedCount} \t unknown = {unknownCount} \t score = {score:F2} \t total = {totalProfit:F4} \t avg = {avgProfit:F4} \t final = {finalPercent:F4}");
        //                        //}
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    logger.WriteLine($"\r\n\r\n\r\n\r\ntopList={topList.Count}\r\n" + JArray.FromObject(topList).ToString());
        //    if (topList.Count > 0)
        //        return topList[topList.Count - 1]["finalPercent"];
        //    return 0;
        //}

        //static float TestSAR_SMA()
        //{
        //    const float takerFee = 0.003f;
        //    const int binSize = 5;
        //    int sma = 144;
        //    decimal diff = -0.005m;
        //    decimal start = 0.05m;
        //    decimal step = start;
        //    decimal max = 0.17m;

        //    //const int binSize = 15;
        //    //int sma = 48;
        //    //decimal diff = -0.014m;
        //    //decimal start = 0.057m;
        //    //decimal step = start;
        //    //decimal max = 0.21m;

        //    DateTime startTime = new DateTime(2022, 3, 23, 0, 0, 0, DateTimeKind.Utc);
        //    DateTime? endTime = null;// new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        //    List<SolBin> list = SolDao.SelectAll("1m");
        //    list = LoadBinListFrom1m(binSize, list);
        //    list.RemoveAll(x => x.Timestamp < startTime.AddDays(-1));
        //    int count = list.Count;
        //    int totalDays = (int)(list[count - 1].Timestamp - startTime).TotalDays;
        //    Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}    bin = {binSize}    ({startTime:yyyy-MM-dd HH.mm.ss} ~ {totalDays:N0} days)");
        //    logger.WriteLine("\n" + logger.LogFilename + "\n");
        //    logger.WriteLine($"{count} loaded. ({totalDays:N0} days)");
        //    Console.Title = logger.LogFilename;

        //    var quoteList = new List<ParabolicSarQuote>();
        //    foreach (var t in list)
        //    {
        //        quoteList.Add(new ParabolicSarQuote
        //        {
        //            Date = t.Timestamp,
        //            Open = t.Open,
        //            High = t.High,
        //            Low = t.Low,
        //            Close = t.Close,
        //            Volume = t.Volume,
        //        });
        //    }
        //    List<SmaResult> smaList = quoteList.GetSma(sma).ToList();
        //    List<ParabolicSarResult> parabolicSarList = ParabolicSar.GetParabolicSar(quoteList, step, max, start).ToList();
        //    list.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
        //    smaList.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
        //    parabolicSarList.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
        //    count = list.Count;

        //    //for (int i = 1; i < count - 1; i++)
        //    //{
        //    //    Console.WriteLine($"{parabolicSarList[i].Date:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t sma = {smaList[i].Sma:F2} \t {parabolicSarList[i].Sar:F2} / {parabolicSarList[i].IsReversal}");
        //    //}
        //    //return 0;

        //    int tryCount = 0;
        //    float minHeight = 0, maxHeight = 0;
        //    float totalProfit = 0;
        //    float finalPercent = 1;
        //    float finalPercent2 = 1;
        //    float finalPercent3 = 1;
        //    int position = 0;
        //    int positionEntryPrice = 0;
        //    for (int i = 1; i < count - 1; i++)
        //    {
        //        if (position == 0)
        //        {
        //            if (parabolicSarList[i].IsReversal.Value && parabolicSarList[i - 1].Sar < list[i].Open && parabolicSarList[i].Sar >= list[i].Open && parabolicSarList[i].Sar < smaList[i].Sma * (1 - diff))
        //            {
        //                position = -1;
        //                positionEntryPrice = list[i].Open;
        //                logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t sma = {smaList[i].Sma:F0} \t {parabolicSarList[i].Sar:F0} / {parabolicSarList[i].IsReversal} \t <SHORT>");
        //            }
        //            else if (parabolicSarList[i].IsReversal.Value && parabolicSarList[i - 1].Sar > list[i].Open && parabolicSarList[i].Sar <= list[i].Open && parabolicSarList[i].Sar > smaList[i].Sma * (1 + diff))
        //            {
        //                position = 1;
        //                positionEntryPrice = list[i].Open;
        //                logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t sma = {smaList[i].Sma:F0} \t {parabolicSarList[i].Sar:F0} / {parabolicSarList[i].IsReversal} \t <LONG>");
        //            }
        //        }
        //        else if (position == 1)
        //        {
        //            if (parabolicSarList[i].IsReversal.Value)
        //            {
        //                tryCount++;
        //                int profit = list[i].Open - positionEntryPrice;
        //                totalProfit += profit - positionEntryPrice * takerFee;
        //                finalPercent *= (float)list[i].Open / positionEntryPrice - takerFee;
        //                finalPercent2 *= 1 + ((float)list[i].Open / positionEntryPrice - takerFee - 1) * 2;
        //                finalPercent3 *= 1 + ((float)list[i].Open / positionEntryPrice - takerFee - 1) * 3;
        //                if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
        //                if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
        //                logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t sma = {smaList[i].Sma:F0} \t {parabolicSarList[i].Sar:F0} / {parabolicSarList[i].IsReversal} \t entry = {positionEntryPrice} \t profit = {profit} \t total = {totalProfit} \t % = {finalPercent:F4} / {finalPercent2:F4}");
        //                //position = -1;
        //                //positionEntryPrice = (int)parabolicSarList[i].Sar.Value;
        //                position = 0;
        //            }
        //            else if (list[i].Low <= parabolicSarList[i].Sar)
        //            {
        //                tryCount++;
        //                int profit = (int)parabolicSarList[i].Sar.Value - positionEntryPrice;
        //                totalProfit += profit - positionEntryPrice * takerFee;
        //                finalPercent *= (float)parabolicSarList[i].Sar.Value / positionEntryPrice - takerFee;
        //                finalPercent2 *= 1 + ((float)parabolicSarList[i].Sar.Value / positionEntryPrice - takerFee - 1) * 2;
        //                finalPercent3 *= 1 + ((float)parabolicSarList[i].Sar.Value / positionEntryPrice - takerFee - 1) * 3;
        //                if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
        //                if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
        //                logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t sma = {smaList[i].Sma:F0} \t {parabolicSarList[i].Sar:F0} / {parabolicSarList[i].IsReversal} \t entry = {positionEntryPrice} \t profit = {profit} \t total = {totalProfit} \t % = {finalPercent:F4} / {finalPercent2:F4}");
        //                //position = -1;
        //                //positionEntryPrice = (int)parabolicSarList[i].Sar.Value;
        //                position = 0;
        //            }
        //        }
        //        else if (position == -1)
        //        {
        //            if (parabolicSarList[i].IsReversal.Value)
        //            {
        //                tryCount++;
        //                int profit = positionEntryPrice - list[i].Open;
        //                totalProfit += profit - positionEntryPrice * takerFee;
        //                finalPercent *= (float)positionEntryPrice / list[i].Open - takerFee;
        //                finalPercent2 *= 1 + ((float)positionEntryPrice / list[i].Open - takerFee - 1) * 2;
        //                finalPercent3 *= 1 + ((float)positionEntryPrice / list[i].Open - takerFee - 1) * 3;
        //                if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
        //                if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
        //                logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t sma = {smaList[i].Sma:F0} \t {parabolicSarList[i].Sar:F0} / {parabolicSarList[i].IsReversal} \t entry = {positionEntryPrice} \t profit = {profit} \t total = {totalProfit} \t % = {finalPercent:F4} / {finalPercent2:F4}");
        //                //position = 1;
        //                //positionEntryPrice = (int)parabolicSarList[i].Sar.Value;
        //                position = 0;
        //            }
        //            else if (list[i].High >= parabolicSarList[i].Sar)
        //            {
        //                tryCount++;
        //                int profit = positionEntryPrice - (int)parabolicSarList[i].Sar.Value;
        //                totalProfit += profit - positionEntryPrice * takerFee;
        //                finalPercent *= (float)positionEntryPrice / (float)parabolicSarList[i].Sar.Value - takerFee;
        //                finalPercent2 *= 1 + ((float)positionEntryPrice / (float)parabolicSarList[i].Sar.Value - takerFee - 1) * 2;
        //                finalPercent3 *= 1 + ((float)positionEntryPrice / (float)parabolicSarList[i].Sar.Value - takerFee - 1) * 3;
        //                if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
        //                if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
        //                logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t sma = {smaList[i].Sma:F0} \t {parabolicSarList[i].Sar:F0} / {parabolicSarList[i].IsReversal} \t entry = {positionEntryPrice} \t profit = {profit} \t total = {totalProfit} \t % = {finalPercent:F4} / {finalPercent2:F4}");
        //                //position = 1;
        //                //positionEntryPrice = (int)parabolicSarList[i].Sar.Value;
        //                position = 0;
        //            }
        //        }
        //    }
        //    float avgProfit = totalProfit / tryCount;
        //    logger.WriteLine($"\r\n<{sma} / {diff}> \t {start:F4} / {step:F4} / {max:F4} \t count = {count} / {smaList.Count} / {parabolicSarList.Count} \t try = {tryCount} \t h = {minHeight:F4} / {maxHeight:F4} \t p = {totalProfit:F2} \t avg = {avgProfit:F2} \t % = {finalPercent:F4} / {finalPercent2:F4} / {finalPercent3:F4}");
        //    return finalPercent;
        //}

        static float BenchmarkSAR2()
        {
            TestSAR2(); return 0;

            const float takerFee = 0.002f;
            const int binSize1 = 5;
            const int binSize2 = 60;

            DateTime startTime = new DateTime(2022, 3, 23, 0, 0, 0, DateTimeKind.Utc);
            DateTime? endTime = null;// new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            List<SolBin> list1m = SolDao.SelectAll("1m");
            List<SolBin> list1 = LoadBinListFrom1m(binSize1, list1m);
            List<SolBin> list2 = LoadBinListFrom1m(binSize2, list1m);
            int count1 = list1.Count;
            int totalDays = (int)(list1.Last().Timestamp - startTime).TotalDays;
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}    BenchmarkSAR2    bin = {binSize}    fee = {takerFee}    ({startTime:yyyy-MM-dd HH.mm.ss} ~ {totalDays:N0} days)");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count1} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;
            List<Dictionary<string, float>> topList = new List<Dictionary<string, float>>();

            var quoteList1 = new List<CandleQuote>();
            foreach (var t in list1)
            {
                quoteList1.Add(new CandleQuote
                {
                    Timestamp = t.Timestamp,
                    Open = t.Open,
                    High = t.High,
                    Low = t.Low,
                    Close = t.Close,
                    Volume = t.Volume,
                });
            }
            var quoteList2 = new List<CandleQuote>();
            foreach (var t in list2)
            {
                quoteList2.Add(new CandleQuote
                {
                    Timestamp = t.Timestamp,
                    Open = t.Open,
                    High = t.High,
                    Low = t.Low,
                    Close = t.Close,
                    Volume = t.Volume,
                });
            }
            list1.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
            list2.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);

            for (float stopLoss = 0.012f; stopLoss <= 0.017f; stopLoss += .001f)
            //float stopLoss = .015f;
            {
                for (float step1 = 0.00145f; step1 <= 0.00155f; step1 += .00001f)
                //float step1 = .00149f;
                {
                    for (float start1 = 0.00145f; start1 <= 0.0019f; start1 += .00001f)
                    //float start1 = step1;
                    //float start1 = .00186f;
                    {
                        for (float max1 = .032f; max1 <= 0.035; max1 += .0001f)
                        //float max1 = .033f;
                        {
                            //if (step1 >= max1) continue;

                            List<ParabolicSarResult> parabolicSarList1 = ParabolicSar.GetParabolicSar(quoteList1, step1, max1, start1).ToList();
                            parabolicSarList1.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
                            int count = parabolicSarList1.Count;

                            //for (float step2 = 0.0004f; step2 <= 0.0005f; step2 += .00001f)
                            float step2 = 0.00044f;
                            {
                                //for (float start2 = 0.0004f; start2 <= 0.0005f; start2 += .00001f)
                                float start2 = step2;
                                {
                                    //for (float max2 = .015f; max2 <= 0.02; max2 += .001f)
                                    float max2 = 0.017f;
                                    {
                                        //if (step2 >= max2) continue;
                                        //Console.WriteLine($"\r\n start = {start} \t step = {step} \t max = {max} \t count = {count} / {smaList.Count} / {parabolicSarList.Count}\r\n");

                                        List<ParabolicSarResult> parabolicSarList2 = ParabolicSar.GetParabolicSar(quoteList2, step2, max2, start2).ToList();
                                        parabolicSarList2.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);

                                        int tryCount = 0;
                                        float minHeight = 0, maxHeight = 0;
                                        float totalProfit = 0;
                                        float finalPercent = 1;
                                        int position = 0;
                                        int positionEntryPrice = 0;
                                        for (int i = 1; i < count - 1; i++)
                                        {
                                            var pSar1 = parabolicSarList1[i];
                                            var pSar2 = parabolicSarList2.Find(x => x.Timestamp > pSar1.Timestamp);

                                            if (position == 0)
                                            {
                                                if (pSar1.IsReversal.Value)
                                                {
                                                    //if (pSar1.OriginalSar.Value < pSar1.Sar.Value && pSar2.Sar > list[i].Open)
                                                    if (pSar1.OriginalSar.Value < pSar1.Sar.Value && pSar2.Sar >= pSar1.Sar.Value)
                                                    {
                                                        position = -1;
                                                        positionEntryPrice = (int)pSar1.OriginalSar.Value;
                                                    }
                                                    //else if (pSar1.OriginalSar.Value > pSar1.Sar.Value && pSar2.Sar < list[i].Open)
                                                    else if (pSar1.OriginalSar.Value > pSar1.Sar.Value && pSar2.Sar <= pSar1.Sar.Value)
                                                    {
                                                        position = 1;
                                                        positionEntryPrice = (int)pSar1.OriginalSar.Value;
                                                    }
                                                }
                                                //else if (pSar2.IsReversal.Value && pSar1.Sar > list[i].Open && pSar2.Sar >= pSar1.Sar)
                                                //{
                                                //    position = -1;
                                                //    positionEntryPrice = (int)parabolicSarList2[i - 1].Sar.Value;
                                                //}
                                            }
                                            else if (position == 1)
                                            {
                                                if ((float)list1[i].Low / positionEntryPrice < 1 - stopLoss)
                                                {
                                                    tryCount++;
                                                    int close = (int)Math.Floor(positionEntryPrice * (1 - stopLoss));
                                                    int profit = close - positionEntryPrice;
                                                    totalProfit += profit - positionEntryPrice * takerFee;
                                                    finalPercent *= (float)close / positionEntryPrice - takerFee;
                                                    if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
                                                    if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
                                                    //position = -1;
                                                    //positionEntryPrice = close;
                                                    position = 0;
                                                }
                                                else if (pSar1.IsReversal.Value)
                                                {
                                                    if (pSar1.OriginalSar.Value < pSar1.Sar.Value)
                                                    {
                                                        tryCount++;
                                                        int close = (int)pSar1.OriginalSar.Value;
                                                        int profit = close - positionEntryPrice;
                                                        totalProfit += profit - positionEntryPrice * takerFee;
                                                        finalPercent *= (float)close / positionEntryPrice - takerFee;
                                                        if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
                                                        if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
                                                        //position = -1;
                                                        //positionEntryPrice = close;
                                                        position = 0;
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar} / {pSar1.IsReversal} / {pSar1.OriginalSar:F0} \t \t ERROR", ConsoleColor.Red);
                                                    }
                                                }
                                            }
                                            else if (position == -1)
                                            {
                                                if ((float)positionEntryPrice / list1[i].High < 1 - stopLoss)
                                                {
                                                    tryCount++;
                                                    int close = (int)Math.Ceiling(positionEntryPrice / (1 - stopLoss));
                                                    int profit = positionEntryPrice - close;
                                                    totalProfit += profit - positionEntryPrice * takerFee;
                                                    finalPercent *= (float)positionEntryPrice / close - takerFee;
                                                    if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
                                                    if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
                                                    //position = 1;
                                                    //positionEntryPrice = close;
                                                    position = 0;
                                                }
                                                else if (pSar1.IsReversal.Value)
                                                {
                                                    if (pSar1.OriginalSar.Value > pSar1.Sar.Value)
                                                    {
                                                        tryCount++;
                                                        int close = (int)pSar1.OriginalSar.Value;
                                                        int profit = positionEntryPrice - close;
                                                        totalProfit += profit - positionEntryPrice * takerFee;
                                                        finalPercent *= (float)positionEntryPrice / close - takerFee;
                                                        if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
                                                        if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
                                                        //position = 1;
                                                        //positionEntryPrice = close;
                                                        position = 0;
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar} / {pSar1.IsReversal} / {pSar1.OriginalSar:F0} \t \t ERROR", ConsoleColor.Red);
                                                    }
                                                }
                                            }
                                        }
                                        if (position == 1)
                                        {
                                            tryCount++;
                                            int close = (int)((list1.Last().Close + parabolicSarList1.Last().Sar.Value) / 2);
                                            int profit = close - positionEntryPrice;
                                            totalProfit += profit - positionEntryPrice * takerFee;
                                            finalPercent *= (float)close / positionEntryPrice - takerFee;
                                            if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
                                            if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
                                            position = 0;
                                        }
                                        else if (position == 2)
                                        {
                                            tryCount++;
                                            int close = (int)((list1.Last().Close + parabolicSarList1.Last().Sar.Value) / 2);
                                            int profit = positionEntryPrice - close;
                                            totalProfit += profit - positionEntryPrice * takerFee;
                                            finalPercent *= (float)positionEntryPrice / close - takerFee;
                                            if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
                                            if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
                                            position = 0;
                                        }
                                        float avgProfit = totalProfit / tryCount;
                                        if (totalProfit > 0 && finalPercent > 1f && tryCount >= totalDays / 2)
                                        {
                                            Dictionary<string, float> dic = new Dictionary<string, float>
                                            {
                                                { "bin1", binSize1 },
                                                { "start1", start1 },
                                                { "step1", step1 },
                                                { "max1", max1 },
                                                { "bin2", binSize2 },
                                                { "start2", start2 },
                                                { "step2", step2 },
                                                { "max2", max2 },
                                                { "stopLoss",stopLoss },
                                                { "tryCount", tryCount },
                                                { "minHeight", minHeight },
                                                { "maxHeight", maxHeight },
                                                { "totalProfit", totalProfit },
                                                { "avgProfit", avgProfit },
                                                { "finalPercent", finalPercent },
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
                                                    if (topList[i]["finalPercent"] > finalPercent ||
                                                            topList[i]["finalPercent"] == finalPercent && topList[i]["totalProfit"] > totalProfit)
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
                                            logger.WriteLine($"{start1:F6} / {step1:F6} / {max1:F6} \t {start2:F6} / {step2:F6} / {max2:F6} \t stop = {stopLoss:F4} \t count = {count} / {parabolicSarList1.Count} / {parabolicSarList2.Count} \t try = {tryCount} \t h = {minHeight:F4} / {maxHeight:F4} \t p = {totalProfit:F2} \t avg = {avgProfit:F2} \t % = {finalPercent:F4}");
                                        }
                                        //else
                                        //{
                                        //    Console.WriteLine($"{minRSI} ~ {maxRSI}    {minDiff} / {maxDiff}    {closeX:F4}    {stopX:F4} \t succeed = {succeedCount} \t failed = {failedCount} \t unknown = {unknownCount} \t score = {score:F2} \t total = {totalProfit:F4} \t avg = {avgProfit:F4} \t final = {finalPercent:F4}");
                                        //}
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

        static string TestSAR2()
        {
            TestSAR2(new DateTime(2022, 3, 23, 0, 0, 0, DateTimeKind.Utc)); return null;

            string result = "";
            result += TestSAR2(new DateTime(2022, 4, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += TestSAR2(new DateTime(2022, 3, 23, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += TestSAR2(new DateTime(2022, 3, 15, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += TestSAR2(new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 4, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += TestSAR2(new DateTime(2022, 2, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += TestSAR2(new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 2, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += TestSAR2(new DateTime(2021, 12, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += TestSAR2(new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 12, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n";
            Console.WriteLine("\r\n\r\n================================\r\n");
            Console.WriteLine(result);
            return result;
        }

        static string TestSAR2(DateTime startTime, DateTime? endTime = null)
        {
            const float takerFee = 0.002f;
            const int binSize1 = 5;
            const int binSize2 = 60;
            //float start1 = 0.00149f;
            //float step1 = 0.00149f;
            //float max1 = 0.0342f;
            //float start2 = 0.00045f;
            //float step2 = start2;
            //float max2 = 0.018f;
            //float stopLoss = 0.016f;
            float start1 = 0.00186f;
            float step1 = 0.00149f;
            float max1 = 0.0331f;
            float start2 = 0.00044f;
            float step2 = start2;
            float max2 = 0.017f;
            float stopLoss = 0.015f;

            //DateTime startTime = new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc);
            //DateTime? endTime = null;
            List<SolBin> list1m = SolDao.SelectAll("1m");
            List<SolBin> list1 = LoadBinListFrom1m(binSize1, list1m);
            List<SolBin> list2 = LoadBinListFrom1m(binSize2, list1m);
            int count1 = list1.Count;
            int totalDays = (int)(list1.Last().Timestamp - startTime).TotalDays;
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}    TestSAR2    bin = {binSize}    takerFee = {takerFee}    ({startTime:yyyy-MM-dd HH.mm.ss} ~ {totalDays:N0} days)");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count1} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;

            var quoteList1 = new List<CandleQuote>();
            foreach (var t in list1)
            {
                quoteList1.Add(new CandleQuote
                {
                    Timestamp = t.Timestamp,
                    Open = t.Open,
                    High = t.High,
                    Low = t.Low,
                    Close = t.Close,
                    Volume = t.Volume,
                });
            }
            var quoteList2 = new List<CandleQuote>();
            foreach (var t in list2)
            {
                quoteList2.Add(new CandleQuote
                {
                    Timestamp = t.Timestamp,
                    Open = t.Open,
                    High = t.High,
                    Low = t.Low,
                    Close = t.Close,
                    Volume = t.Volume,
                });
            }
            list1.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
            list2.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
            int count = list1.Count;

            var parabolicSarList1 = ParabolicSar.GetParabolicSar(quoteList1, step1, max1, start1).ToList();
            parabolicSarList1.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
            var parabolicSarList2 = ParabolicSar.GetParabolicSar(quoteList2, step2, max2, start2).ToList();
            parabolicSarList2.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);

            //for (int i = 1; i < parabolicSarList1.Count - 1; i++)
            //{
            //    var pSar1 = parabolicSarList1[i];
            //    if (pSar1.Sar == pSar1.OriginalSar)
            //        logger.WriteLine($"{pSar1.Date:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar:F4}  /  {pSar1.IsReversal}");
            //    else
            //        logger.WriteLine($"{pSar1.Date:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar:F4}  /  {pSar1.IsReversal}  /  {pSar1.OriginalSar:F4}");
            //}
            //return 0;

            //for (int i = parabolicSarList1.Count - 100; i < parabolicSarList1.Count - 1; i++)
            //{
            //    var pSar2 = parabolicSarList2.Find(x => x.Timestamp > list1[i].Timestamp);
            //    var b = list2.Find(x => x.Timestamp == pSar2.Date);
            //    if (pSar2.Sar == pSar2.OriginalSar)
            //        logger.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {b.Timestamp:yyyy-MM-dd HH:mm:ss} \t {b.Open} / {b.High} / {b.Low} / {b.Close} \t {pSar2.Sar:F4}  /  {pSar2.IsReversal}");
            //    else
            //        logger.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {b.Timestamp:yyyy-MM-dd HH:mm:ss} \t {b.Open} / {b.High} / {b.Low} / {b.Close} \t {pSar2.Sar:F4}  /  {pSar2.IsReversal}  /  {pSar2.OriginalSar:F4}");
            //}
            //return 0;

            int tryCount = 0;
            float minHeight = 0, maxHeight = 0;
            float totalProfit = 0;
            float finalPercent = 1;
            float finalPercent2 = 1;
            float finalPercent3 = 1;
            int position = 0;
            int positionEntryPrice = 0;

            int profitCount = 0, lossCount = 0;
            float bestValue = 0, worstValue = 0;

            for (int i = 1; i < count - 1; i++)
            {
                var pSar1 = parabolicSarList1[i];
                var pSar2 = parabolicSarList2.Find(x => x.Timestamp > pSar1.Timestamp);
                if (position == 0)
                {
                    if (pSar1.OriginalSar.Value < pSar1.Sar.Value && pSar2.Sar.Value >= pSar1.Sar.Value)
                    {
                        position = -1;
                        positionEntryPrice = (int)pSar1.OriginalSar.Value;
                        //if (list1[i].Timestamp.DayOfWeek == DayOfWeek.Saturday || list1[i].Timestamp.DayOfWeek == DayOfWeek.Sunday)
                        //    logger.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar:F0} / {pSar1.IsReversal} \t <SHORT>", ConsoleColor.Red);
                        //else
                        logger.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar:F0} / {pSar1.IsReversal} \t <SHORT>");
                        bestValue = 0;
                        worstValue = 0;
                    }
                    else if (pSar1.OriginalSar.Value > pSar1.Sar.Value && pSar2.Sar.Value <= pSar1.Sar.Value)
                    {
                        position = 1;
                        positionEntryPrice = (int)pSar1.OriginalSar.Value;
                        //if (list1[i].Timestamp.DayOfWeek == DayOfWeek.Saturday || list1[i].Timestamp.DayOfWeek == DayOfWeek.Sunday)
                        //    logger.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar:F0} / {pSar1.IsReversal} \t <LONG>", ConsoleColor.Red);
                        //else
                        logger.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar:F0} / {pSar1.IsReversal} \t <LONG>");
                        bestValue = 0;
                        worstValue = 0;
                    }
                }
                else if (position == 1)
                {
                    if (bestValue < (float)(list1[i].High - positionEntryPrice) / positionEntryPrice) bestValue = (float)(list1[i].High - positionEntryPrice) / positionEntryPrice;
                    if (worstValue > (float)(list1[i].Low - positionEntryPrice) / positionEntryPrice) worstValue = (float)(list1[i].Low - positionEntryPrice) / positionEntryPrice;

                    if ((float)list1[i].Low / positionEntryPrice < 1 - stopLoss)
                    {
                        tryCount++;
                        int close = (int)Math.Floor(positionEntryPrice * (1 - stopLoss));
                        int profit = close - positionEntryPrice;
                        if (profit > 0)
                            profitCount++;
                        else
                            lossCount++;
                        totalProfit += profit - positionEntryPrice * takerFee;
                        finalPercent *= (float)close / positionEntryPrice - takerFee;
                        finalPercent2 *= 1 + ((float)close / positionEntryPrice - takerFee - 1) * 2;
                        finalPercent3 *= 1 + ((float)close / positionEntryPrice - takerFee - 1) * 3;
                        if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
                        if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
                        if (profit > 0)
                            logger.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar:F0} / {pSar1.IsReversal} \t entry = {positionEntryPrice} \t profit = {profit} \t total = {totalProfit} \t % = {finalPercent:F4} / {finalPercent2:F4} \t {bestValue} / {worstValue}");
                        else
                            logger.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar:F0} / {pSar1.IsReversal} \t entry = {positionEntryPrice} \t profit = {profit} \t total = {totalProfit} \t % = {finalPercent:F4} / {finalPercent2:F4} \t {bestValue} / {worstValue}", ConsoleColor.Red);
                        //position = -1;
                        //positionEntryPrice = close;
                        position = 0;
                    }
                    else if (pSar1.IsReversal.Value)
                    {
                        if (pSar1.OriginalSar.Value < pSar1.Sar.Value)
                        {
                            tryCount++;
                            int close = (int)Math.Max(pSar1.OriginalSar.Value, Math.Floor(positionEntryPrice * (1 - stopLoss)));
                            int profit = close - positionEntryPrice;
                            if (profit > 0)
                                profitCount++;
                            else
                                lossCount++;
                            totalProfit += profit - positionEntryPrice * takerFee;
                            finalPercent *= (float)close / positionEntryPrice - takerFee;
                            finalPercent2 *= 1 + ((float)close / positionEntryPrice - takerFee - 1) * 2;
                            finalPercent3 *= 1 + ((float)close / positionEntryPrice - takerFee - 1) * 3;
                            if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
                            if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
                            if (profit > 0)
                                logger.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar:F0} / {pSar1.IsReversal} \t entry = {positionEntryPrice} \t profit = {profit} \t total = {totalProfit} \t % = {finalPercent:F4} / {finalPercent2:F4} \t {bestValue} / {worstValue}");
                            else
                                logger.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar:F0} / {pSar1.IsReversal} \t entry = {positionEntryPrice} \t profit = {profit} \t total = {totalProfit} \t % = {finalPercent:F4} / {finalPercent2:F4} \t {bestValue} / {worstValue}", ConsoleColor.Red);
                            //position = -1;
                            //positionEntryPrice = close;
                            position = 0;
                        }
                        else
                        {
                            Console.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar} / {pSar1.IsReversal} / {pSar1.OriginalSar:F0} \t \t ERROR", ConsoleColor.Red);
                        }
                    }
                }
                else if (position == -1)
                {
                    if (bestValue < (float)(positionEntryPrice - list1[i].Low) / positionEntryPrice) bestValue = (float)(positionEntryPrice - list1[i].Low) / positionEntryPrice;
                    if (worstValue > (float)(positionEntryPrice - list1[i].High) / positionEntryPrice) worstValue = (float)(positionEntryPrice - list1[i].High) / positionEntryPrice;

                    if ((float)positionEntryPrice / list1[i].High < 1 - stopLoss)
                    {
                        tryCount++;
                        int close = (int)Math.Ceiling(positionEntryPrice / (1 - stopLoss));
                        int profit = positionEntryPrice - close;
                        if (profit > 0)
                            profitCount++;
                        else
                            lossCount++;
                        totalProfit += profit - positionEntryPrice * takerFee;
                        finalPercent *= (float)positionEntryPrice / close - takerFee;
                        finalPercent2 *= 1 + ((float)positionEntryPrice / close - takerFee - 1) * 2;
                        finalPercent3 *= 1 + ((float)positionEntryPrice / close - takerFee - 1) * 3;
                        if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
                        if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
                        if (profit > 0)
                            logger.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar:F0} / {pSar1.IsReversal} \t entry = {positionEntryPrice} \t profit = {profit} \t total = {totalProfit} \t % = {finalPercent:F4} / {finalPercent2:F4} \t {bestValue} / {worstValue}");
                        else
                            logger.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar:F0} / {pSar1.IsReversal} \t entry = {positionEntryPrice} \t profit = {profit} \t total = {totalProfit} \t % = {finalPercent:F4} / {finalPercent2:F4} \t {bestValue} / {worstValue}", ConsoleColor.Red);
                        //position = 1;
                        //positionEntryPrice = close;
                        position = 0;
                    }
                    else if (pSar1.IsReversal.Value)
                    {
                        if (pSar1.OriginalSar.Value > pSar1.Sar.Value)
                        {
                            tryCount++;
                            int close = (int)Math.Min(pSar1.OriginalSar.Value, Math.Ceiling(positionEntryPrice / (1 - stopLoss)));
                            int profit = positionEntryPrice - close;
                            if (profit > 0)
                                profitCount++;
                            else
                                lossCount++;
                            totalProfit += profit - positionEntryPrice * takerFee;
                            finalPercent *= (float)positionEntryPrice / close - takerFee;
                            finalPercent2 *= 1 + ((float)positionEntryPrice / close - takerFee - 1) * 2;
                            finalPercent3 *= 1 + ((float)positionEntryPrice / close - takerFee - 1) * 3;
                            if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
                            if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
                            if (profit > 0)
                                logger.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar:F0} / {pSar1.IsReversal} \t entry = {positionEntryPrice} \t profit = {profit} \t total = {totalProfit} \t % = {finalPercent:F4} / {finalPercent2:F4} \t {bestValue} / {worstValue}");
                            else
                                logger.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar:F0} / {pSar1.IsReversal} \t entry = {positionEntryPrice} \t profit = {profit} \t total = {totalProfit} \t % = {finalPercent:F4} / {finalPercent2:F4} \t {bestValue} / {worstValue}", ConsoleColor.Red);
                            //position = 1;
                            //positionEntryPrice = close;
                            position = 0;
                        }
                        else
                        {
                            Console.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar} / {pSar1.IsReversal} / {pSar1.OriginalSar:F0} \t \t ERROR", ConsoleColor.Red);
                        }
                    }
                }
            }
            float avgProfit = totalProfit / tryCount;
            string result = $"{startTime} ~ {endTime} ({totalDays} days) \t {start1:F6} / {step1:F6} / {max1:F6} \t {start2:F6} / {step2:F6} / {max2:F6} \t stop = {stopLoss:F4}" +
                $"\r\ncount = {count} / {parabolicSarList1.Count} / {parabolicSarList2.Count} \t try = {tryCount} / {profitCount} / {lossCount} \t h = {minHeight:F4} / {maxHeight:F4} \t p = {totalProfit:F2} \t avg = {avgProfit:F2} \t % = {finalPercent:F4} / {finalPercent2:F4} / {finalPercent3:F4}";
            logger.WriteLine($"\r\n{result}");
            return result;
        }

        static float BenchmarkSAR3()
        {
            //TestSAR3(); return 0;

            const float makerFee = 0.001f;
            const float takerFee = 0.002f;
            //const float makerFee = 0.0003f;
            //const float takerFee = 0.001f;
            const int binSize1 = 5;
            //const int binSize2 = 60;

            DateTime startTime = new DateTime(2022, 4, 13, 0, 0, 0, DateTimeKind.Utc);
            //DateTime startTime = new DateTime(2022, 2, 1, 0, 0, 0, DateTimeKind.Utc);
            //DateTime? endTime = new DateTime(2022, 4, 14, 0, 0, 0, DateTimeKind.Utc);
            DateTime? endTime = null;
            var list1m = SolDao.SelectAll("1m");
            var list1 = LoadBinListFrom1m(binSize1, list1m);
            //List<SolBin> list2 = LoadBinListFrom1m(binSize2, list1m);
            int count1 = list1.Count;
            int totalDays = (int)(list1.Last().Timestamp - startTime).TotalDays;
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}    BenchmarkSAR2    bin = {binSize1}    fee = {takerFee}    ({startTime:yyyy-MM-dd} ~ {endTime:yyyy-MM-dd} ({totalDays:N0}) days)");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count1} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;
            List<Dictionary<string, float>> topList = new List<Dictionary<string, float>>();

            var quoteList1 = new List<CandleQuote>();
            foreach (var t in list1)
            {
                quoteList1.Add(new CandleQuote
                {
                    Timestamp = t.Timestamp,
                    Open = t.Open,
                    High = t.High,
                    Low = t.Low,
                    Close = t.Close,
                    Volume = t.Volume,
                });
            }
            //var quoteList2 = new List<ParabolicSarQuote>();
            //foreach (var t in list2)
            //{
            //    quoteList2.Add(new ParabolicSarQuote
            //    {
            //        Date = t.Timestamp,
            //        Open = t.Open,
            //        High = t.High,
            //        Low = t.Low,
            //        Close = t.Close,
            //        Volume = t.Volume,
            //    });
            //}
            list1.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
            //list2.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);

            //List<ParabolicSarResult> parabolicSarList2 = ParabolicSar.GetParabolicSar(quoteList1, .0005f, .005f).ToList();
            //parabolicSarList2.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);

            for (float stopLoss = 0.01f; stopLoss <= 0.0151f; stopLoss += .001f)
            //float stopLoss = .012f;
            {
                for (float closeLimit = stopLoss; closeLimit <= stopLoss * 5; closeLimit += .002f)
                //float closeLimit = .02f;
                {
                    for (float step1 = 0.001f; step1 <= 0.01f; step1 += .0005f)
                    //float step1 = .0015f;
                    {
                        //for (float start1 = 0.001f; start1 <= 0.05f; start1 += .001f)
                        float start1 = step1;
                        //float start1 = .00186f;
                        {
                            for (float max1 = .01f; max1 <= .2; max1 += .01f)
                            //float max1 = .03f;
                            {
                                if (step1 >= max1) continue;

                                List<ParabolicSarResult> parabolicSarList1 = ParabolicSar.GetParabolicSar(quoteList1, step1, max1, start1).ToList();
                                parabolicSarList1.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
                                int count = parabolicSarList1.Count;

                                //for (float step2 = 0.0004f; step2 <= 0.0005f; step2 += .00001f)
                                //float step2 = 0.00044f;
                                {
                                    //for (float start2 = 0.0004f; start2 <= 0.0005f; start2 += .00001f)
                                    //float start2 = step2;
                                    {
                                        //for (float max2 = .015f; max2 <= 0.02; max2 += .001f)
                                        //float max2 = 0.017f;
                                        {
                                            //if (step2 >= max2) continue;
                                            //Console.WriteLine($"\r\n start = {start} \t step = {step} \t max = {max} \t count = {count} / {smaList.Count} / {parabolicSarList.Count}\r\n");

                                            //List<ParabolicSarResult> parabolicSarList2 = ParabolicSar.GetParabolicSar(quoteList2, step2, max2, start2).ToList();
                                            //parabolicSarList2.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);

                                            int tryCount = 0;
                                            int succeedCount = 0, failedCount = 0;
                                            float minHeight = 0, maxHeight = 0;
                                            float totalProfit = 0;
                                            float finalPercent = 1;
                                            int position = 0;
                                            int positionEntryPrice = 0;
                                            for (int i = 1; i < count - 1; i++)
                                            {
                                                var pSar1 = parabolicSarList1[i];
                                                //var pSar2 = parabolicSarList2.Find(x => x.Timestamp > pSar1.Date);
                                                //var pSar2 = parabolicSarList2[i];

                                                if (position == 0)
                                                {
                                                    if (pSar1.IsReversal.Value)
                                                    {
                                                        //if (pSar1.OriginalSar.Value < pSar1.Sar.Value && pSar2.Sar.Value >= pSar1.Sar.Value)
                                                        if (pSar1.OriginalSar.Value < pSar1.Sar.Value)
                                                        {
                                                            position = -1;
                                                            positionEntryPrice = (int)pSar1.OriginalSar.Value;
                                                        }
                                                        //else if (pSar1.OriginalSar.Value > pSar1.Sar.Value && pSar2.Sar.Value <= pSar1.Sar.Value)
                                                        else if (pSar1.OriginalSar.Value > pSar1.Sar.Value)
                                                        {
                                                            position = 1;
                                                            positionEntryPrice = (int)pSar1.OriginalSar.Value;
                                                        }
                                                    }
                                                    //else if (pSar2.IsReversal.Value && pSar1.Sar > list[i].Open && pSar2.Sar >= pSar1.Sar)
                                                    //{
                                                    //    position = -1;
                                                    //    positionEntryPrice = (int)parabolicSarList2[i - 1].Sar.Value;
                                                    //}
                                                }
                                                else if (position == 1)
                                                {
                                                    if ((float)list1[i].Low / positionEntryPrice < 1 - stopLoss || pSar1.IsReversal.Value && pSar1.OriginalSar.Value < pSar1.Sar.Value)
                                                    {
                                                        tryCount++;
                                                        int close = (int)Math.Ceiling(positionEntryPrice * (1 - stopLoss));
                                                        if (pSar1.IsReversal.Value) close = (int)Math.Max(close, pSar1.OriginalSar.Value);
                                                        int profit = close - positionEntryPrice;
                                                        totalProfit += profit - positionEntryPrice * takerFee;
                                                        finalPercent *= (float)close / positionEntryPrice - takerFee;
                                                        if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
                                                        if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
                                                        if (profit > 0) succeedCount++;
                                                        else failedCount++;
                                                        position = 0;
                                                        if (pSar1.IsReversal.Value) i--;
                                                    }
                                                    else if ((float)list1[i].High / positionEntryPrice > 1 + closeLimit)
                                                    {
                                                        tryCount++;
                                                        int close = (int)Math.Floor(positionEntryPrice * (1 + closeLimit));
                                                        int profit = close - positionEntryPrice;
                                                        totalProfit += profit - positionEntryPrice * makerFee;
                                                        finalPercent *= (float)close / positionEntryPrice - makerFee;
                                                        if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
                                                        if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
                                                        if (profit > 0) succeedCount++;
                                                        else failedCount++;
                                                        //position = -1;
                                                        //positionEntryPrice = close;
                                                        position = 0;
                                                    }
                                                    else if (pSar1.IsReversal.Value && pSar1.OriginalSar.Value >= pSar1.Sar.Value)
                                                    {
                                                        Console.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar} / {pSar1.IsReversal} / {pSar1.OriginalSar:F0} \t \t ERROR", ConsoleColor.Red);
                                                    }
                                                }
                                                else if (position == -1)
                                                {
                                                    if ((float)positionEntryPrice / list1[i].High < 1 - stopLoss || pSar1.IsReversal.Value && pSar1.OriginalSar.Value > pSar1.Sar.Value)
                                                    {
                                                        tryCount++;
                                                        int close = (int)Math.Floor(positionEntryPrice / (1 - stopLoss));
                                                        if (pSar1.IsReversal.Value) close = (int)Math.Min(close, pSar1.OriginalSar.Value);
                                                        int profit = positionEntryPrice - close;
                                                        totalProfit += profit - positionEntryPrice * takerFee;
                                                        finalPercent *= (float)positionEntryPrice / close - takerFee;
                                                        if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
                                                        if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
                                                        if (profit > 0) succeedCount++;
                                                        else failedCount++;
                                                        //position = 1;
                                                        //positionEntryPrice = close;
                                                        position = 0;
                                                        if (pSar1.IsReversal.Value) i--;
                                                    }
                                                    else if ((float)positionEntryPrice / list1[i].Low > 1 + closeLimit)
                                                    {
                                                        tryCount++;
                                                        int close = (int)Math.Ceiling(positionEntryPrice / (1 + closeLimit));
                                                        int profit = positionEntryPrice - close;
                                                        totalProfit += profit - positionEntryPrice * makerFee;
                                                        finalPercent *= (float)positionEntryPrice / close - makerFee;
                                                        if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
                                                        if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
                                                        if (profit > 0) succeedCount++;
                                                        else failedCount++;
                                                        //position = 1;
                                                        //positionEntryPrice = close;
                                                        position = 0;
                                                    }
                                                    else if (pSar1.IsReversal.Value && pSar1.OriginalSar.Value <= pSar1.Sar.Value)
                                                    {
                                                        Console.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar} / {pSar1.IsReversal} / {pSar1.OriginalSar:F0} \t \t ERROR", ConsoleColor.Red);
                                                    }
                                                }
                                            }
                                            //if (position == 1)
                                            //{
                                            //    tryCount++;
                                            //    int close = (int)((list1.Last().Close + parabolicSarList1.Last().Sar.Value) / 2);
                                            //    int profit = close - positionEntryPrice;
                                            //    totalProfit += profit - positionEntryPrice * takerFee;
                                            //    finalPercent *= (float)close / positionEntryPrice - takerFee;
                                            //    if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
                                            //    if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
                                            //    position = 0;
                                            //}
                                            //else if (position == 2)
                                            //{
                                            //    tryCount++;
                                            //    int close = (int)((list1.Last().Close + parabolicSarList1.Last().Sar.Value) / 2);
                                            //    int profit = positionEntryPrice - close;
                                            //    totalProfit += profit - positionEntryPrice * takerFee;
                                            //    finalPercent *= (float)positionEntryPrice / close - takerFee;
                                            //    if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
                                            //    if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
                                            //    position = 0;
                                            //}
                                            float avgProfit = totalProfit / tryCount;
                                            float successRate = failedCount == 0 ? failedCount : ((float)succeedCount / failedCount);
                                            float score = (finalPercent - 1) / stopLoss;
                                            if (totalProfit > 0 && finalPercent > 1f && tryCount >= totalDays / 2)
                                            {
                                                Dictionary<string, float> dic = new Dictionary<string, float>
                                                {
                                                    { "bin1", binSize1 },
                                                    { "start1", start1 },
                                                    { "step1", step1 },
                                                    { "max1", max1 },
                                                    //{ "bin2", binSize2 },
                                                    //{ "start2", start2 },
                                                    //{ "step2", step2 },
                                                    //{ "max2", max2 },
                                                    { "closeLimit",closeLimit },
                                                    { "stopLoss",stopLoss },
                                                    { "tryCount", tryCount },
                                                    { "succeedCount", succeedCount },
                                                    { "failedCount", failedCount },
                                                    { "successRate", successRate },
                                                    { "minHeight", minHeight },
                                                    { "maxHeight", maxHeight },
                                                    { "totalProfit", totalProfit },
                                                    { "avgProfit", avgProfit },
                                                    { "finalPercent", finalPercent },
                                                    { "score", score },
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
                                                        if (topList[i]["finalPercent"] > finalPercent ||
                                                                topList[i]["finalPercent"] == finalPercent && topList[i]["avgProfit"] > avgProfit)
                                                        //if (topList[i]["score"] > score)
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
                                                logger.WriteLine($"{start1:F6} / {step1:F6} / {max1:F6} \t limit = {closeLimit:F4} / {stopLoss:F4} \t count = {tryCount} / {succeedCount} / {failedCount} / {successRate:F4} \t h = {minHeight:F4} / {maxHeight:F4} \t p = {totalProfit:F2} \t avg = {avgProfit:F2} \t % = {finalPercent:F4} / +{score:F4}");
                                            }
                                            else
                                            {
                                                //Console.WriteLine($"{start1:F6} / {step1:F6} / {max1:F6} \t limit = {closeLimit:F4} / {stopLoss:F4} \t count = {tryCount} / {succeedCount} / {failedCount} / {successRate:F4} \t h = {minHeight:F4} / {maxHeight:F4} \t p = {totalProfit:F2} \t avg = {avgProfit:F2} \t % = {finalPercent:F4}");
                                            }
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

        static string TestSAR3()
        {
            //TestSAR3(new DateTime(2022, 3, 16, 0, 0, 0, DateTimeKind.Utc)); return null;

            string result = "";
            result += TestSAR3(new DateTime(2022, 4, 14, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += TestSAR3(new DateTime(2022, 4, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += TestSAR3(new DateTime(2022, 3, 23, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 4, 14, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += TestSAR3(new DateTime(2022, 3, 16, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 4, 14, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += TestSAR3(new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 4, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += TestSAR3(new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 3, 15, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += TestSAR3(new DateTime(2022, 2, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += TestSAR3(new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 2, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += TestSAR3(new DateTime(2021, 12, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += TestSAR3(new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 12, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n";
            Console.WriteLine("\r\n\r\n================================\r\n");
            Console.WriteLine(result);
            return result;
        }

        static string TestSAR3(DateTime startTime, DateTime? endTime = null)
        {
            //const float makerFee = 0.0003f;
            //const float takerFee = 0.002f;
            const float makerFee = 0.001f;
            const float takerFee = 0.002f;
            const int binSize1 = 5;
            //const int binSize2 = 60;
            float start1 = 0.014f;
            float step1 = 0.014f;
            float max1 = 0.14f;
            //float start2 = 0.00044f;
            //float step2 = start2;
            //float max2 = 0.017f;
            float closeLimit = 0.01f;
            float stopLoss = 0.02f;

            //DateTime startTime = new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc);
            //DateTime? endTime = null;
            List<SolBin> list1m = SolDao.SelectAll("1m");
            List<SolBin> list1 = LoadBinListFrom1m(binSize1, list1m);
            //List<SolBin> list2 = LoadBinListFrom1m(binSize2, list1m);
            int count1 = list1.Count;
            int totalDays = (int)(list1.Last().Timestamp - startTime).TotalDays;
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}    TestSAR2    bin = {binSize}    takerFee = {takerFee}    ({startTime:yyyy-MM-dd HH.mm.ss} ~ {totalDays:N0} days)");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count1} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;

            var quoteList1 = new List<CandleQuote>();
            foreach (var t in list1)
            {
                quoteList1.Add(new CandleQuote
                {
                    Timestamp = t.Timestamp,
                    Open = t.Open,
                    High = t.High,
                    Low = t.Low,
                    Close = t.Close,
                    Volume = t.Volume,
                });
            }
            //var quoteList2 = new List<ParabolicSarQuote>();
            //foreach (var t in list2)
            //{
            //    quoteList2.Add(new ParabolicSarQuote
            //    {
            //        Date = t.Timestamp,
            //        Open = t.Open,
            //        High = t.High,
            //        Low = t.Low,
            //        Close = t.Close,
            //        Volume = t.Volume,
            //    });
            //}
            list1.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
            //list2.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
            int count = list1.Count;

            var parabolicSarList1 = ParabolicSar.GetParabolicSar(quoteList1, step1, max1, start1).ToList();
            parabolicSarList1.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
            var parabolicSarList2 = ParabolicSar.GetParabolicSar(quoteList1, .0005f, .005f).ToList();
            parabolicSarList2.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);

            //for (int i = 1; i < parabolicSarList1.Count - 1; i++)
            //{
            //    var pSar1 = parabolicSarList1[i];
            //    if (pSar1.Sar == pSar1.OriginalSar)
            //        logger.WriteLine($"{pSar1.Date:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar:F4}  /  {pSar1.IsReversal}");
            //    else
            //        logger.WriteLine($"{pSar1.Date:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar:F4}  /  {pSar1.IsReversal}  /  {pSar1.OriginalSar:F4}");
            //}
            //return 0;

            //for (int i = parabolicSarList1.Count - 100; i < parabolicSarList1.Count - 1; i++)
            //{
            //    var pSar2 = parabolicSarList2.Find(x => x.Timestamp > list1[i].Timestamp);
            //    var b = list2.Find(x => x.Timestamp == pSar2.Date);
            //    if (pSar2.Sar == pSar2.OriginalSar)
            //        logger.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {b.Timestamp:yyyy-MM-dd HH:mm:ss} \t {b.Open} / {b.High} / {b.Low} / {b.Close} \t {pSar2.Sar:F4}  /  {pSar2.IsReversal}");
            //    else
            //        logger.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {b.Timestamp:yyyy-MM-dd HH:mm:ss} \t {b.Open} / {b.High} / {b.Low} / {b.Close} \t {pSar2.Sar:F4}  /  {pSar2.IsReversal}  /  {pSar2.OriginalSar:F4}");
            //}
            //return 0;

            int tryCount = 0;
            float minHeight = 0, maxHeight = 0;
            float totalProfit = 0;
            float finalPercent = 1;
            float finalPercent2 = 1;
            float finalPercent3 = 1;
            float finalPercent5 = 1;
            float finalPercent10 = 1;
            int position = 0;
            int positionEntryPrice = 0;
            int positionCreated = 0;

            int profitCount = 0, lossCount = 0, closeCount = 0, stopCount = 0, csrCount = 0;

            for (int i = 1; i < count - 1; i++)
            {
                var pSar1 = parabolicSarList1[i];
                //var pSar2 = parabolicSarList2.Find(x => x.Timestamp > pSar1.Date);
                var pSar2 = parabolicSarList2[i];
                pSar2.Sar = null;
                if (position == 0)
                {
                    if (list1[i].Timestamp.Hour >= 0 && list1[i].Timestamp.Hour <= 3) continue;
                    //if (list1[i].Timestamp.DayOfWeek == DayOfWeek.Saturday) continue;
                    if (pSar1.OriginalSar.Value < pSar1.Sar.Value && (pSar2.Sar == null || pSar2.Sar.Value >= pSar1.Sar.Value))
                    {
                        position = -1;
                        positionEntryPrice = (int)pSar1.OriginalSar.Value;
                        positionCreated = i;
                        //if (list1[i].Timestamp.DayOfWeek == DayOfWeek.Saturday || list1[i].Timestamp.DayOfWeek == DayOfWeek.Sunday)
                        //    logger.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar:F0} / {pSar1.IsReversal} \t <SHORT>", ConsoleColor.Red);
                        //else
                        logger.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar:F0} / {pSar1.IsReversal} \t <SHORT>");
                    }
                    else if (pSar1.OriginalSar.Value > pSar1.Sar.Value && (pSar2.Sar == null || pSar2.Sar.Value <= pSar1.Sar.Value))
                    {
                        position = 1;
                        positionEntryPrice = (int)pSar1.OriginalSar.Value;
                        positionCreated = i;
                        //if (list1[i].Timestamp.DayOfWeek == DayOfWeek.Saturday || list1[i].Timestamp.DayOfWeek == DayOfWeek.Sunday)
                        //    logger.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar:F0} / {pSar1.IsReversal} \t <LONG>", ConsoleColor.Red);
                        //else
                        logger.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar:F0} / {pSar1.IsReversal} \t <LONG>");
                    }
                }
                else if (position == 1)
                {
                    if (i >= positionCreated + 1)
                    {
                        tryCount++;
                        int close = list1[i].Close;
                        int profit = close - positionEntryPrice;
                        if (profit > 0) profitCount++;
                        else lossCount++;
                        totalProfit += profit - positionEntryPrice * takerFee;
                        finalPercent *= (float)close / positionEntryPrice - takerFee;
                        finalPercent2 *= 1 + ((float)close / positionEntryPrice - takerFee - 1) * 2;
                        finalPercent3 *= 1 + ((float)close / positionEntryPrice - takerFee - 1) * 3;
                        finalPercent5 *= 1 + ((float)close / positionEntryPrice - takerFee - 1) * 5;
                        finalPercent10 *= 1 + ((float)close / positionEntryPrice - takerFee - 1) * 10;
                        if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
                        if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
                        if (profit > 0)
                            logger.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar:F0} / {pSar1.IsReversal} \t entry = {positionEntryPrice} \t profit = {profit} \t total = {totalProfit} \t % = {finalPercent:F4} / {finalPercent2:F4}");
                        else
                            logger.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar:F0} / {pSar1.IsReversal} \t entry = {positionEntryPrice} \t profit = {profit} \t total = {totalProfit} \t % = {finalPercent:F4} / {finalPercent2:F4}", ConsoleColor.Red);
                        //position = -1;
                        //positionEntryPrice = close;
                        position = 0;
                    }
                    else if ((float)list1[i].Low / positionEntryPrice < 1 - stopLoss || pSar1.IsReversal.Value && pSar1.OriginalSar.Value < pSar1.Sar.Value)
                    {
                        tryCount++;
                        int close = (int)Math.Ceiling(positionEntryPrice * (1 - stopLoss));
                        if (pSar1.IsReversal.Value) close = (int)Math.Max(close, pSar1.OriginalSar.Value);
                        int profit = close - positionEntryPrice;
                        if (profit > 0) profitCount++;
                        else lossCount++;
                        totalProfit += profit - positionEntryPrice * takerFee;
                        finalPercent *= (float)close / positionEntryPrice - takerFee;
                        finalPercent2 *= 1 + ((float)close / positionEntryPrice - takerFee - 1) * 2;
                        finalPercent3 *= 1 + ((float)close / positionEntryPrice - takerFee - 1) * 3;
                        finalPercent5 *= 1 + ((float)close / positionEntryPrice - takerFee - 1) * 5;
                        finalPercent10 *= 1 + ((float)close / positionEntryPrice - takerFee - 1) * 10;
                        if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
                        if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
                        if (profit > 0)
                            logger.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar:F0} / {pSar1.IsReversal} \t entry = {positionEntryPrice} \t profit = {profit} \t total = {totalProfit} \t % = {finalPercent:F4} / {finalPercent2:F4}");
                        else
                            logger.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar:F0} / {pSar1.IsReversal} \t entry = {positionEntryPrice} \t profit = {profit} \t total = {totalProfit} \t % = {finalPercent:F4} / {finalPercent2:F4}", ConsoleColor.Red);
                        //position = -1;
                        //positionEntryPrice = close;
                        position = 0;
                        if (pSar1.IsReversal.Value)
                        {
                            csrCount++;
                            i--;
                        }
                        else
                        {
                            stopCount++;
                        }
                    }
                    else if ((float)list1[i].High / positionEntryPrice > 1 + closeLimit)
                    {
                        tryCount++;
                        int close = (int)Math.Floor(positionEntryPrice * (1 + closeLimit));
                        int profit = close - positionEntryPrice;
                        profitCount++;
                        closeCount++;
                        totalProfit += profit - positionEntryPrice * makerFee;
                        finalPercent *= (float)close / positionEntryPrice - makerFee;
                        finalPercent2 *= 1 + ((float)close / positionEntryPrice - makerFee - 1) * 2;
                        finalPercent3 *= 1 + ((float)close / positionEntryPrice - makerFee - 1) * 3;
                        finalPercent5 *= 1 + ((float)close / positionEntryPrice - makerFee - 1) * 5;
                        finalPercent10 *= 1 + ((float)close / positionEntryPrice - makerFee - 1) * 10;
                        if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
                        if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
                        logger.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar:F0} / {pSar1.IsReversal} \t entry = {positionEntryPrice} \t profit = {profit} \t total = {totalProfit} \t % = {finalPercent:F4} / {finalPercent2:F4}");
                        //position = -1;
                        //positionEntryPrice = close;
                        position = 0;
                    }
                    else if (pSar1.IsReversal.Value && pSar1.OriginalSar.Value >= pSar1.Sar.Value)
                    {
                        Console.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar} / {pSar1.IsReversal} / {pSar1.OriginalSar:F0} \t \t ERROR", ConsoleColor.Red);
                    }
                }
                else if (position == -1)
                {
                    if (i >= positionCreated + 1)
                    {
                        tryCount++;
                        int close = list1[i].Close;
                        int profit = close - positionEntryPrice;
                        if (profit > 0) profitCount++;
                        else lossCount++;
                        totalProfit += profit - positionEntryPrice * takerFee;
                        finalPercent *= (float)positionEntryPrice / close - takerFee;
                        finalPercent2 *= 1 + ((float)positionEntryPrice / close - takerFee - 1) * 2;
                        finalPercent3 *= 1 + ((float)positionEntryPrice / close - takerFee - 1) * 3;
                        finalPercent5 *= 1 + ((float)positionEntryPrice / close - takerFee - 1) * 5;
                        finalPercent10 *= 1 + ((float)positionEntryPrice / close - takerFee - 1) * 10;
                        if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
                        if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
                        if (profit > 0)
                            logger.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar:F0} / {pSar1.IsReversal} \t entry = {positionEntryPrice} \t profit = {profit} \t total = {totalProfit} \t % = {finalPercent:F4} / {finalPercent2:F4}");
                        else
                            logger.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar:F0} / {pSar1.IsReversal} \t entry = {positionEntryPrice} \t profit = {profit} \t total = {totalProfit} \t % = {finalPercent:F4} / {finalPercent2:F4}", ConsoleColor.Red);
                        //position = 1;
                        //positionEntryPrice = close;
                        position = 0;
                    }
                    else if ((float)positionEntryPrice / list1[i].High < 1 - stopLoss || pSar1.IsReversal.Value && pSar1.OriginalSar.Value > pSar1.Sar.Value)
                    {
                        tryCount++;
                        int close = (int)Math.Floor(positionEntryPrice / (1 - stopLoss));
                        if (pSar1.IsReversal.Value) close = (int)Math.Min(close, pSar1.OriginalSar.Value);
                        int profit = positionEntryPrice - close;
                        if (profit > 0) profitCount++;
                        else lossCount++;
                        totalProfit += profit - positionEntryPrice * takerFee;
                        finalPercent *= (float)positionEntryPrice / close - takerFee;
                        finalPercent2 *= 1 + ((float)positionEntryPrice / close - takerFee - 1) * 2;
                        finalPercent3 *= 1 + ((float)positionEntryPrice / close - takerFee - 1) * 3;
                        finalPercent5 *= 1 + ((float)positionEntryPrice / close - takerFee - 1) * 5;
                        finalPercent10 *= 1 + ((float)positionEntryPrice / close - takerFee - 1) * 10;
                        if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
                        if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
                        if (profit > 0)
                            logger.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar:F0} / {pSar1.IsReversal} \t entry = {positionEntryPrice} \t profit = {profit} \t total = {totalProfit} \t % = {finalPercent:F4} / {finalPercent2:F4}");
                        else
                            logger.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar:F0} / {pSar1.IsReversal} \t entry = {positionEntryPrice} \t profit = {profit} \t total = {totalProfit} \t % = {finalPercent:F4} / {finalPercent2:F4}", ConsoleColor.Red);
                        //position = 1;
                        //positionEntryPrice = close;
                        position = 0;
                        if (pSar1.IsReversal.Value)
                        {
                            csrCount++;
                            i--;
                        }
                        else
                        {
                            stopCount++;
                        }
                    }
                    else if ((float)positionEntryPrice / list1[i].Low > 1 + closeLimit)
                    {
                        tryCount++;
                        int close = (int)Math.Ceiling(positionEntryPrice / (1 + closeLimit));
                        int profit = positionEntryPrice - close;
                        profitCount++;
                        closeCount++;
                        totalProfit += profit - positionEntryPrice * makerFee;
                        finalPercent *= (float)positionEntryPrice / close - makerFee;
                        finalPercent2 *= 1 + ((float)positionEntryPrice / close - makerFee - 1) * 2;
                        finalPercent3 *= 1 + ((float)positionEntryPrice / close - makerFee - 1) * 3;
                        finalPercent5 *= 1 + ((float)positionEntryPrice / close - makerFee - 1) * 5;
                        finalPercent10 *= 1 + ((float)positionEntryPrice / close - makerFee - 1) * 10;
                        if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
                        if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
                        logger.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar:F0} / {pSar1.IsReversal} \t entry = {positionEntryPrice} \t profit = {profit} \t total = {totalProfit} \t % = {finalPercent:F4} / {finalPercent2:F4}");
                        //position = 1;
                        //positionEntryPrice = close;
                        position = 0;
                    }
                    else if ((float)positionEntryPrice / list1[i].Low > 1 + closeLimit)
                    {
                        Console.WriteLine($"{list1[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list1[i].Open} / {list1[i].High} / {list1[i].Low} / {list1[i].Close} \t {pSar1.Sar} / {pSar1.IsReversal} / {pSar1.OriginalSar:F0} \t \t ERROR", ConsoleColor.Red);
                    }
                }
            }
            float avgProfit = totalProfit / tryCount;
            string result = $"{startTime} ~ {endTime} ({totalDays} days) \t {start1:F6} / {step1:F6} / {max1:F6} \t close = {closeLimit:F4} \t stop = {stopLoss:F4}" +
                $"\r\ncount = {count} / {parabolicSarList1.Count} \t try = {tryCount} / {profitCount} / {lossCount} : {closeCount} / {stopCount} / {csrCount} \t h = {minHeight:F4} / {maxHeight:F4} \t p = {totalProfit:F2} \t avg = {avgProfit:F2} \t % = {finalPercent:F4} / {finalPercent2:F4} / {finalPercent3:F4} / {finalPercent5:F4} / {finalPercent10:F4}";
            logger.WriteLine($"\r\n{result}");
            return result;
        }

        const float lossX = 1.5f;

        static string TestSMA(string binSize, int buyOrSell, int smaLength, int delayLength, DateTime startTime, DateTime? endTime = null)
        {
            List<SolBin> list = SolDao.SelectAll(binSize);
            int count = list.Count;
            {
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
            int totalDays = (int)(list[count - 1].Timestamp - list[0].Timestamp).TotalDays;
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  buyOrSell = {buyOrSell}    sma = {smaLength}    delay = {delayLength}    ({list[0].Date} ~ {totalDays:N0} days)");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;
            var topList = new List<Dictionary<string, object>>();

            const int allowMaxWeight = 5;

            for (float limitX = 0.01f; limitX < 0.1f; limitX += 0.001f)
            {
                for (float closeX = 0.005f; closeX < 0.1; closeX += 0.001f)
                {
                    int tryCount = 0, succeedCount = 0;
                    float positionEntryPrice = 0;
                    int positionQty = 0;
                    int maxWeight = 0, maxDeep = 0;
                    double totalProfit = 0;
                    double totalPercent = 1;
                    for (int i = delayLength; i < count - 1; i++)
                    {
                        float sma = list[i - delayLength].SMA;
                        if (buyOrSell == 1)
                        {
                            if (positionEntryPrice > 0 && list[i].High > positionEntryPrice * (1 + closeX))
                            {
                                succeedCount++;
                                totalProfit += (closeX - .0002) * positionQty;
                                totalPercent *= (1 + (closeX - .0002) * positionQty);
                                positionQty = 0;
                                positionEntryPrice = 0;
                                continue;
                            }
                            if (positionQty == 0 || positionEntryPrice > sma)
                            {
                                int limitPrice;
                                int j = 0;
                                do
                                {
                                    limitPrice = (int)Math.Ceiling(sma - sma * limitX * (1f + j));
                                    if (list[i].Open > limitPrice && list[i].Low < limitPrice)
                                    {
                                        tryCount++;
                                        positionEntryPrice = (positionEntryPrice * positionQty + limitPrice) / (positionQty + 1);
                                        positionQty++;
                                        //logger.WriteLine($"{list[i].Timestamp} \t limit = {limitPrice} \t try = {tryCount} \t succeed = {succeedCount} \t entry = {positionEntryPrice} \t qty = {positionQty}");
                                        if (maxWeight < positionQty)
                                        {
                                            maxWeight = positionQty;
                                            if (maxWeight > allowMaxWeight) goto nextLimit;
                                        }
                                    }
                                    else if (list[i].Low > limitPrice) break;
                                    j++;
                                }
                                while (list[i].Open < limitPrice);
                                if (maxDeep < j) maxDeep = j;
                            }
                        }
                    }
                    double score = 100 * totalProfit * limitX;
                    string text = $"{limitX:F4}  /  {closeX:F4} \t try = {tryCount} \t succeed = {succeedCount} \t maxD = {maxDeep} \t maxW = {maxWeight} \t total = {totalProfit:F4} \t score = {score:F4} \t % = {totalPercent:F4}";
                    if (succeedCount > totalDays / 10)
                    {
                        var dic = new Dictionary<string, object>
                        {
                            { "limitX", limitX },
                            { "closeX", closeX },
                            { "tryCount", tryCount },
                            { "succeedCount", succeedCount },
                            { "maxWeight", maxWeight },
                            { "maxDeep", maxDeep },
                            { "totalProfit", totalProfit },
                            { "score", score },
                            { "totalPercent", totalPercent },
                            { "text", text },
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
                                if ((double)topList[i]["score"] > score)
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
                        logger.WriteLine(text);
                    }
                    if (tryCount < 50) goto endLimit;
                }
            nextLimit:;
            }
        endLimit:;
            logger.WriteLine(JArray.FromObject(topList).ToString());
            logger.WriteLine();
            if (topList.Count > 0)
                return (string)topList[topList.Count - 1]["text"];
            return null;
        }

        static float BenchmarkRSILimit()
        {
            //TestRSILimit(); return 0;

            const int buyOrSell = 2;
            const int binSize = 30;

            const float makerFee = 0.0015f;
            const float takerFee = 0.003f;

            //DateTime startTime = new DateTime(2022, 4, 9, 0, 0, 0, DateTimeKind.Utc);
            DateTime startTime = new DateTime(2022, 3, 15, 0, 0, 0, DateTimeKind.Utc);
            DateTime? endTime = null;// new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            List<SolBin> list1m = SolDao.SelectAll("1m");
            List<SolBin> list = LoadBinListFrom1m(binSize, list1m);

            int count = list.Count;
            const int rsiLength = 14;
            //int smaLength = 12 * 60 / binSize;
            int smaLength = 7 * 24 * 60 / binSize;
            {
                for (int i = smaLength; i < count; i++)
                {
                    float[] closeArray = new float[smaLength];
                    for (int j = 0; j < smaLength; j++)
                        closeArray[j] = list[i - smaLength + j].Close;
                    list[i].SMA = closeArray.Average();
                }
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
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  buyOrSell = {buyOrSell}    ({list[0].Date} ~ {totalDays:N0} days)");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;

            List<Dictionary<string, float>> topList = new List<Dictionary<string, float>>();

            for (float minRSI = 0; minRSI < 90; minRSI += 2)
            //float minRSI = 20;
            {
                for (float maxRSI = minRSI + 2; maxRSI < 100; maxRSI += 2f)
                //float maxRSI = 30f;
                {
                    for (float stopX = 0.002f; stopX < 0.025f; stopX += 0.002f)
                    //float stopX = 0.0135f;
                    {
                        for (float closeX = 0.002f; closeX < 0.05f; closeX += 0.002f)
                        //float closeX = 0.0075f;
                        {
                            int succeedCount = 0, failedCount = 0, unknownCount = 0;
                            float totalProfit = 0;
                            float finalPercent = 1;
                            for (int i = 2; i < count - 1; i++)
                            {
                                if (buyOrSell == 1 && list[i].RSI >= minRSI && list[i].RSI < maxRSI && list[i].Open < list[i].Close && list[i].Close > list[i].SMA)
                                {
                                    int positionEntryPrice = list[i].Close;
                                    for (int j = i + 1; j < count && j < i + rsiLength * 2; j++)
                                    {
                                        if (list[j].Low < positionEntryPrice - positionEntryPrice * stopX)
                                        {
                                            failedCount++;
                                            totalProfit -= stopX + takerFee;
                                            finalPercent *= 1 - stopX - takerFee;
                                            i = j;
                                            goto solved;
                                        }
                                        else if (list[j].High > positionEntryPrice + positionEntryPrice * closeX)
                                        {
                                            succeedCount++;
                                            totalProfit += closeX - makerFee;
                                            finalPercent *= 1 + closeX - makerFee;
                                            i = j;
                                            goto solved;
                                        }
                                        //i = j + 1;
                                    }
                                    unknownCount++;
                                    totalProfit -= stopX + takerFee;
                                    finalPercent *= 1 - stopX - takerFee;
                                solved:;
                                }
                                else if (buyOrSell == 2 && (list[i].RSI >= minRSI && list[i].RSI < maxRSI || list[i - 1].RSI >= minRSI && list[i - 1].RSI < maxRSI) && /* list[i - 1].Low > list[i].Low && */ list[i].Open > list[i].Close && list[i - 1].Open > list[i - 1].Close && list[i].Close < list[i].SMA && list[i - 1].Volume > list[i].Volume)
                                {
                                    int positionEntryPrice = list[i].Close;
                                    for (int j = i + 1; j < count && j < i + rsiLength * 2; j++)
                                    {
                                        if (list[j].High > positionEntryPrice + positionEntryPrice * stopX)
                                        {
                                            failedCount++;
                                            totalProfit -= stopX + takerFee;
                                            finalPercent *= 1 - stopX - takerFee;
                                            //i = j;
                                            goto solved;
                                        }
                                        else if (list[j].Low < positionEntryPrice - positionEntryPrice * closeX)
                                        {
                                            succeedCount++;
                                            totalProfit += closeX - makerFee;
                                            finalPercent *= 1 + closeX - makerFee;
                                            //i = j;
                                            goto solved;
                                        }
                                        //i = j + 1;
                                    }
                                    unknownCount++;
                                    totalProfit -= stopX + takerFee;
                                    finalPercent *= 1 - stopX - takerFee;
                                solved:;
                                }
                            }
                            //float score = succeedCount - failedCount * stopX / closeX * lossX;
                            float score;
                            if (failedCount > 0) score = (float)succeedCount / failedCount;
                            else score = succeedCount;
                            float avgProfit = 10000 * totalProfit / totalDays * (float)Math.Sqrt((closeX - 0.001f) / (stopX + 0.002f));
                            if (finalPercent > 1 && succeedCount > 1)
                            {
                                Dictionary<string, float> dic = new Dictionary<string, float>
                                {
                                    { "buyOrSell", buyOrSell },
                                    { "binSize", binSize },
                                    { "smaLength", smaLength },
                                    { "makerFee", makerFee },
                                    { "takerFee", takerFee },
                                    { "totalDays", totalDays },
                                    { "minRSI", minRSI },
                                    { "maxRSI", maxRSI },
                                    { "closeX", closeX },
                                    { "stopX", stopX },
                                    { "succeedCount", succeedCount },
                                    { "failedCount", failedCount },
                                    { "unknownCount", unknownCount },
                                    { "score", score },
                                    { "totalProfit", totalProfit },
                                    { "avgProfit", avgProfit },
                                    { "finalPercent", finalPercent },
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
                                        if (topList[i]["finalPercent"] > finalPercent ||
                                                topList[i]["finalPercent"] == finalPercent && topList[i]["minRSI"] > minRSI ||
                                                topList[i]["finalPercent"] == finalPercent && topList[i]["minRSI"] == minRSI && topList[i]["maxRSI"] < maxRSI ||
                                                topList[i]["finalPercent"] == finalPercent && topList[i]["minRSI"] == minRSI && topList[i]["maxRSI"] == maxRSI && topList[i]["closeX"] > closeX ||
                                                topList[i]["finalPercent"] == finalPercent && topList[i]["minRSI"] == minRSI && topList[i]["maxRSI"] == maxRSI && topList[i]["closeX"] == closeX && topList[i]["stopX"] < stopX)
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
                                logger.WriteLine($"{minRSI} ~ {maxRSI}    {closeX:F4}  /  {stopX:F4} \t succeed = {succeedCount} \t failed = {failedCount} \t unknown = {unknownCount} \t score = {score:F2} \t total = {totalProfit:F4} \t avg = {avgProfit:F4} \t final = {finalPercent:F4}");
                            }
                            else if (finalPercent > .75f && succeedCount >= 1)
                            {
                                Console.WriteLine($"{minRSI} ~ {maxRSI}    {closeX:F4}  /  {stopX:F4} \t succeed = {succeedCount} \t failed = {failedCount} \t unknown = {unknownCount} \t score = {score:F2} \t total = {totalProfit:F4} \t avg = {avgProfit:F4} \t final = {finalPercent:F4}");
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

        static string TestRSILimit()
        {
            //TestRSILimit(new DateTime(2022, 4, 28, 0, 0, 0, DateTimeKind.Utc)); return null;

            string result = "";
            result += TestRSILimit(new DateTime(2022, 5, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += TestRSILimit(new DateTime(2022, 4, 9, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += TestRSILimit(new DateTime(2022, 4, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 5, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += TestRSILimit(new DateTime(2022, 3, 23, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 4, 9, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += TestRSILimit(new DateTime(2022, 3, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 4, 9, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += TestRSILimit(new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 4, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += TestRSILimit(new DateTime(2022, 2, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += TestRSILimit(new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 2, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += TestRSILimit(new DateTime(2021, 12, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += TestRSILimit(new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 12, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += TestRSILimit(new DateTime(2022, 2, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += TestRSILimit(new DateTime(2022, 3, 15, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            Console.WriteLine("\r\n\r\n================================\r\n");
            Console.WriteLine(result);
            return result;
        }

        static string TestRSILimit(DateTime startTime, DateTime? endTime = null)
        {
            const float makerFee = 0.0015f;
            const float takerFee = 0.003f;

            //const int buyOrSell = 2;
            //const int binSize = 30;
            //float minRSI = 30;
            //float maxRSI = 44;
            //float closeX = 0.022f;
            //float stopX = 0.014f;

            const int buyOrSell = 1;
            const int binSize = 30;
            float minRSI = 32;
            float maxRSI = 72;
            float closeX = 0.024f;
            float stopX = 0.016f;
            List<SolBin> list1m = SolDao.SelectAll("1m");
            List<SolBin> list = LoadBinListFrom1m(binSize, list1m);
            int count = list.Count;
            const int rsiLength = 14;
            //int smaLength = 12 * 60 / binSize;
            int smaLength = 7 * 24 * 60 / binSize;
            {
                for (int i = smaLength; i < count; i++)
                {
                    float[] closeArray = new float[smaLength];
                    for (int j = 0; j < smaLength; j++)
                        closeArray[j] = list[i - smaLength + j].Close;
                    list[i].SMA = closeArray.Average();
                }
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
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  buyOrSell = {buyOrSell}    ({list[0].Date} ~ {totalDays:N0} days)");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;

            int succeedCount = 0, failedCount = 0, unknownCount = 0;
            float totalProfit = 0;
            float finalPercent = 1;
            float finalPercent3 = 1;
            float finalPercent5 = 1;
            for (int i = 1; i < count - 1; i++)
            {
                if (buyOrSell == 1 && list[i].RSI >= minRSI && list[i].RSI < maxRSI && list[i].Open < list[i].Close && list[i].Close > list[i].SMA)
                //if (buyOrSell == 1 && (list[i].RSI >= minRSI && list[i].RSI < maxRSI || list[i - 1].RSI >= minRSI && list[i - 1].RSI < maxRSI) && /* list[i - 1].High < list[i].High && */ list[i].Open < list[i].Close && list[i - 1].Open < list[i - 1].Close && list[i].Open > list[i].SMA)
                {
                    int positionEntryPrice = list[i].Close;
                    logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t <LONG> \t entry = {positionEntryPrice}");
                    for (int j = i + 1; j < count && j < i + rsiLength * 2; j++)
                    {
                        if (list[j].Low < positionEntryPrice - positionEntryPrice * stopX)
                        {
                            failedCount++;
                            totalProfit -= stopX + takerFee;
                            finalPercent *= 1 - stopX - takerFee;
                            finalPercent3 *= 1 - (stopX + takerFee) * 3;
                            finalPercent5 *= 1 - (stopX + takerFee) * 5;
                            i = j;
                            logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t <LONG - Failed> \t stop = {positionEntryPrice - positionEntryPrice * stopX}");
                            goto solved;
                        }
                        else if (list[j].High > positionEntryPrice + positionEntryPrice * closeX)
                        {
                            succeedCount++;
                            totalProfit += closeX - makerFee;
                            finalPercent *= 1 + closeX - makerFee;
                            finalPercent3 *= 1 + (closeX - makerFee) * 3;
                            finalPercent5 *= 1 + (closeX - makerFee) * 5;
                            i = j;
                            logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t <LONG - Succeed> \t close = {positionEntryPrice + positionEntryPrice * closeX}");
                            goto solved;
                        }
                        //i = j + 1;
                    }
                    unknownCount++;
                    totalProfit -= stopX + takerFee;
                    finalPercent *= 1 - stopX - takerFee;
                    finalPercent3 *= 1 - (stopX + takerFee) * 3;
                    finalPercent5 *= 1 - (stopX + takerFee) * 5;
                solved:;
                }
                else if (buyOrSell == 2 && (list[i].RSI >= minRSI && list[i].RSI < maxRSI || list[i - 1].RSI >= minRSI && list[i - 1].RSI < maxRSI) && /* list[i - 1].Low > list[i].Low && */ list[i].Open > list[i].Close && list[i - 1].Open > list[i - 1].Close && list[i].Open < list[i].SMA)
                {
                    int positionEntryPrice = list[i].Close;
                    logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t <SHORT> \t entry = {positionEntryPrice}");
                    for (int j = i + 1; j < count && j < i + rsiLength * 2; j++)
                    {
                        if (list[j].High > positionEntryPrice + positionEntryPrice * stopX)
                        {
                            failedCount++;
                            totalProfit -= stopX + takerFee;
                            finalPercent *= 1 - stopX - takerFee;
                            finalPercent3 *= 1 - (stopX + takerFee) * 3;
                            finalPercent5 *= 1 - (stopX + takerFee) * 5;
                            i = j;
                            logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t <SHORT - Failed> \t stop = {positionEntryPrice + positionEntryPrice * stopX}");
                            goto solved;
                        }
                        else if (list[j].Low < positionEntryPrice - positionEntryPrice * closeX)
                        {
                            succeedCount++;
                            totalProfit += closeX - makerFee;
                            finalPercent *= 1 + closeX - makerFee;
                            finalPercent3 *= 1 + (closeX - makerFee) * 3;
                            finalPercent5 *= 1 + (closeX - makerFee) * 5;
                            i = j;
                            logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t <SHORT - Succeed> \t close = {positionEntryPrice - positionEntryPrice * closeX}");
                            goto solved;
                        }
                        //i = j + 1;
                    }
                    unknownCount++;
                    totalProfit -= stopX + takerFee;
                    finalPercent *= 1 - stopX - takerFee;
                    finalPercent3 *= 1 - (stopX + takerFee) * 3;
                    finalPercent5 *= 1 - (stopX + takerFee) * 5;
                solved:;
                }
            }
            //float score = succeedCount - failedCount * stopX / closeX * lossX;
            float score;
            if (failedCount > 0) score = (float)succeedCount / failedCount;
            else score = succeedCount;
            string result = $"{startTime:yyyy-MM-dd} ~ {endTime:MM-dd} ({totalDays} days) \t {minRSI} ~ {maxRSI}    {closeX:F4}  /  {stopX:F4} \t count = {succeedCount} / {failedCount} / {unknownCount} \t score = {score:F2} \t final = {finalPercent:F4} / {finalPercent3:F4} / {finalPercent5:F4}";
            logger.WriteLine(result);
            return result;
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
            List<SolBin> list = SolDao.SelectAll(binSize);
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
                        totalProfit -= (stopX + 0.002f) * lossX;
                        position = 0;
                        positionEntryPrice = 0;
                        logger.WriteLine($"{list[i].Timestamp} \t - failed ({(list[i].Timestamp - positionTime.Value).TotalMinutes})\r\n");
                        positionTime = null;
                    }
                    else if (list[i].High > positionEntryPrice + positionEntryPrice * closeX)
                    {
                        succeedCount++;
                        totalProfit += closeX - 0.001f;
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
                        totalProfit -= (stopX + 0.002f) * lossX;
                        position = 0;
                        positionEntryPrice = 0;
                        logger.WriteLine($"{list[i].Timestamp} \t - failed ({(list[i].Timestamp - positionTime.Value).TotalMinutes})\r\n");
                        positionTime = null;
                    }
                    else if (list[i].Low < positionEntryPrice - positionEntryPrice * closeX)
                    {
                        succeedCount++;
                        totalProfit += closeX - 0.001f;
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

        static float TestBuyOrSell3(int buyOrSell, float minRSI, float maxRSI, float minDiff, float maxDiff, float closeX, float stopX, DateTime startTime, DateTime? endTime = null)
        {
            List<SolBin> list = SolDao.SelectAll(binSize);
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
                            totalProfit -= (stopX + 0.002f) * lossX;
                            positionEntryPrice = 0;
                            logger.WriteLine($"{list[j].Timestamp} \t - failed ({(list[j].Timestamp - positionTime).TotalMinutes})\r\n");
                            break;
                        }
                        else if (list[j].High > positionEntryPrice + positionEntryPrice * closeX)
                        {
                            succeedCount++;
                            totalProfit += closeX - 0.001f;
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
                            totalProfit -= (stopX + 0.002f) * lossX;
                            positionEntryPrice = 0;
                            logger.WriteLine($"{list[j].Timestamp} \t - failed ({(list[j].Timestamp - positionTime).TotalMinutes})\r\n");
                            break;
                        }
                        else if (list[j].Low < positionEntryPrice - positionEntryPrice * closeX)
                        {
                            succeedCount++;
                            totalProfit += closeX - 0.001f;
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
                string paramText = @"2	87.5	100	-1	14	0.0075	0.0135	0.65
2	82.5	87.5	0	2	0.0048	0.0036	1.79
2	82.5	87.5	2	6.5	0.003	0.0057	1.3
2	82.5	87.5	6.5	9.5	0.0115	0.0095	0.87
2	82.5	87.5	9.5	15	0.003	0.009	0.91
2	78.5	82.5	-0.4	10.3	0.0088	0.0036	1.78
2	73.5	78.5	-0.2	0.15	0.0062	0.0055	1.33
2	72.5	78.5	1	1.2	0.0098	0.0031	1.96
2	71.5	78.5	-0.6	-0.3	0.0035	0.003	2
2	70	78.5	0.24	0.53	0.0094	0.0027	2.13
2	70	78.5	0.39	0.55	0.0033	0.0025	2.22
1	0	16	-0.5	5.3	0.0085	0.0045	1.54
1	0	16	5.3	12.5	0.0095	0.0115	0.74
1	16	17.6	-0.5	7.5	0.008	0.0115	0.74
1	17.6	19.9	-0.1	4.6	0.0105	0.003	2
1	19.9	22.7	0.84	2.75	0.006	0.016	0.56
1	22.7	27.2	1.52	2.46	0.0037	0.012	0.71
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

            List<SolBin> list = SolDao.SelectAll(binSize);
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
            float totalProfit = 0;
            double finalPercent10 = 1, finalPercent15 = 1, finalPercent20 = 1;
            for (int i = 3; i < count - 1; i++)
            {
                foreach (ParamMap paramMap in paramMapArray)
                {
                    float closeX = (float)paramMap.CloseX;
                    float stopX = (float)paramMap.StopX;
                    if (paramMap.BuyOrSell == 1 && paramMap.QtyX > 0 && list[i - 3].RSI > list[i - 2].RSI && list[i - 2].RSI >= (float)paramMap.MinRSI && list[i - 2].RSI < (float)paramMap.MaxRSI && list[i - 1].RSI - list[i - 2].RSI >= (float)paramMap.MinDiff && (list[i - 1].RSI - list[i - 2].RSI) < (float)paramMap.MaxDiff)
                    {
                        int positionEntryPrice = list[i].Open;
                        DateTime positionTime = list[i].Timestamp;
                        logger.WriteLine($"{list[i].Timestamp} \t position created: open = {list[i].Open}    high = {list[i].High}    low = {list[i].Low}    close = {list[i].Close}    rsi = {list[i - 2].RSI}    diff = {list[i - 1].RSI - list[i - 2].RSI}");
                        for (int j = i; j < count; j++)
                        {
                            double p = (closeX - 0.001) / (stopX + 0.002);
                            if (list[j].Low < positionEntryPrice - positionEntryPrice * stopX)
                            {
                                failedCount++;
                                totalProfit -= (stopX + 0.002f) * lossX;
                                finalPercent10 *= .95;
                                finalPercent15 *= .925;
                                finalPercent20 *= .9;
                                positionEntryPrice = 0;
                                logger.WriteLine($"{list[j].Timestamp} \t - failed ({(list[j].Timestamp - positionTime).TotalMinutes}) \t p = {p:F2} \t final = {finalPercent10:F2} \t final2 = {finalPercent20:F2}\r\n");
                                break;
                            }
                            if (list[j].High > positionEntryPrice + positionEntryPrice * closeX)
                            {
                                succeedCount++;
                                totalProfit += closeX - 0.001f;
                                finalPercent10 *= 1 + .05 * p;
                                finalPercent15 *= 1 + .075 * p;
                                finalPercent20 *= 1 + .1 * p;
                                positionEntryPrice = 0;
                                logger.WriteLine($"{list[j].Timestamp} \t succeed ({(list[j].Timestamp - positionTime).TotalMinutes}) \t p = {p:F2} \t final = {finalPercent10:F2} / {finalPercent15:F2} / {finalPercent20:F2}\r\n");
                                break;
                            }
                        }
                        break;
                    }
                    if (paramMap.BuyOrSell == 2 && paramMap.QtyX > 0 && list[i - 3].RSI < list[i - 2].RSI && list[i - 2].RSI >= (float)paramMap.MinRSI && list[i - 2].RSI < (float)paramMap.MaxRSI && list[i - 2].RSI - list[i - 1].RSI >= (float)paramMap.MinDiff && (list[i - 2].RSI - list[i - 1].RSI) < (float)paramMap.MaxDiff)
                    {
                        int positionEntryPrice = list[i].Open;
                        DateTime positionTime = list[i].Timestamp;
                        logger.WriteLine($"{list[i].Timestamp} \t position created: open = {list[i].Open}    high = {list[i].High}    low = {list[i].Low}    close = {list[i].Close}    rsi = {list[i - 2].RSI}    diff = {list[i - 1].RSI - list[i - 2].RSI}");
                        for (int j = i; j < count; j++)
                        {
                            double p = (closeX - 0.001) / (stopX + 0.002);
                            if (list[j].High > positionEntryPrice + positionEntryPrice * stopX)
                            {
                                failedCount++;
                                totalProfit -= (stopX + 0.002f) * lossX;
                                finalPercent10 *= .95;
                                finalPercent15 *= .925;
                                finalPercent20 *= .9;
                                positionEntryPrice = 0;
                                logger.WriteLine($"{list[j].Timestamp} \t - failed ({(list[j].Timestamp - positionTime).TotalMinutes}) \t p = {p:F2} \t final = {finalPercent10:F2} / {finalPercent15:F2} / {finalPercent20:F2}\r\n");
                                break;
                            }
                            if (list[j].Low < positionEntryPrice - positionEntryPrice * closeX)
                            {
                                succeedCount++;
                                totalProfit += closeX - 0.001f;
                                finalPercent10 *= 1 + .05 * p;
                                finalPercent15 *= 1 + .075 * p;
                                finalPercent20 *= 1 + .1 * p;
                                positionEntryPrice = 0;
                                logger.WriteLine($"{list[j].Timestamp} \t succeed ({(list[j].Timestamp - positionTime).TotalMinutes}) \t p = {p:F2} \t final = {finalPercent10:F2} / {finalPercent15:F2} / {finalPercent20:F2}\r\n");
                                break;
                            }
                        }
                        break;
                    }
                }
            }
            float avgProfit = totalProfit / totalDays;
            logger.WriteLine($"\r\nsucceed = {succeedCount} \t failed = {failedCount} \t total = {totalProfit:F8} \t avg = {avgProfit:F8} \t final = {finalPercent10:F2} / {finalPercent15:F2} / {finalPercent20:F2}\r\n");
            return avgProfit;
        }

        public static List<SolBin> LoadBinListFrom5m(string binSize, List<SolBin> list)
        {
            int batchLength;
            switch (binSize)
            {
                case "15m":
                    batchLength = 3;
                    break;
                case "30m":
                    batchLength = 6;
                    break;
                default:
                    throw new Exception($"Invalid bin_size: {binSize}");
            }
            int count = list.Count;
            List<SolBin> resultList = new List<SolBin>();
            int i = 0;
            while (i < count)
            {
                if (list[i].Timestamp.Minute % (batchLength * 5) != 5)
                {
                    i++;
                    continue;
                }
                DateTime timestamp = list[i].Timestamp.AddMinutes(25);
                int open = list[i].Open;
                int high = list[i].High;
                int low = list[i].Low;
                int close = list[i].Close;
                int volume = list[i].Volume;
                for (int j = i + 1; j < i + batchLength && j < count; j++)
                {
                    if (high < list[j].High) high = list[j].High;
                    if (low > list[j].Low) low = list[j].Low;
                    close = list[j].Close;
                    volume += list[j].Volume;
                }
                resultList.Add(new SolBin
                {
                    Timestamp = timestamp,
                    Open = open,
                    High = high,
                    Low = low,
                    Close = close,
                    Volume = volume
                });
                i += batchLength;
            }
            return resultList;
        }

        public static List<SolBin> LoadBinListFrom1m(int size, List<SolBin> list)
        {
            if (size == 1) return list;
            int count = list.Count;
            var resultList = new List<SolBin>();
            int i = 0;
            while (i < count)
            {
                if (list[i].Timestamp.Minute % size != 1)
                {
                    i++;
                    continue;
                }
                DateTime timestamp = list[i].Timestamp.AddMinutes(size - 1);
                int open = list[i].Open;
                int high = list[i].High;
                int low = list[i].Low;
                int close = list[i].Close;
                int volume = list[i].Volume;
                for (int j = i + 1; j < i + size && j < count; j++)
                {
                    if (high < list[j].High) high = list[j].High;
                    if (low > list[j].Low) low = list[j].Low;
                    close = list[j].Close;
                    volume += list[j].Volume;
                }
                resultList.Add(new SolBin
                {
                    Timestamp = timestamp,
                    Open = open,
                    High = high,
                    Low = low,
                    Close = close,
                    Volume = volume
                });
                i += size;
            }
            return resultList;
        }

    }
}
