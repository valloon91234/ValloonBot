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
                DateTime endTime = new DateTime(2022, 2, 15, 0, 0, 0, DateTimeKind.Utc);
                JArray resultArray = BuildDataset(null, endTime);
                string resultText = resultArray.ToString(Formatting.Indented);
                File.WriteAllText("Dataset.txt", resultText, Encoding.UTF8);
                return;
            }

            //ThreadPool.QueueUserWorkItem(state => runHCS(new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc)));
        }

        static JArray BuildDataset(DateTime? startTime, DateTime? endTime, double zoom = 2.5d)
        {
            List<BtcBin> list = BtcDao.SelectAll("1h");
            list.RemoveAll(x => startTime != null && x.Timestamp < startTime.Value || endTime != null && x.Timestamp >= endTime.Value);
            int count = list.Count;
            Console.WriteLine($"Count = {count}");
            JArray resultArray = new JArray();
            for (int i = 24; i < count; i++)
            {
                List<double> valueList = new List<double>();
                for (int j = i - 24; j < i; j++)
                {
                    BtcBin b = list[j];
                    valueList.Add(Math.Max(zoom * (b.High - b.Open) / b.Open, 0));
                    valueList.Add(Math.Max(zoom * (b.Open - b.Low) / b.Open, 0));
                    //valueList.Add(Math.Min(Math.Max((double)(b.Close - b.Low) / (b.High - b.Low), 0), 0));
                    valueList.Add(zoom * (b.Close - b.Open) / b.Open / 2 + .5);
                    //valueList.Add(b.Volume);
                }
                JArray values = JArray.FromObject(valueList);
                JArray targets;
                {
                    BtcBin b = list[i];
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

    }
}
