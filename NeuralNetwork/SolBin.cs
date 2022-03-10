using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Valloon.Trading.Backtest
{
    public class SolBin
    {
        public DateTime Timestamp { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public int Open { get; set; }
        public int High { get; set; }
        public int Low { get; set; }
        public int Close { get; set; }
        public int Volume { get; set; }

        public int CalcHigh { get; set; }
        public int CalcLow { get; set; }
        public int CalcClose { get; set; }
        public double XHigh { get; set; }
        public double XLow { get; set; }
        public double XClose { get; set; }

        public double RSI { get; set; }

    }
}
