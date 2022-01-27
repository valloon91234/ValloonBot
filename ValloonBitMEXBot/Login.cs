using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ValloonBitMEXBot.Properties;

namespace Valloon.BitMEX
{
    public partial class Login : Form
    {
        public Login()
        {
            InitializeComponent();
            //textBox_Email.Text = "v91234@testnet.com";
            //textBox_License.Text = "123";
        }

        private void button_Enter_Click(object sender, EventArgs e)
        {
            string email = textBox_Email.Text.Trim();
            string license = textBox_License.Text;
            if (string.IsNullOrWhiteSpace(email))
            {
                textBox_Email.Focus();
            }
            else if (string.IsNullOrWhiteSpace(license))
            {
                textBox_License.Focus();
            }
            else
            {
                Config.Email = email;
                Config.License = license;
                BackendClient.Ping();
                if (Config.Active)
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show(Config.Message, "BitMEX Bot", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void Login_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) Close();
        }

        private void Login_Load(object sender, EventArgs e)
        {
            if (Program.GOLD_VERSION)
            {
                pictureBox1.Image = Resources.bitmex_gold;
            }
            else
            {
                pictureBox1.Image = Resources.bitmex;
            }
        }
    }
}
