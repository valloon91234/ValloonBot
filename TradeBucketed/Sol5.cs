using IO.Swagger.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Valloon.Utils;

namespace Valloon.Trading.Backtest
{
    static class Sol5
    {
        const string binSize = "5m";

        public static void Run()
        {
            Program.MoveWindow(20, 0, 1440, 140);
            //Load2("15m"); return;
            //DateTime startTime = new DateTime(2021, 10, 21, 0, 0, 0, DateTimeKind.Utc);
            //{
            //    DateTime startTime = new DateTime(2022, 3, 31, 0, 0, 0, DateTimeKind.Utc);
            //    DateTime endTime = DateTime.UtcNow;
            //    Load("5m", startTime, endTime);
            //    return;
            //}

            {
                //DateTime startTime = new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc);
                {
                    //Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  buy = 1");

                    //for (int delay = 1; delay <= 15; delay++)
                    //{
                    //    int sma = 1;
                    //    while (sma < 720)
                    //    {
                    //        sma += sma / 60 + 1;
                    //        string result = TestSMA("1m", 1, sma, delay, startTime);
                    //        logger.WriteLine($"  {sma} / {delay} \t {result}");
                    //    }
                    //    logger.WriteLine("\n");
                    //}

                    //TestSMA("5m", 1, 288, startTime); return;
                    //while (i < 1000)
                    //{
                    //    i += i / 12 + 1;
                    //    string result = TestSMA("5m", 1, i, startTime);
                    //    logger.WriteLine($"{i}    {result}");
                    //}
                }

                {
                    //Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  Sell = 2");
                    //float step = 1f;
                    //for (float i = 50f; i < 61; i += step)
                    //{
                    //    float result = Test2(2, i, i + step, startTime);
                    //    logger.WriteLine($"{startTime:yyyy-MM-dd}    {i}    result = {result}");
                    //}
                    //Test2(2, 85, 100, startTime);
                }

                //startTime = new DateTime(2022, 2, 1, 0, 0, 0, DateTimeKind.Utc);
                //DateTime endTime = new DateTime(2022, 4, 1, 0, 0, 0, DateTimeKind.Utc);
                //TestBuyOrSell4(startTime, endTime);
                BenchmarkRSI2();
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

        static float BenchmarkRSI2()
        {
            //TestRSI(); return 0;

            //const string binSize = "5m";
            const string binSize = "15m";
            //const string binSize = "30m";
            //const string binSize = "1h";
            const int buyOrSell = 1;

            DateTime startTime = new DateTime(2022, 3, 23, 0, 0, 0, DateTimeKind.Utc);
            DateTime? endTime = null;// new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            List<SolBin> listAll = SolDao.SelectAll("5m");
            if (binSize != "5m") listAll = LoadBinListFrom5m(binSize, listAll);
            int countAll = listAll.Count;
            int totalDays = (int)(listAll[countAll - 1].Timestamp - startTime).TotalDays;
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}    buyOrSell = {buyOrSell}    bin = {binSize}    ({startTime:yyyy-MM-dd HH.mm.ss} ~ {totalDays:N0} days)");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{countAll} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;
            List<Dictionary<string, float>> topList = new List<Dictionary<string, float>>();

            //for (int window = 6; window <= 30; window++)
            //for (int window = 14; window <= 40; window++)
            int window = 14;
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

                //for (float minDiff = -5; minDiff < 5; minDiff += .5f)
                float minDiff = 3f;
                {
                    //for (float maxDiff = minDiff + 2; maxDiff < 15; maxDiff += 1f)
                    float maxDiff = 11f;
                    {
                        //Console.WriteLine($"Window = {window} \t minDiff = {minDiff} \t maxDiff = {maxDiff}");
                        //for (float minValue = 20; minValue < 50; minValue += 5f)
                        for (float minValue = 20; minValue < 50; minValue += .5f)
                        //float minValue = 32f;
                        {
                            //for (float maxValue = minValue + 10; maxValue < 80; maxValue += 5f)
                            for (float maxValue = minValue + 10; maxValue < 70; maxValue += .5f)
                            //float maxValue = 62;
                            {
                                //for (float close = 5; close <= 20.05f; close += 5)
                                //for (float close = 5f; close <= 20.05f; close += 1)
                                float close = 12;
                                {
                                    //for (float stop = 0.01f; stop < 0.0205f; stop += .005f)
                                    //for (float stop = 0.01f; stop < 0.0205f; stop += .001f)
                                    float stop = 0.01f;
                                    {
                                        int tryCount = 0;
                                        int stopCount = 0;
                                        float minHeight = 0, maxHeight = 0;
                                        float totalProfit = 0;
                                        float finalPercent = 1;
                                        int positionEntryPrice = 0;
                                        float topRSI = 0;
                                        for (int i = 1; i < count - 1; i++)
                                        {
                                            if (buyOrSell == 1)
                                            {
                                                if (positionEntryPrice == 0 && list[i].RSI - list[i - 1].RSI >= minDiff && list[i].RSI - list[i - 1].RSI < maxDiff && list[i].RSI >= minValue && list[i].RSI < maxValue)
                                                {
                                                    tryCount++;
                                                    positionEntryPrice = list[i].Close;
                                                    topRSI = list[i].RSI;
                                                }
                                                else if (positionEntryPrice > 0)
                                                {
                                                    if (list[i].Low < positionEntryPrice * (1 - stop))
                                                    {
                                                        stopCount++;
                                                        totalProfit -= positionEntryPrice * (stop + 0.001f);
                                                        finalPercent *= 1 - stop - 0.001f;
                                                        positionEntryPrice = 0;
                                                    }
                                                    else if (list[i].RSI < topRSI - close && (float)list[i].Close / positionEntryPrice > 1.05f)
                                                    {
                                                        int profit = list[i].Close - positionEntryPrice;
                                                        totalProfit += profit - positionEntryPrice * .001f;
                                                        finalPercent *= (float)list[i].Close / positionEntryPrice - 0.001f;
                                                        if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
                                                        if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
                                                        positionEntryPrice = 0;
                                                    }
                                                    else if (topRSI < list[i].RSI)
                                                    {
                                                        topRSI = list[i].RSI;
                                                    }
                                                }
                                            }
                                            else if (buyOrSell == 2)
                                            {
                                                if (positionEntryPrice == 0 && list[i - 1].RSI - list[i].RSI >= minDiff && list[i - 1].RSI - list[i].RSI < maxDiff && list[i].RSI >= minValue && list[i].RSI < maxValue)
                                                {
                                                    tryCount++;
                                                    positionEntryPrice = list[i].Close;
                                                    topRSI = list[i].RSI;
                                                }
                                                else if (positionEntryPrice > 0)
                                                {
                                                    if (list[i].High > positionEntryPrice * (1 + stop))
                                                    {
                                                        stopCount++;
                                                        totalProfit -= positionEntryPrice * (stop + 0.001f);
                                                        finalPercent *= 1 - stop - 0.001f;
                                                        positionEntryPrice = 0;
                                                    }
                                                    else if (list[i].RSI > topRSI + close && (float)positionEntryPrice / list[i].Close > 1.05f)
                                                    {
                                                        int profit = positionEntryPrice - list[i].Close;
                                                        totalProfit += profit - positionEntryPrice * .001f;
                                                        finalPercent *= (float)positionEntryPrice / list[i].Close - 0.001f;
                                                        if (minHeight > (float)profit / positionEntryPrice) minHeight = (float)profit / positionEntryPrice;
                                                        if (maxHeight < (float)profit / positionEntryPrice) maxHeight = (float)profit / positionEntryPrice;
                                                        positionEntryPrice = 0;
                                                    }
                                                    else if (topRSI > list[i].RSI)
                                                    {
                                                        topRSI = list[i].RSI;
                                                    }
                                                }
                                            }
                                        }
                                        float avgProfit = totalProfit / tryCount;
                                        if (totalProfit > 0 && finalPercent > 1.4f)
                                        {
                                            Dictionary<string, float> dic = new Dictionary<string, float>
                                        {
                                            { "buyOrSell", buyOrSell },
                                            { "window", window },
                                            { "minDiff", minDiff },
                                            { "maxDiff", maxDiff },
                                            { "minRSI", minValue },
                                            { "maxRSI", maxValue },
                                            { "close", close },
                                            { "stop", stop },
                                            { "tryCount", tryCount },
                                            { "stopCount", stopCount },
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
                                            logger.WriteLine($"<{buyOrSell}>  {window} \t diff = {minDiff:F1} / {maxDiff:F1} \t rsi = {minValue:F1} / {maxValue:F1} \t close = {close:F1} / {stop:F3} \t try = {tryCount} / {stopCount} \t h = {minHeight:F4} / {maxHeight:F4} \t p = {totalProfit:F2} \t avg = {avgProfit:F2} \t % = {finalPercent:F4}");
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

        static float Test2(int buyOrSell, float minRSI, float maxRSI, DateTime startTime, DateTime? endTime = null)
        {
            List<SolBin> list = SolDao.SelectAll(binSize);
            int count = list.Count;
            const int rsiLength = 14;
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
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  buyOrSell = {buyOrSell}    {minRSI} ~ {maxRSI}   ({list[0].Date} ~ {totalDays:N0} days)");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;

            List<Dictionary<string, float>> topList = new List<Dictionary<string, float>>();
            //List<Dictionary<string, float>> top0List = new List<Dictionary<string, float>>();

            for (float minDiff = -1f; minDiff < 0f; minDiff += .1f)
            //float minDiff = 9.5f;
            {
                for (float maxDiff = 10; maxDiff < 15f; maxDiff += maxDiff < 5f ? .1f : .1f)
                //float maxDiff = 2.46f;
                {
                    logger.WriteLine($"\n\n----    minDiff = {minDiff}    maxDiff = {maxDiff}    ----\n");
                    //for (float stopX = 0.002f; stopX < 0.015f; stopX += 0.0005f)
                    float stopX = 0.0135f;
                    {
                        //for (float closeX = 0.003f; closeX < 0.015f; closeX += 0.0005f)
                        float closeX = 0.0075f;
                        {
                            int succeedCount = 0, failedCount = 0, unknownCount = 0;
                            float totalProfit = 0;
                            float finalPercent = 1;
                            double p = (closeX - 0.001) / (stopX + 0.002);
                            for (int i = 3; i < count - 1; i++)
                            {
                                if (buyOrSell == 1 && list[i - 3].RSI >= list[i - 2].RSI && list[i - 2].RSI >= minRSI && list[i - 2].RSI < maxRSI && list[i - 1].RSI - list[i - 2].RSI >= minDiff && (list[i - 1].RSI - list[i - 2].RSI) < maxDiff)
                                {
                                    int positionEntryPrice = list[i].Open;
                                    for (int j = i; j < count && j < i + rsiLength * 3; j++)
                                    {
                                        if (list[j].Low < positionEntryPrice - positionEntryPrice * stopX)
                                        {
                                            failedCount++;
                                            totalProfit -= (stopX + 0.002f) * lossX;
                                            finalPercent *= .95f;
                                            goto solved;
                                        }
                                        else if (list[j].High > positionEntryPrice + positionEntryPrice * closeX)
                                        {
                                            succeedCount++;
                                            totalProfit += closeX - 0.001f;
                                            finalPercent *= (float)(1 + .05 * p);
                                            goto solved;
                                        }
                                        //i = j + 1;
                                    }
                                    unknownCount++;
                                    totalProfit -= (stopX + 0.002f) * lossX;
                                    finalPercent *= .95f;
                                solved:;
                                }
                                else if (buyOrSell == 2 && list[i - 3].RSI <= list[i - 2].RSI && list[i - 2].RSI >= minRSI && list[i - 2].RSI < maxRSI && list[i - 2].RSI - list[i - 1].RSI >= minDiff && (list[i - 2].RSI - list[i - 1].RSI) < maxDiff)
                                {
                                    int positionEntryPrice = list[i].Open;
                                    for (int j = i; j < count && j < i + rsiLength * 3; j++)
                                    {
                                        if (list[j].High > positionEntryPrice + positionEntryPrice * stopX)
                                        {
                                            failedCount++;
                                            totalProfit -= (stopX + 0.002f) * lossX;
                                            finalPercent *= .95f;
                                            goto solved;
                                        }
                                        else if (list[j].Low < positionEntryPrice - positionEntryPrice * closeX)
                                        {
                                            succeedCount++;
                                            totalProfit += closeX - 0.001f;
                                            finalPercent *= (float)(1 + .05 * p);
                                            goto solved;
                                        }
                                        //i = j + 1;
                                    }
                                    unknownCount++;
                                    totalProfit -= (stopX + 0.002f) * lossX;
                                    finalPercent *= .95f;
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
                                    { "minDiff", minDiff },
                                    { "maxDiff", maxDiff },
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
                                                topList[i]["finalPercent"] == finalPercent && topList[i]["minDiff"] < minDiff ||
                                                topList[i]["finalPercent"] == finalPercent && topList[i]["minDiff"] == minDiff && topList[i]["maxDiff"] > maxDiff ||
                                                topList[i]["finalPercent"] == finalPercent && topList[i]["minDiff"] == minDiff && topList[i]["maxDiff"] == maxDiff && topList[i]["closeX"] > closeX ||
                                                topList[i]["finalPercent"] == finalPercent && topList[i]["minDiff"] == minDiff && topList[i]["maxDiff"] == maxDiff && topList[i]["closeX"] == closeX && topList[i]["stopX"] < stopX)
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
                                //if (failedCount == 0)
                                //{
                                //    int top0ListCount = top0List.Count;
                                //    if (top0ListCount > 0)
                                //    {
                                //        while (top0ListCount > 1000)
                                //        {
                                //            top0List.RemoveAt(0);
                                //            top0ListCount--;
                                //        }
                                //        for (int i = 0; i < top0ListCount; i++)
                                //        {
                                //            if (top0List[i]["avgProfit"] > avgProfit ||
                                //                top0List[i]["avgProfit"] == avgProfit && top0List[i]["minDiff"] < minDiff ||
                                //                top0List[i]["avgProfit"] == avgProfit && top0List[i]["minDiff"] == minDiff && top0List[i]["maxDiff"] > maxDiff ||
                                //                top0List[i]["avgProfit"] == avgProfit && top0List[i]["minDiff"] == minDiff && top0List[i]["maxDiff"] == maxDiff && top0List[i]["closeX"] > closeX ||
                                //                top0List[i]["avgProfit"] == avgProfit && top0List[i]["minDiff"] == minDiff && top0List[i]["maxDiff"] == maxDiff && top0List[i]["closeX"] == closeX && top0List[i]["stopX"] < stopX)
                                //            {
                                //                top0List.Insert(i, dic);
                                //                goto top0ListEnd;
                                //            }
                                //        }
                                //        top0List.Add(dic);
                                //    top0ListEnd:;
                                //    }
                                //    else
                                //    {
                                //        top0List.Add(dic);
                                //    }
                                //}
                                logger.WriteLine($"{minRSI} ~ {maxRSI}    {minDiff} / {maxDiff}    {closeX:F4}    {stopX:F4} \t succeed = {succeedCount} \t failed = {failedCount} \t unknown = {unknownCount} \t score = {score:F2} \t total = {totalProfit:F4} \t avg = {avgProfit:F4} \t final = {finalPercent:F4}");
                            }
                            else
                            {
                                Console.WriteLine($"{minRSI} ~ {maxRSI}    {minDiff} / {maxDiff}    {closeX:F4}    {stopX:F4} \t succeed = {succeedCount} \t failed = {failedCount} \t unknown = {unknownCount} \t score = {score:F2} \t total = {totalProfit:F4} \t avg = {avgProfit:F4} \t final = {finalPercent:F4}");
                            }
                        }
                    }
                }
            }
            logger.WriteLine($"\r\n\r\n\r\n\r\ntopList={topList.Count}\r\n" + JArray.FromObject(topList).ToString());
            //logger.WriteLine($"\r\n\r\n\r\n\r\ntop0List={top0List.Count}\r\n" + JArray.FromObject(top0List).ToString());
            if (topList.Count > 0)
                return topList[topList.Count - 1]["finalPercent"];
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

    }
}
