using IO.Swagger.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Valloon.Trading;
using Valloon.Utils;

namespace Valloon.BitMEX.Backtest
{
    static class Loader
    {
        public static void LoadCSV(string symbol, string binSize, DateTime startTime, DateTime endTime)
        {
            string filename = $"data-{symbol}-{binSize}  {startTime:yyyy-MM-dd} ~ {endTime:yyyy-MM-dd}.csv";
            int x = CandleQuote.GetX(symbol);
            File.Delete(filename);
            using (var writer = new StreamWriter(filename, false, Encoding.UTF8))
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
                        List<TradeBin> list = apiHelper.GetBinList(binSize, false, symbol, 1000, null, null, startTime, nextTime);
                        int count = list.Count;
                        for (int i = 0; i < count - 1; i++)
                        {
                            TradeBin t = list[i];
                            try
                            {
                                writer.WriteLine($"{t.Timestamp.Value:yyyy-MM-dd HH:mm:ss},{t.Timestamp.Value:yyyy-MM-dd},{t.Timestamp.Value:HH:mm},{t.Open.Value * x},{t.High.Value * x},{t.Low.Value * x},{t.Close.Value * x},{t.Volume.Value}");
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
                        Thread.Sleep(1000);
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

        public static void Load(string symbol, string binSize, DateTime startTime, DateTime endTime)
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
                    List<TradeBin> list = apiHelper.GetBinList(binSize, false, symbol, 1000, null, null, startTime, nextTime);
                    int count = list.Count;
                    for (int i = 0; i < count - 1; i++)
                    {
                        TradeBin t = list[i];
                        try
                        {
                            Dao.Insert(symbol, binSize, new CandleQuote(t, symbol));
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

        public static List<CandleQuote> LoadBinListFrom1m(int size, List<CandleQuote> list)
        {
            if (size == 1) return list;
            int count = list.Count;
            var resultList = new List<CandleQuote>();
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
                resultList.Add(new CandleQuote
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

        public static List<CandleQuote> LoadBinListFrom5m(string binSize, List<CandleQuote> list)
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
            var resultList = new List<CandleQuote>();
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
                resultList.Add(new CandleQuote
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
