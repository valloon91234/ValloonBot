using Newtonsoft.Json.Linq;
using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Valloon.Trading;
using Valloon.Utils;

namespace Valloon.BitMEX.Backtest
{
    static class MACD_GRID
    {
        //static readonly string SYMBOL = "SOLUSDT";
        static readonly string SYMBOL = "ETHUSDT";

        public static void Run()
        {
            Program.MoveWindow(20, 0, 1600, 200);
            //Loader.LoadCSV(SYMBOL, "1h", new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc)); return;
            //Loader.Load(SYMBOL, "1h", new DateTime(2022, 8, 3, 0, 0, 0, DateTimeKind.Utc)); return;
            {
                Benchmark();
                //Test();
                return;
            }
            //ThreadPool.QueueUserWorkItem(state => runHCS(new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc)));
        }

        static float Benchmark()
        {
            const int buyOrSell = 1;

            //DateTime startTime = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime startTime = new DateTime(2021, 12, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime? endTime = null;
            //DateTime? endTime = new DateTime(2022, 7, 1, 0, 0, 0, DateTimeKind.Utc);

            //const int binSize = 4;
            //var list1h = Dao.SelectAll(SYMBOL, "1h");
            //var list = Loader.LoadBinListFrom1h(binSize, list1h, false);
            const int binSize = 15;
            var list1m = Dao.SelectAll(SYMBOL, "1m");
            var list = Loader.LoadBinListFrom1m(binSize, list1m);

            var quoteList = new List<Quote>();
            foreach (var t in list)
            {
                quoteList.Add(new Quote
                {
                    Date = t.Timestamp,
                    Open = t.Open,
                    High = t.High,
                    Low = t.Low,
                    Close = t.Close,
                    Volume = t.Volume,
                });
            }
            list.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
            int count = list.Count;
            int totalDays = (int)(list[count - 1].Timestamp - list[0].Timestamp).TotalDays;
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}    {SYMBOL}-MACD-GRID    buyOrSell = {buyOrSell}    bin = {binSize}    {startTime:yyyy-MM-dd} ~ {endTime:yyyy-MM-dd} ({totalDays:N0}) days");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;

            var macdList = quoteList.GetMacd().ToList();
            macdList.RemoveAll(x => x.Date < startTime || endTime != null && x.Date > endTime.Value);

            //for (int rsiLength = 14; rsiLength <= 16; rsiLength += 2)
            //int rsiLength = 14;
            {
                //var rsiList = quoteList.GetRsi(rsiLength).ToList();
                //rsiList.RemoveAll(x => x.Date < startTime || endTime != null && x.Date > endTime.Value);

                //for (int i = 0; i < count; i++)
                //{
                //    Console.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t {rsiList[i].Rsi:F4}    {macdList[i].Macd:F4}  /  {macdList[i].Histogram:F4}  /  {macdList[i].Signal:F4}    {bbwList[i].Width:F4}");
                //}

                double openX = 0.05;
                double closeX = 0.01;
                double maxRisk = 1;

                int openCount = 0, closeCount = 0, zeroCount = 0;
                int maxSize = 1;
                int positionSize = 0;
                double positionEntryPrice = 0;
                double profit = 0;
                for (int i = 1; i < count - 1; i++)
                {
                    if (buyOrSell == 1)
                    {
                        if (positionEntryPrice > 0)
                            maxRisk = Math.Min((double)list[i].Low / positionEntryPrice, maxRisk);
                        //var closePrice = positionEntryPrice * (1 + closeX);
                        //if (positionSize > 0 && list[i].High > closePrice)
                        //{
                        //    profit += closeX * positionSize;
                        //    positionSize = 0;
                        //    positionEntryPrice = 0;
                        //    logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t <CLOSE> \t price = {closePrice} \t entry = {positionEntryPrice:F4}    size = {positionSize}  /  {maxSize}    profit = {profit:F4}    risk = {maxRisk:F4}", ConsoleColor.Green);
                        //}
                        if (macdList[i - 1].Histogram < 0 && macdList[i].Histogram >= 0)
                        {
                            var price = list[i].Close;
                            if (positionSize == 0)
                            {
                                positionEntryPrice = price;
                                positionSize++;
                                maxSize = Math.Max(positionSize, maxSize);
                                logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t <OPEN> \t price = {price} \t entry = {positionEntryPrice:F4}    size = {positionSize}  /  {maxSize}    profit = {profit:F4}    risk = {maxRisk:F4}");
                            }
                            else if (positionEntryPrice / price > openX + 1)
                            {
                                positionEntryPrice = (positionEntryPrice * positionSize + price) / (positionSize + 1);
                                positionSize++;
                                maxSize = Math.Max(positionSize, maxSize);
                                logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t <OPEN> \t price = {price} \t entry = {positionEntryPrice:F4}    size = {positionSize}  /  {maxSize}    profit = {profit:F4}    risk = {maxRisk:F4}");
                            }
                        }
                        else if (positionSize > 0 && macdList[i - 1].Histogram > 0 && macdList[i].Histogram <= 0)
                        {
                            var price = list[i].Close;
                            if (price / positionEntryPrice > closeX + 1)
                            {
                                int reduce = (positionSize - 1) / 3 + 1;
                                positionSize -= reduce;
                                profit += (price / positionEntryPrice - 1) * reduce;
                                if (positionSize == 0)
                                {
                                    positionEntryPrice = 0;
                                    zeroCount++;
                                }
                                logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t <CLOSE> \t price = {price} \t entry = {positionEntryPrice:F4}    size = {positionSize}  /  {maxSize}    profit = {profit:F4}    risk = {maxRisk:F4}", positionSize == 0 ? ConsoleColor.Red : ConsoleColor.Green);
                            }
                        }
                    }
                    else if (buyOrSell == 2)
                    {

                    }
                }
            }
            return 0;
        }

