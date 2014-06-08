namespace ActivityEditor.Engine
{
    partial class StationInterface
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(StationInterface));
            this.OKButton = new System.Windows.Forms.Button();
            this.AllowCB = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.RemButton = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.ConnectionLabel = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.TCSectionBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // OKButton
            // 
            this.OKButton.Location = new System.Drawing.Point(258, 117);
            this.OKButton.Name = "OKButton";
            this.OKButton.Size = new System.Drawing.Size(75, 23);
            this.OKButton.TabIndex = 0;
            this.OKButton.Text = "OK";
            this.OKButton.UseVisualStyleBackColor = true;
            this.OKButton.Click += new System.EventHandler(this.OKButton_Click);
            // 
            // AllowCB
            // 
            this.AllowCB.FormattingEnabled = true;
            this.AllowCB.Location = new System.Drawing.Point(128, 9);
            this.AllowCB.Name = "AllowCB";
            this.AllowCB.Size = new System.Drawing.Size(109, 21);
            this.AllowCB.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(115, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Connection Direction : ";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // RemButton
            // 
            this.RemButton.Location = new System.Drawing.Point(12, 117);
            this.RemButton.Name = "RemButton";
            this.RemButton.Size = new System.Drawing.Size(75, 23);
            this.RemButton.TabIndex = 3;
            this.RemButton.Text = "Remove";
            this.RemButton.UseVisualStyleBackColor = true;
            this.RemButton.Click += new System.EventHandler(this.RemButton_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(26, 46);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(99, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Connection Label : ";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // ConnectionLabel
            // 
            this.ConnectionLabel.Location = new System.Drawing.Point(128, 43);
            this.ConnectionLabel.Name = "ConnectionLabel";
            this.ConnectionLabel.Size = new System.Drawing.Size(205, 20);
            this.ConnectionLabel.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(56, 82);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(69, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "TC Section : ";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // TCSectionBox
            // 
            this.TCSectionBox.Location = new System.Drawing.Point(128, 79);
            this.TCSectionBox.Name = "TCSectionBox";
            this.TCSectionBox.ReadOnly = true;
            this.TCSectionBox.Size = new System.Drawing.Size(205, 20);
            this.TCSectionBox.TabIndex = 7;
            // 
            // StationInterface
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(345, 152);
            this.Controls.Add(this.TCSectionBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.ConnectionLabel);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.RemButton);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.AllowCB);
            this.Controls.Add(this.OKButton);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "StationInterface";
            this.Text = "Station Route Interface";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button OKButton;
        private System.Windows.Forms.ComboBox AllowCB;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button RemButton;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox ConnectionLabel;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox TCSectionBox;
    }
}