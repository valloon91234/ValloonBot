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
    static class Btc_H
    {
        public static void Run()
        {
            {
                BuildDataset2();
                return;
            }

            {
                DateTime startTime = new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc);
                DateTime endTime = new DateTime(2022, 2, 15, 0, 0, 0, DateTimeKind.Utc);
                JArray resultArray = BuildDataset(startTime, endTime);
                string resultText = resultArray.ToString(Formatting.Indented);
                File.WriteAllText("Dataset.txt", resultText, Encoding.UTF8);
                return;
            }

            //ThreadPool.QueueUserWorkItem(state => runHCS(new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc)));
        }

        static JArray BuildDataset(DateTime? startTime, DateTime? endTime, double zoom = 40d)
        {
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
                list.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp >= endTime.Value);
                count = list.Count;
            }

            Console.WriteLine($"Count = {count}");
            JArray resultArray = new JArray();
            //double getValidValue(double input)
            //{
            //    if (input < 0) return 0;
            //    if (input > 1) return 1;
            //    return input;
            //}
            int tryCount = 0;
            int upperCount = 0, lowerCount = 0;
            for (int i = 3; i < count; i++)
            {
                //if (list[i - 2].High / list[i - 2].Low < 1.005 || list[i - 1].High / list[i - 1].Low < 1.005)
                //    continue;
                tryCount++;
                List<double> valueList = new List<double>();
                //for (int j = i - 24; j < i; j++)
                //{
                //    BtcBin b = list[j];
                //    valueList.Add(getValidValue(zoom * (b.High - b.Open) / b.Open));
                //    valueList.Add(getValidValue(zoom * (b.Open - b.Low) / b.Open));
                //    valueList.Add(getValidValue((b.Close - b.Low) / (b.High - b.Low)));
                //    //valueList.Add(b.Volume);
                //}
                //valueList.Add(list[i].Timestamp.Hour / 24);
                JArray values = JArray.FromObject(new double[] { list[i - 3].RSI / 100, list[i - 2].RSI / 100, list[i - 1].RSI / 100 });
                JArray targets;
                {
                    double targetValue = 0.5;
                    float height = list[i].Open * .005f;
                    float upper = list[i].Open + height;
                    float lower = list[i].Open - height;
                    for (int j = i; j < count && j < i + 14; j++)
                    {
                        if (list[j].High > upper && list[j].Low < lower)
                        {
                            if (list[j].Open < list[j].Close)
                            {
                                upperCount++;
                                targetValue = 1;
                            }
                            else
                            {
                                lowerCount++;
                                targetValue = 0;
                            }
                            break;
                        }
                        else if (list[j].High > upper)
                        {
                            upperCount++;
                            targetValue = 1;
                            break;
                        }
                        else if (list[j].Low < lower)
                        {
                            lowerCount++;
                            targetValue = 0;
                            break;
                        }
                    }
                    targets = JArray.FromObject(new double[] { targetValue });

                    //BtcBin b = list[i];
                    //int diff = b.High - b.Low;
                    //double high = getValidValue(zoom * (b.High - b.Open) / b.Open);
                    //double low = getValidValue(zoom * (b.Open - b.Low) / b.Open);
                    //double high = getValidValue((b.High - b.Open) / (list[i - 1].High - list[i - 1].Low));
                    //double low = getValidValue((b.Open - b.Low) / (list[i - 1].High - list[i - 1].Low));
                    //double close = Math.Min(Math.Max((double)(b.Close - b.Low) / (b.High - b.Low), 0), 0);
                    //double close = zoom * (b.Close - b.Open) / b.Open / 2 + .5;
                    //targets = JArray.FromObject(new double[] { high, low });
                }
                JObject obj = new JObject
                {
                    { "Values", values },
                    { "Targets", targets },
                };
                resultArray.Add(obj);
            }
            Console.WriteLine($"try = {tryCount}    upper = {upperCount}    lower = {lowerCount}");
            return resultArray;
        }


        static void BuildDataset2()
        {
            DateTime startTime = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime? endTime = new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Utc);

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
                list.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp >= endTime.Value);
                count = list.Count;
            }

            Console.WriteLine($"Count = {count}");
            int tryCount = 0;
            int upperCount = 0, lowerCount = 0, unknownCount = 0;
            using (var writer = new StreamWriter("data.csv", false, Encoding.UTF8))
            {
                writer.WriteLine($"rsi_1,rsi_2,rsi_3,rsi_10,rsi_20,rsi_30,value");
                for (int i = 3; i < count; i++)
                {
                    tryCount++;
                    int targetValue = 0;
                    float entry = list[i].Open;
                    float x = 0.01f;
                    for (int j = i; j < count && j < i + 14; j++)
                    {
                        //if (list[j].High > entry * (1 + x) && list[j].Low < entry * (1 - x))
                        //{
                        //    x *= 2;
                        //}

                        if (list[j].High > entry * (1 + x) && list[j].Low < entry * (1 - x))
                        {
                            unknownCount++;
                            targetValue = 0;
                            break;
                        }
                        else if (list[j].High > entry * (1 + x))
                        {
                            upperCount++;
                            targetValue = 1;
                            break;
                        }
                        else if (list[j].Low < entry * (1 - x))
                        {
                            lowerCount++;
                            targetValue = -1;
                            break;
                        }
                    }
                    //if (targetValue != 0)
                    {
                        writer.WriteLine($"{(int)Math.Round(list[i - 1].RSI)},{(int)Math.Round(list[i - 2].RSI)},{(int)Math.Round(list[i - 3].RSI)},{list[i - 1].RSI:F4},{list[i - 2].RSI:F4},{list[i - 3].RSI:F4},{targetValue}");
                        writer.Flush();
                    }
                }
            }
            Console.WriteLine($"try = {tryCount}    upper = {upperCount}    lower = {lowerCount}    unknown = {unknownCount}");
        }

    }
}
