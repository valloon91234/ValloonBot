﻿using NeuralNetwork.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NeuralNetwork.NetworkModels
{
    public class Network
    {
        #region -- Properties --
        public double LearnRate { get; set; }
        public double Momentum { get; set; }
        public List<Neuron> InputLayer { get; set; }
        public List<List<Neuron>> HiddenLayers { get; set; }
        public List<Neuron> OutputLayer { get; set; }
        #endregion

        #region -- Globals --
        private static readonly Random Random = new Random();
        #endregion

        #region -- Constructor --
        public Network()
        {
            LearnRate = 0;
            Momentum = 0;
            InputLayer = new List<Neuron>();
            HiddenLayers = new List<List<Neuron>>();
            OutputLayer = new List<Neuron>();
        }

        public Network(int inputSize, int[] hiddenSizes, int outputSize, double? learnRate = null, double? momentum = null)
        {
            LearnRate = learnRate ?? .4;
            Momentum = momentum ?? .9;
            InputLayer = new List<Neuron>();
            HiddenLayers = new List<List<Neuron>>();
            OutputLayer = new List<Neuron>();

            for (var i = 0; i < inputSize; i++)
                InputLayer.Add(new Neuron());

            var firstHiddenLayer = new List<Neuron>();
            for (var i = 0; i < hiddenSizes[0]; i++)
                firstHiddenLayer.Add(new Neuron(InputLayer));

            HiddenLayers.Add(firstHiddenLayer);

            for (var i = 1; i < hiddenSizes.Length; i++)
            {
                var hiddenLayer = new List<Neuron>();
                for (var j = 0; j < hiddenSizes[i]; j++)
                    hiddenLayer.Add(new Neuron(HiddenLayers[i - 1]));
                HiddenLayers.Add(hiddenLayer);
            }

            for (var i = 0; i < outputSize; i++)
                OutputLayer.Add(new Neuron(HiddenLayers.Last()));
        }
        #endregion

        #region -- Training --
        public double Train(List<DataSet> dataSets, int numEpochs)
        {
            var error = 1.0;
            var minError = error;
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] \t {0} / {numEpochs}");
            for (var i = 0; i < numEpochs; i++)
            {
                var errors = new List<double>();
                foreach (var dataSet in dataSets)
                {
                    ForwardPropagate(dataSet.Values);
                    BackPropagate(dataSet.Targets);
                    errors.Add(CalculateError(dataSet.Targets));
                }
                error = errors.Average();
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] \t {i + 1} / {numEpochs} \t {error}");
                if (error < minError * .98)
                {
                    minError = error;
                    AutoExportNetwork(error);
                }
            }
            if (error < minError) AutoExportNetwork(error);
            return error;
        }

        public double Train(List<DataSet> dataSets, double minimumError)
        {
            var error = 1.0;
            var numEpochs = 0;
            var minError = error;
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] \t {0} / {int.MaxValue} \t {error}");
            while (error > minimumError && numEpochs < int.MaxValue)
            {
                var errors = new List<double>();
                foreach (var dataSet in dataSets)
                {
                    ForwardPropagate(dataSet.Values);
                    BackPropagate(dataSet.Targets);
                    errors.Add(CalculateError(dataSet.Targets));
                }
                error = errors.Average();
                numEpochs++;
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] \t {numEpochs} / {int.MaxValue} \t {error}");
                if (error < minError * .98)
                {
                    minError = error;
                    AutoExportNetwork(error);
                }
            }
            if (error < minError) AutoExportNetwork(error);
            return error;
        }

        private void ForwardPropagate(params double[] inputs)
        {
            var i = 0;
            InputLayer.ForEach(a => a.Value = inputs[i++]);
            HiddenLayers.ForEach(a => a.ForEach(b => b.CalculateValue()));
            OutputLayer.ForEach(a => a.CalculateValue());
        }

        private void BackPropagate(params double[] targets)
        {
            var i = 0;
            OutputLayer.ForEach(a => a.CalculateGradient(targets[i++]));
            HiddenLayers.Reverse();
            HiddenLayers.ForEach(a => a.ForEach(b => b.CalculateGradient()));
            HiddenLayers.ForEach(a => a.ForEach(b => b.UpdateWeights(LearnRate, Momentum)));
            HiddenLayers.Reverse();
            OutputLayer.ForEach(a => a.UpdateWeights(LearnRate, Momentum));
        }

        public double[] Compute(params double[] inputs)
        {
            ForwardPropagate(inputs);
            return OutputLayer.Select(a => a.Value).ToArray();
        }

        private double CalculateError(params double[] targets)
        {
            var i = 0;
            return OutputLayer.Sum(a => Math.Abs(a.CalculateError(targets[i++])));
        }
        #endregion

        #region -- Helpers --
        public static double GetRandom()
        {
            return 2 * Random.NextDouble() - 1;
        }
        #endregion

        public void AutoExportNetwork(double error)
        {
            string networkFilename = $"Network - {HiddenLayers.Count} - {error:F16}.txt";
            Console.Write($"> Exporting to \"{networkFilename}\" ... ");
            Console.Title = networkFilename;
            using (var file = File.CreateText(networkFilename))
            {
                var serializer = new JsonSerializer { Formatting = Formatting.Indented };
                serializer.Serialize(file, ExportHelper.GetHelperNetwork(this));
            }
            Console.WriteLine($"OK");
            Console.Title = networkFilename;
        }
    }

    #region -- Enum --
    public enum TrainingType
    {
        Epoch,
        MinimumError
    }
    #endregion
}