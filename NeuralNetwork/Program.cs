using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using IO.Swagger.Model;
using NeuralNetwork.Helpers;
using NeuralNetwork.NetworkModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Valloon.Trading;
using Valloon.Trading.Backtest;

namespace NeuralNetwork
{
    internal class Program
    {
        #region -- Variables --
        private static int _numInputParameters;
        private static int _numHiddenLayers;
        private static int[] _hiddenNeurons;
        private static int _numOutputParameters;
        private static Network _network;
        private static List<DataSet> _dataSets;
        #endregion

        //private static void Run5()
        //{
        //    Console.WriteLine("\tCreating Network...");
        //    _numInputParameters = 72;

        //    Console.WriteLine("\tHow many hidden layers? (1 or more)");
        //    _numHiddenLayers = GetInput("\tHidden Layers: ", 1, int.MaxValue) ?? 0;
        //    //_numHiddenLayers = 12;
        //    _hiddenNeurons = new int[_numHiddenLayers];
        //    for (int i = 0; i < _numHiddenLayers; i++)
        //    {
        //        _hiddenNeurons[i] = _numInputParameters;
        //    }
        //    //_hiddenNeurons = new int[] { 192, 192, 192, 192 };
        //    //_numHiddenLayers = _hiddenNeurons.Length;

        //    _numOutputParameters = 3;
        //    _network = new Network(_numInputParameters, _hiddenNeurons, _numOutputParameters, .4, .9);
        //    Console.WriteLine("\t**Network Created!**");
        //    PrintNewLine();

        //    ImportDatasets();
        //    Train();
        //    PrintNewLine();
        //}

        private static void Run5()
        {
            Console.WriteLine("\tCreating Network...");
            _numInputParameters = 3;

            _numHiddenLayers = GetInput("\tHidden Layer Count = ", 1, int.MaxValue) ?? 0;
            int hc = GetInput("\tHidden Layer's Neuron Count = ", 1, int.MaxValue) ?? 0;
            //_numHiddenLayers = 12;
            _hiddenNeurons = new int[_numHiddenLayers];
            for (int i = 0; i < _numHiddenLayers; i++)
            {
                _hiddenNeurons[i] = hc;
            }
            //_hiddenNeurons = new int[] { 192, 192, 192, 192 };
            //_numHiddenLayers = _hiddenNeurons.Length;

            _numOutputParameters = 1;
            _network = new Network(_numInputParameters, _hiddenNeurons, _numOutputParameters, .4, .9);
            Console.WriteLine("\t**Network Created!**");
            PrintNewLine();

            ImportDatasets();
            Train();
            PrintNewLine();
        }

