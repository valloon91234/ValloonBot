using IO.Swagger.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Valloon.Utils;

namespace Valloon.Trading.Backtest
{
    static class Btc1h
    {
        const string binSize = "1h";
        public static string ProcessName = Process.GetCurrentProcess().ProcessName;

        public static void Run()
        {
            Program.MoveWindow(60, 0, 1400, 140);


            //{
            //    DateTime startTime = new DateTime(2022, 3, 9, 0, 0, 0, DateTimeKind.Utc);
            //    DateTime endTime = DateTime.UtcNow;
            //    Load("5m", startTime, endTime);
            //    //LoadCSV("1m", startTime, endTime);
            //    return;
            //}

            {
                DateTime startTime = new DateTime(2022, 2, 1, 0, 0, 0, DateTimeKind.Utc);
                DateTime? endTime = new DateTime(2022, 4, 1, 0, 0, 0, DateTimeKind.Utc);

                {
                    Logger logger = new Logger($"_Result  -  {ProcessName}  -  {DateTime.Now:yyyy-MM-dd  HH.mm.ss}");
                    for (int i = 1; i < 60; i += 1)
                    {
                        logger.WriteLine($"{startTime:yyyy-MM-dd}    sma = {i}    {SimulateSMA("1m", 1, i, 1, 0.0025f, 0.0025f, startTime)}");
                    }
                    return;
                }
                SimulateSMA("1m", 1, 15, 1, 0.01f, 0.005f, startTime); return;

                List<BtcBin> list = BtcDao.SelectAll("5m");
                const int rsiLength = 14;
                {
                    List<TradeBin> binList = new List<TradeBin>();
                    foreach (BtcBin m in list)
                    {
                        binList.Add(new TradeBin(m.Timestamp, BitMEXApiHelper.SYMBOL_XBTUSD, (decimal)m.Open, (decimal)m.High, (decimal)m.Low, (decimal)m.Close));
                    }
                    double[] rsiArray = RSI.CalculateRSIValues(binList.ToArray(), rsiLength);
                    int count = list.Count;
                    for (int i = 0; i < count; i++)
                    {
                        BtcBin m = list[i];
                        m.RSI = (float)rsiArray[i];
                    }
                    list.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp >= endTime.Value);
                }

                Simulate(startTime, endTime); return;

                Benchmark2(list, 1, 15, 20); return;

                {
                    Logger logger = new Logger($"_Result  -  {ProcessName}  -  {DateTime.Now:yyyy-MM-dd  HH.mm.ss}");
                    int start = 0;
                    while (start < 100)
                    {
                        int end;
                        if (start == 0 || start == 90)
                            end = start + 10;
                        else
                            end = start + 5;
                        //logger.WriteLine($"{startTime:yyyy-MM-dd}    {Benchmark(list, 1, start, end)}");
                        //logger.WriteLine($"{startTime:yyyy-MM-dd}    {Benchmark(list, 2, start, end)}");
                        logger.WriteLine($"{startTime:yyyy-MM-dd}    {Benchmark2(list, 1, start, end)}");
                        //logger.WriteLine($"{startTime:yyyy-MM-dd}    {Benchmark2(list, 2, start, end)}");
                        start = end;
                    }
                }

                //startTime = new DateTime(2022, 2, 1, 0, 0, 0, DateTimeKind.Utc);
                //DateTime endTime = new DateTime(2022, 4, 1, 0, 0, 0, DateTimeKind.Utc);
                //TestBuyOrSell4(startTime, endTime);
                //Test_Grid();
                return;
            }

            //ThreadPool.QueueUserWorkItem(state => runHCS(new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc)));
        }

