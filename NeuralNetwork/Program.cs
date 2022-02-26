using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

        private static void Run5()
        {
            Console.WriteLine("\tCreating Network...");
            _numInputParameters = 72;

            Console.WriteLine("\tHow many hidden layers? (1 or more)");
            _numHiddenLayers = GetInput("\tHidden Layers: ", 1, int.MaxValue) ?? 0;
            //_numHiddenLayers = 12;
            _hiddenNeurons = new int[_numHiddenLayers];
            for (int i = 0; i < _numHiddenLayers; i++)
            {
                _hiddenNeurons[i] = _numInputParameters;
            }
            //_hiddenNeurons = new int[] { 192, 192, 192, 192 };
            //_numHiddenLayers = _hiddenNeurons.Length;

            _numOutputParameters = 3;
            _network = new Network(_numInputParameters, _hiddenNeurons, _numOutputParameters, .4, .9);
            Console.WriteLine("\t**Network Created!**");
            PrintNewLine();

            ImportDatasets();
            Train();
            PrintNewLine();
        }

        private static void Run6()
        {
            ImportNetwork();

            const double zoom = 15d;
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

        private static void Run7()
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
            Console.WriteLine("\t6. Run6");
            Console.WriteLine("\t7. Run7");
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
                case 6:
                    Run6();
                    NetworkMenu();
                    break;
                case 7:
                    Run7();
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
