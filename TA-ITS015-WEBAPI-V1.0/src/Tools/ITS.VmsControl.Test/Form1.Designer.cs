namespace ITS.VmsControl.Test
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            cbSdk = new ComboBox();
            txtIp = new TextBox();
            txtPort = new TextBox();
            label1 = new Label();
            label2 = new Label();
            btnConnect = new Button();
            btnGetData = new Button();
            txtInfo = new TextBox();
            Connect = new Label();
            txtConnect = new Label();
            comboBox1 = new ComboBox();
            txtImage = new TextBox();
            btnSend = new Button();
            SuspendLayout();
            // 
            // cbSdk
            // 
            cbSdk.FormattingEnabled = true;
            cbSdk.Items.AddRange(new object[] { "chainzone", "dysten" });
            cbSdk.Location = new Point(42, 12);
            cbSdk.Name = "cbSdk";
            cbSdk.Size = new Size(121, 23);
            cbSdk.TabIndex = 0;
            // 
            // txtIp
            // 
            txtIp.Location = new Point(108, 60);
            txtIp.Name = "txtIp";
            txtIp.Size = new Size(205, 23);
            txtIp.TabIndex = 1;
            txtIp.Text = "127.0.0.1";
            // 
            // txtPort
            // 
            txtPort.Location = new Point(108, 110);
            txtPort.Name = "txtPort";
            txtPort.Size = new Size(205, 23);
            txtPort.TabIndex = 2;
            txtPort.Text = "80";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(27, 68);
            label1.Name = "label1";
            label1.Size = new Size(17, 15);
            label1.TabIndex = 3;
            label1.Text = "Ip";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(27, 118);
            label2.Name = "label2";
            label2.Size = new Size(29, 15);
            label2.TabIndex = 4;
            label2.Text = "Port";
            // 
            // btnConnect
            // 
            btnConnect.Location = new Point(377, 42);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(75, 23);
            btnConnect.TabIndex = 5;
            btnConnect.Text = "Connect";
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Click += btnConnect_Click;
            // 
            // btnGetData
            // 
            btnGetData.Location = new Point(377, 180);
            btnGetData.Name = "btnGetData";
            btnGetData.Size = new Size(75, 23);
            btnGetData.TabIndex = 6;
            btnGetData.Text = "Get data";
            btnGetData.UseVisualStyleBackColor = true;
            btnGetData.Click += btnGetData_Click;
            // 
            // txtInfo
            // 
            txtInfo.Location = new Point(21, 260);
            txtInfo.Multiline = true;
            txtInfo.Name = "txtInfo";
            txtInfo.ScrollBars = ScrollBars.Vertical;
            txtInfo.Size = new Size(417, 178);
            txtInfo.TabIndex = 7;
            // 
            // Connect
            // 
            Connect.AutoSize = true;
            Connect.Location = new Point(27, 146);
            Connect.Name = "Connect";
            Connect.Size = new Size(52, 15);
            Connect.TabIndex = 8;
            Connect.Text = "Connect";
            // 
            // txtConnect
            // 
            txtConnect.AutoSize = true;
            txtConnect.Location = new Point(108, 146);
            txtConnect.Name = "txtConnect";
            txtConnect.Size = new Size(79, 15);
            txtConnect.TabIndex = 9;
            txtConnect.Text = "Disconnected";
            // 
            // comboBox1
            // 
            comboBox1.FormattingEnabled = true;
            comboBox1.Items.AddRange(new object[] { "Brightness", "Size", "Auto" });
            comboBox1.Location = new Point(250, 180);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(121, 23);
            comboBox1.TabIndex = 10;
            comboBox1.Text = "Brightness";
            // 
            // txtImage
            // 
            txtImage.Location = new Point(88, 232);
            txtImage.Name = "txtImage";
            txtImage.Size = new Size(287, 23);
            txtImage.TabIndex = 11;
            // 
            // btnSend
            // 
            btnSend.Location = new Point(381, 231);
            btnSend.Name = "btnSend";
            btnSend.Size = new Size(75, 23);
            btnSend.TabIndex = 12;
            btnSend.Text = "Send Image";
            btnSend.UseVisualStyleBackColor = true;
            btnSend.Click += btnSend_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(468, 450);
            Controls.Add(btnSend);
            Controls.Add(txtImage);
            Controls.Add(comboBox1);
            Controls.Add(txtConnect);
            Controls.Add(Connect);
            Controls.Add(txtInfo);
            Controls.Add(btnGetData);
            Controls.Add(btnConnect);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(txtPort);
            Controls.Add(txtIp);
            Controls.Add(cbSdk);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ComboBox cbSdk;
        private TextBox txtIp;
        private TextBox txtPort;
        private Label label1;
        private Label label2;
        private Button btnConnect;
        private Button btnGetData;
        private TextBox txtInfo;
        private Label Connect;
        private Label txtConnect;
        private ComboBox comboBox1;
        private TextBox txtImage;
        private Button btnSend;
    }
}
