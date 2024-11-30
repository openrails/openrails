namespace ORTS
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
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(TelemetryForm));
            title1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            title3 = new System.Windows.Forms.Label();
            button1 = new System.Windows.Forms.Button();
            comboBoxSystem = new System.Windows.Forms.ComboBox();
            label4 = new System.Windows.Forms.Label();
            linkLabelServer = new System.Windows.Forms.LinkLabel();
            linkLabelPreviewSystem = new System.Windows.Forms.LinkLabel();
            label5 = new System.Windows.Forms.Label();
            title2 = new System.Windows.Forms.Label();
            SuspendLayout();
            // 
            // title1
            // 
            title1.AutoSize = true;
            title1.Location = new System.Drawing.Point(12, 12);
            title1.Margin = new System.Windows.Forms.Padding(3);
            title1.Name = "title1";
            title1.Size = new System.Drawing.Size(244, 15);
            title1.TabIndex = 1;
            title1.Text = "Why share anonymous data with Open Rails?";
            // 
            // label2
            // 
            label2.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            label2.Location = new System.Drawing.Point(12, 33);
            label2.Margin = new System.Windows.Forms.Padding(3);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(701, 90);
            label2.TabIndex = 2;
            label2.Text = resources.GetString("label2.Text");
            // 
            // title3
            // 
            title3.AutoSize = true;
            title3.Location = new System.Drawing.Point(12, 276);
            title3.Margin = new System.Windows.Forms.Padding(3);
            title3.Name = "title3";
            title3.Size = new System.Drawing.Size(229, 15);
            title3.TabIndex = 3;
            title3.Text = "Choose which anonymous data to collect:";
            // 
            // button1
            // 
            button1.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            button1.DialogResult = System.Windows.Forms.DialogResult.OK;
            button1.Location = new System.Drawing.Point(638, 337);
            button1.Name = "button1";
            button1.Size = new System.Drawing.Size(75, 23);
            button1.TabIndex = 0;
            button1.Text = "Close";
            button1.UseVisualStyleBackColor = true;
            // 
            // comboBoxSystem
            // 
            comboBoxSystem.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            comboBoxSystem.Items.AddRange(new object[] { "Undecided (off)", "Off", "On" });
            comboBoxSystem.Location = new System.Drawing.Point(12, 297);
            comboBoxSystem.Name = "comboBoxSystem";
            comboBoxSystem.Size = new System.Drawing.Size(121, 23);
            comboBoxSystem.TabIndex = 4;
            comboBoxSystem.SelectedIndexChanged += comboBoxSystem_SelectedIndexChanged;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(139, 300);
            label4.Margin = new System.Windows.Forms.Padding(3);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(347, 15);
            label4.TabIndex = 5;
            label4.Text = "Application, runtime, operating system, and hardware properties";
            // 
            // linkLabelServer
            // 
            linkLabelServer.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            linkLabelServer.AutoSize = true;
            linkLabelServer.Location = new System.Drawing.Point(12, 345);
            linkLabelServer.Margin = new System.Windows.Forms.Padding(3);
            linkLabelServer.Name = "linkLabelServer";
            linkLabelServer.Size = new System.Drawing.Size(182, 15);
            linkLabelServer.TabIndex = 7;
            linkLabelServer.TabStop = true;
            linkLabelServer.Text = "Telemetry server and source code";
            linkLabelServer.LinkClicked += linkLabelServer_LinkClicked;
            // 
            // linkLabelPreviewSystem
            // 
            linkLabelPreviewSystem.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            linkLabelPreviewSystem.AutoSize = true;
            linkLabelPreviewSystem.Location = new System.Drawing.Point(665, 300);
            linkLabelPreviewSystem.Name = "linkLabelPreviewSystem";
            linkLabelPreviewSystem.Size = new System.Drawing.Size(48, 15);
            linkLabelPreviewSystem.TabIndex = 6;
            linkLabelPreviewSystem.TabStop = true;
            linkLabelPreviewSystem.Text = "Preview";
            linkLabelPreviewSystem.LinkClicked += linkLabelPreviewSystem_LinkClicked;
            // 
            // label5
            // 
            label5.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            label5.Location = new System.Drawing.Point(12, 150);
            label5.Margin = new System.Windows.Forms.Padding(3);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(701, 120);
            label5.TabIndex = 9;
            label5.Text = resources.GetString("label5.Text");
            // 
            // title2
            // 
            title2.AutoSize = true;
            title2.Location = new System.Drawing.Point(12, 129);
            title2.Margin = new System.Windows.Forms.Padding(3);
            title2.Name = "title2";
            title2.Size = new System.Drawing.Size(237, 15);
            title2.TabIndex = 8;
            title2.Text = "How does Open Rails use anonymous data?";
            // 
            // TelemetryForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            CancelButton = button1;
            ClientSize = new System.Drawing.Size(725, 372);
            Controls.Add(label5);
            Controls.Add(title2);
            Controls.Add(linkLabelPreviewSystem);
            Controls.Add(linkLabelServer);
            Controls.Add(label4);
            Controls.Add(comboBoxSystem);
            Controls.Add(button1);
            Controls.Add(title3);
            Controls.Add(label2);
            Controls.Add(title1);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "TelemetryForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Telemetry Options";
            ResumeLayout(false);
            PerformLayout();
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