        static string Test()
        {
            //Test(new DateTime(2022, 3, 16, 0, 0, 0, DateTimeKind.Utc)); return null;

            string result = "";
            result += Test(new DateTime(2022, 8, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2022, 7, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 8, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2022, 6, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 7, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2022, 5, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 6, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2022, 4, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 5, 2, 8, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 4, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2022, 2, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 2, 2, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2021, 12, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 12, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2021, 10, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2021, 9, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 10, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2021, 8, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 9, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 8, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2022, 6, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2022, 5, 15, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2022, 2, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            result += Test(new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc)) + "\r\n\r\n";
            Console.WriteLine("\r\n\r\n================================\r\n");
            Console.WriteLine(result);
            return result;
        }

        static string Test(DateTime startTime, DateTime? endTime = null)
        {
            int buyOrSell = 3;
            int binSize = 4;
            int rsiLength = 14;

            int rsiValue1 = 69;
            int bbwLength1 = 8;
            float bbwOpen1 = 0.088f;
            float bbwClose1 = 0.085f;
            float closeX1 = 0.06f;
            float stopX1 = 0.01f;

            int rsiValue2 = 30;
            int bbwLength2 = 4;
            float bbwOpen2 = 0.058f;
            float bbwClose2 = 0;
            float closeX2 = 0.07f;
            float stopX2 = 0.01f;

            //const float makerFee = 0.0003f;
            const float takerFee = 0.002f;

            var list1h = Dao.SelectAll(SYMBOL, "1h");
            var list = Loader.LoadBinListFrom1h(binSize, list1h, false);

            var quoteList = new List<Quote>();
            foreach (var t in list)
            {
                quoteList.Add(new Quote
                {
                    Date = t.Timestamp,
                    Open = t.Open,
                    High = t.High,
                    Low = t.Low,
                    Close = t.Close,
                    Volume = t.Volume,
                });
            }

            list.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
            int count = list.Count;
            int totalDays = (int)(list.Last().Timestamp - startTime).TotalDays;
            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}    TestSAR2    bin = {binSize}    takerFee = {takerFee}    ({startTime:yyyy-MM-dd HH.mm.ss} ~ {totalDays:N0} days)");
            logger.WriteLine("\n" + logger.LogFilename + "\n");
            logger.WriteLine($"{count} loaded. ({totalDays:N0} days)");
            Console.Title = logger.LogFilename;

            var rsiList = quoteList.GetRsi(rsiLength).ToList();
            rsiList.RemoveAll(x => x.Date < startTime || endTime != null && x.Date > endTime.Value);
            var macdList = quoteList.GetMacd().ToList();
            macdList.RemoveAll(x => x.Date < startTime || endTime != null && x.Date > endTime.Value);

            var bbwList1 = quoteList.GetBollingerBands(bbwLength1, 2).ToList();
            bbwList1.RemoveAll(x => x.Date < startTime || endTime != null && x.Date > endTime.Value);
            var bbwList2 = quoteList.GetBollingerBands(bbwLength2, 2).ToList();
            bbwList2.RemoveAll(x => x.Date < startTime || endTime != null && x.Date > endTime.Value);

            //for (int i = 0; i < count; i++)
            //{
            //    Console.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t {rsiList[i].Rsi:F4}    {macdList[i].Macd:F4}  /  {macdList[i].Histogram:F4}  /  {macdList[i].Signal:F4}");
            //}

            int tryCount = 0;
            int succeedCount = 0, failedCount = 0;
            float finalPercent = 1, finalPercent2 = 1, finalPercent3 = 1, finalPercent4 = 1, finalPercent5 = 1, finalPercent10 = 1;
            int position = 0;
            int positionEntryPrice = 0;
            int lastPositionEntryPrice = 0;
            for (int i = 2; i < count - 1; i++)
            {
                if (position == 0)
                {
                    if (buyOrSell == 1 || buyOrSell == 3)
                    {
                        //if (macdList[i].Histogram > 0 && bbwList1[i - 1].Width < bbwOpen1 && bbwList1[i].Width >= bbwOpen1)
                        if (macdList[i].Histogram > 0 && macdList[i].Histogram > macdList[i - 1].Histogram && bbwList1[i - 1].Width < bbwOpen1 && bbwList1[i].Width >= bbwOpen1)
                        {
                            lastPositionEntryPrice = list[i].Close;
                            if (rsiList[i].Rsi < rsiValue1)
                            {
                                tryCount++;
                                position = 1;
                                positionEntryPrice = list[i].Close;
                                logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t {rsiList[i].Rsi:F4}    {macdList[i].Macd:F4}  /  {macdList[i].Histogram:F4}  /  {macdList[i].Signal:F4} \t Position = {position} \t Entry = {positionEntryPrice} \t <LONG>");
                            }
                        }
                    }
                    if (buyOrSell == 2 || buyOrSell == 3)
                    {
                        //if (macdList[i].Histogram < 0 && bbwList2[i - 1].Width < bbwOpen2 && bbwList2[i].Width >= bbwOpen2)
                        if (macdList[i].Histogram < 0 && macdList[i].Histogram < macdList[i - 1].Histogram && bbwList2[i - 1].Width < bbwOpen2 && bbwList2[i].Width >= bbwOpen2)
                        {
                            lastPositionEntryPrice = list[i].Close;
                            if (rsiList[i].Rsi > rsiValue2)
                            {
                                tryCount++;
                                position = -1;
                                positionEntryPrice = list[i].Close;
                                logger.WriteLine($"{list[i].Timestamp:yyyy-MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t {rsiList[i].Rsi:F4}    {macdList[i].Macd:F4}  /  {macdList[i].Histogram:F4}  /  {macdList[i].Signal:F4} \t Position = {position} \t Entry = {positionEntryPrice} \t <SHORT>");
                            }
                        }
                    }
                }
                else if (position == 1)
                {
                    int closePrice = (int)Math.Floor(positionEntryPrice * (1 + closeX1));
                    int stopPrice = (int)Math.Ceiling(positionEntryPrice * (1 - stopX1));
                    if (stopX1 > 0 && list[i].Low < stopPrice)
                    {
                        failedCount++;
                        float percent = (float)stopPrice / positionEntryPrice - takerFee;
                        finalPercent *= percent;
                        finalPercent2 *= 1 + (percent - 1) * 2;
                        finalPercent3 *= 1 + (percent - 1) * 3;
                        finalPercent4 *= 1 + (percent - 1) * 4;
                        finalPercent5 *= 1 + (percent - 1) * 5;
                        finalPercent10 *= 1 + (percent - 1) * 10;
                        logger.WriteLine($"     {list[i].Timestamp:MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t Failed \t <LONG> \t Entry = {positionEntryPrice} \t Stop = {stopPrice} \t % = {percent:F4}  /  {finalPercent}", ConsoleColor.Red);
                        position = 0;
                        positionEntryPrice = 0;
                        i--;
                    }
                    else if (closeX1 > 0 && list[i].High > closePrice)
                    {
                        succeedCount++;
                        float percent = (float)closePrice / positionEntryPrice - takerFee;
                        finalPercent *= percent;
                        finalPercent2 *= 1 + (percent - 1) * 2;
                        finalPercent3 *= 1 + (percent - 1) * 3;
                        finalPercent4 *= 1 + (percent - 1) * 4;
                        finalPercent5 *= 1 + (percent - 1) * 5;
                        finalPercent10 *= 1 + (percent - 1) * 10;
                        logger.WriteLine($"     {list[i].Timestamp:MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t Succeed \t <LONG> \t Entry = {positionEntryPrice} \t Close = {closePrice} \t % = {percent:F4}  /  {finalPercent}", ConsoleColor.Green);
                        position = 0;
                        positionEntryPrice = 0;
                        i--;
                    }
                    else if (macdList[i].Histogram < 0 || bbwList1[i - 1].Width >= bbwClose1 && bbwList1[i].Width < bbwClose1)
                    {
                        float percent = (float)list[i].Close / positionEntryPrice - takerFee;
                        if (percent > 1)
                            succeedCount++;
                        else
                            failedCount++;
                        finalPercent *= percent;
                        finalPercent2 *= 1 + (percent - 1) * 2;
                        finalPercent3 *= 1 + (percent - 1) * 3;
                        finalPercent4 *= 1 + (percent - 1) * 4;
                        finalPercent5 *= 1 + (percent - 1) * 5;
                        finalPercent10 *= 1 + (percent - 1) * 10;
                        logger.WriteLine($"     {list[i].Timestamp:MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t Stopped \t <LONG> \t Entry = {positionEntryPrice} \t Stop = {list[i].Close} \t % = {percent:F4}  /  {finalPercent}", ConsoleColor.DarkYellow);
                        position = 0;
                        positionEntryPrice = 0;
                        i--;
                    }

                }
                else if (position == -1)
                {
                    int closePrice = (int)Math.Ceiling(positionEntryPrice * (1 - closeX2));
                    int stopPrice = (int)Math.Floor(positionEntryPrice * (1 + stopX2));
                    if (stopX2 > 0 && list[i].High > stopPrice)
                    {
                        failedCount++;
                        float percent = 2 - (float)stopPrice / positionEntryPrice - takerFee;
                        finalPercent *= percent;
                        finalPercent2 *= 1 + (percent - 1) * 2;
                        finalPercent3 *= 1 + (percent - 1) * 3;
                        finalPercent4 *= 1 + (percent - 1) * 4;
                        finalPercent5 *= 1 + (percent - 1) * 5;
                        finalPercent10 *= 1 + (percent - 1) * 10;
                        logger.WriteLine($"     {list[i].Timestamp:MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t Failed \t <SHORT> \t Entry = {positionEntryPrice} \t Stop = {stopPrice} \t % = {percent:F4}  /  {finalPercent}", ConsoleColor.Red);
                        position = 0;
                        positionEntryPrice = 0;
                        i--;
                    }
                    else if (closeX2 > 0 && list[i].Low < closePrice)
                    {
                        succeedCount++;
                        float percent = 2 - (float)closePrice / positionEntryPrice - takerFee;
                        finalPercent *= percent;
                        finalPercent2 *= 1 + (percent - 1) * 2;
                        finalPercent3 *= 1 + (percent - 1) * 3;
                        finalPercent4 *= 1 + (percent - 1) * 4;
                        finalPercent5 *= 1 + (percent - 1) * 5;
                        finalPercent10 *= 1 + (percent - 1) * 10;
                        logger.WriteLine($"     {list[i].Timestamp:MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t Succeed \t <SHORT> \t Entry = {positionEntryPrice} \t Close = {closePrice} \t % = {percent:F4}  /  {finalPercent}", ConsoleColor.Green);
                        position = 0;
                        positionEntryPrice = 0;
                        i--;
                    }
                    else if (macdList[i].Histogram > 0 || bbwList2[i - 1].Width >= bbwClose2 && bbwList2[i].Width < bbwClose2)
                    {
                        float percent = 2 - (float)list[i].Close / positionEntryPrice - takerFee;
                        if (percent > 1)
                            succeedCount++;
                        else
                            failedCount++;
                        finalPercent *= percent;
                        finalPercent2 *= 1 + (percent - 1) * 2;
                        finalPercent3 *= 1 + (percent - 1) * 3;
                        finalPercent4 *= 1 + (percent - 1) * 4;
                        finalPercent5 *= 1 + (percent - 1) * 5;
                        finalPercent10 *= 1 + (percent - 1) * 10;
                        logger.WriteLine($"     {list[i].Timestamp:MM-dd HH:mm:ss} \t {list[i].Open} / {list[i].High} / {list[i].Low} / {list[i].Close} \t Stopped \t <SHORT> \t Entry = {positionEntryPrice} \t Stop = {list[i].Close} \t % = {percent:F4}  /  {finalPercent}", ConsoleColor.DarkYellow);
                        position = 0;
                        positionEntryPrice = 0;
                        i--;
                    }
                }
            }
            float successRate = failedCount > 0 ? (float)succeedCount / failedCount : succeedCount;
            string result = $"{startTime} ~ {list.Last().Timestamp} ({totalDays} days) \t count = {tryCount} / {succeedCount} / {failedCount} / {successRate:F2} \t % = {finalPercent:F4} / {finalPercent2:F4} / {finalPercent3:F4} / {finalPercent4:F4} / {finalPercent5:F4} / {finalPercent10:F4}";
            logger.WriteLine($"\r\n{result}\r\n");
            return result;
        }

    }

}
