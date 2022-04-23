using IO.Swagger.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Valloon.Trading.Backtest
{
    class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        public static void MoveWindow(int X, int Y, int nWidth, int nHeight)
        {
            MoveWindow(GetConsoleWindow(), X, Y, nWidth, nHeight, true);
        }

        static void Main(string[] args)
        {
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            {
                //Sol5.Run();
                //Btc5.Run();
                //Btc1h.Run();
                //Btc.Run();
                //Btc_H.Run();
                //Sol_H.Run();
                Sol5.Run();
                //Luna.Run();
                goto end;
            }

            //{
            //    DateTime startTime = new DateTime(2022, 1, 27, 0, 0, 0, DateTimeKind.Utc);
            //    DateTime endTime = DateTime.UtcNow;
            //    Load_1m(startTime, endTime);
            //    goto end;
            //}

            //{
            //    DateTime startTime = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            //    TestBuyOrSell(null, startTime, 1, 0, 660, 0.009m, 0.95m, 6.2m);
            //    goto end;
            //}

            //BB("1m", 20, 0.004d / 3);
            //Test1("1m", 30);

            //WriteSMA("1m", 660);
            //goto end;


            //int[] lengthArray = { 5, 10, 15, 20, 25, 30, 40, 50, 60, 90, 120, 180, 240, 360, 420, 480, 520, 600, 660, 720, 780, 840, 900, 960 };
            //int[] lengthArray = { 660 };
            //decimal[] stopXArray = { 1m / 10, 1m / 8, 1m / 6, 1m / 5, 1m / 4, 1m / 3, 1m / 2, 2m / 3, 1, 3m / 2, 2, 3, 4, 5, 6, 8, 10 };
            //decimal[] stopXArray = { 1m / 5, 1m / 4, 1m / 3, 1m / 2, 2m / 3, 1, 3m / 2, 2, 3, 4, 5, 6, 8, 10 };
            //decimal[] stopXArray = { 6.5m, 6, 5.5m, 5 };
            //foreach (int length in lengthArray)
            //    foreach (decimal stopX in stopXArray)
            //    {
            //        //{
            //        //    DateTime startTime = new DateTime(2021, 4, 1, 0, 0, 0, DateTimeKind.Utc);
            //        //    Test2(startTime, 1, length, stopX);
            //        //}
            //        {
            //            DateTime startTime = new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc);
            //            Test2(startTime, 1, length, stopX);
            //        }
            //        {
            //            DateTime startTime = new DateTime(2021, 10, 1, 0, 0, 0, DateTimeKind.Utc);
            //            Test2(startTime, 1, length, stopX);
            //        }
            //        {
            //            DateTime startTime = new DateTime(2021, 12, 1, 0, 0, 0, DateTimeKind.Utc);
            //            Test2(startTime, 1, length, stopX);
            //        }
            //        {
            //            DateTime startTime = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            //            Test2(startTime, 1, length, stopX);
            //        }
            //    }

            List<TradeBinModel> getList(DateTime startTime, DateTime? endTime = null)
            {
                List<TradeBinModel> list = MainDao.SelectAll("1m");
                {
                    int mLength = 660;
                    int countAll = list.Count;
                    for (int i = mLength; i < countAll; i++)
                    {
                        double[] closeArray = new double[mLength];
                        for (int j = 0; j < mLength; j++)
                            closeArray[j] = (double)list[i - mLength + j].Close;
                        list[i].BB_SMA = closeArray.Average();
                    }
                    //int removeCount = 0;
                    //for (int i = 0; i < countAll - 1; i++)
                    //{
                    //    if (list[i].Timestamp < startTime)
                    //        removeCount++;
                    //    else
                    //        break;
                    //}
                    //list.RemoveRange(0, removeCount);
                    list.RemoveAll(x => x.Timestamp < startTime || endTime != null && x.Timestamp > endTime.Value);
                }
                Console.WriteLine($"----    Ready from {list[0].Date}    ----");
                return list;
            }

            void runHCS(DateTime startTime, DateTime? endTime = null)
            {
                List<TradeBinModel> list = getList(startTime, endTime);
                //TestHCS(list, 1);
                //TestHCS(list, 2);
            }

            runHCS(new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc));
            runHCS(new DateTime(2021, 10, 1, 0, 0, 0, DateTimeKind.Utc));
            runHCS(new DateTime(2021, 12, 1, 0, 0, 0, DateTimeKind.Utc));
            runHCS(new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            goto end;


            ThreadPool.QueueUserWorkItem(state => runHCS(new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc)));
            ThreadPool.QueueUserWorkItem(state => runHCS(new DateTime(2021, 8, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc)));
            ThreadPool.QueueUserWorkItem(state => runHCS(new DateTime(2021, 9, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc)));
            ThreadPool.QueueUserWorkItem(state => runHCS(new DateTime(2021, 10, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc)));
            //ThreadPool.QueueUserWorkItem(state => runHCS(new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 12, 1, 0, 0, 0, DateTimeKind.Utc)));

            //ThreadPool.QueueUserWorkItem(state => runHCS(new DateTime(2021, 12, 1, 0, 0, 0, DateTimeKind.Utc)));
            //ThreadPool.QueueUserWorkItem(state => runHCS(new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc)));

            while (true) Console.ReadKey(false);
            goto end;


            Logger logger = new Logger($"{DateTime.Now:yyyy-MM-dd  HH.mm.ss}  -  hLength-StopX2 Test");
            int hLength = 0;
            while (hLength <= 48)
            {
                //decimal result = TestBuyAndSell(startTime, hLength, 660, 0.01m, 1.6m, 5, 0.009m, 0.8m, 7);

                //for (decimal stopX2 = -3; stopX2 <= 1; stopX2 += .5m)
                //{
                //    decimal result = TestBuyOrSell(list, startTime, 2, hLength, 660, 0.01m, 1.6m, 5, stopX2);
                //    logger.WriteLine($"{startTime:yyyy-MM-dd}    hLength = {hLength}    stopX2 = {stopX2}    result = {result}");
                //}
                //hLength += 2;

                //if (hLength < 24) hLength += 6;
                //else if (hLength < 48) hLength += 12;
                //else if (hLength < 360) hLength += 24;
                //else if (hLength < 720) hLength += 72;
                //else hLength += 144;
            }

            {
                //DateTime startTime = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                //TestBuyOrSell(startTime, 1, 660, 0.009m, 0.8m, 7, false);
                //TestBuyOrSell(startTime, 1, 660, 0.009m, 0.8m, 7, true);
                //TestBuyOrSell(startTime, 2, 660, 0.01m, 1.6m, 5, false);
                //TestBuyOrSell(startTime, 2, 660, 0.01m, 1.6m, 5, true);
            }

        end:;
            Console.WriteLine($"\nCompleted. Press any key to exit... ");
            Console.ReadKey(false);
        }

    }
}
