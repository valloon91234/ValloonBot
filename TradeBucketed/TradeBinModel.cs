using IO.Swagger.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Valloon.Trading.Backtest
{
    public class TradeBinModel
    {
        public DateTime Timestamp { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public int Volume { get; set; }
        public double BB_SMA { get; set; }
        public double BB_SMA_H { get; set; }
        public double BB_SD { get; set; }
        public double BB_Value { get; set; }
        public decimal SMA_M { get; set; }
        public double RSI { get; set; }
        public decimal RSI_M { get; set; }

        public int BB_Level { get; set; }

        public TradeBinModel() { }

        public TradeBinModel(TradeBin t)
        {
            this.Timestamp = t.Timestamp.Value;
            this.Date = t.Timestamp.Value.ToString(MainDao.DATE_FORMAT);
            this.Time = t.Timestamp.Value.ToString(MainDao.TIME_FORMAT);
            this.Open = t.Open.Value;
            this.High = t.High.Value;
            this.Low = t.Low.Value;
            this.Close = t.Close.Value;
            this.Volume = (int)t.Volume.Value;
        }
    }
}
