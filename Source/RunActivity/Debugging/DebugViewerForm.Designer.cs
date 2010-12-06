namespace ORTS.Debugging
{
   partial class DebugViewerForm
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
         this.pictureBox1 = new System.Windows.Forms.PictureBox();
         this.refreshButton = new System.Windows.Forms.Button();
         this.leftButton = new System.Windows.Forms.Button();
         this.rightButton = new System.Windows.Forms.Button();
         this.upButton = new System.Windows.Forms.Button();
         this.downButton = new System.Windows.Forms.Button();
         this.windowSizeUpDown = new System.Windows.Forms.NumericUpDown();
         this.label1 = new System.Windows.Forms.Label();
         this.label2 = new System.Windows.Forms.Label();
         this.showSwitches = new System.Windows.Forms.CheckBox();
         ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
         ((System.ComponentModel.ISupportInitialize)(this.windowSizeUpDown)).BeginInit();
         this.SuspendLayout();
         // 
         // pictureBox1
         // 
         this.pictureBox1.Location = new System.Drawing.Point(12, 120);
         this.pictureBox1.Name = "pictureBox1";
         this.pictureBox1.Size = new System.Drawing.Size(512, 512);
         this.pictureBox1.TabIndex = 0;
         this.pictureBox1.TabStop = false;
         // 
         // refreshButton
         // 
         this.refreshButton.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.refreshButton.Location = new System.Drawing.Point(37, 18);
         this.refreshButton.Name = "refreshButton";
         this.refreshButton.Size = new System.Drawing.Size(75, 23);
         this.refreshButton.TabIndex = 1;
         this.refreshButton.Text = "Refresh";
         this.refreshButton.UseVisualStyleBackColor = true;
         this.refreshButton.Click += new System.EventHandler(this.refreshButton_Click);
         // 
         // leftButton
         // 
         this.leftButton.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.leftButton.Location = new System.Drawing.Point(134, 12);
         this.leftButton.Name = "leftButton";
         this.leftButton.Size = new System.Drawing.Size(35, 35);
         this.leftButton.TabIndex = 2;
         this.leftButton.Text = "L";
         this.leftButton.UseVisualStyleBackColor = true;
         this.leftButton.MouseLeave += new System.EventHandler(this.leftButton_MouseLeave);
         this.leftButton.Click += new System.EventHandler(this.leftButton_Click);
         this.leftButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.leftButton_MouseDown);
         this.leftButton.MouseUp += new System.Windows.Forms.MouseEventHandler(this.leftButton_MouseUp);
         // 
         // rightButton
         // 
         this.rightButton.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.rightButton.Location = new System.Drawing.Point(216, 12);
         this.rightButton.Name = "rightButton";
         this.rightButton.Size = new System.Drawing.Size(35, 35);
         this.rightButton.TabIndex = 3;
         this.rightButton.Text = "R";
         this.rightButton.UseVisualStyleBackColor = true;
         this.rightButton.MouseLeave += new System.EventHandler(this.rightButton_MouseLeave);
         this.rightButton.Click += new System.EventHandler(this.rightButton_Click);
         this.rightButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.rightButton_MouseDown);
         this.rightButton.MouseUp += new System.Windows.Forms.MouseEventHandler(this.rightButton_MouseUp);
         // 
         // upButton
         // 
         this.upButton.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.upButton.Location = new System.Drawing.Point(175, 12);
         this.upButton.Name = "upButton";
         this.upButton.Size = new System.Drawing.Size(35, 35);
         this.upButton.TabIndex = 4;
         this.upButton.Text = "U";
         this.upButton.UseVisualStyleBackColor = true;
         this.upButton.MouseLeave += new System.EventHandler(this.upButton_MouseLeave);
         this.upButton.Click += new System.EventHandler(this.upButton_Click);
         this.upButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.upButton_MouseDown);
         this.upButton.MouseUp += new System.Windows.Forms.MouseEventHandler(this.upButton_MouseUp);
         // 
         // downButton
         // 
         this.downButton.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.downButton.Location = new System.Drawing.Point(257, 12);
         this.downButton.Name = "downButton";
         this.downButton.Size = new System.Drawing.Size(35, 35);
         this.downButton.TabIndex = 5;
         this.downButton.Text = "D";
         this.downButton.UseVisualStyleBackColor = true;
         this.downButton.MouseLeave += new System.EventHandler(this.downButton_MouseLeave);
         this.downButton.Click += new System.EventHandler(this.downButton_Click);
         this.downButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.downButton_MouseDown);
         this.downButton.MouseUp += new System.Windows.Forms.MouseEventHandler(this.downButton_MouseUp);
         // 
         // windowSizeUpDown
         // 
         this.windowSizeUpDown.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.windowSizeUpDown.Increment = new decimal(new int[] {
            50,
            0,
            0,
            0});
         this.windowSizeUpDown.Location = new System.Drawing.Point(318, 24);
         this.windowSizeUpDown.Maximum = new decimal(new int[] {
            50000,
            0,
            0,
            0});
         this.windowSizeUpDown.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
         this.windowSizeUpDown.Name = "windowSizeUpDown";
         this.windowSizeUpDown.Size = new System.Drawing.Size(79, 23);
         this.windowSizeUpDown.TabIndex = 6;
         this.windowSizeUpDown.Value = new decimal(new int[] {
            5000,
            0,
            0,
            0});
         this.windowSizeUpDown.ValueChanged += new System.EventHandler(this.windowSizeUpDown_ValueChanged);
         // 
         // label1
         // 
         this.label1.AutoSize = true;
         this.label1.Location = new System.Drawing.Point(315, 8);
         this.label1.Name = "label1";
         this.label1.Size = new System.Drawing.Size(57, 13);
         this.label1.TabIndex = 7;
         this.label1.Text = "Resolution";
         // 
         // label2
         // 
         this.label2.AutoSize = true;
         this.label2.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.label2.Location = new System.Drawing.Point(403, 31);
         this.label2.Name = "label2";
         this.label2.Size = new System.Drawing.Size(54, 16);
         this.label2.TabIndex = 8;
         this.label2.Text = "metres";
         // 
         // showSwitches
         // 
         this.showSwitches.AutoSize = true;
         this.showSwitches.Checked = true;
         this.showSwitches.CheckState = System.Windows.Forms.CheckState.Checked;
         this.showSwitches.Location = new System.Drawing.Point(318, 63);
         this.showSwitches.Name = "showSwitches";
         this.showSwitches.Size = new System.Drawing.Size(160, 17);
         this.showSwitches.TabIndex = 9;
         this.showSwitches.Text = "Show Switches (Black Dots)";
         this.showSwitches.UseVisualStyleBackColor = true;
         this.showSwitches.CheckedChanged += new System.EventHandler(this.showSwitches_CheckedChanged);
         // 
         // DebugViewerForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.AutoScroll = true;
         this.ClientSize = new System.Drawing.Size(650, 644);
         this.Controls.Add(this.showSwitches);
         this.Controls.Add(this.label2);
         this.Controls.Add(this.label1);
         this.Controls.Add(this.windowSizeUpDown);
         this.Controls.Add(this.downButton);
         this.Controls.Add(this.upButton);
         this.Controls.Add(this.rightButton);
         this.Controls.Add(this.leftButton);
         this.Controls.Add(this.refreshButton);
         this.Controls.Add(this.pictureBox1);
         this.Name = "DebugViewerForm";
         this.Text = "DebugViewerForm";
         ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
         ((System.ComponentModel.ISupportInitialize)(this.windowSizeUpDown)).EndInit();
         this.ResumeLayout(false);
         this.PerformLayout();

      }

      #endregion

      private System.Windows.Forms.PictureBox pictureBox1;
      private System.Windows.Forms.Button refreshButton;
      private System.Windows.Forms.Button leftButton;
      private System.Windows.Forms.Button rightButton;
      private System.Windows.Forms.Button upButton;
      private System.Windows.Forms.Button downButton;
      private System.Windows.Forms.NumericUpDown windowSizeUpDown;
      private System.Windows.Forms.Label label1;
      private System.Windows.Forms.Label label2;
      private System.Windows.Forms.CheckBox showSwitches;
   }
}