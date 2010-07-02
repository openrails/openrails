namespace ORTS
{
    partial class ExploreForm
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
            this.pathListBox = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.consistListBox = new System.Windows.Forms.ListBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.startHourNumeric = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.seasonListBox = new System.Windows.Forms.ListBox();
            this.label5 = new System.Windows.Forms.Label();
            this.weatherListBox = new System.Windows.Forms.ListBox();
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.startHourNumeric)).BeginInit();
            this.SuspendLayout();
            // 
            // pathListBox
            // 
            this.pathListBox.FormattingEnabled = true;
            this.pathListBox.Location = new System.Drawing.Point(12, 25);
            this.pathListBox.Name = "pathListBox";
            this.pathListBox.Size = new System.Drawing.Size(248, 95);
            this.pathListBox.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(29, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Path";
            // 
            // consistListBox
            // 
            this.consistListBox.FormattingEnabled = true;
            this.consistListBox.Location = new System.Drawing.Point(12, 148);
            this.consistListBox.Name = "consistListBox";
            this.consistListBox.Size = new System.Drawing.Size(248, 121);
            this.consistListBox.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 132);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(41, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Consist";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 291);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(55, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Start Time";
            // 
            // startHourNumeric
            // 
            this.startHourNumeric.Location = new System.Drawing.Point(73, 289);
            this.startHourNumeric.Maximum = new decimal(new int[] {
            23,
            0,
            0,
            0});
            this.startHourNumeric.Name = "startHourNumeric";
            this.startHourNumeric.Size = new System.Drawing.Size(45, 20);
            this.startHourNumeric.TabIndex = 5;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 314);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(43, 13);
            this.label4.TabIndex = 6;
            this.label4.Text = "Season";
            // 
            // seasonListBox
            // 
            this.seasonListBox.AllowDrop = true;
            this.seasonListBox.FormattingEnabled = true;
            this.seasonListBox.Items.AddRange(new object[] {
            "Spring",
            "Summer",
            "Autumn",
            "Winter"});
            this.seasonListBox.Location = new System.Drawing.Point(12, 330);
            this.seasonListBox.Name = "seasonListBox";
            this.seasonListBox.Size = new System.Drawing.Size(55, 56);
            this.seasonListBox.TabIndex = 7;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(91, 314);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(48, 13);
            this.label5.TabIndex = 8;
            this.label5.Text = "Weather";
            // 
            // weatherListBox
            // 
            this.weatherListBox.AllowDrop = true;
            this.weatherListBox.FormattingEnabled = true;
            this.weatherListBox.Items.AddRange(new object[] {
            "Clear",
            "Snow",
            "Rain"});
            this.weatherListBox.Location = new System.Drawing.Point(94, 330);
            this.weatherListBox.Name = "weatherListBox";
            this.weatherListBox.Size = new System.Drawing.Size(55, 56);
            this.weatherListBox.TabIndex = 9;
            // 
            // cancelButton
            // 
            this.cancelButton.Location = new System.Drawing.Point(185, 363);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 11;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // okButton
            // 
            this.okButton.Location = new System.Drawing.Point(185, 330);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 12;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // ExploreForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 400);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.weatherListBox);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.seasonListBox);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.startHourNumeric);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.consistListBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pathListBox);
            this.Location = new System.Drawing.Point(200, 200);
            this.Name = "ExploreForm";
            this.Text = "Explore Route Details";
            ((System.ComponentModel.ISupportInitialize)(this.startHourNumeric)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox pathListBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox consistListBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown startHourNumeric;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ListBox seasonListBox;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ListBox weatherListBox;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button okButton;
    }
}