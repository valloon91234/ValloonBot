﻿using IO.Swagger.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Valloon.Indicators;
using Valloon.Trading;
using Valloon.Utils;

namespace Notify
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Color backColor = Color.FromArgb(29, 38, 49); ;
            this.BackColor = backColor;
            numericUpDown1.BackColor = backColor;
            numericUpDown2.BackColor = backColor;
            textBox_BTC.BackColor = backColor;
            textBox_SOL.BackColor = backColor;
            trackBar1.BackColor = backColor;
            textBox_RSI.BackColor = backColor;
            textBox_RSI_State.BackColor = backColor;

            trackBar1_Scroll(null, null);
            RunThread();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            this.Activate();
        }

        private WMPLib.WindowsMediaPlayer Player = new WMPLib.WindowsMediaPlayer();

        private void PlayFile(String url)
        {
            Player = new WMPLib.WindowsMediaPlayer();
            Player.PlayStateChange += Player_PlayStateChange;
            Player.URL = url;
            Player.controls.play();
        }

        private void Player_PlayStateChange(int NewState)
        {
            if ((WMPLib.WMPPlayState)NewState == WMPLib.WMPPlayState.wmppsStopped)
            {
                //Actions on stop
            }
        }

        private void Form1_Activated(object sender, EventArgs e)
        {
            Player.controls.stop();
        }

        public void Exit()
        {
            notifyIcon1.Visible = false;
            Process.GetCurrentProcess().Kill();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Exit();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            this.TopMost = checkBox1.Checked;
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            this.Opacity = trackBar1.Value / 100d;
        }

        public static string HttpGet(string url)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.Timeout = 15000;
            httpWebRequest.ReadWriteTimeout = 15000;
            httpWebRequest.Method = "Get";
            using (var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
            {
                string Charset = httpWebResponse.CharacterSet;
                using (var receiveStream = httpWebResponse.GetResponseStream())
                using (var streamReader = new StreamReader(receiveStream, Encoding.GetEncoding(Charset)))
                    return streamReader.ReadToEnd();
            }
        }

        public string HttpGetFromBitMEX(string url)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.Timeout = 10000;
            httpWebRequest.ReadWriteTimeout = 10000;
            httpWebRequest.Method = "Get";
            //httpWebRequest.Proxy = new WebProxy("http://54.249.108.164:443");

            using (var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
            {
                string Charset = httpWebResponse.CharacterSet;
                ServerTime = DateTime.Parse(httpWebResponse.GetResponseHeader("Date")).ToUniversalTime();
                using (var receiveStream = httpWebResponse.GetResponseStream())
                using (var streamReader = new StreamReader(receiveStream, Encoding.GetEncoding(Charset)))
                    return streamReader.ReadToEnd();
            }
        }

        //private class ParamConfig
        //{
        //    public int BuyOrSell { get; set; }
        //    public int BinSize { get; set; }
        //    public int WindowBuy { get; set; }
        //    public double MinDiffBuy { get; set; }
        //    public double MaxDiffBuy { get; set; }
        //    public double MinValueBuy { get; set; }
        //    public double MaxValueBuy { get; set; }
        //    public double CloseBuy { get; set; }
        //    public decimal StopBuy { get; set; }
        //    public int WindowSell { get; set; }
        //    public double MinDiffSell { get; set; }
        //    public double MaxDiffSell { get; set; }
        //    public double MinValueSell { get; set; }
        //    public double MaxValueSell { get; set; }
        //    public double CloseSell { get; set; }
        //    public decimal StopSell { get; set; }
        //}

        DateTime? LastConnected;
        DateTime? ServerTime;
        decimal LastSolPrice;

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (SolPrice == null || textBox_RSI_State.Text != "" && SolPrice.LastPrice.Value == LastSolPrice)
            {
                if (ErrorMessage != null) textBox_RSI.Text = ErrorMessage;
                return;
            }
            textBox_BTC.Text = BtcPrice.LastPrice.Value.ToString("N1");
            textBox_SOL.Text = SolPrice.LastPrice.Value.ToString("N2");
            string title = $"{SolPrice.LastPrice.Value:N2}   [ {ServerTime:HH:mm:ss} ]";
            if (LastSolPrice == 0)
            {
                numericUpDown1.Value = Math.Round(SolPrice.LastPrice.Value - 3);
                numericUpDown2.Value = Math.Round(SolPrice.LastPrice.Value + 3);
            }
            else if (SolPrice.LastPrice.Value <= numericUpDown1.Value)
            {
                if (LastConnected == null || LastConnected.Value.Minute != ServerTime.Value.Minute)
                {
                    notifyIcon1.ShowBalloonTip(0, $"{SolPrice.LastPrice.Value:N2}\r\n", $"{SolPrice.LastPrice.Value:N2} <= {numericUpDown1.Value:N2}", ToolTipIcon.Info);
                    FlashWindow.Flash(this);
                    PlayFile("down.mp3");
                    LastConnected = ServerTime;
                }
                textBox_SOL.ForeColor = Color.FromArgb(255, 65, 88);
            }
            else if (SolPrice.LastPrice.Value >= numericUpDown2.Value)
            {
                if (LastConnected == null || LastConnected.Value.Minute != ServerTime.Value.Minute)
                {
                    notifyIcon1.ShowBalloonTip(0, $"{SolPrice.LastPrice.Value:N2}\r\n", $"{SolPrice.LastPrice.Value:N2} >= {numericUpDown2.Value:N2}", ToolTipIcon.Info);
                    FlashWindow.Flash(this);
                    PlayFile("up.mp3");
                    LastConnected = ServerTime;
                }
                textBox_SOL.ForeColor = Color.FromArgb(0, 218, 133);
            }
            else
            {
                textBox_SOL.ForeColor = Color.FromArgb(227, 247, 237);
                LastConnected = null;
            }

            if (BinList != null)
            {
                List<TradeBin> reversedBinList = new List<TradeBin>(BinList);
                reversedBinList.Reverse();
                List<TradeBin> list5 = BitMEXApiHelper.LoadBinListFrom1h(4, reversedBinList);
                var quoteList = IndicatorHelper.ToQuote(list5);
                var macdList = quoteList.GetMacd().ToList();
                var rsiList = quoteList.GetRsi(6).ToList();
                textBox_RSI.Text = $"{macdList[macdList.Count - 3].Histogram:F4}  {macdList[macdList.Count - 2].Histogram:F4}  {macdList[macdList.Count - 1].Histogram:F4}";
                textBox_RSI_State.Text = $"{rsiList[rsiList.Count - 2].Rsi:F1}  {rsiList[rsiList.Count - 1].Rsi:F1}";
                if (macdList[macdList.Count - 2].Histogram > 0 && macdList[macdList.Count - 1].Histogram <= 0 || macdList[macdList.Count - 2].Histogram < 0 && macdList[macdList.Count - 1].Histogram >= 0 || rsiList[rsiList.Count - 1].Rsi >= 80 || rsiList[rsiList.Count - 1].Rsi <= 20)
                    title = "* " + title;
            }
            if (ErrorMessage != null) textBox_RSI.Text = ErrorMessage;
            LastSolPrice = SolPrice.LastPrice.Value;
            this.Text = title;
        }

        Thread SyncThread;
        Instrument BtcPrice, SolPrice;
        List<TradeBin> BinList;
        string ErrorMessage;

        public void RunThread()
        {
            if (SyncThread == null)
            {
                SyncThread = new Thread(() => Run());
                SyncThread.Start();
            }
            else
            {
                try
                {
                    SyncThread.Abort();
                }
                catch (Exception) { }
                SyncThread = null;
            }
        }

        public void Run()
        {
            while (true)
            {
                try
                {
                    List<Instrument> instrumentList = (List<Instrument>)JsonConvert.DeserializeObject(HttpGetFromBitMEX("https://www.bitmex.com/api/v1/instrument/active"), typeof(List<Instrument>));
                    foreach (Instrument instrument in instrumentList)
                    {
                        if (instrument.Symbol == "XBTUSD") BtcPrice = instrument;
                        if (instrument.Symbol == "SOLUSD") SolPrice = instrument;
                    }
                    //if (Param == null || LastConnected == null || LastConnected.Value.Minute != ServerTime.Value.Minute)
                    //{
                    //    string url = $"https://raw.githubusercontent.com/maksimg1002/_upload1002/main/sol_0328.txt";
                    //    string paramText = HttpClient2.HttpGet(url);
                    //    Param = JsonConvert.DeserializeObject<ParamConfig>(paramText);
                    //}
                    BinList = (List<TradeBin>)JsonConvert.DeserializeObject(HttpGetFromBitMEX("https://www.bitmex.com/api/v1/trade/bucketed?binSize=1h&symbol=SOLUSD&count=1000&reverse=true&partial=true"), typeof(List<TradeBin>));
                    ErrorMessage = null;
                }
                catch (Exception ex)
                {
                    ErrorMessage = ex.ToString();
                }
                Thread.Sleep(5000);
            }
        }
    }
}
