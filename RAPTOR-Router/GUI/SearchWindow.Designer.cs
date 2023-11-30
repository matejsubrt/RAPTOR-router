namespace GUI
{
    partial class SearchWindow
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            button1 = new Button();
            label16 = new Label();
            label15 = new Label();
            label14 = new Label();
            label13 = new Label();
            label12 = new Label();
            label11 = new Label();
            label10 = new Label();
            label9 = new Label();
            label8 = new Label();
            label7 = new Label();
            label6 = new Label();
            label5 = new Label();
            label4 = new Label();
            destStopTextBox = new TextBox();
            label3 = new Label();
            label2 = new Label();
            label1 = new Label();
            trackBar3 = new TrackBar();
            trackBar2 = new TrackBar();
            trackBar1 = new TrackBar();
            departureDatePicker = new DateTimePicker();
            walkingPaceNumericUpDown = new NumericUpDown();
            srcStopTextBox = new TextBox();
            departureTimePicker = new DateTimePicker();
            ((System.ComponentModel.ISupportInitialize)trackBar3).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trackBar2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trackBar1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)walkingPaceNumericUpDown).BeginInit();
            SuspendLayout();
            // 
            // button1
            // 
            button1.BackColor = Color.ForestGreen;
            button1.Font = new Font("Arial", 14.25F, FontStyle.Bold, GraphicsUnit.Point);
            button1.ForeColor = SystemColors.ButtonHighlight;
            button1.Location = new Point(79, 435);
            button1.Name = "button1";
            button1.Size = new Size(99, 38);
            button1.TabIndex = 48;
            button1.Text = "Search";
            button1.UseVisualStyleBackColor = false;
            button1.Click += button1_Click;
            // 
            // label16
            // 
            label16.AutoSize = true;
            label16.Font = new Font("Arial", 8.25F, FontStyle.Italic, GraphicsUnit.Point);
            label16.Location = new Point(389, 403);
            label16.Name = "label16";
            label16.Size = new Size(26, 14);
            label16.TabIndex = 47;
            label16.Text = "Low";
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.Font = new Font("Arial", 8.25F, FontStyle.Italic, GraphicsUnit.Point);
            label15.Location = new Point(191, 402);
            label15.Name = "label15";
            label15.Size = new Size(30, 14);
            label15.TabIndex = 46;
            label15.Text = "High";
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Location = new Point(79, 402);
            label14.Name = "label14";
            label14.Size = new Size(112, 15);
            label14.TabIndex = 45;
            label14.Text = "Walking preference:";
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Font = new Font("Arial", 8.25F, FontStyle.Italic, GraphicsUnit.Point);
            label13.Location = new Point(389, 352);
            label13.Name = "label13";
            label13.Size = new Size(81, 14);
            label13.TabIndex = 44;
            label13.Text = "Least transfers";
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Font = new Font("Arial", 8.25F, FontStyle.Italic, GraphicsUnit.Point);
            label12.Location = new Point(191, 352);
            label12.Name = "label12";
            label12.Size = new Size(72, 14);
            label12.TabIndex = 43;
            label12.Text = "Shortest time";
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new Point(79, 352);
            label11.Name = "label11";
            label11.Size = new Size(99, 15);
            label11.TabIndex = 42;
            label11.Text = "Comfort balance:";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Font = new Font("Arial", 8.25F, FontStyle.Italic, GraphicsUnit.Point);
            label10.Location = new Point(389, 302);
            label10.Name = "label10";
            label10.Size = new Size(31, 14);
            label10.TabIndex = 41;
            label10.Text = "Long";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Font = new Font("Arial", 8.25F, FontStyle.Italic, GraphicsUnit.Point);
            label9.Location = new Point(191, 301);
            label9.Name = "label9";
            label9.Size = new Size(56, 14);
            label9.TabIndex = 40;
            label9.Text = "UltraShort";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(79, 301);
            label8.Name = "label8";
            label8.Size = new Size(78, 15);
            label8.TabIndex = 39;
            label8.Text = "Transfer time:";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(321, 263);
            label7.Name = "label7";
            label7.Size = new Size(50, 15);
            label7.TabIndex = 38;
            label7.Text = "min/km";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(79, 263);
            label6.Name = "label6";
            label6.Size = new Size(81, 15);
            label6.TabIndex = 37;
            label6.Text = "Walking pace:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Arial", 14.25F, FontStyle.Bold, GraphicsUnit.Point);
            label5.Location = new Point(68, 226);
            label5.Name = "label5";
            label5.Size = new Size(93, 22);
            label5.TabIndex = 36;
            label5.Text = "Settings:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Arial Narrow", 12F, FontStyle.Bold, GraphicsUnit.Point);
            label4.Location = new Point(79, 178);
            label4.Name = "label4";
            label4.Size = new Size(102, 20);
            label4.TabIndex = 35;
            label4.Text = "Date and Time:";
            // 
            // destStopTextBox
            // 
            destStopTextBox.Location = new Point(139, 134);
            destStopTextBox.Name = "destStopTextBox";
            destStopTextBox.Size = new Size(234, 23);
            destStopTextBox.TabIndex = 34;
            destStopTextBox.KeyDown += destStopTextBox_KeyDown;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Arial Narrow", 12F, FontStyle.Bold, GraphicsUnit.Point);
            label3.Location = new Point(79, 133);
            label3.Name = "label3";
            label3.Size = new Size(29, 20);
            label3.TabIndex = 33;
            label3.Text = "To:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Arial Narrow", 12F, FontStyle.Bold, GraphicsUnit.Point);
            label2.Location = new Point(79, 91);
            label2.Name = "label2";
            label2.Size = new Size(46, 20);
            label2.TabIndex = 32;
            label2.Text = "From:";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Arial", 20.25F, FontStyle.Bold, GraphicsUnit.Point);
            label1.Location = new Point(68, 40);
            label1.Name = "label1";
            label1.Size = new Size(305, 32);
            label1.TabIndex = 31;
            label1.Text = "Search for connection";
            // 
            // trackBar3
            // 
            trackBar3.Location = new Point(269, 392);
            trackBar3.Maximum = 2;
            trackBar3.Name = "trackBar3";
            trackBar3.Size = new Size(104, 45);
            trackBar3.TabIndex = 30;
            // 
            // trackBar2
            // 
            trackBar2.Location = new Point(269, 341);
            trackBar2.Maximum = 3;
            trackBar2.Name = "trackBar2";
            trackBar2.Size = new Size(104, 45);
            trackBar2.TabIndex = 29;
            trackBar2.Value = 2;
            // 
            // trackBar1
            // 
            trackBar1.Location = new Point(269, 290);
            trackBar1.Maximum = 3;
            trackBar1.Name = "trackBar1";
            trackBar1.Size = new Size(104, 45);
            trackBar1.TabIndex = 28;
            trackBar1.Value = 2;
            // 
            // departureDatePicker
            // 
            departureDatePicker.Format = DateTimePickerFormat.Short;
            departureDatePicker.Location = new Point(191, 178);
            departureDatePicker.Name = "departureDatePicker";
            departureDatePicker.Size = new Size(82, 23);
            departureDatePicker.TabIndex = 27;
            // 
            // walkingPaceNumericUpDown
            // 
            walkingPaceNumericUpDown.Location = new Point(195, 261);
            walkingPaceNumericUpDown.Name = "walkingPaceNumericUpDown";
            walkingPaceNumericUpDown.Size = new Size(120, 23);
            walkingPaceNumericUpDown.TabIndex = 26;
            walkingPaceNumericUpDown.Value = new decimal(new int[] { 12, 0, 0, 0 });
            // 
            // srcStopTextBox
            // 
            srcStopTextBox.Location = new Point(139, 91);
            srcStopTextBox.Name = "srcStopTextBox";
            srcStopTextBox.Size = new Size(232, 23);
            srcStopTextBox.TabIndex = 25;
            srcStopTextBox.KeyDown += srcStopTextBox_KeyDown;
            // 
            // departureTimePicker
            // 
            departureTimePicker.Format = DateTimePickerFormat.Time;
            departureTimePicker.Location = new Point(289, 178);
            departureTimePicker.Name = "departureTimePicker";
            departureTimePicker.Size = new Size(82, 23);
            departureTimePicker.TabIndex = 49;
            // 
            // SearchWindow
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(departureTimePicker);
            Controls.Add(button1);
            Controls.Add(label16);
            Controls.Add(label15);
            Controls.Add(label14);
            Controls.Add(label13);
            Controls.Add(label12);
            Controls.Add(label11);
            Controls.Add(label10);
            Controls.Add(label9);
            Controls.Add(label8);
            Controls.Add(label7);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(destStopTextBox);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(trackBar3);
            Controls.Add(trackBar2);
            Controls.Add(trackBar1);
            Controls.Add(departureDatePicker);
            Controls.Add(walkingPaceNumericUpDown);
            Controls.Add(srcStopTextBox);
            Name = "SearchWindow";
            Size = new Size(800, 500);
            Load += SearchWindow_Load;
            Enter += SearchWindow_Enter;
            KeyDown += SearchWindow_KeyDown;
            ((System.ComponentModel.ISupportInitialize)trackBar3).EndInit();
            ((System.ComponentModel.ISupportInitialize)trackBar2).EndInit();
            ((System.ComponentModel.ISupportInitialize)trackBar1).EndInit();
            ((System.ComponentModel.ISupportInitialize)walkingPaceNumericUpDown).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button button1;
        private Label label16;
        private Label label15;
        private Label label14;
        private Label label13;
        private Label label12;
        private Label label11;
        private Label label10;
        private Label label9;
        private Label label8;
        private Label label7;
        private Label label6;
        private Label label5;
        private Label label4;
        private TextBox destStopTextBox;
        private Label label3;
        private Label label2;
        private Label label1;
        private TrackBar trackBar3;
        private TrackBar trackBar2;
        private TrackBar trackBar1;
        private DateTimePicker departureDatePicker;
        private NumericUpDown walkingPaceNumericUpDown;
        private TextBox srcStopTextBox;
        private DateTimePicker departureTimePicker;
    }
}
