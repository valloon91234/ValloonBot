using IO.Swagger.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Valloon.Utils;

namespace Valloon.Trading.Backtest
{
    static class Sol_H
    {
        public static void Run()
        {
            //DateTime startTime = new DateTime(2021, 10, 21, 0, 0, 0, DateTimeKind.Utc);
            //{
            //    DateTime startTime = new DateTime(2021, 10, 21, 0, 0, 0, DateTimeKind.Utc);
            //    DateTime endTime = DateTime.UtcNow;
            //    Load("1h", startTime, endTime);
            //    return;
            //}

            {
                DateTime startTime = new DateTime(2021, 10, 24, 0, 0, 0, DateTimeKind.Utc);
                DateTime endTime = new DateTime(2022, 2, 15, 0, 0, 0, DateTimeKind.Utc);
                JArray resultArray = BuildDataset2(startTime, endTime);
                string resultText = resultArray.ToString(Formatting.Indented);
                File.WriteAllText("Dataset.txt", resultText, Encoding.UTF8);
                Console.WriteLine($"{resultArray.Count}");
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

        private static double ConvertPrice(int price)
        {
            int min = 8107;
            int max = 26061;
            return ((double)price - (double)min) / ((double)max - (double)min);
        }

        static JArray BuildDataset(DateTime startTime, DateTime endTime, double zoom = 7d)
        {
            List<SolBin> list = SolDao.SelectAll("1h");
            list.RemoveAll(x => x.Timestamp < startTime || x.Timestamp >= endTime);
            int count = list.Count;
            Console.WriteLine($"Count = {count}");
            JArray resultArray = new JArray();
            for (int i = 24; i < count; i++)
            {
                List<double> valueList = new List<double>();
                for (int j = i - 24; j < i; j++)
                {
                    SolBin b = list[j];
                    valueList.Add(Math.Max(zoom * (b.High - b.Open) / b.Open, 0));
                    valueList.Add(Math.Max(zoom * (b.Open - b.Low) / b.Open, 0));
                    //valueList.Add(Math.Min(Math.Max((double)(b.Close - b.Low) / (b.High - b.Low), 0), 0));
                    valueList.Add(zoom * (b.Close - b.Open) / b.Open / 2 + .5);
                    //valueList.Add(b.Volume);
                }
                JArray values = JArray.FromObject(valueList);
                JArray targets;
                {
                    SolBin b = list[i];
                    //int diff = b.High - b.Low;
                    //int targetHigh = (int)Math.Floor(b.High - diff * marginX);
                    //int targetLow = (int)Math.Ceiling(b.Low + diff * marginX);
                    double high = Math.Max(zoom * (b.High - b.Open) / b.Open, 0);
                    double low = Math.Max(zoom * (b.Open - b.Low) / b.Open, 0);
                    //double close = Math.Min(Math.Max((double)(b.Close - b.Low) / (b.High - b.Low), 0), 0);
                    double close = zoom * (b.Close - b.Open) / b.Open / 2 + .5;
                    targets = JArray.FromObject(new double[] { high, low, close });
                }
                JObject obj = new JObject
                {
                    { "Values", values },
                    { "Targets", targets },
                };
                resultArray.Add(obj);
            }
            return resultArray;
        }

        static JArray BuildDataset2(DateTime startTime, DateTime? endTime, int inputCount = 8)
        {
            const float closeX = 0.16f, stopX = 0.14f;
            List<SolBin> list = SolDao.SelectAll("5m");
            int count = list.Count;
            Console.WriteLine($"Count = {count}");
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
            JArray resultArray = new JArray();
            int succeedCount = 0, failedCount = 0;
            for (int i = inputCount; i < count; i++)
            {
                List<double> valueList = new List<double>();
                for (int j = i - inputCount; j < i; j++)
                {
                    valueList.Add(list[j].RSI / 100);
                }
                int entryPrice = list[i].Open;
                int closePrice, stopPrice;
                int? targetValue = null;
                if (list[i - 2].RSI >= 70 && list[i - 2].RSI >= list[i - 1].RSI - 0.5f)
                {
                    closePrice = (int)Math.Ceiling(entryPrice * (1 - closeX));
                    stopPrice = (int)Math.Ceiling(entryPrice * (1 + stopX));
                    for (int j = i; j < count; j++)
                    {
                        if (list[j].High > stopPrice)
                        {
                            targetValue = 0;
                            failedCount++;
                            break;
                        }
                        if (list[j].Low < closePrice)
                        {
                            targetValue = 1;
                            succeedCount++;
                            break;
                        }
                    }
                }
                //else
                //if (list[i - 2].RSI <= 30 && list[i - 2].RSI < list[i - 1].RSI + 0.5f)
                //{
                //    closePrice = (int)Math.Ceiling(entryPrice * (1 + closeX));
                //    stopPrice = (int)Math.Ceiling(entryPrice * (1 - stopX));
                //    for (int j = i; j < count; j++)
                //    {
                //        if (list[j].Low < stopPrice)
                //        {
                //            targetValue = 0;
                //            failedCount++;
                //            break;
                //        }
                //        if (list[j].High > closePrice)
                //        {
                //            targetValue = 1;
                //            succeedCount++;
                //            break;
                //        }
                //    }
                //}
                else
                {
                    continue;
                }
                if (targetValue == null) break;
                JArray values = JArray.FromObject(valueList);
                //double target = targetValue == 0 ? 0.3 : 0.7;
                JArray targets = JArray.FromObject(new double[] { targetValue.Value });
                JObject obj = new JObject
                {
                    { "Values", values },
                    { "Targets", targets },
                };
                resultArray.Add(obj);
            }
            Console.WriteLine($"succeed = {succeedCount} \t failed = {failedCount}");
            return resultArray;
        }

    }
}