        static void LoadCSV(string binSize, DateTime startTime, DateTime endTime)
        {
            using (var writer = new StreamWriter("data.csv", true, Encoding.UTF8))
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
                        List<TradeBin> list = apiHelper.GetBinList(binSize, false, BitMEXApiHelper.SYMBOL_XBTUSD, 1000, null, null, startTime, nextTime);
                        int count = list.Count;
                        for (int i = 0; i < count - 1; i++)
                        {
                            TradeBin t = list[i];
                            try
                            {
                                writer.WriteLine($"{t.Timestamp.Value:yyyy-MM-dd HH:mm:ss},{t.Timestamp.Value:yyyy-MM-dd},{t.Timestamp.Value:HH:mm},{t.Open.Value},{t.High.Value},{t.Low.Value},{t.Close.Value},{t.Volume.Value}");
                                writer.Flush();
                                //BtcBin b = new BtcBin(t);
                                //BtcDao.Insert(b, binSize);
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
                    List<TradeBin> list = apiHelper.GetBinList(binSize, false, BitMEXApiHelper.SYMBOL_XBTUSD, 1000, null, null, startTime, nextTime);
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

        static void Load2(string binSize, DateTime? startTime = null)
        {
            List<BtcBin> list;
            int batchLength;
            switch (binSize)
            {
                case "30m":
                    list = BtcDao.SelectAll("5m");
                    batchLength = 6;
                    break;
                default:
                    Console.WriteLine($"Invalid bin_size: {binSize}");
                    return;
            }
            if (startTime != null) list.RemoveAll(x => x.Timestamp < startTime.Value);
            int count = list.Count;
            int i = 0;
            while (i < count - batchLength)
            {
                float high = list[i + 1].High;
                float low = list[i + 1].Low;
                int volume = list[i + 1].Volume;
                for (int j = i + 2; j <= i + batchLength; j++)
                {
                    if (high < list[j].High) high = list[j].High;
                    if (low > list[j].Low) low = list[j].Low;
                    volume += list[j].Volume;
                }
                try
                {
                    BtcBin b = new BtcBin
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
                    BtcDao.Insert(b, binSize);
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

        const float lossX = 1f;

        static void ExportCSV(DateTime? startTime = null, DateTime? endTime = null)
        {
            List<BtcBin> list = BtcDao.SelectAll(binSize);
            int count = list.Count;
            const int rsiLength = 14;
            {
                List<TradeBin> binList = new List<TradeBin>();
                foreach (BtcBin m in list)
                {
                    binList.Add(new TradeBin(m.Timestamp, BitMEXApiHelper.SYMBOL_XBTUSD, (decimal)m.Open, (decimal)m.High, (decimal)m.Low, (decimal)m.Close));
                }
                double[] rsiArray = RSI.CalculateRSIValues(binList.ToArray(), rsiLength);
                for (int i = 0; i < count; i++)
                {
                    BtcBin m = list[i];
                    m.RSI = (float)rsiArray[i];
                }
                list.RemoveAll(x => startTime != null && x.Timestamp < startTime.Value || endTime != null && x.Timestamp >= endTime.Value);
                count = list.Count;
            }
            using (var writer = new StreamWriter($"data-export-{DateTime.Now:yyyy-MM-dd HH.mm.ss}.csv", true, Encoding.UTF8))
            {
                writer.WriteLine($"timestamp,date,time,open,high,low,close,volume,rsi");
                for (int i = 0; i < count; i++)
                {
                    BtcBin t = list[i];
                    writer.WriteLine($"{t.Timestamp:yyyy-MM-dd HH:mm:ss},{t.Date:yyyy-MM-dd},{t.Time:HH:mm},{t.Open},{t.High},{t.Low},{t.Close},{t.Volume},{t.RSI}");
                    writer.Flush();
                }
            }
        }

        static void Test_Grid()
        {
            DateTime? startTime = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime? endTime = null;// new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            List<BtcBin> list = BtcDao.SelectAll("1m");
            list.RemoveAll(x => startTime != null && x.Timestamp < startTime.Value || endTime != null && x.Timestamp >= endTime.Value);
            int count = list.Count;

            int totalDays = (int)(list[count - 1].Timestamp - list[0].Timestamp).TotalDays;
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  ({list[0].Date} ~ {totalDays:N0} days)");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;

            int height = 1000;
            int upper = (int)list[0].Open / height * height + height;
            int lower = (int)list[0].Open / height * height;
            int position = 0, profit = 0;
            for (int i = 0; i < count - 1; i++)
            {
                if (list[i].High > upper)
                {
                    position--;
                    if (position > 0) profit++;
                    logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t position = {position} \t profit = {profit}");
                    lower = upper - height;
                    upper += height;
                }
                if (list[i].Low < lower)
                {
                    position++;
                    if (position < 0) profit++;
                    logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t position = {position} \t profit = {profit}");
                    upper = lower + height;
                    lower -= height;
                }
            }
            logger.WriteLine("\r\n");
        }

        static void Test_1m()
        {
            DateTime? startTime = new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime? endTime = null;// new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            const int leverage = 5;

            List<BtcBin> list = BtcDao.SelectAll("5m");
            int count = list.Count;
            const int rsiLength = 14;
            {
                List<TradeBin> binList = new List<TradeBin>();
                foreach (BtcBin m in list)
                {
                    binList.Add(new TradeBin(m.Timestamp, BitMEXApiHelper.SYMBOL_XBTUSD, (decimal)m.Open, (decimal)m.High, (decimal)m.Low, (decimal)m.Close));
                }
                double[] rsiArray = RSI.CalculateRSIValues(binList.ToArray(), rsiLength);
                for (int i = 0; i < count; i++)
                {
                    BtcBin m = list[i];
                    m.RSI = (float)rsiArray[i];
                }
                list.RemoveAll(x => startTime != null && x.Timestamp < startTime.Value || endTime != null && x.Timestamp >= endTime.Value);
                count = list.Count;
            }

            int totalDays = (int)(list[count - 1].Timestamp - list[0].Timestamp).TotalDays;
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  ({list[0].Date} ~ {totalDays:N0} days)");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;

            bool benchmark = false;
            if (benchmark)
            {
                bool tryUpper = true;
                if (tryUpper)
                {
                    //for (int bbLength = 5; bbLength <= 300; bbLength += 5)
                    int bbLength = 7;
                    {
                        double lastProfit = 0, lastProfitRate = 0;
                        for (int i = bbLength - 1; i < count; i++)
                        {
                            int candleCount = bbLength;
                            float[] closeArray = new float[candleCount];
                            float[] sd2Array = new float[candleCount];
                            for (int j = 0; j < candleCount; j++)
                            {
                                closeArray[j] = list[i - candleCount + j + 1].Close;
                            }
                            float movingAverage = closeArray.Average();
                            for (int j = 0; j < candleCount; j++)
                            {
                                sd2Array[j] = (float)Math.Pow(closeArray[j] - movingAverage, 2);
                            }
                            list[i].SD = (float)Math.Pow(sd2Array.Average(), 0.5d);
                            list[i].SMA = movingAverage;
                        }

                        for (float maxBBW = .002f; maxBBW < .003; maxBBW += .0001f)
                        //float maxBBW = 0.002f;
                        {
                            Console.Title = $"{totalDays} days    {lastProfit}    {bbLength}  /  {maxBBW}";
                            //for (float rsi = 30; rsi > 10; rsi -= 1)
                            float rsi = 25;
                            {
                                for (float limitX = .005f; limitX < .02; limitX += .0005f)
                                {
                                    for (float closeX = .005f; closeX < .02; closeX += .0005f)
                                    {
                                        for (float stopX = .005f; stopX < .02; stopX += .0005f)
                                        {
                                            int upperTry = 0, upperSucceed = 0, upperFailed = 0;
                                            double upperProfit = 0;
                                            double upperProfitRate = 1;
                                            float positionEntryPrice = 0, positionClosePrice = 0, positionStopPrice = 0;
                                            double maxLoss = 0;
                                            for (int i = bbLength; i < count - 1; i++)
                                            {
                                                if (list[i - 1].SD / list[i - 1].SMA > maxBBW || list[i - 1].RSI < rsi) continue;
                                                if (positionEntryPrice == 0)
                                                {
                                                    float limitPrice = list[i - 1].SMA * (1 + limitX);
                                                    if (limitPrice < list[i].Open) continue;
                                                    float closePrice = limitPrice * (1 - closeX);
                                                    float stopPrice = limitPrice * (1 + stopX);
                                                    if (limitPrice / closePrice < 1.0005) continue;
                                                    if (list[i].High > stopPrice)
                                                    {
                                                        upperFailed++;
                                                        upperProfit -= (stopX + 0.001) / stopX;
                                                        double loss = (limitPrice / stopPrice - 1.001) * leverage;
                                                        upperProfitRate *= 1 + loss;
                                                        if (maxLoss > loss) maxLoss = loss;
                                                        i += bbLength;
                                                    }
                                                    else if (list[i].High > limitPrice)
                                                    {
                                                        upperTry++;
                                                        positionEntryPrice = limitPrice;
                                                        positionClosePrice = closePrice;
                                                        positionStopPrice = stopPrice;
                                                    }
                                                }
                                                else
                                                {
                                                    if (list[i].High > positionStopPrice)
                                                    {
                                                        upperFailed++;
                                                        upperProfit -= (stopX + 0.001) / stopX;
                                                        double loss = (positionEntryPrice / positionStopPrice - 1.001) * leverage;
                                                        upperProfitRate *= 1 + loss;
                                                        if (maxLoss > loss) maxLoss = loss;
                                                        positionEntryPrice = 0;
                                                        i += bbLength;
                                                    }
                                                    else if (list[i].Low < positionClosePrice)
                                                    {
                                                        upperSucceed++;
                                                        upperProfit += (closeX - 0.0002) / closeX * ((closeX - 0.0002) / (stopX + 0.001));
                                                        upperProfitRate *= 1 + (positionEntryPrice / positionClosePrice - 1.0002) * leverage;
                                                        positionEntryPrice = 0;
                                                    }
                                                }
                                            }

                                            if (upperSucceed > 0 && (upperProfit > lastProfit || upperProfitRate > lastProfitRate))
                                            {
                                                logger.WriteLine($"{bbLength} / {maxBBW:F8} / {rsi:F4} / {limitX:F4} / {closeX:F4} / {stopX:F4} \t count = {upperTry} / {upperSucceed} / {upperFailed}    profit = {upperProfit:F4}  /  {upperProfitRate:F8}  /  {maxLoss:F8}");
                                                if (upperProfit > lastProfit) lastProfit = upperProfit;
                                                if (upperProfitRate > lastProfitRate) lastProfitRate = upperProfitRate;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    for (int bbLength = 5; bbLength <= 300; bbLength += 5)
                    //int bbLength = 7;
                    {
                        double lastProfit = 0, lastProfitRate = 0;
                        for (int i = bbLength - 1; i < count; i++)
                        {
                            int candleCount = bbLength;
                            float[] closeArray = new float[candleCount];
                            float[] sd2Array = new float[candleCount];
                            for (int j = 0; j < candleCount; j++)
                            {
                                closeArray[j] = list[i - candleCount + j + 1].Close;
                            }
                            float movingAverage = closeArray.Average();
                            for (int j = 0; j < candleCount; j++)
                            {
                                sd2Array[j] = (float)Math.Pow(closeArray[j] - movingAverage, 2);
                            }
                            list[i].SD = (float)Math.Pow(sd2Array.Average(), 0.5d);
                            list[i].SMA = movingAverage;
                        }

                        for (float maxBBW = .001f; maxBBW < .003; maxBBW += .0005f)
                        {
                            Console.Title = $"{totalDays} days    {lastProfit}    {bbLength}  /  {maxBBW}";
                            for (float rsi = 70; rsi < 80; rsi += 1)
                            {
                                for (float limitX = .005f; limitX < .04; limitX += .001f)
                                {
                                    for (float closeX = .005f; closeX < .03; closeX += .001f)
                                    {
                                        for (float stopX = .005f; stopX < .03; stopX += .001f)
                                        {
                                            int lowerTry = 0, lowerSucceed = 0, lowerFailed = 0;
                                            double lowerProfit = 0;
                                            double lowerProfitRate = 1;
                                            float positionEntryPrice = 0, positionClosePrice = 0, positionStopPrice = 0;
                                            double maxLoss = 0;
                                            for (int i = bbLength; i < count - 1; i++)
                                            {
                                                if (list[i - 1].SD / list[i - 1].SMA > maxBBW || list[i - 1].RSI > rsi) continue;
                                                if (positionEntryPrice == 0)
                                                {
                                                    float limitPrice = list[i - 1].SMA * (1 - limitX);
                                                    if (limitPrice > list[i].Open) continue;
                                                    float closePrice = limitPrice * (1 + closeX);
                                                    float stopPrice = limitPrice * (1 - stopX);
                                                    if (closePrice / limitPrice < 1.0005) continue;
                                                    if (list[i].Low < stopPrice)
                                                    {
                                                        lowerFailed++;
                                                        lowerProfit -= (stopX + 0.001) / stopX;
                                                        double loss = (stopX + 0.001) * leverage;
                                                        lowerProfitRate *= 1 - loss;
                                                        if (maxLoss < loss) maxLoss = loss;
                                                    }
                                                    else if (list[i].Low < limitPrice)
                                                    {
                                                        lowerTry++;
                                                        positionEntryPrice = limitPrice;
                                                        positionClosePrice = closePrice;
                                                        positionStopPrice = stopPrice;
                                                    }
                                                }
                                                else
                                                {
                                                    if (list[i].Low < positionStopPrice)
                                                    {
                                                        lowerFailed++;
                                                        lowerProfit -= (stopX + 0.001) / stopX;
                                                        double loss = (stopX + 0.001) * leverage;
                                                        lowerProfitRate *= 1 - loss;
                                                        if (maxLoss < loss) maxLoss = loss;
                                                        positionEntryPrice = 0;
                                                    }
                                                    else if (list[i].High > positionClosePrice)
                                                    {
                                                        lowerSucceed++;
                                                        lowerProfit += (closeX - 0.0002) / closeX * ((closeX - 0.0002) / (stopX + 0.001));
                                                        lowerProfitRate *= 1 + (closeX - 0.0002) * leverage;
                                                        positionEntryPrice = 0;
                                                    }
                                                }
                                            }

                                            if (lowerProfit > 0 && (lowerProfit > lastProfit || lowerProfitRate > lastProfitRate))
                                            {
                                                logger.WriteLine($"{bbLength} / {maxBBW:F8} / {rsi:F4} / {limitX:F4} / {closeX:F4} / {stopX:F4} \t count = {lowerTry} / {lowerSucceed} / {lowerFailed}    profit = {lowerProfit:F4}  /  {lowerProfitRate:F8}  /  {maxLoss:F8}");
                                                lastProfit = lowerProfit;
                                                lastProfitRate = lowerProfitRate;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                logger.WriteLine($"end");
                return;
            }
            else
            {
                bool tryUpper = true;
                if (tryUpper)
                {
                    int bbLength = 7;
                    float maxBBW = 0.0022f;
                    float rsi = 25;
                    float limitX = 0.0075f;
                    float closeX = 0.0115f;
                    float stopX = 0.013f;

                    for (int i = bbLength - 1; i < count; i++)
                    {
                        int candleCount = bbLength;
                        float[] closeArray = new float[candleCount];
                        float[] sd2Array = new float[candleCount];
                        for (int j = 0; j < candleCount; j++)
                        {
                            closeArray[j] = list[i - candleCount + j + 1].Close;
                        }
                        float movingAverage = closeArray.Average();
                        for (int j = 0; j < candleCount; j++)
                        {
                            sd2Array[j] = (float)Math.Pow(closeArray[j] - movingAverage, 2);
                        }
                        list[i].SD = (float)Math.Pow(sd2Array.Average(), 0.5d);
                        list[i].SMA = movingAverage;
                    }
                    int upperTry = 0, upperSucceed = 0, upperFailed = 0;
                    double upperProfit = 0;
                    double upperProfitRate = 1;
                    float positionEntryPrice = 0, positionClosePrice = 0, positionStopPrice = 0;
                    double maxLoss = 0;
                    for (int i = bbLength; i < count - 1; i++)
                    {
                        if (list[i - 1].SD / list[i - 1].SMA > maxBBW || list[i - 1].RSI < rsi) continue;
                        if (positionEntryPrice == 0)
                        {
                            float limitPrice = list[i - 1].SMA * (1 + limitX);
                            if (limitPrice < list[i].Open) continue;
                            float closePrice = limitPrice * (1 - closeX);
                            float stopPrice = limitPrice * (1 + stopX);
                            if (limitPrice / closePrice < 1.0005) continue;
                            if (list[i].High > stopPrice)
                            {
                                upperFailed++;
                                upperProfit -= (stopX + 0.001) / stopX;
                                double loss = (limitPrice / stopPrice - 1.001) * leverage;
                                upperProfitRate *= 1 + loss;
                                if (maxLoss > loss) maxLoss = loss;
                                i += bbLength;
                            }
                            else if (list[i].High > limitPrice)
                            {
                                upperTry++;
                                positionEntryPrice = limitPrice;
                                positionClosePrice = closePrice;
                                positionStopPrice = stopPrice;
                            }
                        }
                        else
                        {
                            if (list[i].High > positionStopPrice)
                            {
                                upperFailed++;
                                upperProfit -= (stopX + 0.001) / stopX;
                                double loss = (positionEntryPrice / positionStopPrice - 1.001) * leverage;
                                upperProfitRate *= 1 + loss;
                                if (maxLoss > loss) maxLoss = loss;
                                positionEntryPrice = 0;
                                i += bbLength;
                            }
                            else if (list[i].Low < positionClosePrice)
                            {
                                upperSucceed++;
                                upperProfit += (closeX - 0.0002) / closeX * ((closeX - 0.0002) / (stopX + 0.001));
                                upperProfitRate *= 1 + (positionEntryPrice / positionClosePrice - 1.0002) * leverage;
                                positionEntryPrice = 0;
                            }
                        }
                    }
                    logger.WriteLine("\r\n");
                    logger.WriteLine($"{bbLength} / {maxBBW:F8} / {rsi:F4} / {limitX:F4} / {closeX:F4} / {stopX:F4} \t count = {upperTry} / {upperSucceed} / {upperFailed}    profit = {upperProfit:F4}  /  {upperProfitRate:F8}  /  {maxLoss:F8}");
                }
                else
                {
                    int bbLength = 7;
                    float maxBBW = 0.0022f;
                    float rsi = 25;
                    float limitX = 0.0075f;
                    float closeX = 0.0115f;
                    float stopX = 0.013f;

                    for (int i = bbLength - 1; i < count; i++)
                    {
                        int candleCount = bbLength;
                        float[] closeArray = new float[candleCount];
                        float[] sd2Array = new float[candleCount];
                        for (int j = 0; j < candleCount; j++)
                        {
                            closeArray[j] = list[i - candleCount + j + 1].Close;
                        }
                        float movingAverage = closeArray.Average();
                        for (int j = 0; j < candleCount; j++)
                        {
                            sd2Array[j] = (float)Math.Pow(closeArray[j] - movingAverage, 2);
                        }
                        list[i].SD = (float)Math.Pow(sd2Array.Average(), 0.5d);
                        list[i].SMA = movingAverage;
                    }
                    int lowerTry = 0, lowerSucceed = 0, lowerFailed = 0;
                    double lowerProfit = 0;
                    double lowerProfitRate = 1;
                    float positionEntryPrice = 0, positionClosePrice = 0, positionStopPrice = 0;
                    double maxLoss = 0;
                    for (int i = bbLength; i < count - 1; i++)
                    {
                        if (list[i - 1].SD / list[i - 1].SMA > maxBBW || list[i - 1].RSI > rsi) continue;
                        if (positionEntryPrice == 0)
                        {
                            float limitPrice = list[i - 1].SMA * (1 - limitX);
                            if (limitPrice > list[i].Open) continue;
                            float closePrice = limitPrice * (1 + closeX);
                            float stopPrice = limitPrice * (1 - stopX);
                            if (closePrice / limitPrice < 1.0005) continue;
                            if (list[i].Low < stopPrice)
                            {
                                lowerFailed++;
                                lowerProfit -= (stopX + 0.001) / stopX;
                                double loss = (stopX + 0.001) * leverage;
                                lowerProfitRate *= 1 - loss;
                                if (maxLoss < loss) maxLoss = loss;
                            }
                            else if (list[i].Low < limitPrice)
                            {
                                lowerTry++;
                                positionEntryPrice = limitPrice;
                                positionClosePrice = closePrice;
                                positionStopPrice = stopPrice;
                            }
                        }
                        else
                        {
                            if (list[i].Low < positionStopPrice)
                            {
                                lowerFailed++;
                                lowerProfit -= (stopX + 0.001) / stopX;
                                double loss = (stopX + 0.001) * leverage;
                                lowerProfitRate *= 1 - loss;
                                if (maxLoss < loss) maxLoss = loss;
                                positionEntryPrice = 0;
                            }
                            else if (list[i].High > positionClosePrice)
                            {
                                lowerSucceed++;
                                lowerProfit += (closeX - 0.0002) / closeX * ((closeX - 0.0002) / (stopX + 0.001));
                                lowerProfitRate *= 1 + (closeX - 0.0002) * leverage;
                                positionEntryPrice = 0;
                            }
                        }
                    }

                    logger.WriteLine("\r\n");
                    logger.WriteLine($"{bbLength} / {maxBBW:F8} / {rsi:F4} / {limitX:F4} / {closeX:F4} / {stopX:F4} \t count = {lowerTry} / {lowerSucceed} / {lowerFailed}    profit = {lowerProfit:F4}  /  {lowerProfitRate:F8}  /  {maxLoss:F8}");
                }
            }
        }

        static void Test_5m()
        {
            DateTime startTime = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime? endTime = null;
            const int leverage = 5;

            List<BtcBin> list = BtcDao.SelectAll("5m");
            list.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
            int count = list.Count;
            int totalDays = (int)(list[count - 1].Timestamp - list[0].Timestamp).TotalDays;
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  ({list[0].Date} ~ {totalDays:N0} days)");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;

            bool benchmark = true;
            if (benchmark)
            {
                bool tryUpper = true;
                if (tryUpper)
                {
                    double lastProfit = 0;
                    //for (float x1 = 1.0015f; x1 > .5; x1 -= 0.0001f)
                    for (float x0 = .9999f; x0 < 1.005; x0 += 0.0005f)
                    {
                        for (float x1 = .9998f; x1 < 1.005; x1 += 0.0005f)
                        {
                            Console.Title = $"{lastProfit}    {x0}  /  {x1}";
                            for (float x2 = -1f; x2 < 2f; x2 += 0.05f)
                            {
                                for (float x3 = 1f; x3 < 1.01; x3 += 0.0005f)
                                {
                                    for (float x4 = .99f; x4 < .9995; x4 += 0.0005f)
                                    {
                                        for (float stopX = .001f; stopX < .02; stopX += 0.001f)
                                        //float stopX = .017f;
                                        {
                                            int upperTry = 0, upperSucceed = 0, upperFailed = 0, upperHalfSucceed = 0, upperHalfFailed = 0;
                                            double upperProfit = 1;
                                            for (int i = 2; i < count - 1; i++)
                                            {
                                                if (list[i - 2].Open / list[i - 2].Close > x1 && list[i - 1].Open / list[i - 1].Close > x1 /*&& list[i - 1].High / list[i - 1].Close > 1.00275 && (list[i - 1].Close - list[i - 1].Open) / (list[i - 1].High - list[i - 1].Close) > 1.225*/)
                                                {
                                                    upperTry++;
                                                    float limitPrice = list[i - 1].Open + (list[i - 1].Open - list[i - 1].Close) * x2;
                                                    //if (list[i - 1].Low > limitPrice * .999f) continue;
                                                    float closePrice = Math.Max(list[i - 1].Low * x3, limitPrice * x4);
                                                    if (limitPrice / closePrice < 1.0005) continue;
                                                    float closeHeight = limitPrice - closePrice;
                                                    float stopPrice = limitPrice * (1 + stopX);
                                                    if (list[i].High >= limitPrice)
                                                    {
                                                        if (list[i].High >= stopPrice)
                                                        {
                                                            upperFailed++;
                                                            upperProfit *= 1 + (limitPrice / stopPrice - 1.001) * leverage;
                                                            //Console.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss}    upperFailed \t profit = {upperProfit}");
                                                        }
                                                        else if (list[i].Close < closePrice)
                                                        {
                                                            upperSucceed++;
                                                            upperProfit *= 1 + (limitPrice / closePrice - 1.0002) * leverage;
                                                            //Console.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss}    upperSucceed \t profit = {upperProfit}");
                                                        }
                                                        else if (list[i].Close < limitPrice)
                                                        {
                                                            upperHalfSucceed++;
                                                            upperProfit *= 1 + (limitPrice / list[i].Close - 1.001) * leverage;
                                                            //Console.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss}    upperHalfSucceed \t profit = {upperProfit}");
                                                        }
                                                        else
                                                        {
                                                            upperHalfFailed++;
                                                            upperProfit *= 1 + (limitPrice / list[i].Close - 1.001) * leverage;
                                                            //Console.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss}    upperHalfFailed \t profit = {upperProfit}");
                                                        }
                                                    }
                                                }
                                            }
                                            if (upperProfit > lastProfit && (upperSucceed > 0 || upperHalfSucceed > 0))
                                            {
                                                logger.WriteLine($"{x0:F8} / {x1:F8} / {x2:F8} / {x3:F8} / {x4:F8} / {stopX:F8} \t count = {upperTry} / {upperSucceed} / {upperFailed} / {upperHalfSucceed} / {upperHalfFailed}    score = {(double)upperSucceed / upperFailed:F4}    profit = {upperProfit:F8}");
                                                lastProfit = upperProfit;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    double lastProfit = 0;
                    for (float x1 = 1.0011f; x1 > .5f; x1 -= 0.0001f)
                    {
                        Console.WriteLine($"\r\ncandleX = {x1}");
                        for (float x2 = -1f; x2 < 2; x2 += 0.05f)
                        {
                            Console.Title = $"{lastProfit}    x1 = {x1}    x2 = {x2}";
                            for (float x3 = .998f; x3 < 1.00; x3 += 0.0001f)
                            {
                                for (float x4 = 1.0005f; x4 < 1.002; x4 += 0.0001f)
                                {
                                    for (float stopX = .001f; stopX < .03; stopX += 0.001f)
                                    //float stopX = .03f;
                                    {
                                        int lowerTry = 0, lowerSucceed = 0, lowerFailed = 0, lowerHalfSucceed = 0, lowerHalfFailed = 0;
                                        double lowerProfit = 1;
                                        for (int i = 2; i < count - 1; i++)
                                        {
                                            if (list[i - 1].Close / list[i - 1].Open > x1 /*&& list[i - 1].High / list[i - 1].Close > 1.00275 && (list[i - 1].Close - list[i - 1].Open) / (list[i - 1].High - list[i - 1].Close) > 1.225*/)
                                            {
                                                lowerTry++;
                                                float limitPrice = list[i - 1].Open - (list[i - 1].Close - list[i - 1].Open) * x2;
                                                //if (list[i - 1].High < limitPrice * .995f) continue;
                                                float closePrice = Math.Min(list[i - 1].High * x3, limitPrice * x4);
                                                if (closePrice / limitPrice < 1.0005) continue;
                                                float closeHeight = closePrice - limitPrice;
                                                //float stopPrice = limitPrice - closeHeight * stopX;
                                                float stopPrice = limitPrice * (1 - stopX);
                                                if (list[i].Low < limitPrice)
                                                {
                                                    if (list[i].Low <= stopPrice)
                                                    {
                                                        lowerFailed++;
                                                        lowerProfit *= 1 + (stopPrice / limitPrice - 1.001) * leverage;
                                                    }
                                                    else if (list[i].Close > closePrice)
                                                    {
                                                        lowerSucceed++;
                                                        lowerProfit *= 1 + (closePrice / limitPrice - 1.0002) * leverage;
                                                    }
                                                    else if (list[i].Close > limitPrice)
                                                    {
                                                        lowerHalfSucceed++;
                                                        lowerProfit *= 1 + (list[i].Close / limitPrice - 1.001) * leverage;
                                                    }
                                                    else
                                                    {
                                                        lowerHalfFailed++;
                                                        lowerProfit *= 1 + (list[i].Close / limitPrice - 1.001) * leverage;
                                                    }
                                                }
                                            }
                                        }
                                        if (lowerProfit > lastProfit && (lowerSucceed > 0 || lowerHalfSucceed > 0))
                                        {
                                            logger.WriteLine($"{x1:F8} / {x2:F8} / {x3:F8} / {x4:F8} / {stopX:F8} \t count = {lowerTry} / {lowerSucceed} / {lowerFailed} / {lowerHalfSucceed} / {lowerHalfFailed}    score = {(double)lowerSucceed / lowerFailed:F4}    profit = {lowerProfit:F8}");
                                            lastProfit = lowerProfit;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                logger.WriteLine($"end");
                return;
            }
            else
            {
                int upperTry = 0, upperSucceed = 0, upperFailed = 0, upperHalfSucceed = 0, upperHalfFailed = 0;
                int lowerTry = 0, lowerSucceed = 0, lowerFailed = 0, lowerHalfSucceed = 0, lowerHalfFailed = 0;
                double upperProfit = 1, lowerProfit = 1;
                for (int i = 2; i < count - 1; i++)
                {
                    if (list[i - 1].Open / list[i - 1].Close > 1.00065 /* && list[i - 1].Open / list[i - 1].Low > 1.00125&& (list[i - 1].Open - list[i - 1].Close) / (list[i - 1].Close - list[i - 1].Low) < .875*/)
                    {
                        upperTry++;
                        float limitPrice = list[i - 1].Open + (list[i - 1].Open - list[i - 1].Close) * .3f;
                        //if (list[i - 1].Low > limitPrice * .999f) continue;
                        float closePrice = Math.Max(list[i - 1].Low * 1.0006f, limitPrice * .99855f);
                        if (limitPrice / closePrice < 1.0005) continue;
                        float closeHeight = limitPrice - closePrice;
                        float stopPrice = limitPrice * 1.017f;
                        if (list[i].High >= limitPrice)
                        {
                            if (list[i].High >= stopPrice)
                            {
                                upperFailed++;
                                upperProfit *= 1 + (limitPrice / stopPrice - 1.001) * leverage;
                                logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss}    upperFailed \t profit = {upperProfit}");
                            }
                            else if (list[i].Close < closePrice)
                            {
                                upperSucceed++;
                                upperProfit *= 1 + (limitPrice / closePrice - 1.0002) * leverage;
                                logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss}    upperSucceed \t profit = {upperProfit}");
                            }
                            else if (list[i].Close < limitPrice)
                            {
                                upperHalfSucceed++;
                                upperProfit *= 1 + (limitPrice / list[i].Close - 1.002) * leverage;
                                logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss}    upperHalfSucceed \t profit = {upperProfit}");
                            }
                            else
                            {
                                upperHalfFailed++;
                                upperProfit *= 1 + (limitPrice / list[i].Close - 1.002) * leverage;
                                logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss}    upperHalfFailed \t profit = {upperProfit}");
                            }
                        }
                    }
                    if (list[i - 1].Close / list[i - 1].Open > 1.001 /*&& list[i - 1].High / list[i - 1].Close > 1.00275 && (list[i - 1].Close - list[i - 1].Open) / (list[i - 1].High - list[i - 1].Close) > 1.225*/)
                    {
                        lowerTry++;
                        float limitPrice = list[i - 1].Open + (list[i - 1].Close - list[i - 1].Open) * .05f;
                        //if (list[i - 1].High < limitPrice * .995f) continue;
                        float closePrice = Math.Min(list[i - 1].High * .9997f, limitPrice * 1.0012f);
                        if (closePrice / limitPrice < 1.0005) continue;
                        float closeHeight = closePrice - limitPrice;
                        float stopPrice = limitPrice * .971f;
                        if (list[i].Low < limitPrice)
                        {
                            if (list[i].Low <= stopPrice)
                            {
                                lowerFailed++;
                                lowerProfit *= 1 + (stopPrice / limitPrice - 1.001) * leverage;
                                logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss}    lowerFailed \t profit = {lowerProfit}");
                            }
                            else if (list[i].Close > closePrice)
                            {
                                lowerSucceed++;
                                lowerProfit *= 1 + (closePrice / limitPrice - 1.0002) * leverage;
                                logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss}    lowerSucceed \t profit = {lowerProfit}");
                            }
                            else if (list[i].Close > limitPrice)
                            {
                                lowerHalfSucceed++;
                                lowerProfit *= 1 + (list[i].Close / limitPrice - 1.002) * leverage;
                                logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss}    lowerHalfSucceed \t profit = {lowerProfit}");
                            }
                            else
                            {
                                lowerHalfFailed++;
                                lowerProfit *= 1 + (list[i].Close / limitPrice - 1.002) * leverage;
                                logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss}    lowerHalfFailed \t profit = {lowerProfit}");
                            }
                        }
                    }
                }
                logger.WriteLine("\r\n");
                logger.WriteLine($"upper = {upperTry}  /  {upperSucceed}  /  {upperFailed}  /  {upperHalfSucceed}  /  {upperHalfFailed}    score = {(double)upperSucceed / upperFailed:F4}    profit = {upperProfit:F8}");
                logger.WriteLine($"lower = {lowerTry}  /  {lowerSucceed}  /  {lowerFailed}  /  {lowerHalfSucceed}  /  {lowerHalfFailed}    score = {(double)lowerSucceed / lowerFailed:F4}    profit = {lowerProfit:F8}");
            }
        }


        static void Test_1h(DateTime startTime, DateTime? endTime = null)
        {
            List<BtcBin> list = BtcDao.SelectAll("1h");
            list.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
            int count = list.Count;
            int totalDays = (int)(list[count - 1].Timestamp - list[0].Timestamp).TotalDays;
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  ({list[0].Date} ~ {totalDays:N0} days)");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;

            int upperTry = 0, upperSucceed = 0, upperFailed = 0, upperHalfSucceed = 0, upperHalfFailed = 0;
            int lowerTry = 0, lowerSucceed = 0, lowerFailed = 0, lowerHalfSucceed = 0, lowerHalfFailed = 0;
            for (int i = 2; i < count - 1; i++)
            {
                if (list[i - 1].Open > list[i - 1].Close && list[i - 1].Open / list[i - 1].Low > 1.00275 && (list[i - 1].Open - list[i - 1].Close) / (list[i - 1].Close - list[i - 1].Low) < .875)
                {
                    upperTry++;
                    float limitPrice = list[i - 1].Open + (list[i - 1].Open - list[i - 1].Close) * .675f;
                    //if (list[i - 1].Low > limitPrice * .995f) continue;
                    float closePrice = Math.Min(list[i - 1].Low, limitPrice * .9946f);
                    float closeHeight = limitPrice - closePrice;
                    float stopPrice = limitPrice + closeHeight * .9f;
                    if (list[i].High >= limitPrice)
                    {
                        if (list[i].High >= stopPrice)
                        {
                            upperFailed++;
                            Console.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss}    upperFailed");
                        }
                        else if (list[i].Low < closePrice)
                        {
                            upperSucceed++;
                            Console.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss}    upperSucceed");
                        }
                        else if (list[i].Close < limitPrice)
                        {
                            upperHalfSucceed++;
                            Console.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss}    upperHalfSucceed");
                        }
                        else
                        {
                            upperHalfFailed++;
                            Console.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss}    upperHalfFailed");
                        }
                    }
                }
                if (list[i - 2].Open < list[i - 2].Close && list[i - 1].Open < list[i - 1].Close && list[i - 1].High / list[i - 1].Close > 1.00275 && (list[i - 1].Close - list[i - 1].Open) / (list[i - 1].High - list[i - 1].Close) > 1.225)
                {
                    lowerTry++;
                    float limitPrice = list[i - 1].Open + (list[i - 1].Close - list[i - 1].Open) * .35f;
                    //if (list[i - 1].High < limitPrice * .995f) continue;
                    float closePrice = Math.Max(list[i - 1].High, limitPrice * 1.006f);
                    float closeHeight = closePrice - limitPrice;
                    float stopPrice = limitPrice - closeHeight * .875f;
                    if (list[i].Low < limitPrice)
                    {
                        if (list[i].Low <= stopPrice)
                        {
                            lowerFailed++;
                            Console.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss}    lowerFailed");
                        }
                        else if (list[i].High > closePrice)
                        {
                            lowerSucceed++;
                            Console.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss}    lowerSucceed");
                        }
                        else if (list[i].Close > limitPrice)
                        {
                            lowerHalfSucceed++;
                            Console.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss}    lowerHalfSucceed");
                        }
                        else
                        {
                            lowerHalfFailed++;
                            Console.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss}    lowerHalfFailed");
                        }
                    }
                }
            }

            Console.WriteLine($"upper = {upperTry}  /  {upperSucceed}  /  {upperFailed}  /  {upperHalfSucceed}  /  {upperHalfFailed}    {(double)upperSucceed / upperFailed:F4}");
            Console.WriteLine($"lower = {lowerTry}  /  {lowerSucceed}  /  {lowerFailed}  /  {lowerHalfSucceed}  /  {lowerHalfFailed}    {(double)lowerSucceed / lowerFailed:F4}");
        }

        static string SimulateSMA(string binSize, int buyOrSell, int smaLength, int delayLength, float limitX, float closeX, DateTime startTime, DateTime? endTime = null)
        {
            List<BtcBin> list = BtcDao.SelectAll(binSize);
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
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  buyOrSell = {buyOrSell}    sma = {smaLength}    delay = {delayLength}    limitX = {limitX:F4}    closeX = {closeX:F4}    ({list[0].Date} ~ {totalDays:N0} days)");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;
            var topList = new List<Dictionary<string, object>>();

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
                        logger.WriteLine($"{list[i].Timestamp} \t Closed \t close = {list[i].Close:F1} \t high = {list[i].High}");
                        continue;
                    }
                    //if (positionQty == 0 || positionEntryPrice > sma)
                    if (list[i].Open > sma)
                    {
                        int limitPrice;
                        int deep = 0;
                        do
                        {
                            //if (positionQty == 0)
                            //    limitPrice = (int)Math.Ceiling(sma - sma * limitX * (2 + deep / 5f));
                            //else 
                            //if (positionEntryPrice > sma)
                            //    limitPrice = (int)Math.Ceiling(sma - sma * limitX * (1 + /* (positionQty - 1) */ deep));
                            if (positionQty == 0)
                                limitPrice = (int)Math.Ceiling(100000 - 100000 * limitX * (1 + deep));
                            //limitPrice = (int)Math.Ceiling(sma - sma * limitX * (1 + deep));
                            else
                                limitPrice = (int)Math.Ceiling(positionEntryPrice - positionEntryPrice * limitX * (1 + /* (positionQty - 1) */ deep));
                            if (list[i].Open > limitPrice && list[i].Low < limitPrice)
                            {
                                tryCount++;
                                positionEntryPrice = (positionEntryPrice * positionQty + limitPrice) / (positionQty + 1);
                                positionQty++;
                                logger.WriteLine($"{list[i].Timestamp} \t price = {list[i].Close:F1} \t sma = {sma:F1} \t limit = {limitPrice} \t try = {tryCount} \t succeed = {succeedCount} \t entry = {positionEntryPrice:F1} \t deep = {deep} \t qty = {positionQty}");
                                if (maxWeight < positionQty)
                                    maxWeight = positionQty;
                            }
                            else if (list[i].Low > limitPrice) break;
                            deep++;
                        }
                        while (list[i].Open < limitPrice);
                        if (maxDeep < deep) maxDeep = deep;
                    }
                }
            }
            double score = totalProfit / ((maxWeight - 1) / 10 + 1);
            //double score = totalPercent;
            string text = $"{limitX:F4}  /  {closeX:F4} \t count = {tryCount}  /  {succeedCount} \t maxD = {maxDeep} \t maxW = {maxWeight} \t total = {totalProfit:F4} \t score = {score:F4} \t % = {totalPercent:F4}";
            logger.WriteLine("\r\n" + text);
            return text;
        }

        static string Benchmark(List<BtcBin> list, int buyOrSell, float minRSI, float maxRSI)
        {
            int count = list.Count;
            int totalDays = (int)(list[count - 1].Timestamp - list[0].Timestamp).TotalDays;
            Logger logger = new Logger($"{ProcessName}  -  {DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  buyOrSell = {buyOrSell}    {minRSI} ~ {maxRSI}   ({list[0].Date} ~ {totalDays:N0} days)");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;

            List<Dictionary<string, object>> topList = new List<Dictionary<string, object>>();

            for (float minDiff = -20f; minDiff < 5f; minDiff += minDiff < -10 ? 1 : .5f)
            //float minDiff = -4;
            {
                for (float maxDiff = 20; maxDiff > minDiff + 1; maxDiff -= maxDiff > 10 ? 1 : .5f)
                //float maxDiff = 3f;
                {
                    logger.WriteLine($"\n\n----    minDiff = {minDiff}    maxDiff = {maxDiff}    ----\n");
                    for (float stopX = 0.001f; stopX < 0.01f; stopX += 0.001f)
                    //float stopX = 0.005f;
                    {
                        for (float closeX = 0.001f; closeX < 0.01f; closeX += .001f)
                        //float closeX = 0.006f;
                        {
                            int tryCount = 0, succeedCount = 0, failedCount = 0;
                            float totalProfit = 0;
                            float finalPercent = 1;
                            double p = (closeX - 0.001) / (stopX + 0.002);
                            for (int i = 3; i < count - 1; i++)
                            {
                                if (buyOrSell == 1 && list[i - 3].RSI >= list[i - 2].RSI && list[i - 1].RSI >= minRSI && list[i - 1].RSI < maxRSI && list[i - 1].RSI - list[i - 2].RSI >= minDiff && (list[i - 1].RSI - list[i - 2].RSI) < maxDiff)
                                {
                                    tryCount++;
                                    float positionEntryPrice = list[i].Open;
                                    for (int j = i; j < count; j++)
                                    {
                                        if (list[j].Low < positionEntryPrice - positionEntryPrice * stopX)
                                        {
                                            failedCount++;
                                            totalProfit -= stopX + 0.002f;
                                            finalPercent *= .95f;
                                            i = j + 1;
                                            goto solved;
                                        }
                                        else if (list[j].High > positionEntryPrice + positionEntryPrice * closeX)
                                        {
                                            succeedCount++;
                                            totalProfit += closeX - 0.001f;
                                            finalPercent *= (float)(1 + .05 * p);
                                            i = j + 1;
                                            goto solved;
                                        }
                                    }
                                solved:;
                                }
                                else if (buyOrSell == 2 && list[i - 3].RSI <= list[i - 2].RSI && list[i - 1].RSI >= minRSI && list[i - 1].RSI < maxRSI && list[i - 2].RSI - list[i - 1].RSI >= minDiff && (list[i - 2].RSI - list[i - 1].RSI) < maxDiff)
                                {
                                    tryCount++;
                                    float positionEntryPrice = list[i].Open;
                                    for (int j = i; j < count; j++)
                                    {
                                        if (list[j].High > positionEntryPrice + positionEntryPrice * stopX)
                                        {
                                            failedCount++;
                                            totalProfit -= stopX + 0.002f;
                                            finalPercent *= .95f;
                                            i = j + 1;
                                            goto solved;
                                        }
                                        else if (list[j].Low < positionEntryPrice - positionEntryPrice * closeX)
                                        {
                                            succeedCount++;
                                            totalProfit += closeX - 0.001f;
                                            finalPercent *= (float)(1 + .05 * p);
                                            i = j + 1;
                                            goto solved;
                                        }
                                    }
                                solved:;
                                }
                            }
                            //float score = succeedCount - failedCount * stopX / closeX * lossX;
                            float score;
                            if (failedCount > 0) score = (float)succeedCount / failedCount;
                            else score = succeedCount;
                            float avgProfit = 10000 * totalProfit / totalDays * (float)Math.Sqrt((closeX - 0.001f) / (stopX + 0.002f));
                            string text = $"{minRSI} ~ {maxRSI}    {minDiff} / {maxDiff}    <{buyOrSell}>    {closeX:F4}  /  {stopX:F4}    count = {tryCount}    {succeedCount}    {failedCount}    score = {score:F2} \t total = {totalProfit:F4} \t avg = {avgProfit:F4} \t final = {finalPercent:F4}";
                            if (finalPercent > 1 && succeedCount >= 2)
                            {
                                Dictionary<string, object> dic = new Dictionary<string, object>
                                {
                                    { "minDiff", minDiff },
                                    { "maxDiff", maxDiff },
                                    { "closeX", closeX },
                                    { "stopX", stopX },
                                    { "tryCount", tryCount },
                                    { "succeedCount", succeedCount },
                                    { "failedCount", failedCount },
                                    { "score", score },
                                    { "totalProfit", totalProfit },
                                    { "avgProfit", avgProfit },
                                    { "finalPercent", finalPercent },
                                    { "text", text },
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
                                        if ((float)topList[i]["finalPercent"] > finalPercent ||
                                                (float)topList[i]["finalPercent"] == finalPercent && (float)topList[i]["minDiff"] > minDiff ||
                                                (float)topList[i]["finalPercent"] == finalPercent && (float)topList[i]["minDiff"] == minDiff && (float)topList[i]["maxDiff"] < maxDiff)
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
                            //else if (finalPercent > .5f)
                            //{
                            //    Console.WriteLine(text);
                            //}
                            if (tryCount < 2) goto next_minDiff;
                            if (succeedCount < 2) break;
                        }
                    }
                }
            next_minDiff:;
            }
            logger.WriteLine($"\r\n\r\n\r\n\r\ntopList={topList.Count}\r\n" + JArray.FromObject(topList).ToString());
            //logger.WriteLine($"\r\n\r\n\r\n\r\ntop0List={top0List.Count}\r\n" + JArray.FromObject(top0List).ToString());
            if (topList.Count > 0)
                return (string)topList[topList.Count - 1]["text"];
            return null;
        }

        static string Benchmark2(List<BtcBin> list, int buyOrSell, float minRSI, float maxRSI)
        {
            minRSI = 30;
            maxRSI = 35;

            #region
            int count = list.Count;
            int totalDays = (int)(list[count - 1].Timestamp - list[0].Timestamp).TotalDays;
            Logger logger = new Logger($"{ProcessName}  -  {DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  buyOrSell = {buyOrSell}    {minRSI} ~ {maxRSI}   ({list[0].Date} ~ {totalDays:N0} days)");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;
            List<Dictionary<string, object>> topList = new List<Dictionary<string, object>>();
            #endregion
            //for (float minDiff2 = -30f; minDiff2 < 10f; minDiff2 += 1)
            float minDiff2 = -23;
            {
                //for (float maxDiff2 = 40; maxDiff2 > minDiff2 + 1; maxDiff2 -= 1)
                float maxDiff2 = 0;
                {
                    for (float minDiff = -25f; minDiff < 5; minDiff += .5f)
                    //float minDiff = 0.5f;
                    {
                        for (float maxDiff = 20; maxDiff > minDiff + 1; maxDiff -= .5f)
                        //float maxDiff = 3.5f;
                        {
                            for (float stopX = 0.003f; stopX < 0.0105f; stopX += 0.001f)
                            //float stopX = 0.004f;
                            {
                                for (float closeX = 0.005f; closeX < 0.0105f; closeX += .001f)
                                //float closeX = 0.01f;
                                {
                                    int tryCount = 0, succeedCount = 0, failedCount = 0;
                                    float totalProfit = 0;
                                    float finalPercent = 1;
                                    double p = (closeX - 0.001) / (stopX + 0.002);
                                    for (int i = 3; i < count - 1; i++)
                                    {
                                        if (buyOrSell == 1 && list[i - 1].RSI >= minRSI && list[i - 1].RSI < maxRSI && list[i - 1].RSI - list[i - 2].RSI >= minDiff && (list[i - 1].RSI - list[i - 2].RSI) < maxDiff && list[i - 1].RSI - list[i - 3].RSI >= minDiff2 && (list[i - 1].RSI - list[i - 3].RSI) < maxDiff2)
                                        {
                                            tryCount++;
                                            float positionEntryPrice = list[i].Open;
                                            for (int j = i; j < count; j++)
                                            {
                                                if (list[j].Low < positionEntryPrice - positionEntryPrice * stopX)
                                                {
                                                    failedCount++;
                                                    totalProfit -= stopX + 0.002f;
                                                    finalPercent *= .95f;
                                                    i = j + 1;
                                                    goto solved;
                                                }
                                                else if (list[j].High > positionEntryPrice + positionEntryPrice * closeX)
                                                {
                                                    succeedCount++;
                                                    totalProfit += closeX - 0.001f;
                                                    finalPercent *= (float)(1 + .05 * p);
                                                    i = j + 1;
                                                    goto solved;
                                                }
                                            }
                                        solved:;
                                        }
                                        else if (buyOrSell == 2 && list[i - 1].RSI >= minRSI && list[i - 1].RSI < maxRSI && list[i - 2].RSI - list[i - 1].RSI >= minDiff && (list[i - 2].RSI - list[i - 1].RSI) < maxDiff && list[i - 3].RSI - list[i - 1].RSI >= minDiff2 && (list[i - 3].RSI - list[i - 1].RSI) < maxDiff2)
                                        {
                                            tryCount++;
                                            float positionEntryPrice = list[i].Open;
                                            for (int j = i; j < count; j++)
                                            {
                                                if (list[j].High > positionEntryPrice + positionEntryPrice * stopX)
                                                {
                                                    failedCount++;
                                                    totalProfit -= stopX + 0.002f;
                                                    finalPercent *= .95f;
                                                    i = j + 1;
                                                    goto solved;
                                                }
                                                else if (list[j].Low < positionEntryPrice - positionEntryPrice * closeX)
                                                {
                                                    succeedCount++;
                                                    totalProfit += closeX - 0.001f;
                                                    finalPercent *= (float)(1 + .05 * p);
                                                    i = j + 1;
                                                    goto solved;
                                                }
                                            }
                                        solved:;
                                        }
                                    }
                                    //float score = succeedCount - failedCount * stopX / closeX * lossX;
                                    float score;
                                    if (failedCount > 0) score = (float)succeedCount / failedCount;
                                    else score = succeedCount;
                                    float avgProfit = 10000 * totalProfit / totalDays * (float)Math.Sqrt((closeX - 0.001f) / (stopX + 0.002f));
                                    string text = $"{minRSI} ~ {maxRSI}    {minDiff2} / {maxDiff2}    {minDiff} / {maxDiff}    <{buyOrSell}>    {closeX:F4}  /  {stopX:F4}    count = {tryCount}    {succeedCount}    {failedCount}    score = {score:F2} \t total = {totalProfit:F4} \t avg = {avgProfit:F4} \t final = {finalPercent:F4}";
                                    if (finalPercent > 1 && succeedCount >= 2)
                                    {
                                        Dictionary<string, object> dic = new Dictionary<string, object>
                                        {
                                            { "minRSI", minRSI },
                                            { "maxRSI", maxRSI },
                                            { "minDiff2", minDiff2 },
                                            { "maxDiff2", maxDiff2 },
                                            { "minDiff", minDiff },
                                            { "maxDiff", maxDiff },
                                            { "buyOrSell", buyOrSell },
                                            { "closeX", closeX },
                                            { "stopX", stopX },
                                            { "tryCount", tryCount },
                                            { "succeedCount", succeedCount },
                                            { "failedCount", failedCount },
                                            { "score", score },
                                            { "totalProfit", totalProfit },
                                            { "avgProfit", avgProfit },
                                            { "finalPercent", finalPercent },
                                            { "text", text },
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
                                                if ((float)topList[i]["finalPercent"] > finalPercent ||
                                                        (float)topList[i]["finalPercent"] == finalPercent && (float)topList[i]["minDiff"] > minDiff ||
                                                        (float)topList[i]["finalPercent"] == finalPercent && (float)topList[i]["minDiff"] == minDiff && (float)topList[i]["maxDiff"] < maxDiff ||
                                                        (float)topList[i]["finalPercent"] == finalPercent && (float)topList[i]["minDiff"] == minDiff && (float)topList[i]["maxDiff"] == maxDiff && (float)topList[i]["minDiff2"] > minDiff2 ||
                                                        (float)topList[i]["finalPercent"] == finalPercent && (float)topList[i]["minDiff"] == minDiff && (float)topList[i]["maxDiff"] == maxDiff && (float)topList[i]["minDiff2"] == minDiff2 && (float)topList[i]["maxDiff2"] < maxDiff2)
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
                                    //else if (finalPercent > .5f)
                                    //{
                                    //    Console.WriteLine(text);
                                    //}
                                    if (tryCount < 2) goto next_minDiff;
                                    if (succeedCount < 2) break;
                                }
                            }
                        }
                    next_minDiff:;
                    }
                }
            }
            logger.WriteLine($"\r\n\r\n\r\n\r\ntopList={topList.Count}\r\n" + JArray.FromObject(topList).ToString());
            //logger.WriteLine($"\r\n\r\n\r\n\r\ntop0List={top0List.Count}\r\n" + JArray.FromObject(top0List).ToString());
            if (topList.Count > 0)
                return (string)topList[topList.Count - 1]["text"];
            return null;
        }



        public class ParamMap
        {
            public string ID { get; set; }
            public int BuyOrSell { get; set; }
            public decimal MinRSI { get; set; }
            public decimal MaxRSI { get; set; }
            public decimal MinDiff2 { get; set; }
            public decimal MaxDiff2 { get; set; }
            public decimal MinDiff { get; set; }
            public decimal MaxDiff { get; set; }
            public decimal CloseX { get; set; }
            public decimal StopX { get; set; }
            public decimal QtyX { get; set; }

            public int SucceedCount { get; set; }
            public int FailedCount { get; set; }
        }

        static string Simulate(DateTime startTime, DateTime? endTime = null)
        {
            ParamMap[] paramMapArray;
            {
                string paramText = @"1	1	0	10	-15	0	-5	-1	0.01	0.008	1
2	1	10	15	-5	5	-3	-1.5	0.01	0.008	1
3	1	15	20	-25	-6	-10.5	-6.5	0.01	0.007	1.111111111
4	1	20	25	-20	-10	-11	-8.5	0.008	0.007	1.111111111
5	1	20	25	-5	0	2.5	5	0.01	0.01	0.833333333
6	1	25	30	-15	-2	-0.5	1	0.01	0.01	0.833333333
7	1	30	35	-7	-2	-10	-8	0.01	0.007	1.111111111
8	1	30	35	-19	0	-18	-15.5	0.009	0.007	1.111111111
9	1	30	35	-23	-13	-11	-9.5	0.01	0.01	0.833333333
10	1	35	40	-25	-5	-18	-14.5	0.009	0.009	0.909090909
11	1	35	40	-8	0	4	10	0.01	0.008	1
12	1	40	45	-15	-10	-4.5	-3	0.01	0.01	0.833333333
13	1	45	50	-22	10	-18	-13	0.01	0.009	0.909090909
14	1	45	50	-22	-10	-9	-7.5	0.007	0.007	1.111111111
15	1	45	50	-25	-10	-2.5	0.5	0.01	0.008	1
16	1	50	55	6	9	-6.5	-2.5	0.009	0.01	0.833333333
17	1	55	60	-14	-5	-16	-13	0.01	0.008	1
18	1	55	60	-15	-8	-11	-9.5	0.008	0.009	0.909090909
19	1	60	65	-7	9	-12	-9.5	0.01	0.003	2
20	1	65	70	-13	2	-13	-11.5	0.007	0.003	2
21	1	65	70	2	5	4.5	8	0.009	0.009	0.909090909
22	1	70	75	-20	-10	-20	-5	0.01	0.004	1.666666667
23	1	70	75	-16	2	-7.5	-6	0.009	0.007	1.111111111
24	1	70	75	1	5	-2	0.5	0.009	0.007	1.111111111
25	1	75	80	-10	19	-7	-5	0.009	0.004	1.666666667
26	1	75	80	-15	-4	-3.5	-2	0.007	0.004	1.666666667
27	1	80	85	-5	4	-4.5	0.5	0.01	0.005	1.428571429
28	1	80	85	-5	2	-2	11	0.01	0.006	1.25
29	1	85	90	9	11	3	6.5	0.01	0.004	1.666666667
30	1	90	100	7	33	0.5	3.5	0.01	0.003	2
";
                string[] paramLines = paramText.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                List<ParamMap> paramMapList = new List<ParamMap>();
                foreach (string paramLine in paramLines)
                {
                    string[] paramValues = paramLine.Split(new string[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
                    paramMapList.Add(new ParamMap
                    {
                        ID = paramValues[0],
                        BuyOrSell = Int32.Parse(paramValues[1]),
                        MinRSI = decimal.Parse(paramValues[2]),
                        MaxRSI = decimal.Parse(paramValues[3]),
                        MinDiff2 = decimal.Parse(paramValues[4]),
                        MaxDiff2 = decimal.Parse(paramValues[5]),
                        MinDiff = decimal.Parse(paramValues[6]),
                        MaxDiff = decimal.Parse(paramValues[7]),
                        CloseX = decimal.Parse(paramValues[8]),
                        StopX = decimal.Parse(paramValues[9]),
                        QtyX = decimal.Parse(paramValues[10]),
                    });
                }
                paramMapArray = paramMapList.ToArray();
            }

            List<BtcBin> list = BtcDao.SelectAll("5m");
            int count = list.Count;
            {
                const int rsiLength = 14;
                List<TradeBin> binList = new List<TradeBin>();
                foreach (BtcBin m in list)
                {
                    binList.Add(new TradeBin(m.Timestamp, BitMEXApiHelper.SYMBOL_XBTUSD, (decimal)m.Open, (decimal)m.High, (decimal)m.Low, (decimal)m.Close));
                }
                double[] rsiArray = RSI.CalculateRSIValues(binList.ToArray(), rsiLength);
                for (int i = 0; i < count; i++)
                {
                    BtcBin m = list[i];
                    m.RSI = (float)rsiArray[i];
                }
                list.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp >= endTime.Value);
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
                    if (paramMap.BuyOrSell == 1 && paramMap.QtyX > 0 && list[i - 1].RSI >= (float)paramMap.MinRSI && list[i - 1].RSI < (float)paramMap.MaxRSI && list[i - 1].RSI - list[i - 2].RSI >= (float)paramMap.MinDiff && (list[i - 1].RSI - list[i - 2].RSI) < (float)paramMap.MaxDiff && list[i - 1].RSI - list[i - 3].RSI >= (float)paramMap.MinDiff2 && (list[i - 1].RSI - list[i - 3].RSI) < (float)paramMap.MaxDiff2)
                    {
                        float positionEntryPrice = list[i].Open;
                        DateTime positionTime = list[i].Timestamp;
                        logger.WriteLine($"{list[i].Timestamp} \t position created: open = {list[i].Open}    high = {list[i].High}    low = {list[i].Low}    close = {list[i].Close}    rsi = {list[i - 2].RSI}    diff = {list[i - 1].RSI - list[i - 2].RSI}");
                        for (int j = i; j < count; j++)
                        {
                            double p = (closeX - 0.0002) / (stopX + 0.001);
                            if (list[j].Low < positionEntryPrice - positionEntryPrice * stopX)
                            {
                                failedCount++;
                                totalProfit -= (stopX + 0.001f) * lossX;
                                finalPercent10 *= .95;
                                finalPercent15 *= .925;
                                finalPercent20 *= .9;
                                positionEntryPrice = 0;
                                logger.WriteLine($"{list[j].Timestamp} \t - failed on {paramMap.ID} ({(list[j].Timestamp - positionTime).TotalMinutes}) \t p = {p:F2} \t final = {finalPercent10:F2} \t final2 = {finalPercent20:F2}\r\n");
                                paramMap.FailedCount++;
                                //i = j + 1;
                                break;
                            }
                            if (list[j].High > positionEntryPrice + positionEntryPrice * closeX)
                            {
                                succeedCount++;
                                totalProfit += closeX - 0.0002f;
                                finalPercent10 *= 1 + .05 * p;
                                finalPercent15 *= 1 + .075 * p;
                                finalPercent20 *= 1 + .1 * p;
                                positionEntryPrice = 0;
                                logger.WriteLine($"{list[j].Timestamp} \t succeed on {paramMap.ID} ({(list[j].Timestamp - positionTime).TotalMinutes}) \t p = {p:F2} \t final = {finalPercent10:F2} / {finalPercent15:F2} / {finalPercent20:F2}\r\n");
                                paramMap.SucceedCount++;
                                //i = j + 1;
                                break;
                            }
                        }
                        break;
                    }
                    if (paramMap.BuyOrSell == 2 && paramMap.QtyX > 0 && list[i - 1].RSI >= (float)paramMap.MinRSI && list[i - 1].RSI < (float)paramMap.MaxRSI && list[i - 2].RSI - list[i - 1].RSI >= (float)paramMap.MinDiff && (list[i - 2].RSI - list[i - 1].RSI) < (float)paramMap.MaxDiff && list[i - 3].RSI - list[i - 1].RSI >= (float)paramMap.MinDiff2 && (list[i - 3].RSI - list[i - 1].RSI) < (float)paramMap.MaxDiff2)
                    {
                        float positionEntryPrice = list[i].Open;
                        DateTime positionTime = list[i].Timestamp;
                        logger.WriteLine($"{list[i].Timestamp} \t position created: open = {list[i].Open}    high = {list[i].High}    low = {list[i].Low}    close = {list[i].Close}    rsi = {list[i - 2].RSI}    diff = {list[i - 1].RSI - list[i - 2].RSI}");
                        for (int j = i; j < count; j++)
                        {
                            double p = (closeX - 0.0002) / (stopX + 0.001);
                            if (list[j].High > positionEntryPrice + positionEntryPrice * stopX)
                            {
                                failedCount++;
                                totalProfit -= (stopX + 0.001f) * lossX;
                                finalPercent10 *= .95;
                                finalPercent15 *= .925;
                                finalPercent20 *= .9;
                                positionEntryPrice = 0;
                                logger.WriteLine($"{list[j].Timestamp} \t - failed on {paramMap.ID} ({(list[j].Timestamp - positionTime).TotalMinutes}) \t p = {p:F2} \t final = {finalPercent10:F2} / {finalPercent15:F2} / {finalPercent20:F2}\r\n");
                                paramMap.FailedCount++;
                                //i = j + 1;
                                break;
                            }
                            if (list[j].Low < positionEntryPrice - positionEntryPrice * closeX)
                            {
                                succeedCount++;
                                totalProfit += closeX - 0.0002f;
                                finalPercent10 *= 1 + .05 * p;
                                finalPercent15 *= 1 + .075 * p;
                                finalPercent20 *= 1 + .1 * p;
                                positionEntryPrice = 0;
                                logger.WriteLine($"{list[j].Timestamp} \t succeed on {paramMap.ID} ({(list[j].Timestamp - positionTime).TotalMinutes}) \t p = {p:F2} \t final = {finalPercent10:F2} / {finalPercent15:F2} / {finalPercent20:F2}\r\n");
                                paramMap.SucceedCount++;
                                //i = j + 1;
                                break;
                            }
                        }
                        break;
                    }
                }
            }
            float avgProfit = totalProfit / totalDays;
            string result = $"\r\nsucceed = {succeedCount} \t failed = {failedCount} \t total = {totalProfit:F8} \t avg = {avgProfit:F8} \t final = {finalPercent10:F2} / {finalPercent15:F2} / {finalPercent20:F2}\r\n";
            logger.WriteLine(result);
            foreach (ParamMap paramMap in paramMapArray)
            {
                logger.WriteLine($"{paramMap.ID} \t {paramMap.MinRSI} ~ {paramMap.MaxRSI} \t {paramMap.SucceedCount} \t {paramMap.FailedCount}");
            }
            return result;
        }

    }
}
