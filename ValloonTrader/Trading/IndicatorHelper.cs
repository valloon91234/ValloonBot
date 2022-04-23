using IO.Swagger.Model;
using System.Collections.Generic;

namespace Valloon.Trading
{
    /**
     * https://daveskender.github.io/Stock.Indicators/indicators/
     */
    public class IndicatorHelper
    {
        public static List<Skender.Stock.Indicators.Quote> TradeBinToQuote(List<TradeBin> tradeBinList)
        {
            var quoteList = new List<Skender.Stock.Indicators.Quote>();
            foreach (var t in tradeBinList)
            {
                quoteList.Add(new Skender.Stock.Indicators.Quote
                {
                    Date = t.Timestamp.Value,
                    Open = t.Open.Value,
                    High = t.High.Value,
                    Low = t.Low.Value,
                    Close = t.Close.Value,
                    Volume = t.Volume.Value,
                });
            }
            return quoteList;
        }

    }
}
