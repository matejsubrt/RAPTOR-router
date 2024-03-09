namespace GUI
{
    partial class TransferWindow
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TransferWindow));
            distance = new Label();
            label4 = new Label();
            lineName = new Label();
            pictureBox1 = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // distance
            // 
            distance.BackColor = Color.Azure;
            distance.Font = new Font("Segoe UI", 15.75F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point);
            distance.Location = new Point(504, 21);
            distance.Name = "distance";
            distance.Size = new Size(103, 28);
            distance.TabIndex = 16;
            distance.Text = "270 m";
            distance.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label4
            // 
            label4.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point);
            label4.Location = new Point(504, -1);
            label4.Name = "label4";
            label4.Size = new Size(100, 20);
            label4.TabIndex = 13;
            label4.Text = "Distance:";
            label4.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lineName
            // 
            lineName.Font = new Font("Segoe UI", 26.25F, FontStyle.Bold, GraphicsUnit.Point);
            lineName.ForeColor = Color.DimGray;
            lineName.Location = new Point(66, 2);
            lineName.Margin = new Padding(0);
            lineName.Name = "lineName";
            lineName.Size = new Size(165, 48);
            lineName.TabIndex = 10;
            lineName.Text = "Transfer";
            lineName.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // pictureBox1
            // 
            pictureBox1.BackgroundImage = (Image)resources.GetObject("pictureBox1.BackgroundImage");
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(4, 2);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(48, 48);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.TabIndex = 9;
            pictureBox1.TabStop = false;
            // 
            // TransferWindow
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Silver;
            BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(distance);
            Controls.Add(label4);
            Controls.Add(lineName);
            Controls.Add(pictureBox1);
            Name = "TransferWindow";
            Size = new Size(618, 54);
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
        }

        #endregion
        private Label distance;
        private Label label4;
        private Label lineName;
        private PictureBox pictureBox1;
    }
}
