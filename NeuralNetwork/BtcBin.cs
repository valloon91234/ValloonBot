using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Valloon.Trading.Backtest
{
    public class BtcBin
    {
        public DateTime Timestamp { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public int Volume { get; set; }

        public double CalcHigh { get; set; }
        public double CalcLow { get; set; }
        public double CalcClose { get; set; }
        public double XHigh { get; set; }
        public double XLow { get; set; }
        public double XClose { get; set; }

    }

}
