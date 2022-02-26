namespace Valloon.BitMEX
{
    partial class Main
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.label2 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.numericUpDown_Qty = new System.Windows.Forms.NumericUpDown();
            this.button_Buy = new System.Windows.Forms.Button();
            this.button_Sell = new System.Windows.Forms.Button();
            this.numericUpDown_LimitProfit = new System.Windows.Forms.NumericUpDown();
            this.numericUpDown_StopMarket = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.label_Status = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_Qty)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_LimitProfit)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_StopMarket)).BeginInit();
            this.SuspendLayout();
            // 
            // imageList1
            // 
            this.imageList1.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            this.imageList1.ImageSize = new System.Drawing.Size(16, 16);
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.label2.Location = new System.Drawing.Point(14, 14);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(32, 19);
            this.label2.TabIndex = 8;
            this.label2.Text = "Qty";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.label4.Location = new System.Drawing.Point(3, 111);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(79, 19);
            this.label4.TabIndex = 9;
            this.label4.Text = "Limit Profit";
            // 
            // numericUpDown_Qty
            // 
            this.numericUpDown_Qty.Location = new System.Drawing.Point(63, 12);
            this.numericUpDown_Qty.Maximum = new decimal(new int[] {
            100000000,
            0,
            0,
            0});
            this.numericUpDown_Qty.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown_Qty.Name = "numericUpDown_Qty";
            this.numericUpDown_Qty.Size = new System.Drawing.Size(99, 27);
            this.numericUpDown_Qty.TabIndex = 18;
            this.numericUpDown_Qty.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numericUpDown_Qty.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            // 
            // button_Buy
            // 
            this.button_Buy.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(86)))), ((int)(((byte)(188)))), ((int)(((byte)(118)))));
            this.button_Buy.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_Buy.ForeColor = System.Drawing.Color.White;
            this.button_Buy.Location = new System.Drawing.Point(7, 51);
            this.button_Buy.Name = "button_Buy";
            this.button_Buy.Size = new System.Drawing.Size(160, 40);
            this.button_Buy.TabIndex = 19;
            this.button_Buy.Text = "Buy / Long";
            this.button_Buy.UseVisualStyleBackColor = false;
            this.button_Buy.Click += new System.EventHandler(this.button_Buy_Click);
            // 
            // button_Sell
            // 
            this.button_Sell.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(229)))), ((int)(((byte)(96)))), ((int)(((byte)(59)))));
            this.button_Sell.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_Sell.ForeColor = System.Drawing.Color.White;
            this.button_Sell.Location = new System.Drawing.Point(178, 51);
            this.button_Sell.Name = "button_Sell";
            this.button_Sell.Size = new System.Drawing.Size(160, 40);
            this.button_Sell.TabIndex = 20;
            this.button_Sell.Text = "Sell / Short";
            this.button_Sell.UseVisualStyleBackColor = false;
            this.button_Sell.Click += new System.EventHandler(this.button_Sell_Click);
            // 
            // numericUpDown_LimitProfit
            // 
            this.numericUpDown_LimitProfit.Location = new System.Drawing.Point(99, 105);
            this.numericUpDown_LimitProfit.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numericUpDown_LimitProfit.Name = "numericUpDown_LimitProfit";
            this.numericUpDown_LimitProfit.Size = new System.Drawing.Size(68, 27);
            this.numericUpDown_LimitProfit.TabIndex = 21;
            this.numericUpDown_LimitProfit.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numericUpDown_LimitProfit.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            // 
            // numericUpDown_StopMarket
            // 
            this.numericUpDown_StopMarket.Location = new System.Drawing.Point(272, 105);
            this.numericUpDown_StopMarket.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numericUpDown_StopMarket.Name = "numericUpDown_StopMarket";
            this.numericUpDown_StopMarket.Size = new System.Drawing.Size(66, 27);
            this.numericUpDown_StopMarket.TabIndex = 23;
            this.numericUpDown_StopMarket.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numericUpDown_StopMarket.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.label1.Location = new System.Drawing.Point(174, 110);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(86, 19);
            this.label1.TabIndex = 22;
            this.label1.Text = "Stop Market";
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.ForeColor = System.Drawing.Color.Silver;
            this.checkBox1.Location = new System.Drawing.Point(291, 8);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(51, 23);
            this.checkBox1.TabIndex = 24;
            this.checkBox1.Text = "Top";
            this.checkBox1.UseVisualStyleBackColor = true;
            this.checkBox1.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // label_Status
            // 
            this.label_Status.AutoSize = true;
            this.label_Status.ForeColor = System.Drawing.Color.Lime;
            this.label_Status.Location = new System.Drawing.Point(3, 145);
            this.label_Status.Name = "label_Status";
            this.label_Status.Size = new System.Drawing.Size(52, 19);
            this.label_Status.TabIndex = 25;
            this.label_Status.Text = "Ready.";
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 19F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(344, 171);
            this.Controls.Add(this.label_Status);
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.numericUpDown_StopMarket);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.numericUpDown_LimitProfit);
            this.Controls.Add(this.button_Sell);
            this.Controls.Add(this.button_Buy);
            this.Controls.Add(this.numericUpDown_Qty);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label2);
            this.Font = new System.Drawing.Font("Calibri", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.Name = "Main";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "BitMEX";
            this.Load += new System.EventHandler(this.Main_Load);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_Qty)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_LimitProfit)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_StopMarket)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown numericUpDown_Qty;
        private System.Windows.Forms.Button button_Buy;
        private System.Windows.Forms.Button button_Sell;
        private System.Windows.Forms.NumericUpDown numericUpDown_LimitProfit;
        private System.Windows.Forms.NumericUpDown numericUpDown_StopMarket;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.Label label_Status;
    }
}