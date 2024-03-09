namespace GUI
{
    partial class ResultWindow
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
            label1 = new Label();
            returnToSearchButton = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.Font = new Font("Segoe UI", 26.25F, FontStyle.Bold, GraphicsUnit.Point);
            label1.Location = new Point(71, 20);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(514, 60);
            label1.TabIndex = 0;
            label1.Text = "Best Search Result:";
            // 
            // returnToSearchButton
            // 
            returnToSearchButton.BackColor = Color.ForestGreen;
            returnToSearchButton.Font = new Font("Arial", 14.25F, FontStyle.Bold, GraphicsUnit.Point);
            returnToSearchButton.ForeColor = SystemColors.ButtonHighlight;
            returnToSearchButton.Location = new Point(819, 710);
            returnToSearchButton.Margin = new Padding(4, 5, 4, 5);
            returnToSearchButton.Name = "returnToSearchButton";
            returnToSearchButton.Size = new Size(279, 75);
            returnToSearchButton.TabIndex = 49;
            returnToSearchButton.Text = "Return to search";
            returnToSearchButton.UseVisualStyleBackColor = false;
            returnToSearchButton.Click += returnToSearchButton_Click;
            // 
            // ResultWindow
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(returnToSearchButton);
            Controls.Add(label1);
            Margin = new Padding(4, 5, 4, 5);
            Name = "ResultWindow";
            Size = new Size(1143, 833);
            Enter += ResultWindow_Enter;
            KeyDown += ResultWindow_KeyDown;
            ResumeLayout(false);
        }

        #endregion

        private Label label1;
        private Button returnToSearchButton;
    }
}
