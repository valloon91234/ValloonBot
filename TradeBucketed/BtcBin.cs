using IO.Swagger.Model;
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
        public float Open { get; set; }
        public float High { get; set; }
        public float Low { get; set; }
        public float Close { get; set; }
        public int Volume { get; set; }
        public float SD { get; set; }
        public float SMA { get; set; }
        public float RSI { get; set; }

        public BtcBin() { }

        public BtcBin(TradeBin t)
        {
            this.Timestamp = t.Timestamp.Value;
            this.Date = t.Timestamp.Value.ToString(MainDao.DATE_FORMAT);
            this.Time = t.Timestamp.Value.ToString(MainDao.TIME_FORMAT);
            this.Open = (float)t.Open.Value;
            this.High = (float)t.High.Value;
            this.Low = (float)t.Low.Value;
            this.Close = (float)t.Close.Value;
            this.Volume = (int)t.Volume.Value;
        }
    }
}