        private static void Run9()
        {
            int inputCount = 8;
            DateTime startTime = new DateTime(2021, 10, 24, 0, 0, 0, DateTimeKind.Utc);
            DateTime? endTime = new DateTime(2022, 2, 15, 0, 0, 0, DateTimeKind.Utc);

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
            int succeedCount = 0, failedCount = 0, succeedValue = 0, failedValue = 0;
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

                double[] values = valueList.ToArray();
                double[] results = _network.Compute(values);
                int target = targetValue.Value;
                double result = results[0];
                Console.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm} \t target = {target} \t result = {result}");
                if (result > .5)
                {
                    if (target == 0) failedValue++;
                    else succeedValue++;
                }
            }
            Console.WriteLine($"succeed = {succeedCount} \t failed = {failedCount} \t result = {succeedValue} / {failedValue}");
        }

        private static void RunSolCalc()
        {
            ImportNetwork();

            const double zoom = 7d;
            DateTime startTime = new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc);
            //DateTime endTime = new DateTime(2022, 2, 1, 0, 0, 0, DateTimeKind.Utc);

            List<SolBin> list = SolDao.SelectAll("1h");
            list.RemoveAll(x => x.Timestamp < startTime /*|| x.Timestamp >= endTime*/);
            int count = list.Count;
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
                {
                    SolBin b = list[i];
                    //int diff = b.High - b.Low;
                    //b.TargetHigh = (int)Math.Floor(b.High - diff * marginX);
                    //b.TargetLow = (int)Math.Ceiling(b.Low + diff * marginX);
                    double[] values = valueList.ToArray();
                    double[] results = _network.Compute(values);
                    b.CalcHigh = (int)(b.Open + b.Open * results[0] / zoom);
                    b.CalcLow = (int)(b.Open - b.Open * results[1] / zoom);
                    b.CalcClose = (int)(b.Open + b.Open * (results[2] - .5) * 2 / zoom);
                    b.XHigh = results[0];
                    b.XLow = results[1];
                    b.XClose = results[2];
                    SolDao.Update(b, "1h");
                    Console.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open} \t {b.High} \t {b.Low} \t {b.Close}    \t    {b.CalcHigh} \t {b.CalcLow} \t {b.CalcClose}    \t    {results[0]:F4}    {results[1]:F4}    {results[2]:F4}");
                }
            }

            Console.WriteLine("\t**Complete!**");
            PrintNewLine();
        }

        private static void RunSolBenchmark()
        {
            DateTime startTime = new DateTime(2022, 2, 15, 0, 0, 0, DateTimeKind.Utc);

            List<SolBin> list = SolDao.SelectAll("1h");
            list.RemoveAll(x => x.Timestamp < startTime /*|| x.Timestamp >= endTime*/);
            int count = list.Count;
            List<Dictionary<string, float>> topList = new List<Dictionary<string, float>>();

            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  Run7");
            logger.WriteLine("\n" + logger.LogFilename + "\n");

            for (float upperLimitX = 0; upperLimitX < 0.3f; upperLimitX += 0.05f)
            {

                for (float lowerLimitX = 0; lowerLimitX < 0.3f; lowerLimitX += 0.05f)
                {
                    for (float upperStopX = 0; upperStopX < 1 - upperLimitX - lowerLimitX; upperStopX += 0.05f)
                    {
                        for (float lowerStopX = 0; lowerStopX < 1 - upperLimitX - lowerLimitX; lowerStopX += 0.05f)
                        {
                            int succeed = 0, halfSucceed = 0, halfFailed = 0, failed = 0, unknown = 0, nothing = 0;
                            for (int i = 0; i < count; i++)
                            {
                                SolBin b = list[i];
                                float height = b.CalcHigh - b.CalcLow;
                                if (height < 0)
                                {
                                    Console.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open} / {b.High} / {b.Low} / {b.Close}    height < 0");
                                    failed++;
                                    continue;
                                }
                                float upperLimit = b.CalcHigh - height * upperLimitX;
                                float upperStop = b.CalcHigh + height * upperStopX;
                                float lowerLimit = b.CalcLow + height * lowerLimitX;
                                float lowerStop = b.CalcLow - height * lowerStopX;
                                if (upperStop > b.High && upperLimit < b.High && lowerStop < b.Low && lowerLimit > b.Low)
                                {
                                    succeed++;
                                    //logger.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open} / {b.High} / {b.Low} / {b.Close}    succeed");
                                }
                                else if (upperStop > b.High && upperLimit < b.High && lowerLimit < b.Low && upperLimit - b.Close >= height * 0.4)
                                {
                                    succeed++;
                                    //logger.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open} / {b.High} / {b.Low} / {b.Close}    succeed2");
                                }
                                else if (upperStop > b.High && upperLimit < b.High && lowerLimit < b.Low && upperLimit - b.Close >= height * 0.1)
                                {
                                    halfSucceed++;
                                    //logger.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open} / {b.High} / {b.Low} / {b.Close}    halfSucceed");
                                }
                                else if (upperStop > b.High && upperLimit < b.High && lowerLimit < b.Low && upperLimit - b.Close < height * 0.1)
                                {
                                    halfFailed++;
                                    //logger.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open} / {b.High} / {b.Low} / {b.Close}        halfFailed");
                                }
                                else if (lowerStop < b.Low && lowerLimit > b.Low && upperLimit > b.High && b.Close - lowerLimit >= height * 0.4)
                                {
                                    succeed++;
                                    //logger.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open} / {b.High} / {b.Low} / {b.Close}    succeed2");
                                }
                                else if (lowerStop < b.Low && lowerLimit > b.Low && upperLimit > b.High && b.Close - lowerLimit >= height * 0.1)
                                {
                                    halfSucceed++;
                                    //logger.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open} / {b.High} / {b.Low} / {b.Close}    halfSucceed");
                                }
                                else if (lowerStop < b.Low && lowerLimit > b.Low && upperLimit > b.High && b.Close - lowerLimit < height * 0.1)
                                {
                                    halfFailed++;
                                    //logger.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open} / {b.High} / {b.Low} / {b.Close}        halfFailed");
                                }
                                else if (upperLimit > b.High && upperLimit < b.Low)
                                {
                                    nothing++;
                                    //logger.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open} / {b.High} / {b.Low} / {b.Close}        nothing");
                                }
                                else if (upperStop > b.High && upperLimit < b.High && lowerStop > b.Low && (upperLimit < b.Open || b.Open > b.Close))
                                {
                                    succeed++;
                                    //logger.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open} / {b.High} / {b.Low} / {b.Close}    succeed");
                                }
                                else if (lowerStop < b.Low && lowerLimit > b.Low && upperStop < b.High && (lowerLimit > b.Open || b.Open < b.Close))
                                {
                                    succeed++;
                                    //logger.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open} / {b.High} / {b.Low} / {b.Close}    succeed");
                                }
                                else if (upperStop < b.High && lowerStop > b.Low)
                                {
                                    failed++;
                                    //logger.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open} / {b.High} / {b.Low} / {b.Close}            failed");
                                }
                                else
                                {
                                    unknown++;
                                    //logger.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open} / {b.High} / {b.Low} / {b.Close}                unknown");
                                }
                            }
                            int score = succeed * 2 + halfSucceed - halfFailed - failed * 2 - unknown;
                            logger.WriteLine($"\r\n{upperLimitX} / {upperStopX}    {lowerLimitX} / {lowerStopX}    count = {count} \t succeed = {succeed} \t hSucceed = {halfSucceed} \t hFailed = {halfFailed} \t no = {nothing} \t fail = {failed} \t un = {unknown}    \t score = {score}");
                            Dictionary<string, float> dic = new Dictionary<string, float>
                            {
                                { "upperLimitX", upperLimitX },
                                { "upperStopX", upperStopX },
                                { "lowerLimitX", lowerLimitX },
                                { "lowerStopX", lowerStopX },
                                { "count", count },
                                { "succeed", succeed },
                                { "halfSucceed", halfSucceed },
                                { "halfFailed", halfFailed },
                                { "nothing", nothing },
                                { "failed", failed },
                                { "unknown", unknown },
                                { "score", score },
                            };
                            topList.Add(dic);
                        }
                    }
                }
            }
            List<Dictionary<string, float>> topListSort = topList.OrderBy(o => o["score"]).ThenByDescending(o => o["upperStopX"]).ThenByDescending(o => o["lowerStopX"]).ToList();
            logger.WriteLine($"\r\n\r\ntopList={topListSort.Count}\r\n" + JArray.FromObject(topListSort).ToString());
        }

        private static void RunBtcCalc()
        {
            ImportNetwork();

            double getValidValue(double input)
            {
                if (input < 0) return 0;
                if (input > 1) return 1;
                return input;
            }

            const double zoom = 40d;
            var list = BtcDao.SelectAll("1h");
            list.RemoveAll(x => x.Timestamp < new DateTime(2021, 12, 15, 0, 0, 0, DateTimeKind.Utc));
            int count = list.Count;
            for (int i = 24; i < count; i++)
            {
                List<double> valueList = new List<double>();
                for (int j = i - 24; j < i; j++)
                {
                    var b = list[j];
                    valueList.Add(getValidValue(zoom * (b.High - b.Open) / b.Open));
                    valueList.Add(getValidValue(zoom * (b.Open - b.Low) / b.Open));
                    valueList.Add(getValidValue((b.Close - b.Low) / (b.High - b.Low)));
                }
                valueList.Add(list[i].Timestamp.Hour / 24);
                {
                    var b = list[i];
                    double[] values = valueList.ToArray();
                    double[] results = _network.Compute(values);
                    b.CalcHigh = b.Open + results[0] * (list[i - 1].High - list[i - 1].Low);
                    b.CalcLow = b.Open - results[1] * (list[i - 1].High - list[i - 1].Low);
                    b.XHigh = results[0];
                    b.XLow = results[1];
                    b.XClose = 0;
                    BtcDao.Update(b, "1h");
                    Console.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open:F1} \t {b.High:F1} \t {b.Low:F1} \t {b.Close:F1}    \t    {b.CalcHigh:F2} \t {b.CalcLow:F2}    \t    {results[0]:F4}    {results[1]:F4}");
                    //Console.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open} \t {b.High} \t {b.Low} \t {b.Close}    \t    {b.CalcHigh} \t {b.CalcLow} \t {b.CalcClose}    \t    {results[0]:F4}    {results[1]:F4}    {results[2]:F4}");
                }
            }

            Console.WriteLine("\t**Complete!**");
            PrintNewLine();
        }

        private static void RunBtcBenchmark()
        {
            var list = BtcDao.SelectAll("1h");

            //DateTime startTime = new DateTime(2021, 7, 2, 0, 0, 0, DateTimeKind.Utc);
            //DateTime endTime = new DateTime(2022, 2, 15, 0, 0, 0, DateTimeKind.Utc);
            //list.RemoveAll(x => x.Timestamp < startTime || x.Timestamp >= endTime);

            //DateTime startTime = new DateTime(2021, 12, 15, 0, 0, 0, DateTimeKind.Utc);
            DateTime startTime = new DateTime(2022, 2, 15, 0, 0, 0, DateTimeKind.Utc);
            list.RemoveAll(x => x.Timestamp < startTime);

            int count = list.Count;
            var topUpperList = new List<Dictionary<string, double>>();
            var topLowerList = new List<Dictionary<string, double>>();
            var topList = new List<Dictionary<string, double>>();

            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  Run8");
            logger.WriteLine("\n" + logger.LogFilename + "\n");

            //for (double upperLimitX = -.05; upperLimitX < .05; upperLimitX += 0.005)
            double upperLimitX = .035;
            {
                //for (double lowerLimitX = -.05; lowerLimitX < .05; lowerLimitX += 0.005)
                double lowerLimitX = .05;
                {
                    //for (double upperStopX = .1; upperStopX < 1; upperStopX += 0.1)
                    double upperStopX = .9;
                    {
                        //for (double lowerStopX = .1; lowerStopX < 1; lowerStopX += 0.1)
                        double lowerStopX = .9;
                        {
                            int succeed = 0, halfSucceed = 0, halfFailed = 0, failed = 0, unknown = 0, nothing = 0;
                            double upperProfit = 0, lowerProfit = 0;
                            for (int i = 2; i < count; i++)
                            {
                                if (list[i - 2].High / list[i - 2].Low < 1.005 || list[i - 1].High / list[i - 1].Low < 1.005)
                                    continue;
                                var b = list[i];
                                double prevHeight = b.CalcHigh - b.CalcLow;
                                //double prevHeight = list[i - 1].High - list[i - 1].Low;
                                //if (prevHeight < 0)
                                //{
                                //    Console.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open} / {b.High} / {b.Low} / {b.Close}    height < 0");
                                //    failed++;
                                //    continue;
                                //}
                                double upperLimit = b.CalcHigh - prevHeight * upperLimitX;
                                double lowerLimit = b.CalcLow + prevHeight * lowerLimitX;
                                if (upperLimit / lowerLimit < 1.0025) continue;
                                double limitHeight = upperLimit - lowerLimit;
                                double upperStop = upperLimit + limitHeight * upperStopX;
                                double lowerStop = lowerLimit - limitHeight * lowerStopX;
                                if (upperStop > b.High && upperLimit < b.High && lowerStop < b.Low && lowerLimit > b.Low)
                                {
                                    succeed++;
                                    upperProfit += upperLimit / lowerLimit - 1.0002;
                                    lowerProfit += upperLimit / lowerLimit - 1.0002;
                                    //logger.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open} / {b.High} / {b.Low} / {b.Close}    succeed");
                                }
                                else if (upperStop > b.High && upperLimit < b.High && lowerLimit < b.Low && upperLimit / b.Close > 1.001)
                                {
                                    halfSucceed++;
                                    upperProfit += upperLimit / b.Close - 1.001;
                                    //logger.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open} / {b.High} / {b.Low} / {b.Close}    succeed2");
                                }
                                else if (upperStop > b.High && upperLimit < b.High && lowerLimit < b.Low)
                                {
                                    halfFailed++;
                                    upperProfit += upperLimit / b.Close - 1.001;
                                    //logger.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open} / {b.High} / {b.Low} / {b.Close}        halfFailed");
                                }
                                else if (lowerStop < b.Low && lowerLimit > b.Low && upperLimit > b.High && b.Close / lowerLimit > 1.001)
                                {
                                    halfSucceed++;
                                    lowerProfit += b.Close / lowerLimit - 1.001;
                                    //logger.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open} / {b.High} / {b.Low} / {b.Close}    succeed2");
                                }
                                else if (lowerStop < b.Low && lowerLimit > b.Low && upperLimit > b.High)
                                {
                                    halfFailed++;
                                    lowerProfit += b.Close / lowerLimit - 1.001;
                                    //logger.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open} / {b.High} / {b.Low} / {b.Close}    halfSucceed");
                                }
                                else if (upperLimit > b.High && lowerLimit < b.Low)
                                {
                                    nothing++;
                                    //logger.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open} / {b.High} / {b.Low} / {b.Close}        nothing");
                                }
                                else if (upperStop < b.High)
                                {
                                    failed++;
                                    upperProfit += upperLimit / upperStop - 1.001;
                                    //logger.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open} / {b.High} / {b.Low} / {b.Close}            failed");
                                }
                                else if (lowerStop > b.Low)
                                {
                                    failed++;
                                    lowerProfit += lowerStop / lowerLimit - 1.001;
                                    //logger.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open} / {b.High} / {b.Low} / {b.Close}            failed");
                                }
                                else
                                {
                                    unknown++;
                                    //logger.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open} / {b.High} / {b.Low} / {b.Close}                unknown");
                                }
                            }
                            double profit = upperProfit + lowerProfit;
                            int score = succeed + halfSucceed - halfFailed - failed - unknown;
                            string text = $"\r\n{upperLimitX:F2} / {lowerLimitX:F2}    {upperStopX:F2} / {lowerStopX:F2}    count = {count} \t succeed = {succeed} \t hSucceed = {halfSucceed} \t hFailed = {halfFailed} \t no = {nothing} \t fail = {failed} \t un = {unknown}    \t score = {score}    \t profit = {upperProfit:F8} / {lowerProfit:F8}";
                            logger.WriteLine(text);
                            var dic = new Dictionary<string, double>
                            {
                                { "upperLimitX", upperLimitX },
                                { "lowerLimitX", lowerLimitX },
                                { "upperStopX", upperStopX },
                                { "lowerStopX", lowerStopX },
                                { "count", count },
                                { "succeed", succeed },
                                { "halfSucceed", halfSucceed },
                                { "halfFailed", halfFailed },
                                { "nothing", nothing },
                                { "failed", failed },
                                { "unknown", unknown },
                                { "score", score },
                                { "upperProfit", upperProfit },
                                { "lowerProfit", lowerProfit },
                                { "profit", profit },
                            };
                            if (upperProfit > 0)
                            {
                                int topListCount = topUpperList.Count;
                                if (topListCount > 0)
                                {
                                    while (topListCount > 10)
                                    {
                                        topUpperList.RemoveAt(0);
                                        topListCount--;
                                    }
                                    for (int i = 0; i < topListCount; i++)
                                    {
                                        if ((double)topUpperList[i]["upperProfit"] > upperProfit ||
                                            (double)topUpperList[i]["lowerProfit"] == lowerProfit && (double)topUpperList[i]["upperStopX"] < upperStopX ||
                                            (double)topUpperList[i]["lowerProfit"] == lowerProfit && (double)topUpperList[i]["upperStopX"] == upperStopX && (double)topUpperList[i]["lowerStopX"] < lowerStopX)
                                        {
                                            topUpperList.Insert(i, dic);
                                            goto topListEnd;
                                        }
                                    }
                                    topUpperList.Add(dic);
                                topListEnd:;
                                }
                                else
                                {
                                    topUpperList.Add(dic);
                                }
                            }
                            if (lowerProfit > 0)
                            {
                                int topListCount = topLowerList.Count;
                                if (topListCount > 0)
                                {
                                    while (topListCount > 10)
                                    {
                                        topLowerList.RemoveAt(0);
                                        topListCount--;
                                    }
                                    for (int i = 0; i < topListCount; i++)
                                    {
                                        if ((double)topLowerList[i]["lowerProfit"] > lowerProfit ||
                                            (double)topLowerList[i]["lowerProfit"] == lowerProfit && (double)topLowerList[i]["lowerStopX"] < lowerStopX ||
                                            (double)topLowerList[i]["lowerProfit"] == lowerProfit && (double)topLowerList[i]["lowerStopX"] == lowerStopX && (double)topLowerList[i]["upperStopX"] < upperStopX)
                                        {
                                            topLowerList.Insert(i, dic);
                                            goto topListEnd;
                                        }
                                    }
                                    topLowerList.Add(dic);
                                topListEnd:;
                                }
                                else
                                {
                                    topLowerList.Add(dic);
                                }
                            }
                            if (profit > 0)
                            {
                                int topListCount = topLowerList.Count;
                                if (topListCount > 0)
                                {
                                    while (topListCount > 10)
                                    {
                                        topLowerList.RemoveAt(0);
                                        topListCount--;
                                    }
                                    for (int i = 0; i < topListCount; i++)
                                    {
                                        if ((double)topLowerList[i]["profit"] > profit)
                                        {
                                            topLowerList.Insert(i, dic);
                                            goto topListEnd;
                                        }
                                    }
                                    topLowerList.Add(dic);
                                topListEnd:;
                                }
                                else
                                {
                                    topLowerList.Add(dic);
                                }
                            }
                        }
                    }
                }
            }
            logger.WriteLine($"\r\n\r\ntopUpperList={topUpperList.Count}\r\n" + JArray.FromObject(topUpperList).ToString());
            logger.WriteLine($"\r\n\r\ntopLowerList={topLowerList.Count}\r\n" + JArray.FromObject(topLowerList).ToString());
        }

        private static void RunBtcBenchmark1()
        {
            DateTime startTime = new DateTime(2022, 2, 15, 0, 0, 0, DateTimeKind.Utc);

            var list = BtcDao.SelectAll("1h");
            list.RemoveAll(x => x.Timestamp < startTime /*|| x.Timestamp >= endTime*/);
            int count = list.Count;
            var topUpperList = new List<Dictionary<string, double>>();
            var topLowerList = new List<Dictionary<string, double>>();
            var topList = new List<Dictionary<string, double>>();

            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  Run8");
            logger.WriteLine("\n" + logger.LogFilename + "\n");

            for (double upperLimitX = -.5; upperLimitX < .5; upperLimitX += 0.05)
            //double upperLimitX = 0.5;
            {
                for (double lowerLimitX = -.5; lowerLimitX < .5; lowerLimitX += 0.05)
                //double lowerLimitX = 0.5;
                {
                    for (double upperStopX = .1; upperStopX < 10; upperStopX += 0.5)
                    //double upperStopX = 100;
                    {
                        for (double lowerStopX = .1; lowerStopX < 10; lowerStopX += 0.5)
                        //double lowerStopX = 100;
                        {
                            int succeed = 0, halfSucceed = 0, halfFailed = 0, failed = 0, unknown = 0, nothing = 0;
                            double upperProfit = 0, lowerProfit = 0;
                            for (int i = 2; i < count; i++)
                            {
                                if (list[i - 2].High / list[i - 2].Low < 1.005 || list[i - 1].High / list[i - 1].Low < 1.005)
                                    continue;
                                var b = list[i];
                                double prevHeight = b.CalcHigh - b.CalcLow;
                                //double prevHeight = list[i - 1].High - list[i - 1].Low;
                                //if (prevHeight < 0)
                                //{
                                //    Console.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open} / {b.High} / {b.Low} / {b.Close}    height < 0");
                                //    failed++;
                                //    continue;
                                //}
                                double upperLimit = b.CalcHigh - prevHeight * upperLimitX;
                                double lowerLimit = b.CalcLow + prevHeight * lowerLimitX;
                                if (upperLimit / lowerLimit < 1.0025) continue;
                                double limitHeight = upperLimit - lowerLimit;
                                double upperStop = upperLimit + limitHeight * upperStopX;
                                double lowerStop = lowerLimit - limitHeight * lowerStopX;
                                if (upperStop > b.High && upperLimit < b.High && lowerStop < b.Low && lowerLimit > b.Low)
                                {
                                    succeed++;
                                    upperProfit += upperLimit / lowerLimit - 1.0002;
                                    //lowerProfit += upperLimit / lowerLimit - 1.0002;
                                    //logger.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open} / {b.High} / {b.Low} / {b.Close}    succeed");
                                }
                                else if (upperStop > b.High && upperLimit < b.High && lowerLimit < b.Low && upperLimit / b.Close > 1.001)
                                {
                                    halfSucceed++;
                                    upperProfit += upperLimit / b.Close - 1.0002;
                                    //logger.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open} / {b.High} / {b.Low} / {b.Close}    succeed2");
                                }
                                else if (upperStop > b.High && upperLimit < b.High && lowerLimit < b.Low)
                                {
                                    halfFailed++;
                                    upperProfit += upperLimit / b.Close - 1.0002;
                                    //logger.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open} / {b.High} / {b.Low} / {b.Close}        halfFailed");
                                }
                                //else if (lowerStop < b.Low && lowerLimit > b.Low && upperLimit > b.High && b.Close / lowerLimit > 1.001)
                                //{
                                //    halfSucceed++;
                                //    lowerProfit += b.Close / lowerLimit - 1.0002;
                                //    //logger.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open} / {b.High} / {b.Low} / {b.Close}    succeed2");
                                //}
                                //else if (lowerStop < b.Low && lowerLimit > b.Low && upperLimit > b.High)
                                //{
                                //    halfFailed++;
                                //    lowerProfit += b.Close / lowerLimit - 1.0002;
                                //    //logger.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open} / {b.High} / {b.Low} / {b.Close}    halfSucceed");
                                //}
                                else if (upperLimit > b.High && lowerLimit < b.Low)
                                {
                                    nothing++;
                                    //logger.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open} / {b.High} / {b.Low} / {b.Close}        nothing");
                                }
                                else if (upperStop < b.High)
                                {
                                    failed++;
                                    upperProfit += upperLimit / upperStop - 1.001;
                                    //logger.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open} / {b.High} / {b.Low} / {b.Close}            failed");
                                }
                                //else if (lowerStop > b.Low)
                                //{
                                //    failed++;
                                //    lowerProfit += lowerStop / lowerLimit - 1.001;
                                //    //logger.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open} / {b.High} / {b.Low} / {b.Close}            failed");
                                //}
                                else
                                {
                                    unknown++;
                                    //logger.WriteLine($"{b.Timestamp:yyyy-MM-dd HH:mm}    {b.Open} / {b.High} / {b.Low} / {b.Close}                unknown");
                                }
                            }
                            double profit = upperProfit + lowerProfit;
                            int score = succeed + halfSucceed - halfFailed - failed - unknown;
                            string text = $"\r\n{upperLimitX:F2} / {lowerLimitX:F2}    {upperStopX:F2} / {lowerStopX:F2}    count = {count} \t succeed = {succeed} \t hSucceed = {halfSucceed} \t hFailed = {halfFailed} \t no = {nothing} \t fail = {failed} \t un = {unknown}    \t score = {score}    \t profit = {upperProfit:F8} / {lowerProfit:F8}";
                            logger.WriteLine(text);
                            var dic = new Dictionary<string, double>
                            {
                                { "upperLimitX", upperLimitX },
                                { "lowerLimitX", lowerLimitX },
                                { "upperStopX", upperStopX },
                                { "lowerStopX", lowerStopX },
                                { "count", count },
                                { "succeed", succeed },
                                { "halfSucceed", halfSucceed },
                                { "halfFailed", halfFailed },
                                { "nothing", nothing },
                                { "failed", failed },
                                { "unknown", unknown },
                                { "score", score },
                                { "upperProfit", upperProfit },
                                { "lowerProfit", lowerProfit },
                                { "profit", profit },
                            };
                            if (upperProfit > 0)
                            {
                                int topListCount = topUpperList.Count;
                                if (topListCount > 0)
                                {
                                    while (topListCount > 10)
                                    {
                                        topUpperList.RemoveAt(0);
                                        topListCount--;
                                    }
                                    for (int i = 0; i < topListCount; i++)
                                    {
                                        if ((double)topUpperList[i]["upperProfit"] > upperProfit)
                                        {
                                            topUpperList.Insert(i, dic);
                                            goto topListEnd;
                                        }
                                    }
                                    topUpperList.Add(dic);
                                topListEnd:;
                                }
                                else
                                {
                                    topUpperList.Add(dic);
                                }
                            }
                            if (lowerProfit > 0)
                            {
                                int topListCount = topLowerList.Count;
                                if (topListCount > 0)
                                {
                                    while (topListCount > 10)
                                    {
                                        topLowerList.RemoveAt(0);
                                        topListCount--;
                                    }
                                    for (int i = 0; i < topListCount; i++)
                                    {
                                        if ((double)topLowerList[i]["lowerProfit"] > lowerProfit)
                                        {
                                            topLowerList.Insert(i, dic);
                                            goto topListEnd;
                                        }
                                    }
                                    topLowerList.Add(dic);
                                topListEnd:;
                                }
                                else
                                {
                                    topLowerList.Add(dic);
                                }
                            }
                            if (profit > 0)
                            {
                                int topListCount = topLowerList.Count;
                                if (topListCount > 0)
                                {
                                    while (topListCount > 10)
                                    {
                                        topLowerList.RemoveAt(0);
                                        topListCount--;
                                    }
                                    for (int i = 0; i < topListCount; i++)
                                    {
                                        if ((double)topLowerList[i]["profit"] > profit)
                                        {
                                            topLowerList.Insert(i, dic);
                                            goto topListEnd;
                                        }
                                    }
                                    topLowerList.Add(dic);
                                topListEnd:;
                                }
                                else
                                {
                                    topLowerList.Add(dic);
                                }
                            }
                        }
                    }
                nextUpperLimitX:;
                }
            }
            logger.WriteLine($"\r\n\r\ntopUpperList={topUpperList.Count}\r\n" + JArray.FromObject(topUpperList).ToString());
            logger.WriteLine($"\r\n\r\ntopLowerList={topLowerList.Count}\r\n" + JArray.FromObject(topLowerList).ToString());
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        #region -- Main --
        [STAThread]
        private static void Main()
        {
            MoveWindow(GetConsoleWindow(), 24, 0, 600, 200, true);

            //Greet();
            InitialMenu();
        }
        #endregion

        #region -- Network Setup --
        private static void Greet()
        {
            Console.WriteLine("C# Neural Network Manager");
            Console.WriteLine("Created by Trent Sartain (trentsartain on GitHub)");
            PrintUnderline(50);
            PrintNewLine();
        }

        private static void InitialMenu()
        {
            Console.WriteLine("Main Menu");
            PrintUnderline(50);
            Console.WriteLine("\t1. New Network");
            Console.WriteLine("\t2. Import Network");
            Console.WriteLine("\t3. Exit");
            Console.WriteLine("\t5. Run5");
            Console.WriteLine("\t6. Run Calc");
            Console.WriteLine("\t7. Run Benchmark");
            Console.WriteLine("\t9. Run9");
            PrintNewLine();

            switch (GetInput("\tYour Choice: ", 1, 9))
            {
                case 1:
                    if (SetupNetwork()) DatasetMenu();
                    else InitialMenu();
                    break;
                case 2:
                    ImportNetwork();
                    DatasetMenu();
                    break;
                case 3:
                    Exit();
                    break;
                case 5:
                    Run5();
                    NetworkMenu();
                    break;
                //case 6:
                //    RunSolCalc();
                //    NetworkMenu();
                //    break;
                //case 7:
                //    RunSolBenchmark();
                //    NetworkMenu();
                //    break;
                case 6:
                    RunBtcCalc();
                    NetworkMenu();
                    break;
                case 7:
                    RunBtcBenchmark();
                    NetworkMenu();
                    break;
                case 9:
                    Run9();
                    NetworkMenu();
                    break;
            }
        }

        private static void DatasetMenu()
        {
            Console.WriteLine("Dataset Menu");
            PrintUnderline(50);
            Console.WriteLine("\t1. Type Dataset");
            Console.WriteLine("\t2. Import Dataset");
            Console.WriteLine("\t3. Test Network");
            Console.WriteLine("\t4. Export Network");
            Console.WriteLine("\t5. Main Menu");
            Console.WriteLine("\t6. Exit");
            PrintNewLine();

            switch (GetInput("\tYour Choice: ", 1, 6))
            {
                case 1:
                    if (GetTrainingData()) NetworkMenu();
                    else DatasetMenu();
                    break;
                case 2:
                    ImportDatasets();
                    NetworkMenu();
                    break;
                case 3:
                    TestNetwork();
                    DatasetMenu();
                    break;
                case 4:
                    ExportNetwork();
                    DatasetMenu();
                    break;
                case 5:
                    InitialMenu();
                    break;
                case 6:
                    Exit();
                    break;
            }
        }

        private static void NetworkMenu()
        {
            Console.WriteLine("Network Menu");
            PrintUnderline(50);
            Console.WriteLine("\t1. Train Network");
            Console.WriteLine("\t2. Test Network");
            Console.WriteLine("\t3. Export Network");
            Console.WriteLine("\t4. Export Dataset");
            Console.WriteLine("\t5. Dataset Menu");
            Console.WriteLine("\t6. Main Menu");
            Console.WriteLine("\t7. Exit");
            PrintNewLine();

            switch (GetInput("\tYour Choice: ", 1, 7))
            {
                case 1:
                    Train();
                    NetworkMenu();
                    break;
                case 2:
                    TestNetwork();
                    NetworkMenu();
                    break;
                case 3:
                    ExportNetwork();
                    NetworkMenu();
                    break;
                case 4:
                    ExportDatasets();
                    NetworkMenu();
                    break;
                case 5:
                    DatasetMenu();
                    break;
                case 6:
                    InitialMenu();
                    break;
                case 7:
                    Exit();
                    break;
            }
        }

        private static bool SetupNetwork()
        {
            PrintNewLine();
            Console.WriteLine("Network Setup");
            PrintUnderline(50);
            SetNumInputParameters();
            if (_numInputParameters == 0) return false;
            SetNumNeuronsInHiddenLayer();
            if (_numHiddenLayers == 0) return false;
            SetNumOutputParameters();
            if (_numInputParameters == 0) return false;

            Console.WriteLine("\tCreating Network...");
            _network = new Network(_numInputParameters, _hiddenNeurons, _numOutputParameters);
            Console.WriteLine("\t**Network Created!**");
            PrintNewLine();
            return true;
        }

        private static void SetNumInputParameters()
        {
            Console.WriteLine("\tHow many input parameters will there be? (2 or more)");
            _numInputParameters = GetInput("\tInput Parameters: ", 2, int.MaxValue) ?? 0;
            PrintNewLine(2);
        }

        private static void SetNumNeuronsInHiddenLayer()
        {
            Console.WriteLine("\tHow many hidden layers? (1 or more)");
            _numHiddenLayers = GetInput("\tHidden Layers: ", 1, int.MaxValue) ?? 0;

            Console.WriteLine("\tHow many neurons in the hidden layers? (2 or more)");
            _hiddenNeurons = GetArrayInput("\tNeurons in layer", 2, _numHiddenLayers);
            PrintNewLine(2);
        }

        private static void SetNumOutputParameters()
        {
            Console.WriteLine("\tHow many output parameters will there be? (1 or more)");
            _numOutputParameters = GetInput("\tOutput Parameters: ", 1, int.MaxValue) ?? 0;
            PrintNewLine(2);
        }

        private static bool GetTrainingData()
        {
            PrintUnderline(50);
            Console.WriteLine("\tManually Enter the Datasets. Type 'menu' at any time to go back.");
            PrintNewLine();

            var numDataSets = GetInput("\tHow many datasets are you going to enter? ", 1, int.MaxValue);

            var newDatasets = new List<DataSet>();
            for (var i = 0; i < numDataSets; i++)
            {
                var values = GetInputData($"\tData Set {i + 1}: ");
                if (values == null)
                {
                    PrintNewLine();
                    return false;
                }

                var expectedResult = GetExpectedResult($"\tExpected Result for Data Set {i + 1}: ");
                if (expectedResult == null)
                {
                    PrintNewLine();
                    return false;
                }

                newDatasets.Add(new DataSet(values, expectedResult));
            }

            _dataSets = newDatasets;
            PrintNewLine();
            return true;
        }

        private static double[] GetInputData(string message)
        {
            Console.Write(message);
            var line = GetLine();

            if (line.Equals("menu", StringComparison.InvariantCultureIgnoreCase)) return null;

            while (line == null || line.Split(' ').Length != _numInputParameters)
            {
                Console.WriteLine($"\t{_numInputParameters} inputs are required.");
                PrintNewLine();
                Console.WriteLine(message);
                line = GetLine();
            }

            var values = new double[_numInputParameters];
            var lineNums = line.Split(' ');
            for (var i = 0; i < lineNums.Length; i++)
            {
                double num;
                if (double.TryParse(lineNums[i], out num))
                {
                    values[i] = num;
                }
                else
                {
                    Console.WriteLine("\tYou entered an invalid number.  Try again");
                    PrintNewLine(2);
                    return GetInputData(message);
                }
            }

            return values;
        }

        private static double[] GetExpectedResult(string message)
        {
            Console.Write(message);
            var line = GetLine();

            if (line != null && line.Equals("menu", StringComparison.InvariantCultureIgnoreCase)) return null;

            while (line == null || line.Split(' ').Length != _numOutputParameters)
            {
                Console.WriteLine($"\t{_numOutputParameters} outputs are required.");
                PrintNewLine();
                Console.WriteLine(message);
                line = GetLine();
            }

            var values = new double[_numOutputParameters];
            var lineNums = line.Split(' ');
            for (var i = 0; i < lineNums.Length; i++)
            {
                int num;
                if (int.TryParse(lineNums[i], out num) && (num == 0 || num == 1))
                {
                    values[i] = num;
                }
                else
                {
                    Console.WriteLine("\tYou must enter 1s and 0s!");
                    PrintNewLine(2);
                    return GetExpectedResult(message);
                }
            }

            return values;
        }
        #endregion

        #region -- Network Training --
        private static void TestNetwork()
        {
            Console.WriteLine("\tTesting Network");
            Console.WriteLine("\tType 'menu' at any time to return to the previous menu.");
            PrintNewLine();

            while (true)
            {
                PrintUnderline(50);
                var values = GetInputData($"\tType {_numInputParameters} inputs (or 'menu' to exit): ");
                if (values == null)
                {
                    PrintNewLine();
                    return;
                }

                var results = _network.Compute(values);
                PrintNewLine();

                foreach (var result in results)
                {
                    Console.WriteLine($"\tOutput: {result}");
                }

                PrintNewLine();
            }
        }

        private static void Train()
        {
            Console.WriteLine("Network Training");
            PrintUnderline(50);
            Console.WriteLine("\t1. Train to minimum error");
            Console.WriteLine("\t2. Train to max epoch");
            Console.WriteLine("\t3. Network Menu");
            PrintNewLine();
            switch (GetInput("\tYour Choice: ", 1, 3))
            {
                case 1:
                    var minError = GetDouble("\tMinimum Error: ", 0.000000001, 1.0);
                    PrintNewLine();
                    Console.WriteLine("\tTraining...");
                    _network.Train(_dataSets, minError);
                    Console.WriteLine("\t**Training Complete**");
                    PrintNewLine();
                    NetworkMenu();
                    break;
                case 2:
                    var maxEpoch = GetInput("\tMax Epoch: ", 1, int.MaxValue);
                    if (!maxEpoch.HasValue)
                    {
                        PrintNewLine();
                        NetworkMenu();
                        return;
                    }
                    PrintNewLine();
                    Console.WriteLine("\tTraining...");
                    _network.Train(_dataSets, maxEpoch.Value);
                    Console.WriteLine("\t**Training Complete**");
                    PrintNewLine();
                    break;
                case 3:
                    NetworkMenu();
                    break;
            }
            PrintNewLine();
        }
        #endregion

        #region -- I/O Help --
        private static void ImportNetwork()
        {
            PrintNewLine();
            _network = ImportHelper.ImportNetwork();
            if (_network == null)
            {
                WriteError("\t****Something went wrong while importing your network.****");
                return;
            }

            _numInputParameters = _network.InputLayer.Count;
            _hiddenNeurons = new int[_network.HiddenLayers.Count];
            _numOutputParameters = _network.OutputLayer.Count;

            Console.WriteLine("\t**Network successfully imported.**");
            PrintNewLine();
        }

        private static void ExportNetwork()
        {
            PrintNewLine();
            Console.WriteLine("\tExporting Network...");
            ExportHelper.ExportNetwork(_network);
            Console.WriteLine("\t**Exporting Complete!**");
            PrintNewLine();
        }

        private static void ImportDatasets()
        {
            PrintNewLine();
            _dataSets = ImportHelper.ImportDatasets();
            if (_dataSets == null)
            {
                WriteError("\t--Something went wrong while importing your datasets.--");
                return;
            }

            if (_dataSets.Any(x => x.Values.Length != _numInputParameters || _dataSets.Any(y => y.Targets.Length != _numOutputParameters)))
            {
                WriteError($"\t--The dataset does not fit the network.  Network requires datasets that have {_numInputParameters} inputs and {_numOutputParameters} outputs.--");
                return;
            }

            Console.WriteLine("\t**Datasets successfully imported.**");
            PrintNewLine();
        }

        private static void ExportDatasets()
        {
            PrintNewLine();
            Console.WriteLine("\tExporting Datasets...");
            ExportHelper.ExportDatasets(_dataSets);
            Console.WriteLine("\t**Exporting Complete!**");
            PrintNewLine();
        }
        #endregion

        #region -- Console Helpers --

        private static string GetLine()
        {
            var line = Console.ReadLine();
            return line?.Trim() ?? string.Empty;
        }

        private static int? GetInput(string message, int min, int max)
        {
            Console.Write(message);
            var num = GetNumber();
            if (!num.HasValue) return null;

            while (!num.HasValue || num < min || num > max)
            {
                Console.Write(message);
                num = GetNumber();
            }

            return num.Value;
        }

        private static double GetDouble(string message, double min, double max)
        {
            Console.Write(message);
            var num = GetDouble();

            while (num < min || num > max)
            {
                Console.Write(message);
                num = GetDouble();

            }

            return num;
        }

        private static int[] GetArrayInput(string message, int min, int numToGet)
        {
            var nums = new int[numToGet];

            for (var i = 0; i < numToGet; i++)
            {
                Console.Write(message + " " + (i + 1) + ": ");
                var num = GetNumber();

                while (!num.HasValue || num < min)
                {
                    Console.Write(message + " " + (i + 1) + ": ");
                    num = GetNumber();
                }

                nums[i] = num.Value;
            }

            return nums;
        }

        private static int? GetNumber()
        {
            int num;
            var line = GetLine();

            if (line.Equals("menu", StringComparison.InvariantCultureIgnoreCase)) return null;

            return int.TryParse(line, out num) ? num : 0;
        }

        private static double GetDouble()
        {
            double num;
            var line = GetLine();
            return line != null && double.TryParse(line, out num) ? num : 0;
        }


        private static void PrintNewLine(int numNewLines = 1)
        {
            for (var i = 0; i < numNewLines; i++)
                Console.WriteLine();
        }

        private static void PrintUnderline(int numUnderlines)
        {
            for (var i = 0; i < numUnderlines; i++)
                Console.Write('-');
            PrintNewLine(2);
        }

        private static void WriteError(string error)
        {
            Console.WriteLine(error);
            Exit();
        }

        private static void Exit()
        {
            Console.WriteLine("Exiting...");
            Console.ReadLine();
            Environment.Exit(0);
        }
        #endregion
    }
}
