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
                DateTime startTime = new DateTime(2021, 12, 15, 0, 0, 0, DateTimeKind.Utc);
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
            List<BtcBin> list = BtcDao.SelectAll("1h");
            list.RemoveAll(x => startTime != null && x.Timestamp < startTime.Value || endTime != null && x.Timestamp >= endTime.Value);
            int count = list.Count;
            Console.WriteLine($"Count = {count}");
            JArray resultArray = new JArray();
            double getValidValue(double input)
            {
                if (input < 0) return 0;
                if (input > 1) return 1;
                return input;
            }
            int tryCount = 0;
            for (int i = 24; i < count; i++)
            {
                if (list[i - 2].High / list[i - 2].Low < 1.005 || list[i - 1].High / list[i - 1].Low < 1.005)
                    continue;
                tryCount++;
                List<double> valueList = new List<double>();
                for (int j = i - 24; j < i; j++)
                {
                    BtcBin b = list[j];
                    valueList.Add(getValidValue(zoom * (b.High - b.Open) / b.Open));
                    valueList.Add(getValidValue(zoom * (b.Open - b.Low) / b.Open));
                    valueList.Add(getValidValue((b.Close - b.Low) / (b.High - b.Low)));
                    //valueList.Add(b.Volume);
                }
                valueList.Add(list[i].Timestamp.Hour / 24);
                JArray values = JArray.FromObject(valueList);
                JArray targets;
                {
                    BtcBin b = list[i];
                    //int diff = b.High - b.Low;
                    //double high = getValidValue(zoom * (b.High - b.Open) / b.Open);
                    //double low = getValidValue(zoom * (b.Open - b.Low) / b.Open);
                    double high = getValidValue((b.High - b.Open) / (list[i - 1].High - list[i - 1].Low));
                    double low = getValidValue((b.Open - b.Low) / (list[i - 1].High - list[i - 1].Low));
                    //double close = Math.Min(Math.Max((double)(b.Close - b.Low) / (b.High - b.Low), 0), 0);
                    //double close = zoom * (b.Close - b.Open) / b.Open / 2 + .5;
                    targets = JArray.FromObject(new double[] { high, low });
                }
                JObject obj = new JObject
                {
                    { "Values", values },
                    { "Targets", targets },
                };
                resultArray.Add(obj);
            }
            Console.WriteLine($"try = {tryCount}");
            return resultArray;
        }

    }
}
