using IO.Swagger.Model;
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

namespace Valloon.BitMEX
{
    public partial class Main : Form
    {
        private readonly BitMEXApiHelper ApiHelper;

        public Main()
        {
            InitializeComponent();
            Config config = Config.Load();
            ApiHelper = new BitMEXApiHelper(config.ApiKey, config.ApiSecret, config.TestnetMode);
            numericUpDown_Qty.Value = config.Qty;
            numericUpDown_LimitProfit.Value = config.LimitProfit;
            numericUpDown_StopMarket.Value = config.StopMarket;
        }

        private void Main_Load(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            this.TopMost = ((CheckBox)sender).Checked;
        }

        private void button_Buy_Click(object sender, EventArgs e)
        {
            label_Status.Text = "Ready.";
            try
            {
                ApiHelper.OrderNewMarket("Buy", (int)numericUpDown_Qty.Value);
                Position position = ApiHelper.GetPosition();
                if (numericUpDown_StopMarket.Value > 0)
                    ApiHelper.OrderNewStopMarketClose("Sell", (int)(position.AvgEntryPrice.Value - numericUpDown_StopMarket.Value));
                if (numericUpDown_LimitProfit.Value > 0)
                    ApiHelper.OrderNewLimitClose("Sell", (int)(position.AvgEntryPrice.Value + numericUpDown_LimitProfit.Value));
                label_Status.Text = "Succeed.";
            }
            catch (Exception ex)
            {
                label_Status.Text = ex.Message;
            }
        }

        private void button_Sell_Click(object sender, EventArgs e)
        {
            label_Status.Text = "Ready.";
            try
            {
                ApiHelper.OrderNewMarket("Sell", (int)numericUpDown_Qty.Value);
                Position position = ApiHelper.GetPosition();
                if (numericUpDown_StopMarket.Value > 0)
                    ApiHelper.OrderNewStopMarketClose("Buy", (int)(position.AvgEntryPrice.Value + numericUpDown_StopMarket.Value));
                if (numericUpDown_LimitProfit.Value > 0)
                    ApiHelper.OrderNewLimitClose("Buy", (int)(position.AvgEntryPrice.Value - numericUpDown_LimitProfit.Value));
                label_Status.Text = "Succeed.";
            }
            catch (Exception ex)
            {
                label_Status.Text = ex.Message;
            }
        }
    }
}
