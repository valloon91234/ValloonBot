namespace Valloon.Trading
{
    partial class Form1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.textBox_ApiSecret = new System.Windows.Forms.TextBox();
            this.textBox_ApiKey = new System.Windows.Forms.TextBox();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.textBox_Result = new System.Windows.Forms.TextBox();
            this.button_Margin = new System.Windows.Forms.Button();
            this.button_Summary = new System.Windows.Forms.Button();
            this.button_History = new System.Windows.Forms.Button();
            this.button_ClosePosition = new System.Windows.Forms.Button();
            this.button_ViewAll = new System.Windows.Forms.Button();
            this.button_CancelAllOrders = new System.Windows.Forms.Button();
            this.button_User = new System.Windows.Forms.Button();
            this.button_Chat7 = new System.Windows.Forms.Button();
            this.button_Chat6 = new System.Windows.Forms.Button();
            this.button_Chat3 = new System.Windows.Forms.Button();
            this.button_Chat2 = new System.Windows.Forms.Button();
            this.button_Chat = new System.Windows.Forms.Button();
            this.button_Wallet = new System.Windows.Forms.Button();
            this.button_ApiKeyAll = new System.Windows.Forms.Button();
            this.button_ApiKey = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer1.IsSplitterFixed = true;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.textBox_ApiSecret);
            this.splitContainer1.Panel1.Controls.Add(this.textBox_ApiKey);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
            this.splitContainer1.Size = new System.Drawing.Size(1033, 577);
            this.splitContainer1.SplitterDistance = 34;
            this.splitContainer1.TabIndex = 3;
            // 
            // textBox_ApiSecret
            // 
            this.textBox_ApiSecret.Location = new System.Drawing.Point(276, 12);
            this.textBox_ApiSecret.Name = "textBox_ApiSecret";
            this.textBox_ApiSecret.Size = new System.Drawing.Size(457, 25);
            this.textBox_ApiSecret.TabIndex = 3;
            this.textBox_ApiSecret.Text = "8rpTnZKpUK_KRe3ez-wA2CvK6gYLicMcYRlG7P5WDwvA0c0-";
            // 
            // textBox_ApiKey
            // 
            this.textBox_ApiKey.Location = new System.Drawing.Point(12, 12);
            this.textBox_ApiKey.Name = "textBox_ApiKey";
            this.textBox_ApiKey.Size = new System.Drawing.Size(258, 25);
            this.textBox_ApiKey.TabIndex = 2;
            this.textBox_ApiKey.Text = "CnrbNq6BknhT8jG0v2mj7SmN";
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.textBox_Result);
            this.splitContainer2.Panel1.Padding = new System.Windows.Forms.Padding(10, 10, 3, 10);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.button_Margin);
            this.splitContainer2.Panel2.Controls.Add(this.button_Summary);
            this.splitContainer2.Panel2.Controls.Add(this.button_History);
            this.splitContainer2.Panel2.Controls.Add(this.button_ClosePosition);
            this.splitContainer2.Panel2.Controls.Add(this.button_ViewAll);
            this.splitContainer2.Panel2.Controls.Add(this.button_CancelAllOrders);
            this.splitContainer2.Panel2.Controls.Add(this.button_User);
            this.splitContainer2.Panel2.Controls.Add(this.button_Chat7);
            this.splitContainer2.Panel2.Controls.Add(this.button_Chat6);
            this.splitContainer2.Panel2.Controls.Add(this.button_Chat3);
            this.splitContainer2.Panel2.Controls.Add(this.button_Chat2);
            this.splitContainer2.Panel2.Controls.Add(this.button_Chat);
            this.splitContainer2.Panel2.Controls.Add(this.button_Wallet);
            this.splitContainer2.Panel2.Controls.Add(this.button_ApiKeyAll);
            this.splitContainer2.Panel2.Controls.Add(this.button_ApiKey);
            this.splitContainer2.Size = new System.Drawing.Size(1033, 539);
            this.splitContainer2.SplitterDistance = 736;
            this.splitContainer2.TabIndex = 0;
            // 
            // textBox_Result
            // 
            this.textBox_Result.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox_Result.Location = new System.Drawing.Point(10, 10);
            this.textBox_Result.MaxLength = 9999999;
            this.textBox_Result.Multiline = true;
            this.textBox_Result.Name = "textBox_Result";
            this.textBox_Result.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBox_Result.Size = new System.Drawing.Size(723, 519);
            this.textBox_Result.TabIndex = 3;
            // 
            // button_Margin
            // 
            this.button_Margin.Location = new System.Drawing.Point(3, 150);
            this.button_Margin.Name = "button_Margin";
            this.button_Margin.Size = new System.Drawing.Size(119, 37);
            this.button_Margin.TabIndex = 16;
            this.button_Margin.Text = "Margin";
            this.button_Margin.UseVisualStyleBackColor = true;
            this.button_Margin.Click += new System.EventHandler(this.button_Margin_Click);
            // 
            // button_Summary
            // 
            this.button_Summary.Location = new System.Drawing.Point(3, 279);
            this.button_Summary.Name = "button_Summary";
            this.button_Summary.Size = new System.Drawing.Size(119, 37);
            this.button_Summary.TabIndex = 15;
            this.button_Summary.Text = "Summary";
            this.button_Summary.UseVisualStyleBackColor = true;
            this.button_Summary.Click += new System.EventHandler(this.button_Summary_Click);
            // 
            // button_History
            // 
            this.button_History.Location = new System.Drawing.Point(3, 236);
            this.button_History.Name = "button_History";
            this.button_History.Size = new System.Drawing.Size(119, 37);
            this.button_History.TabIndex = 14;
            this.button_History.Text = "History";
            this.button_History.UseVisualStyleBackColor = true;
            this.button_History.Click += new System.EventHandler(this.button_History_Click);
            // 
            // button_ClosePosition
            // 
            this.button_ClosePosition.Location = new System.Drawing.Point(128, 107);
            this.button_ClosePosition.Name = "button_ClosePosition";
            this.button_ClosePosition.Size = new System.Drawing.Size(155, 37);
            this.button_ClosePosition.TabIndex = 13;
            this.button_ClosePosition.Text = "Close Position";
            this.button_ClosePosition.UseVisualStyleBackColor = true;
            this.button_ClosePosition.Click += new System.EventHandler(this.button_ClosePosition_Click);
            // 
            // button_ViewAll
            // 
            this.button_ViewAll.Location = new System.Drawing.Point(128, 10);
            this.button_ViewAll.Name = "button_ViewAll";
            this.button_ViewAll.Size = new System.Drawing.Size(155, 37);
            this.button_ViewAll.TabIndex = 12;
            this.button_ViewAll.Text = "Active Orders";
            this.button_ViewAll.UseVisualStyleBackColor = true;
            this.button_ViewAll.Click += new System.EventHandler(this.button_ViewAll_Click);
            // 
            // button_CancelAllOrders
            // 
            this.button_CancelAllOrders.Location = new System.Drawing.Point(128, 53);
            this.button_CancelAllOrders.Name = "button_CancelAllOrders";
            this.button_CancelAllOrders.Size = new System.Drawing.Size(155, 37);
            this.button_CancelAllOrders.TabIndex = 11;
            this.button_CancelAllOrders.Text = "Clear Orders";
            this.button_CancelAllOrders.UseVisualStyleBackColor = true;
            this.button_CancelAllOrders.Click += new System.EventHandler(this.button_CancelAllOrders_Click);
            // 
            // button_User
            // 
            this.button_User.Location = new System.Drawing.Point(3, 107);
            this.button_User.Name = "button_User";
            this.button_User.Size = new System.Drawing.Size(119, 37);
            this.button_User.TabIndex = 8;
            this.button_User.Text = "User";
            this.button_User.UseVisualStyleBackColor = true;
            this.button_User.Click += new System.EventHandler(this.button_User_Click);
            // 
            // button_Chat7
            // 
            this.button_Chat7.Location = new System.Drawing.Point(162, 493);
            this.button_Chat7.Name = "button_Chat7";
            this.button_Chat7.Size = new System.Drawing.Size(119, 37);
            this.button_Chat7.TabIndex = 7;
            this.button_Chat7.Text = "Chat 7";
            this.button_Chat7.UseVisualStyleBackColor = true;
            this.button_Chat7.Click += new System.EventHandler(this.button_Chat7_Click);
            // 
            // button_Chat6
            // 
            this.button_Chat6.Location = new System.Drawing.Point(162, 450);
            this.button_Chat6.Name = "button_Chat6";
            this.button_Chat6.Size = new System.Drawing.Size(119, 37);
            this.button_Chat6.TabIndex = 6;
            this.button_Chat6.Text = "Chat 6";
            this.button_Chat6.UseVisualStyleBackColor = true;
            this.button_Chat6.Click += new System.EventHandler(this.button_Chat6_Click);
            // 
            // button_Chat3
            // 
            this.button_Chat3.Location = new System.Drawing.Point(162, 407);
            this.button_Chat3.Name = "button_Chat3";
            this.button_Chat3.Size = new System.Drawing.Size(119, 37);
            this.button_Chat3.TabIndex = 5;
            this.button_Chat3.Text = "Chat 3";
            this.button_Chat3.UseVisualStyleBackColor = true;
            this.button_Chat3.Click += new System.EventHandler(this.button_Chat3_Click);
            // 
            // button_Chat2
            // 
            this.button_Chat2.Location = new System.Drawing.Point(162, 364);
            this.button_Chat2.Name = "button_Chat2";
            this.button_Chat2.Size = new System.Drawing.Size(119, 37);
            this.button_Chat2.TabIndex = 4;
            this.button_Chat2.Text = "Chat 2";
            this.button_Chat2.UseVisualStyleBackColor = true;
            this.button_Chat2.Click += new System.EventHandler(this.button_Chat2_Click);
            // 
            // button_Chat
            // 
            this.button_Chat.Location = new System.Drawing.Point(162, 321);
            this.button_Chat.Name = "button_Chat";
            this.button_Chat.Size = new System.Drawing.Size(119, 37);
            this.button_Chat.TabIndex = 3;
            this.button_Chat.Text = "Chat";
            this.button_Chat.UseVisualStyleBackColor = true;
            this.button_Chat.Click += new System.EventHandler(this.button_Chat_Click);
            // 
            // button_Wallet
            // 
            this.button_Wallet.Location = new System.Drawing.Point(3, 193);
            this.button_Wallet.Name = "button_Wallet";
            this.button_Wallet.Size = new System.Drawing.Size(119, 37);
            this.button_Wallet.TabIndex = 2;
            this.button_Wallet.Text = "Wallet";
            this.button_Wallet.UseVisualStyleBackColor = true;
            this.button_Wallet.Click += new System.EventHandler(this.button_Wallet_Click);
            // 
            // button_ApiKeyAll
            // 
            this.button_ApiKeyAll.Location = new System.Drawing.Point(3, 53);
            this.button_ApiKeyAll.Name = "button_ApiKeyAll";
            this.button_ApiKeyAll.Size = new System.Drawing.Size(119, 37);
            this.button_ApiKeyAll.TabIndex = 1;
            this.button_ApiKeyAll.Text = "APIKey All";
            this.button_ApiKeyAll.UseVisualStyleBackColor = true;
            this.button_ApiKeyAll.Click += new System.EventHandler(this.button_ApiKeyAll_Click);
            // 
            // button_ApiKey
            // 
            this.button_ApiKey.Location = new System.Drawing.Point(3, 10);
            this.button_ApiKey.Name = "button_ApiKey";
            this.button_ApiKey.Size = new System.Drawing.Size(119, 37);
            this.button_ApiKey.TabIndex = 0;
            this.button_ApiKey.Text = "APIKey";
            this.button_ApiKey.UseVisualStyleBackColor = true;
            this.button_ApiKey.Click += new System.EventHandler(this.button_ApiKey_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(1033, 577);
            this.Controls.Add(this.splitContainer1);
            this.Font = new System.Drawing.Font("Consolas", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "Form1";
            this.Text = "BitMEX API Tester";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel1.PerformLayout();
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TextBox textBox_ApiSecret;
        private System.Windows.Forms.TextBox textBox_ApiKey;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.TextBox textBox_Result;
        private System.Windows.Forms.Button button_ApiKey;
        private System.Windows.Forms.Button button_ApiKeyAll;
        private System.Windows.Forms.Button button_Wallet;
        private System.Windows.Forms.Button button_Chat;
        private System.Windows.Forms.Button button_Chat7;
        private System.Windows.Forms.Button button_Chat6;
        private System.Windows.Forms.Button button_Chat3;
        private System.Windows.Forms.Button button_Chat2;
        private System.Windows.Forms.Button button_User;
        private System.Windows.Forms.Button button_CancelAllOrders;
        private System.Windows.Forms.Button button_ViewAll;
        private System.Windows.Forms.Button button_ClosePosition;
        private System.Windows.Forms.Button button_Summary;
        private System.Windows.Forms.Button button_History;
        private System.Windows.Forms.Button button_Margin;
    }
}

