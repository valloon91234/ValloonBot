using IO.Swagger.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Valloon.Trading.Backtest
{
    public class XbtBin
    {
        public DateTime Timestamp { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public int Open { get; set; }
        public int High { get; set; }
        public int Low { get; set; }
        public int Close { get; set; }
        public int Volume { get; set; }
        public float SMA { get; set; }
        public float RSI { get; set; }

        public XbtBin() { }

        public XbtBin(TradeBin t)
        {
            this.Timestamp = t.Timestamp.Value;
            this.Date = t.Timestamp.Value.ToString(MainDao.DATE_FORMAT);
            this.Time = t.Timestamp.Value.ToString(MainDao.TIME_FORMAT);
            this.Open = (int)t.Open.Value;
            this.High = (int)t.High.Value;
            this.Low = (int)t.Low.Value;
            this.Close = (int)t.Close.Value;
            this.Volume = (int)t.Volume.Value;
        }
    }
}
