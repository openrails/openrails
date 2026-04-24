namespace Menu
{
    partial class TelemetryForm
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
            this.title1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.title3 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.comboBoxSystem = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.linkLabelServer = new System.Windows.Forms.LinkLabel();
            this.linkLabelPreviewSystem = new System.Windows.Forms.LinkLabel();
            this.label5 = new System.Windows.Forms.Label();
            this.title2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // title1
            // 
            this.title1.AutoSize = true;
            this.title1.Location = new System.Drawing.Point(10, 10);
            this.title1.Margin = new System.Windows.Forms.Padding(3);
            this.title1.Name = "title1";
            this.title1.Size = new System.Drawing.Size(222, 13);
            this.title1.TabIndex = 1;
            this.title1.Text = "Why share anonymous data with Open Rails?";
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.Location = new System.Drawing.Point(10, 29);
            this.label2.Margin = new System.Windows.Forms.Padding(3);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(601, 78);
            this.label2.TabIndex = 2;
            // 
            // title3
            // 
            this.title3.AutoSize = true;
            this.title3.Location = new System.Drawing.Point(10, 239);
            this.title3.Margin = new System.Windows.Forms.Padding(3);
            this.title3.Name = "title3";
            this.title3.Size = new System.Drawing.Size(204, 13);
            this.title3.TabIndex = 3;
            this.title3.Text = "Choose which anonymous data to collect:";
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.button1.Location = new System.Drawing.Point(547, 292);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(64, 20);
            this.button1.TabIndex = 0;
            this.button1.Text = "Close";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // comboBoxSystem
            // 
            this.comboBoxSystem.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxSystem.Location = new System.Drawing.Point(10, 257);
            this.comboBoxSystem.Name = "comboBoxSystem";
            this.comboBoxSystem.Size = new System.Drawing.Size(128, 21);
            this.comboBoxSystem.TabIndex = 4;
            this.comboBoxSystem.SelectedIndexChanged += new System.EventHandler(this.comboBoxSystem_SelectedIndexChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(144, 260);
            this.label4.Margin = new System.Windows.Forms.Padding(3);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(304, 13);
            this.label4.TabIndex = 5;
            this.label4.Text = "Application, runtime, operating system, and hardware properties";
            // 
            // linkLabelServer
            // 
            this.linkLabelServer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.linkLabelServer.AutoSize = true;
            this.linkLabelServer.Location = new System.Drawing.Point(10, 299);
            this.linkLabelServer.Margin = new System.Windows.Forms.Padding(3);
            this.linkLabelServer.Name = "linkLabelServer";
            this.linkLabelServer.Size = new System.Drawing.Size(168, 13);
            this.linkLabelServer.TabIndex = 7;
            this.linkLabelServer.TabStop = true;
            this.linkLabelServer.Text = "Telemetry server and source code";
            this.linkLabelServer.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelServer_LinkClicked);
            // 
            // linkLabelPreviewSystem
            // 
            this.linkLabelPreviewSystem.Location = new System.Drawing.Point(483, 260);
            this.linkLabelPreviewSystem.Name = "linkLabelPreviewSystem";
            this.linkLabelPreviewSystem.Size = new System.Drawing.Size(128, 13);
            this.linkLabelPreviewSystem.TabIndex = 6;
            this.linkLabelPreviewSystem.TabStop = true;
            this.linkLabelPreviewSystem.Text = "Preview";
            this.linkLabelPreviewSystem.TextAlign = System.Drawing.ContentAlignment.TopRight;
            this.linkLabelPreviewSystem.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelPreviewSystem_LinkClicked);
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.Location = new System.Drawing.Point(10, 130);
            this.label5.Margin = new System.Windows.Forms.Padding(3);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(601, 104);
            this.label5.TabIndex = 9;
            // 
            // title2
            // 
            this.title2.AutoSize = true;
            this.title2.Location = new System.Drawing.Point(10, 112);
            this.title2.Margin = new System.Windows.Forms.Padding(3);
            this.title2.Name = "title2";
            this.title2.Size = new System.Drawing.Size(217, 13);
            this.title2.TabIndex = 8;
            this.title2.Text = "How does Open Rails use anonymous data?";
            // 
            // TelemetryForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.button1;
            this.ClientSize = new System.Drawing.Size(621, 322);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.title2);
            this.Controls.Add(this.linkLabelPreviewSystem);
            this.Controls.Add(this.linkLabelServer);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.comboBoxSystem);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.title3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.title1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "TelemetryForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Telemetry Options";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label title1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label title3;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ComboBox comboBoxSystem;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.LinkLabel linkLabelServer;
        private System.Windows.Forms.LinkLabel linkLabelPreviewSystem;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label title2;
    }
}
