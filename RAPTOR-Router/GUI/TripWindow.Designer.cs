namespace GUI
{
    partial class TripWindow
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TripWindow));
            pictureBox1 = new PictureBox();
            lineName = new Label();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            fromStop = new Label();
            toStop = new Label();
            stopsNo = new Label();
            pictureBox2 = new PictureBox();
            departureTime = new Label();
            label6 = new Label();
            label1 = new Label();
            arrivalTime = new Label();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).BeginInit();
            SuspendLayout();
            // 
            // pictureBox1
            // 
            pictureBox1.BackgroundImage = (Image)resources.GetObject("pictureBox1.BackgroundImage");
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(6, 33);
            pictureBox1.Margin = new Padding(4, 5, 4, 5);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(86, 100);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // lineName
            // 
            lineName.BackColor = Color.Silver;
            lineName.Font = new Font("Segoe UI", 24F, FontStyle.Bold, GraphicsUnit.Point);
            lineName.ForeColor = Color.White;
            lineName.Location = new Point(104, 45);
            lineName.Margin = new Padding(0);
            lineName.Name = "lineName";
            lineName.Size = new Size(112, 77);
            lineName.TabIndex = 1;
            lineName.Text = "191";
            lineName.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            label2.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point);
            label2.Location = new Point(229, -2);
            label2.Margin = new Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new Size(86, 33);
            label2.TabIndex = 2;
            label2.Text = "From:";
            label2.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // label3
            // 
            label3.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point);
            label3.Location = new Point(500, -2);
            label3.Margin = new Padding(4, 0, 4, 0);
            label3.Name = "label3";
            label3.Size = new Size(86, 33);
            label3.TabIndex = 3;
            label3.Text = "To:";
            label3.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // label4
            // 
            label4.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point);
            label4.Location = new Point(754, -2);
            label4.Margin = new Padding(4, 0, 4, 0);
            label4.Name = "label4";
            label4.Size = new Size(86, 33);
            label4.TabIndex = 4;
            label4.Text = "Stops:";
            label4.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // fromStop
            // 
            fromStop.BackColor = Color.Azure;
            fromStop.Font = new Font("Segoe UI Semibold", 11.25F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point);
            fromStop.Location = new Point(229, 35);
            fromStop.Margin = new Padding(4, 0, 4, 0);
            fromStop.Name = "fromStop";
            fromStop.Size = new Size(226, 67);
            fromStop.TabIndex = 5;
            fromStop.Text = "Správa sociálního zabezpečení";
            fromStop.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // toStop
            // 
            toStop.BackColor = Color.Azure;
            toStop.Font = new Font("Segoe UI Semibold", 11.25F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point);
            toStop.Location = new Point(500, 35);
            toStop.Margin = new Padding(4, 0, 4, 0);
            toStop.Name = "toStop";
            toStop.Size = new Size(226, 67);
            toStop.TabIndex = 6;
            toStop.Text = "Správa sociálního zabezpečení";
            toStop.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // stopsNo
            // 
            stopsNo.BackColor = Color.Azure;
            stopsNo.Font = new Font("Segoe UI", 15.75F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point);
            stopsNo.Location = new Point(754, 35);
            stopsNo.Margin = new Padding(4, 0, 4, 0);
            stopsNo.Name = "stopsNo";
            stopsNo.Size = new Size(90, 67);
            stopsNo.TabIndex = 7;
            stopsNo.Text = "27";
            stopsNo.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // pictureBox2
            // 
            pictureBox2.Image = (Image)resources.GetObject("pictureBox2.Image");
            pictureBox2.Location = new Point(459, 45);
            pictureBox2.Margin = new Padding(4, 5, 4, 5);
            pictureBox2.Name = "pictureBox2";
            pictureBox2.Size = new Size(37, 47);
            pictureBox2.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox2.TabIndex = 8;
            pictureBox2.TabStop = false;
            // 
            // departureTime
            // 
            departureTime.BackColor = Color.Azure;
            departureTime.Font = new Font("Segoe UI Semibold", 11.25F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point);
            departureTime.Location = new Point(353, 112);
            departureTime.Margin = new Padding(4, 0, 4, 0);
            departureTime.Name = "departureTime";
            departureTime.Size = new Size(101, 50);
            departureTime.TabIndex = 9;
            departureTime.Text = "18:07:07";
            departureTime.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // label6
            // 
            label6.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point);
            label6.Location = new Point(229, 110);
            label6.Margin = new Padding(4, 0, 4, 0);
            label6.Name = "label6";
            label6.Size = new Size(116, 50);
            label6.TabIndex = 11;
            label6.Text = "Departure:";
            label6.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // label1
            // 
            label1.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point);
            label1.Location = new Point(500, 112);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(116, 50);
            label1.TabIndex = 13;
            label1.Text = "Arrival:";
            label1.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // arrivalTime
            // 
            arrivalTime.BackColor = Color.Azure;
            arrivalTime.Font = new Font("Segoe UI Semibold", 11.25F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point);
            arrivalTime.Location = new Point(624, 113);
            arrivalTime.Margin = new Padding(4, 0, 4, 0);
            arrivalTime.Name = "arrivalTime";
            arrivalTime.Size = new Size(101, 50);
            arrivalTime.TabIndex = 12;
            arrivalTime.Text = "18:07:07";
            arrivalTime.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // TripWindow
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Gray;
            BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(label1);
            Controls.Add(arrivalTime);
            Controls.Add(label6);
            Controls.Add(departureTime);
            Controls.Add(pictureBox2);
            Controls.Add(stopsNo);
            Controls.Add(toStop);
            Controls.Add(fromStop);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(lineName);
            Controls.Add(pictureBox1);
            Margin = new Padding(4, 5, 4, 5);
            Name = "TripWindow";
            Size = new Size(883, 167);
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private PictureBox pictureBox1;
        private Label lineName;
        private Label label2;
        private Label label3;
        private Label label4;
        private Label fromStop;
        private Label toStop;
        private Label stopsNo;
        private PictureBox pictureBox2;
        private Label departureTime;
        private Label label6;
        private Label label1;
        private Label arrivalTime;
    }
}
