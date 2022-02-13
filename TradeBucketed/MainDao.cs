using IO.Swagger.Model;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Valloon.Trading.Backtest
{
    abstract class MainDao
    {
        public const string DB_FILENAME = @"data.db";
        public const string DATETIME_FORMAT = @"yyyy-MM-dd HH:mm:ss";
        public const string DATE_FORMAT = @"yyyy-MM-dd";
        public const string TIME_FORMAT = @"HH:mm";
        public static SQLiteConnection connection { get; }
        public static Boolean Encrypted;

        static MainDao()
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
            connection = con;
        }

        public static Boolean Close()
        {
            try
            {
                connection.Close();
                connection.Dispose();
                return true;
            }
            catch { }
            return false;
        }

        public static List<TradeBinModel> SelectAll(string binSize)
        {
            using (SQLiteCommand command = connection.CreateCommand())
            {
                command.CommandText = $"SELECT * FROM tbl_{binSize} ORDER BY timestamp";
                SQLiteDataReader dr = command.ExecuteReader();
                List<TradeBinModel> list = new List<TradeBinModel>();
                while (dr.Read())
                {
                    try
                    {
                        TradeBinModel m = new TradeBinModel
                        {
                            Timestamp = ParseDateTimeString(GetValue<string>(dr["timestamp"])),
                            Date = GetValue<string>(dr["date"]),
                            Time = GetValue<string>(dr["time"]),
                            Open = GetValue<decimal>(dr["open"]),
                            High = GetValue<decimal>(dr["high"]),
                            Low = GetValue<decimal>(dr["low"]),
                            Close = GetValue<decimal>(dr["close"]),
                            Volume = GetValue<int>(dr["volume"]),
                            RSI_M = GetValue<decimal>(dr["rsi_14"]),
                        };
                        list.Add(m);
                    }
                    catch (FormatException e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine(GetValue<string>(dr["timestamp"]));
                    }
                }
                return list;
            }
        }

        public static List<TradeBinModel> SelectAllSMA(string binSize, int bbLength)
        {
            using (SQLiteCommand command = connection.CreateCommand())
            {
                command.CommandText = $"SELECT * FROM tbl_{binSize} ORDER BY timestamp";
                SQLiteDataReader dr = command.ExecuteReader();
                List<TradeBinModel> list = new List<TradeBinModel>();
                while (dr.Read())
                {
                    try
                    {
                        TradeBinModel m = new TradeBinModel
                        {
                            Timestamp = ParseDateTimeString(GetValue<string>(dr["timestamp"])),
                            Date = GetValue<string>(dr["date"]),
                            Time = GetValue<string>(dr["time"]),
                            Open = GetValue<decimal>(dr["open"]),
                            High = GetValue<decimal>(dr["high"]),
                            Low = GetValue<decimal>(dr["low"]),
                            Close = GetValue<decimal>(dr["close"]),
                            Volume = GetValue<int>(dr["volume"]),
                            BB_SMA = GetValue<double>(dr[$"sma_{bbLength}"]),
                        };
                        list.Add(m);
                    }
                    catch (FormatException e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine(GetValue<string>(dr["timestamp"]));
                    }
                }
                return list;
            }
        }

        public static List<TradeBinModel> SelectAll(string binSize, int bbLength)
        {
            using (SQLiteCommand command = connection.CreateCommand())
            {
                command.CommandText = $"SELECT * FROM tbl_{binSize} ORDER BY timestamp";
                SQLiteDataReader dr = command.ExecuteReader();
                List<TradeBinModel> list = new List<TradeBinModel>();
                while (dr.Read())
                {
                    try
                    {
                        TradeBinModel m = new TradeBinModel
                        {
                            Timestamp = ParseDateTimeString(GetValue<string>(dr["timestamp"])),
                            Date = GetValue<string>(dr["date"]),
                            Time = GetValue<string>(dr["time"]),
                            Open = GetValue<decimal>(dr["open"]),
                            High = GetValue<decimal>(dr["high"]),
                            Low = GetValue<decimal>(dr["low"]),
                            Close = GetValue<decimal>(dr["close"]),
                            Volume = GetValue<int>(dr["volume"]),
                            BB_SMA = GetValue<double>(dr[$"bb_{bbLength}_sma"]),
                            BB_SD = GetValue<double>(dr[$"bb_{bbLength}_sd"]),
                            BB_Value = GetValue<double>(dr[$"bb_{bbLength}_value"]),
                            BB_Level = GetValue<int>(dr[$"bb_{bbLength}_level"])
                        };
                        list.Add(m);
                    }
                    catch (FormatException e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine(GetValue<string>(dr["timestamp"]));
                    }
                }
                return list;
            }
        }

        public static DateTime? SelectLastTimestamp(string binSize)
        {
            using (SQLiteCommand command = connection.CreateCommand())
            {
                command.CommandText = $"SELECT MAX(timestamp) FROM tbl_{binSize}";
                SQLiteDataReader dr = command.ExecuteReader();
                if (dr.Read())
                {
                    String value = GetValue<String>(dr[0]);
                    if (value == null) return null;
                    return ParseDateTimeString(value);
                }
                return null;
            }
        }

        public static int Insert(TradeBinModel m, string binSize)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"INSERT INTO tbl_{binSize}(timestamp,date,time,open,high,low,close,volume) VALUES(@timestamp,@date,@time,@open,@high,@low,@close,@volume)";
                command.Parameters.Add("timestamp", System.Data.DbType.String).Value = ToDateTimestring(m.Timestamp);
                command.Parameters.Add("date", System.Data.DbType.String).Value = m.Date;
                command.Parameters.Add("time", System.Data.DbType.String).Value = m.Time;
                command.Parameters.Add("open", System.Data.DbType.Int32).Value = m.Open;
                command.Parameters.Add("high", System.Data.DbType.Int32).Value = m.High;
                command.Parameters.Add("low", System.Data.DbType.Int32).Value = m.Low;
                command.Parameters.Add("close", System.Data.DbType.Int32).Value = m.Close;
                command.Parameters.Add("volume", System.Data.DbType.Int32).Value = m.Volume;
                return command.ExecuteNonQuery();
            }
        }

        public static int UpdateBB(TradeBinModel m, int bbLength, string binSize)
        {
            using (SQLiteCommand command = connection.CreateCommand())
            {
                command.CommandText = $"UPDATE tbl_{binSize} SET bb_{bbLength}_sma=@bb_sma,bb_{bbLength}_sd=@bb_sd,bb_{bbLength}_value=@bb_value,bb_{bbLength}_level=@bb_level WHERE timestamp=@timestamp";
                command.Parameters.Add("timestamp", System.Data.DbType.String).Value = ToDateTimestring(m.Timestamp);
                command.Parameters.Add("bb_sma", System.Data.DbType.Int32).Value = m.BB_SMA;
                command.Parameters.Add("bb_sd", System.Data.DbType.Double).Value = m.BB_SD;
                command.Parameters.Add("bb_value", System.Data.DbType.Double).Value = m.BB_Value;
                command.Parameters.Add("bb_level", System.Data.DbType.Double).Value = m.BB_Level;
                return command.ExecuteNonQuery();
            }
        }

        public static int UpdateSMA(TradeBinModel m, string binSize, int bbLength)
        {
            using (SQLiteCommand command = connection.CreateCommand())
            {
                command.CommandText = $"UPDATE tbl_{binSize} SET sma_{bbLength}=@bb_sma WHERE timestamp=@timestamp";
                command.Parameters.Add("timestamp", System.Data.DbType.String).Value = ToDateTimestring(m.Timestamp);
                command.Parameters.Add("bb_sma", System.Data.DbType.Double).Value = m.BB_SMA;
                return command.ExecuteNonQuery();
            }
        }

        public static int UpdateRSI(TradeBinModel m, string binSize, int length)
        {
            using (SQLiteCommand command = connection.CreateCommand())
            {
                command.CommandText = $"UPDATE tbl_{binSize} SET rsi_{length}=@rsi WHERE timestamp=@timestamp";
                command.Parameters.Add("timestamp", System.Data.DbType.String).Value = ToDateTimestring(m.Timestamp);
                command.Parameters.Add("rsi", System.Data.DbType.Double).Value = m.RSI;
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
