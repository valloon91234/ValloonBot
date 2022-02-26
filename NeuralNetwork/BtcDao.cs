using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;

namespace Valloon.Trading.Backtest
{
    public class BtcDao
    {
        public const string DB_FILENAME = @"data.db";
        public const string DATETIME_FORMAT = @"yyyy-MM-dd HH:mm:ss";
        public const string DATE_FORMAT = @"yyyy-MM-dd";
        public const string TIME_FORMAT = @"HH:mm";
        public static SQLiteConnection Connection { get; }
        public static Boolean Encrypted;

        static BtcDao()
        {
            FileInfo fileInfo = new FileInfo(DB_FILENAME);
            if (!fileInfo.Exists) throw new FileNotFoundException("Can not find database file - " + DB_FILENAME);
            string connectionstring = @"Data Source=" + DB_FILENAME + ";Version=3";
            SQLiteConnection con = new SQLiteConnection(connectionstring);
            con.Open();
            using (SQLiteCommand command = con.CreateCommand())
            {
                command.CommandText = "PRAGMA encoding";
                object result = command.ExecuteScalar();
            }
            Encrypted = false;
            Connection = con;
        }

        public static Boolean Close()
        {
            try
            {
                Connection.Close();
                Connection.Dispose();
                return true;
            }
            catch { }
            return false;
        }

        public static List<BtcBin> SelectAll(string binSize)
        {
            using (SQLiteCommand command = Connection.CreateCommand())
            {
                command.CommandText = $"SELECT * FROM btc_{binSize} ORDER BY timestamp";
                SQLiteDataReader dr = command.ExecuteReader();
                List<BtcBin> list = new List<BtcBin>();
                while (dr.Read())
                {
                    try
                    {
                        BtcBin m = new BtcBin
                        {
                            Timestamp = ParseDateTimeString(GetValue<string>(dr["timestamp"])),
                            Date = GetValue<string>(dr["date"]),
                            Time = GetValue<string>(dr["time"]),
                            Open = GetValue<double>(dr["open"]),
                            High = GetValue<double>(dr["high"]),
                            Low = GetValue<double>(dr["low"]),
                            Close = GetValue<double>(dr["close"]),
                            Volume = GetValue<int>(dr["volume"]),
                            CalcHigh = GetValue<double>(dr["calc_high"]),
                            CalcLow = GetValue<double>(dr["calc_low"]),
                            CalcClose = GetValue<double>(dr["calc_close"]),
                            XHigh = GetValue<double>(dr["x_high"]),
                            XLow = GetValue<double>(dr["x_low"]),
                            XClose = GetValue<double>(dr["x_close"]),
                        };
                        list.Add(m);
                    }
                    catch (FormatException e)
                    {
                        Console.WriteLine(GetValue<string>(dr["timestamp"]) + " \t " + e.Message);
                    }
                }
                return list;
            }
        }

        public static int Insert(BtcBin m, string binSize)
        {
            using (var command = Connection.CreateCommand())
            {
                command.CommandText = $"INSERT INTO btc_{binSize}(timestamp,date,time,open,high,low,close,volume) VALUES(@timestamp,@date,@time,@open,@high,@low,@close,@volume)";
                command.Parameters.Add("timestamp", System.Data.DbType.String).Value = ToDateTimestring(m.Timestamp);
                command.Parameters.Add("date", System.Data.DbType.String).Value = m.Date;
                command.Parameters.Add("time", System.Data.DbType.String).Value = m.Time;
                command.Parameters.Add("open", System.Data.DbType.Double).Value = m.Open;
                command.Parameters.Add("high", System.Data.DbType.Double).Value = m.High;
                command.Parameters.Add("low", System.Data.DbType.Double).Value = m.Low;
                command.Parameters.Add("close", System.Data.DbType.Double).Value = m.Close;
                command.Parameters.Add("volume", System.Data.DbType.Int32).Value = m.Volume;
                return command.ExecuteNonQuery();
            }
        }

        public static int Update(BtcBin m, string binSize)
        {
            using (var command = Connection.CreateCommand())
            {
                command.CommandText = $"UPDATE btc_{binSize} SET calc_high=@calc_high, calc_low=@calc_low, calc_close=@calc_close, x_high=@x_high, x_low=@x_low, x_close=@x_close WHERE timestamp=@timestamp";
                command.Parameters.Add("timestamp", System.Data.DbType.String).Value = ToDateTimestring(m.Timestamp);
                command.Parameters.Add("calc_high", System.Data.DbType.Double).Value = m.CalcHigh;
                command.Parameters.Add("calc_low", System.Data.DbType.Double).Value = m.CalcLow;
                command.Parameters.Add("calc_close", System.Data.DbType.Double).Value = m.CalcClose;
                command.Parameters.Add("x_high", System.Data.DbType.Double).Value = m.XHigh;
                command.Parameters.Add("x_low", System.Data.DbType.Double).Value = m.XLow;
                command.Parameters.Add("x_close", System.Data.DbType.Double).Value = m.XClose;
                return command.ExecuteNonQuery();
            }
        }

        //public static void Encrypt()
        //{
        //    connection.ChangePassword(DB_PASSWORD);
        //}

        //public static void Decrypt()
        //{
        //    connection.ChangePassword("");
        //}

        public static T GetValue<T>(object obj)
        {
            Type type = typeof(T);
            if (obj == null || obj == DBNull.Value)
            {
                return default; // returns the default value for the type
            }
            else if (type == typeof(DateTime))
            {
                return (T)(Object)DateTime.ParseExact((string)obj, DATETIME_FORMAT, CultureInfo.CurrentCulture);
            }
            else
            {
                return (T)Convert.ChangeType(obj, typeof(T));
            }
        }

        public static string ToDateString(DateTime dt)
        {
            return dt.ToString(DATE_FORMAT);
        }

        public static DateTime ParseDateString(string s)
        {
            return DateTime.ParseExact(s, DATE_FORMAT, CultureInfo.CurrentCulture);
        }

        public static string ToDateTimestring(DateTime dt)
        {
            return dt.ToString(DATETIME_FORMAT);
        }

        public static DateTime ParseDateTimeString(string s)
        {
            return DateTime.ParseExact(s, DATETIME_FORMAT, CultureInfo.CurrentCulture);
        }
    }
}
