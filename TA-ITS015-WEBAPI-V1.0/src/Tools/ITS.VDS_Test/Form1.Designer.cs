namespace ITS.VDS_Test
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
            components = new System.ComponentModel.Container();
            button1 = new Button();
            button2 = new Button();
            button3 = new Button();
            tmrSend = new System.Windows.Forms.Timer(components);
            label1 = new Label();
            textBox1 = new TextBox();
            button4 = new Button();
            label2 = new Label();
            textBox2 = new TextBox();
            label3 = new Label();
            label4 = new Label();
            txtShort = new TextBox();
            label5 = new Label();
            txtMedium = new TextBox();
            label6 = new Label();
            txtLong = new TextBox();
            label7 = new Label();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(180, 224);
            button1.Name = "button1";
            button1.Size = new Size(75, 23);
            button1.TabIndex = 0;
            button1.Text = "Run";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // button2
            // 
            button2.Location = new Point(261, 224);
            button2.Name = "button2";
            button2.Size = new Size(75, 23);
            button2.TabIndex = 1;
            button2.Text = "Stop";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // button3
            // 
            button3.Location = new Point(107, 12);
            button3.Name = "button3";
            button3.Size = new Size(114, 23);
            button3.TabIndex = 2;
            button3.Text = "Reload config data";
            button3.UseVisualStyleBackColor = true;
            button3.Click += button3_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 227);
            label1.Name = "label1";
            label1.Size = new Size(88, 15);
            label1.TabIndex = 3;
            label1.Text = "Time send (ms)";
            // 
            // textBox1
            // 
            textBox1.Location = new Point(106, 224);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(68, 23);
            textBox1.TabIndex = 4;
            textBox1.Text = "1500";
            // 
            // button4
            // 
            button4.Location = new Point(252, 45);
            button4.Name = "button4";
            button4.Size = new Size(75, 23);
            button4.TabIndex = 5;
            button4.Text = "Insert One";
            button4.UseVisualStyleBackColor = true;
            button4.Click += button4_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 54);
            label2.Name = "label2";
            label2.Size = new Size(67, 15);
            label2.TabIndex = 6;
            label2.Text = "Cam Name";
            label2.TextAlign = ContentAlignment.TopCenter;
            // 
            // textBox2
            // 
            textBox2.Location = new Point(121, 46);
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(100, 23);
            textBox2.TabIndex = 7;
            textBox2.Text = "CCTV|VDS";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 202);
            label3.Name = "label3";
            label3.Size = new Size(33, 15);
            label3.TabIndex = 8;
            label3.Text = "Auto";
            label3.TextAlign = ContentAlignment.TopCenter;
            label3.Click += label3_Click;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(12, 83);
            label4.Name = "label4";
            label4.Size = new Size(44, 15);
            label4.TabIndex = 9;
            label4.Text = "Vehicle";
            label4.TextAlign = ContentAlignment.TopCenter;
            // 
            // txtShort
            // 
            txtShort.Location = new Point(71, 106);
            txtShort.Name = "txtShort";
            txtShort.Size = new Size(103, 23);
            txtShort.TabIndex = 11;
            txtShort.Text = "short";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(12, 109);
            label5.Name = "label5";
            label5.Size = new Size(40, 15);
            label5.TabIndex = 10;
            label5.Text = "Type 1";
            // 
            // txtMedium
            // 
            txtMedium.Location = new Point(71, 135);
            txtMedium.Name = "txtMedium";
            txtMedium.Size = new Size(103, 23);
            txtMedium.TabIndex = 13;
            txtMedium.Text = "medium";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(12, 138);
            label6.Name = "label6";
            label6.Size = new Size(40, 15);
            label6.TabIndex = 12;
            label6.Text = "Type 2";
            // 
            // txtLong
            // 
            txtLong.Location = new Point(71, 164);
            txtLong.Name = "txtLong";
            txtLong.Size = new Size(104, 23);
            txtLong.TabIndex = 15;
            txtLong.Text = "long";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(13, 167);
            label7.Name = "label7";
            label7.Size = new Size(40, 15);
            label7.TabIndex = 14;
            label7.Text = "Type 3";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(339, 259);
            Controls.Add(txtLong);
            Controls.Add(label7);
            Controls.Add(txtMedium);
            Controls.Add(label6);
            Controls.Add(txtShort);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(textBox2);
            Controls.Add(label2);
            Controls.Add(button4);
            Controls.Add(textBox1);
            Controls.Add(label1);
            Controls.Add(button3);
            Controls.Add(button2);
            Controls.Add(button1);
            Name = "Form1";
            Text = "VDS Test";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button button1;
        private Button button2;
        private Button button3;
        private System.Windows.Forms.Timer tmrSend;
        private Label label1;
        private TextBox textBox1;
        private Button button4;
        private Label label2;
        private TextBox textBox2;
        private Label label3;
        private Label label4;
        private TextBox txtShort;
        private Label label5;
        private TextBox txtMedium;
        private Label label6;
        private TextBox txtLong;
        private Label label7;
    }
}
