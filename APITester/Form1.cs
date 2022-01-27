﻿using IO.Swagger.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Valloon.BitMEX
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private BitMEXApiHelper GetApiHelper()
        {
            string apiKey = textBox_ApiKey.Text.Trim();
            string apiSecret = textBox_ApiSecret.Text.Trim();
            BitMEXApiHelper apiHelper = new BitMEXApiHelper(apiKey, apiSecret, false);
            return apiHelper;
        }

        private void button_ApiKey_Click(object sender, EventArgs e)
        {
            try
            {
                var apiHelper = GetApiHelper();
                List<APIKey> list = apiHelper.GetApiKey();
                textBox_Result.Text = JArray.FromObject(list).ToString();
            }
            catch (Exception ex)
            {
                textBox_Result.Text = ex.Message;
            }
        }

        private void button_ApiKeyAll_Click(object sender, EventArgs e)
        {
            string text = textBox_Result.Text;
            //string[] lineArray = text.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            //string result = "";
            //foreach (string line in lineArray)
            //{
            //    string one;
            //    try
            //    {
            //        string[] words = line.Trim().Split('\t');
            //        string apiKey = words[2];
            //        string apiSecret = words[3];
            //        BitMEXApiHelper apiHelper = new BitMEXApiHelper(apiKey, apiSecret, false);
            //        List<APIKey> list = apiHelper.GetApiKey();
            //        one = JArray.FromObject(list).ToString();
            //    }catch(Exception ex)
            //    {
            //        one = ex.Message;
            //    }
            //    Console.WriteLine(one);
            //    Console.WriteLine();
            //    result += one + "\r\n\r\n";
            //}
            JObject root = JObject.Parse(text);
            JArray resultArray = (JArray)root["result"];
            string result = "";
            foreach (JToken j in resultArray)
            {
                string apiKey = (string)j["api"];
                string apiSecret = (string)j["secret_key"];
                string one;
                try
                {
                    BitMEXApiHelper apiHelper = new BitMEXApiHelper(apiKey, apiSecret, false);
                    List<APIKey> list = apiHelper.GetApiKey();
                    one = JArray.FromObject(list).ToString();
                }
                catch (Exception ex)
                {
                    one = ex.Message;
                }
                Console.WriteLine(one);
                Console.WriteLine();
                result += one + "\r\n\r\n";
            }
            textBox_Result.Text = result;
        }

        private void button_User_Click(object sender, EventArgs e)
        {
            var apiHelper = GetApiHelper();
            User user = apiHelper.GetUser();
            textBox_Result.Text = JObject.FromObject(user).ToString();
        }

        private void button_Wallet_Click(object sender, EventArgs e)
        {
            var apiHelper = GetApiHelper();
            Wallet w = apiHelper.GetWallet();
            textBox_Result.Text = JObject.FromObject(w).ToString();
        }

        private void button_Chat_Click(object sender, EventArgs e)
        {
            var apiHelper = GetApiHelper();
            string message = textBox_Result.Text;
            textBox_Result.Text = message + "\r\n\t\n" + apiHelper.SendChat(message).ToString();
        }

        private void button_Chat2_Click(object sender, EventArgs e)
        {
            var apiHelper = GetApiHelper();
            string message = textBox_Result.Text;
            textBox_Result.Text = message + "\r\n\t\n" + apiHelper.SendChat(message, 2).ToString();
        }

        private void button_Chat3_Click(object sender, EventArgs e)
        {
            var apiHelper = GetApiHelper();
            string message = textBox_Result.Text;
            textBox_Result.Text = message + "\r\n\t\n" + apiHelper.SendChat(message, 3).ToString();
        }

        private void button_Chat6_Click(object sender, EventArgs e)
        {
            var apiHelper = GetApiHelper();
            string message = textBox_Result.Text;
            textBox_Result.Text = message + "\r\n\t\n" + apiHelper.SendChat(message, 6).ToString();
        }

        private void button_Chat7_Click(object sender, EventArgs e)
        {
            var apiHelper = GetApiHelper();
            string message = textBox_Result.Text;
            textBox_Result.Text = message + "\r\n\t\n" + apiHelper.SendChat(message, 7).ToString();
        }

    }
}
