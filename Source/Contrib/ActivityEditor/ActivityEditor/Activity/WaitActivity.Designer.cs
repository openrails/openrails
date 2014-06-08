namespace ActivityEditor.Activity
{
    partial class WaitActivity
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
            this.SW_GDescr = new System.Windows.Forms.TextBox();
            this.SW_OK = new System.Windows.Forms.Button();
            this.SW_DescrText = new System.Windows.Forms.Label();
            this.SW_GB_Env = new System.Windows.Forms.GroupBox();
            this.SW_PlaceValue = new System.Windows.Forms.TextBox();
            this.SW_GB_Descr = new System.Windows.Forms.GroupBox();
            this.SW_Name = new System.Windows.Forms.Label();
            this.SW_Descr = new System.Windows.Forms.Label();
            this.SW_GB_Descr.SuspendLayout();
            this.SuspendLayout();
            // 
            // SW_GDescr
            // 
            this.SW_GDescr.Location = new System.Drawing.Point(21, 81);
            this.SW_GDescr.Multiline = true;
            this.SW_GDescr.Name = "SW_GDescr";
            this.SW_GDescr.Size = new System.Drawing.Size(254, 95);
            this.SW_GDescr.TabIndex = 4;
            // 
            // SW_OK
            // 
            this.SW_OK.Location = new System.Drawing.Point(584, 11);
            this.SW_OK.Name = "SW_OK";
            this.SW_OK.Size = new System.Drawing.Size(78, 19);
            this.SW_OK.TabIndex = 8;
            this.SW_OK.Text = "OK";
            this.SW_OK.UseVisualStyleBackColor = true;
            this.SW_OK.Click += new System.EventHandler(this.SW_OK_Click);
            // 
            // SW_DescrText
            // 
            this.SW_DescrText.AutoSize = true;
            this.SW_DescrText.Location = new System.Drawing.Point(6, 59);
            this.SW_DescrText.Name = "SW_DescrText";
            this.SW_DescrText.Size = new System.Drawing.Size(112, 13);
            this.SW_DescrText.TabIndex = 3;
            this.SW_DescrText.Text = "Give your description :";
            // 
            // SW_GB_Env
            // 
            this.SW_GB_Env.Location = new System.Drawing.Point(310, 47);
            this.SW_GB_Env.Name = "SW_GB_Env";
            this.SW_GB_Env.Size = new System.Drawing.Size(365, 255);
            this.SW_GB_Env.TabIndex = 7;
            this.SW_GB_Env.TabStop = false;
            this.SW_GB_Env.Text = "Environment";
            // 
            // SW_PlaceValue
            // 
            this.SW_PlaceValue.Location = new System.Drawing.Point(117, 25);
            this.SW_PlaceValue.Name = "SW_PlaceValue";
            this.SW_PlaceValue.Size = new System.Drawing.Size(159, 20);
            this.SW_PlaceValue.TabIndex = 2;
            // 
            // SW_GB_Descr
            // 
            this.SW_GB_Descr.Controls.Add(this.SW_GDescr);
            this.SW_GB_Descr.Controls.Add(this.SW_DescrText);
            this.SW_GB_Descr.Controls.Add(this.SW_PlaceValue);
            this.SW_GB_Descr.Controls.Add(this.SW_Name);
            this.SW_GB_Descr.Location = new System.Drawing.Point(9, 49);
            this.SW_GB_Descr.Name = "SW_GB_Descr";
            this.SW_GB_Descr.Size = new System.Drawing.Size(282, 254);
            this.SW_GB_Descr.TabIndex = 6;
            this.SW_GB_Descr.TabStop = false;
            this.SW_GB_Descr.Text = "Description";
            // 
            // SW_Name
            // 
            this.SW_Name.AutoSize = true;
            this.SW_Name.Location = new System.Drawing.Point(6, 28);
            this.SW_Name.Name = "SW_Name";
            this.SW_Name.Size = new System.Drawing.Size(74, 13);
            this.SW_Name.TabIndex = 1;
            this.SW_Name.Text = "Place Name : ";
            // 
            // SW_Descr
            // 
            this.SW_Descr.AutoSize = true;
            this.SW_Descr.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Italic | System.Drawing.FontStyle.Underline))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SW_Descr.Location = new System.Drawing.Point(5, 11);
            this.SW_Descr.Name = "SW_Descr";
            this.SW_Descr.Size = new System.Drawing.Size(186, 20);
            this.SW_Descr.TabIndex = 5;
            this.SW_Descr.Text = "WAIT Activity Description";
            // 
            // WaitActivity
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(687, 311);
            this.Controls.Add(this.SW_OK);
            this.Controls.Add(this.SW_GB_Env);
            this.Controls.Add(this.SW_GB_Descr);
            this.Controls.Add(this.SW_Descr);
            this.Name = "WaitActivity";
            this.Text = "WaitActivity";
            this.SW_GB_Descr.ResumeLayout(false);
            this.SW_GB_Descr.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox SW_GDescr;
        private System.Windows.Forms.Button SW_OK;
        private System.Windows.Forms.Label SW_DescrText;
        private System.Windows.Forms.GroupBox SW_GB_Env;
        public System.Windows.Forms.TextBox SW_PlaceValue;
        private System.Windows.Forms.GroupBox SW_GB_Descr;
        private System.Windows.Forms.Label SW_Name;
        private System.Windows.Forms.Label SW_Descr;
    }
}