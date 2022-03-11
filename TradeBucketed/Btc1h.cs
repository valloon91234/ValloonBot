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
    static class Btc1h
    {
        const string binSize = "1h";

        public static void Run()
        {
            //Program.MoveWindow(24, 0, 1400, 120);

            //{
            //    DateTime startTime = new DateTime(2022, 3, 9, 0, 0, 0, DateTimeKind.Utc);
            //    DateTime endTime = DateTime.UtcNow;
            //    Load("5m", startTime, endTime);
            //    //LoadCSV("1m", startTime, endTime);
            //    return;
            //}

            {
                DateTime startTime = new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Utc);
                {
                    Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  buy = 1");

                    //for (int delay = 1; delay <= 15; delay++)
                    //{
                    //    int sma = 25;
                    //    while (sma < 1000)
                    //    {
                    //        sma += sma / 60 + 1;
                    //        string result = TestSMA("1m", 1, sma, delay, startTime);
                    //        logger.WriteLine($"  {sma} / {delay} \t {result}");
                    //    }
                    //    logger.WriteLine("\n");
                    //}

                    //TestSMA("5m", 1, 390, 22, startTime); return;
                    //for (int delay = 1; delay <= 160; delay += 6)
                    //{
                    //    int sma = 312;
                    //    string result = TestSMA("5m", 1, sma, delay, startTime);
                    //    logger.WriteLine($"  {sma} / {delay} \t {result}");
                    //}
                    //return;

                    //for (int delay = 1; delay <= 240; delay += 6)
                    //{
                    //    int sma = 240;
                    //    while (sma < 6000)
                    //    {
                    //        string result = TestSMA("5m", 1, sma, delay, startTime);
                    //        logger.WriteLine($"  {sma} / {delay} \t {result}");
                    //        sma += sma < 996 ? 24 : 48;
                    //    }
                    //    logger.WriteLine("\n");
                    //}

                    //for (int delay = 1; delay <= 120; delay += 3)
                    //{
                    //    for (int sma = 120; sma <= 720; sma += 6)
                    //    {
                    //        string result = BenchmarkSMA("1h", 1, sma, delay, startTime);
                    //        logger.WriteLine($"  {sma} / {delay} \t {result}");
                    //    }
                    //    logger.WriteLine("\n");
                    //}

                    //SimulateSMA("1h", 1, 186, 25, 0.0125f, 0.025f, startTime);

                    Test_1m();
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

        const float lossX = 1.5f;

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
                    if (list[i].Date == "2022-01-24")
                        list[i].Date = "2022-01-24";
                    //if (positionQty == 0 || positionEntryPrice > sma)
                    {
                        int limitPrice;
                        int deep = 0;
                        do
                        {
                            if (positionQty == 0)
                                limitPrice = (int)Math.Ceiling(sma - sma * limitX * (2 + deep / 5f));
                            else if (positionEntryPrice > sma)
                                limitPrice = (int)Math.Ceiling(sma - sma * limitX * (1 + /* (positionQty - 1) */ deep / 5f));
                            else
                                limitPrice = (int)Math.Ceiling(positionEntryPrice - positionEntryPrice * limitX * (1 + /* (positionQty - 1) */ deep / 5f));
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

        static string Benchmark(int buyOrSell, float minRSI, float maxRSI, DateTime startTime, DateTime? endTime = null)
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
                list.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp >= endTime.Value);
                count = list.Count;
            }
            int totalDays = (int)(list[count - 1].Timestamp - list[0].Timestamp).TotalDays;
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  buyOrSell = {buyOrSell}    {minRSI} ~ {maxRSI}   ({list[0].Date} ~ {totalDays:N0} days)");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;

            List<Dictionary<string, object>> topList = new List<Dictionary<string, object>>();
            //List<Dictionary<string, float>> top0List = new List<Dictionary<string, float>>();

            for (float minDiff = -5f; minDiff < 5f; minDiff += .5f)
            //float minDiff = 9.5f;
            {
                for (float maxDiff = 20; maxDiff > minDiff + 1f; maxDiff -= maxDiff < 10 ? .5f : 1f)
                //float maxDiff = 2.46f;
                {
                    logger.WriteLine($"\n\n----    minDiff = {minDiff}    maxDiff = {maxDiff}    ----\n");
                    for (float stopX = 0.002f; stopX < 0.015f; stopX += 0.001f)
                    //float stopX = 0.0135f;
                    {
                        for (float closeX = 0.003f; closeX < 0.015f; closeX += closeX < 0.01f ? 0.001f : 0.001f)
                        //float closeX = 0.0075f;
                        {
                            int tryCount = 0, succeedCount = 0, failedCount = 0, unknownCount = 0;
                            float totalProfit = 0;
                            float finalPercent = 1;
                            double p = (closeX - 0.001) / (stopX + 0.002);
                            for (int i = 3; i < count - 1; i++)
                            {
                                if (buyOrSell == 1 && list[i - 3].RSI >= list[i - 2].RSI && list[i - 2].RSI >= minRSI && list[i - 2].RSI < maxRSI && list[i - 1].RSI - list[i - 2].RSI >= minDiff && (list[i - 1].RSI - list[i - 2].RSI) < maxDiff)
                                {
                                    tryCount++;
                                    float positionEntryPrice = list[i].Open;
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
                                    tryCount++;
                                    float positionEntryPrice = list[i].Open;
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
                            string text = $"{minRSI} ~ {maxRSI}    {minDiff} / {maxDiff}    {closeX:F4}  /  {stopX:F4}    count = {tryCount}    {succeedCount}    {failedCount}    {unknownCount}    score = {score:F2} \t total = {totalProfit:F4} \t avg = {avgProfit:F4} \t final = {finalPercent:F4}";
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
                                    { "unknownCount", unknownCount },
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
                                                (float)topList[i]["finalPercent"] == finalPercent && (float)topList[i]["minDiff"] < minDiff ||
                                                (float)topList[i]["finalPercent"] == finalPercent && (float)topList[i]["minDiff"] == minDiff && (float)topList[i]["maxDiff"] > maxDiff)
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
                            else if (finalPercent > .5f)
                            {
                                Console.WriteLine(text);
                            }
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


        static string Benchmark2(int buyOrSell, float minRSI, float maxRSI, DateTime startTime, DateTime? endTime = null)
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
                list.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp >= endTime.Value);
                count = list.Count;
            }
            int totalDays = (int)(list[count - 1].Timestamp - list[0].Timestamp).TotalDays;
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  buyOrSell = {buyOrSell}    {minRSI} ~ {maxRSI}   ({list[0].Date} ~ {totalDays:N0} days)");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;

            List<Dictionary<string, object>> topList = new List<Dictionary<string, object>>();
            //List<Dictionary<string, float>> top0List = new List<Dictionary<string, float>>();

            for (float minDiff = 0f; minDiff < 20f; minDiff += 1f)
            //float minDiff = 9.5f;
            {
                logger.WriteLine($"\n\n----    minDiff = {minDiff}    ----\n");
                for (float stopX = 0.002f; stopX < 0.02f; stopX += 0.001f)
                //float stopX = 0.0135f;
                {
                    for (float closeX = stopX; closeX < 0.02f; closeX += 0.001f)
                    //float closeX = 0.0075f;
                    {
                        int tryCount = 0, succeedCount = 0, failedCount = 0, unknownCount = 0;
                        float totalProfit = 0;
                        float finalPercent = 1;
                        double p = (closeX - 0.001) / (stopX + 0.002);
                        for (int i = 3; i < count - 1; i++)
                        {
                            if (buyOrSell == 1 && list[i - 2].RSI >= minRSI && list[i - 2].RSI < maxRSI && list[i - 2].RSI - list[i - 1].RSI >= minDiff)
                            {
                                tryCount++;
                                float positionEntryPrice = list[i].Open;
                                for (int j = i; j < count && j < i + rsiLength * 24; j++)
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
                            else if (buyOrSell == 2 && list[i - 2].RSI >= minRSI && list[i - 2].RSI < maxRSI && list[i - 1].RSI - list[i - 2].RSI >= minDiff)
                            {
                                tryCount++;
                                float positionEntryPrice = list[i].Open;
                                for (int j = i; j < count && j < i + rsiLength * 24; j++)
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
                        }
                        //float score = succeedCount - failedCount * stopX / closeX * lossX;
                        float score;
                        if (failedCount > 0) score = (float)succeedCount / failedCount;
                        else score = succeedCount;
                        float avgProfit = 10000 * totalProfit / totalDays * (float)Math.Sqrt((closeX - 0.001f) / (stopX + 0.002f));
                        string text = $"{minRSI} ~ {maxRSI}    minDiff = {minDiff}    {closeX:F4}  /  {stopX:F4}    count = {tryCount}    {succeedCount}    {failedCount}    {unknownCount}    score = {score:F2} \t total = {totalProfit:F4} \t avg = {avgProfit:F4} \t final = {finalPercent:F4}";
                        if (finalPercent > 1 && succeedCount >= 2)
                        {
                            Dictionary<string, object> dic = new Dictionary<string, object>
                                {
                                    { "minDiff", minDiff },
                                    { "closeX", closeX },
                                    { "stopX", stopX },
                                    { "tryCount", tryCount },
                                    { "succeedCount", succeedCount },
                                    { "failedCount", failedCount },
                                    { "unknownCount", unknownCount },
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
                                            (float)topList[i]["finalPercent"] == finalPercent && (float)topList[i]["minDiff"] < minDiff)
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
                        else
                        //if (finalPercent > .5f)
                        {
                            Console.WriteLine(text);
                        }
                        if (tryCount < 2) goto next_minDiff;
                        if (succeedCount < 2) break;
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

        static string Simulate(DateTime startTime, DateTime? endTime = null)
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

            List<BtcBin> list = BtcDao.SelectAll(binSize);
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
                    if (paramMap.BuyOrSell == 1 && paramMap.QtyX > 0 && list[i - 3].RSI > list[i - 2].RSI && list[i - 2].RSI >= (float)paramMap.MinRSI && list[i - 2].RSI < (float)paramMap.MaxRSI && list[i - 1].RSI - list[i - 2].RSI >= (float)paramMap.MinDiff && (list[i - 1].RSI - list[i - 2].RSI) < (float)paramMap.MaxDiff)
                    {
                        float positionEntryPrice = list[i].Open;
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
                        float positionEntryPrice = list[i].Open;
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
            string result = $"\r\nsucceed = {succeedCount} \t failed = {failedCount} \t total = {totalProfit:F8} \t avg = {avgProfit:F8} \t final = {finalPercent10:F2} / {finalPercent15:F2} / {finalPercent20:F2}\r\n";
            logger.WriteLine(result);
            return result;
        }

    }
}
