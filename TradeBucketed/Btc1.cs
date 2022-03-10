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
    static class Btc1
    {
        const string binSize = "1m";

        public static void Run()
        {
            //{
            //    DateTime startTime = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            //    DateTime endTime = DateTime.UtcNow;
            //    LoadCSV(binSize, startTime, endTime);
            //    return;
            //}

            //{
            //    ExportCSV();
            //    return;
            //}

            {
                DateTime startTime = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                DateTime endTime = new DateTime(2022, 4, 1, 0, 0, 0, DateTimeKind.Utc);
                Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  buy = 1");

                for (int i = 3; i < 30; i += 1)
                {
                    string result = Benchmark(2, i, startTime, endTime);
                    logger.WriteLine($"{startTime:yyyy-MM-dd}  ~  {endTime:yyyy-MM-dd} \t\t {result}");
                }

                //Benchmark(1, 22.7f, 27.2f, startTime); return;
                //for (float i = 5f; i < 15f; i += 1f)
                //{
                //    float result = Benchmark(1, 0, i, startTime, endTime);
                //    logger.WriteLine($"{startTime:yyyy-MM-dd}    {i}    result = {result}");
                //}
            }

            //{
            //    Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  buy = 1");
            //    DateTime startTime = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            //    while (startTime < DateTime.UtcNow)
            //    {
            //        DateTime endTime = startTime.AddMonths(1);
            //        string result = Simulate(startTime, endTime);
            //        logger.WriteLine($"{startTime:yyyy-MM-dd}  ~  {endTime:yyyy-MM-dd}    {result}");
            //        startTime = endTime;
            //    }
            //}
            return;

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

        static string Benchmark(int buyOrSell, int candleCount, DateTime? startTime, DateTime? endTime = null)
        {
            List<BtcBin> list = BtcDao.SelectAll(binSize);
            list.RemoveAll(x => startTime != null && x.Timestamp < startTime.Value || endTime != null && x.Timestamp >= endTime.Value);
            int count = list.Count;
            int totalDays = (int)(list[count - 1].Timestamp - list[0].Timestamp).TotalDays;
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  buyOrSell = {buyOrSell}    candle = {candleCount}    ({list[0].Date} ~ {totalDays:N0} days)");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;
            List<Dictionary<string, object>> topList = new List<Dictionary<string, object>>();

            for (float minDiff = 0.01f; minDiff < .02f; minDiff += .001f)
            //float minDiff = 9.5f;
            {
                for (float maxDiff = minDiff + .005f; maxDiff < .03f; maxDiff += .001f)
                //float maxDiff = 2.46f;
                {
                    logger.WriteLine($"\n\n----    minDiff = {minDiff}    maxDiff = {maxDiff}    ----\n");
                    for (float stopX = 0.005f; stopX < 0.02f; stopX += 0.002f)
                    //float stopX = 0.0135f;
                    {
                        for (float closeX = 0.005f; closeX < 0.03f; closeX += .002f)
                        //float closeX = 0.0075f;
                        {
                            int tryCount = 0, succeedCount = 0, failedCount = 0;
                            double totalPercent = 1;
                            double diff = (minDiff + maxDiff) / 2;
                            double p = (closeX - 0.0002) / (stopX + 0.006);
                            //double p= (closeX - 0.0002) / (stopX + 0.0006);
                            int position = 0;
                            float entryPrice = 0, closePrice = 0, stopPrice = 0;
                            for (int i = candleCount; i < count - 1; i++)
                            {
                                float lastPrice = list[i].Open;
                                if (buyOrSell == 2)
                                {
                                    if (position == 0 && list[i - 1].Close > list[i - 1].Open && list[i - 2].Close > list[i - 2].Open)
                                    {
                                        //float lastHigh = list[i - 1].High;
                                        float height = 0;
                                        for (int j = i - candleCount; j < i; j++)
                                        {
                                            if (list[j].Open > lastPrice)
                                            {
                                                height = 0;
                                                break;
                                            }
                                            float h = lastPrice - list[j].Low;
                                            if (height < h) height = h;
                                        }
                                        if (height > lastPrice * minDiff && height < lastPrice * maxDiff)
                                        {
                                            tryCount++;
                                            position = -1;
                                            entryPrice = lastPrice;
                                            closePrice = lastPrice - lastPrice * closeX;
                                            stopPrice = lastPrice + lastPrice * stopX;
                                            i--;
                                        }
                                    }
                                    else if (position == -1)
                                    {
                                        if (list[i].High > stopPrice)
                                        {
                                            //if (list[i].Date == "2022-02-28")
                                            //    list[i].Date = "2022-02-28";
                                            failedCount++;
                                            totalPercent *= .98;
                                            position = 0;
                                        }
                                        else if (list[i].Low < closePrice)
                                        {
                                            //if (list[i].Date == "2022-02-28")
                                            //    list[i].Date = "2022-02-28";
                                            succeedCount++;
                                            totalPercent *= 1 + .02 * p;
                                            position = 0;
                                        }
                                    }
                                }
                            }
                            string text = $"candle = {candleCount}    {minDiff} / {maxDiff}    {closeX:F4}  /  {stopX:F4}    p = {p:F4}    count = {tryCount} \t {succeedCount} \t {failedCount} \t final = {totalPercent:F4}";
                            //totalPercent = Math.Pow(totalPercent, 30 / totalDays);
                            if (totalPercent > 1)
                            {
                                Dictionary<string, object> dic = new Dictionary<string, object>
                                {
                                    { "candleCount", candleCount },
                                    { "minDiff", minDiff },
                                    { "maxDiff", maxDiff },
                                    { "closeX", closeX },
                                    { "stopX", stopX },
                                    { "tryCount", tryCount },
                                    { "succeedCount", succeedCount },
                                    { "failedCount", failedCount },
                                    { "finalPercent", totalPercent },
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
                                        if ((double)topList[i]["finalPercent"] > totalPercent ||
                                                (double)topList[i]["finalPercent"] == totalPercent && (float)topList[i]["minDiff"] < minDiff ||
                                                (double)topList[i]["finalPercent"] == totalPercent && (float)topList[i]["minDiff"] == minDiff && (float)topList[i]["maxDiff"] > maxDiff)
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
                            if (totalPercent > .001f)
                            {
                                Console.WriteLine(text);
                            }
                            if (succeedCount < 2) break;
                        }
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
                                totalProfit -= (stopX + 0.002f);
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
                                totalProfit -= (stopX + 0.002f);
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
