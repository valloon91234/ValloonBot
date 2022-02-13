using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ValloonBitMEXBot.Properties;

namespace Valloon.Trading
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            if (Program.GOLD_VERSION)
            {
                pictureBox1.Image = Resources.bitmex_gold;
                label_Caption.ForeColor = Color.FromArgb(143, 122, 7);
                label_Balance.ForeColor = Color.FromArgb(143, 122, 7);
                label_ProfitToday.ForeColor = Color.FromArgb(143, 122, 7);
                label_Opened.ForeColor = Color.FromArgb(143, 122, 7);
            }
            else
            {
                pictureBox1.Image = Resources.bitmex;
                label_Caption.ForeColor = Color.FromArgb(0, 192, 192);
                label_Balance.ForeColor = Color.FromArgb(0, 192, 192);
                label_ProfitToday.ForeColor = Color.FromArgb(0, 192, 192);
                label_Opened.ForeColor = Color.FromArgb(0, 192, 192);
            }
            Login login = new Login();
            var loginResult = login.ShowDialog();
            if (loginResult == DialogResult.OK)
            {
                Thread thread = new Thread(() => InertiaStrategy.Run(this));
                thread.Start();
            }
            else
            {
                Close();
            }
        }

        delegate void SetCaptionCallback(string text);

        public void SetCaption(string text)
        {
            if (label_Caption.InvokeRequired)
            {
                try
                {
                    SetCaptionCallback d = new SetCaptionCallback(SetCaption);
                    this.Invoke(d, new object[] { text });
                }
                catch { }
            }
            else
            {
                label_Caption.Text = text;
            }
        }

        delegate void SetMessageCallback(string text);

        public void SetMessage(string text)
        {
            if (label_Message.InvokeRequired)
            {
                try
                {
                    SetMessageCallback d = new SetMessageCallback(SetMessage);
                    this.Invoke(d, new object[] { text });
                }
                catch { }
            }
            else
            {
                label_Message.Text = text;
            }
        }

        delegate void SetBalanceCallback(decimal value);

        public void SetBalance(decimal value)
        {
            if (label_Balance.InvokeRequired)
            {
                try
                {
                    SetBalanceCallback d = new SetBalanceCallback(SetBalance);
                    this.Invoke(d, new object[] { value });
                }
                catch { }
            }
            else
            {
                label_Balance.Text = $"{value:N4} XBT";
            }
        }

        delegate void SetProfitTodayCallback(decimal value, decimal percent);

        public void SetProfitToday(decimal value, decimal percent)
        {
            if (label_ProfitToday.InvokeRequired)
            {
                try
                {
                    SetProfitTodayCallback d = new SetProfitTodayCallback(SetProfitToday);
                    this.Invoke(d, new object[] { value, percent });
                }
                catch { }
            }
            else
            {
                label_ProfitToday.Text = $"{value:N4} XBT / {percent:N2}%";
            }
        }

        delegate void SetOpenedCallback(decimal value, decimal percent);

        public void SetOpened(decimal value, decimal percent)
        {
            if (label_Opened.InvokeRequired)
            {
                try
                {
                    SetOpenedCallback d = new SetOpenedCallback(SetOpened);
                    this.Invoke(d, new object[] { value, percent });
                }
                catch { }
            }
            else
            {
                label_Opened.Text = $"{value:N4} XBT / {percent:N2}%";
            }
        }

        delegate void SetLiqPriceCallback(decimal value);

        public void SetLiqPrice(decimal value)
        {
            if (label_LiqPrice.InvokeRequired)
            {
                try
                {
                    SetLiqPriceCallback d = new SetLiqPriceCallback(SetLiqPrice);
                    this.Invoke(d, new object[] { value });
                }
                catch { }
            }
            else
            {
                label_LiqPrice.Text = $"{value:F2}";
            }
        }

        private void Main_FormClosed(object sender, FormClosedEventArgs e)
        {
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
    }
}
