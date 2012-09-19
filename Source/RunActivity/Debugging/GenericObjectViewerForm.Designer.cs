namespace ORTS.Debugging
{
   partial class GenericObjectViewerForm
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
         this.propertyGrid = new System.Windows.Forms.PropertyGrid();
         this.comboBox1 = new System.Windows.Forms.ComboBox();
         this.pauseResume = new System.Windows.Forms.Button();
         this.SuspendLayout();
         // 
         // propertyGrid
         // 
         this.propertyGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                     | System.Windows.Forms.AnchorStyles.Left)
                     | System.Windows.Forms.AnchorStyles.Right)));
         this.propertyGrid.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.propertyGrid.Location = new System.Drawing.Point(0, 58);
         this.propertyGrid.Margin = new System.Windows.Forms.Padding(0);
         this.propertyGrid.Name = "propertyGrid";
         this.propertyGrid.Size = new System.Drawing.Size(292, 208);
         this.propertyGrid.TabIndex = 0;
         // 
         // comboBox1
         // 
         this.comboBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                     | System.Windows.Forms.AnchorStyles.Right)));
         this.comboBox1.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.comboBox1.FormattingEnabled = true;
         this.comboBox1.Location = new System.Drawing.Point(0, -1);
         this.comboBox1.Name = "comboBox1";
         this.comboBox1.Size = new System.Drawing.Size(292, 24);
         this.comboBox1.TabIndex = 1;
         this.comboBox1.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
         // 
         // pauseResume
         // 
         this.pauseResume.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.pauseResume.Location = new System.Drawing.Point(12, 29);
         this.pauseResume.Name = "pauseResume";
         this.pauseResume.Size = new System.Drawing.Size(88, 26);
         this.pauseResume.TabIndex = 2;
         this.pauseResume.Text = "Pause";
         this.pauseResume.UseVisualStyleBackColor = true;
         this.pauseResume.Click += new System.EventHandler(this.pauseResume_Click);
         // 
         // GenericObjectViewerForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(292, 266);
         this.Controls.Add(this.pauseResume);
         this.Controls.Add(this.comboBox1);
         this.Controls.Add(this.propertyGrid);
         this.Name = "GenericObjectViewerForm";
         this.Text = "GenericObjectViewerForm";
         this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.GenericObjectViewerForm_FormClosing);
         this.ResumeLayout(false);

      }

      #endregion

      private System.Windows.Forms.PropertyGrid propertyGrid;
      private System.Windows.Forms.ComboBox comboBox1;
      private System.Windows.Forms.Button pauseResume;
   }
}