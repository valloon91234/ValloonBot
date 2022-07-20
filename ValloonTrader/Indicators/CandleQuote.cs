using IO.Swagger.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valloon.Trading;

namespace Valloon.Indicators
{
    /*
     * https://github.com/DaveSkender/Stock.Indicators/blob/main/src/_common/Candles/Candles.cs
     */
    [Serializable]
    public class CandleQuote
    {
        public DateTime Timestamp { get; set; }
        public int Open { get; set; }
        public int High { get; set; }
        public int Low { get; set; }
        public int Close { get; set; }
        public int Volume { get; set; }
        public float RSI { get; set; }

        public CandleQuote() { }

        public CandleQuote(TradeBin t, int x)
        {
            this.Timestamp = t.Timestamp.Value;
            this.Open = (int)Math.Round(t.Open.Value * x);
            this.High = (int)Math.Round(t.High.Value * x);
            this.Low = (int)Math.Round(t.Low.Value * x);
            this.Close = (int)Math.Round(t.Close.Value * x);
            this.Volume = (int)Math.Round(t.Volume.Value);
        }

        public CandleQuote(TradeBin t, string symbol)
        {
            int x = GetX(symbol);
            this.Timestamp = t.Timestamp.Value;
            this.Open = (int)Math.Round(t.Open.Value * x);
            this.High = (int)Math.Round(t.High.Value * x);
            this.Low = (int)Math.Round(t.Low.Value * x);
            this.Close = (int)Math.Round(t.Close.Value * x);
            this.Volume = (int)Math.Round(t.Volume.Value);
        }

        public static int GetX(string symbol)
        {
            switch (symbol)
            {
                case BitMEXApiHelper.SYMBOL_XBTUSD:
                    return 10;
                case BitMEXApiHelper.SYMBOL_SOLUSD:
                    return 100;
                default:
                    throw new ArgumentException($"Invalid symbol: {symbol}");
            }
        }

    }

}
