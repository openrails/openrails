namespace ORTS {
    partial class MultiplayerForm {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose( bool disposing ) {
            if( disposing && (components != null) ) {
                components.Dispose();
            }
            base.Dispose( disposing );
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.labelUser = new System.Windows.Forms.Label();
            this.labelHost = new System.Windows.Forms.Label();
            this.labelPort = new System.Windows.Forms.Label();
            this.textBoxUser = new System.Windows.Forms.TextBox();
            this.textBoxHost = new System.Windows.Forms.TextBox();
            this.epUser = new System.Windows.Forms.ErrorProvider(this.components);
            this.epHost = new System.Windows.Forms.ErrorProvider(this.components);
            this.epPort = new System.Windows.Forms.ErrorProvider(this.components);
            this.buttonClient = new System.Windows.Forms.Button();
            this.buttonServer = new System.Windows.Forms.Button();
            this.numericPort = new System.Windows.Forms.NumericUpDown();
            this.textMPUpdate = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.showAvatar = new System.Windows.Forms.CheckBox();
            this.avatarURL = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.epUser)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.epHost)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.epPort)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericPort)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.CausesValidation = false;
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(229, 170);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 8;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // labelUser
            // 
            this.labelUser.AutoSize = true;
            this.labelUser.Location = new System.Drawing.Point(12, 15);
            this.labelUser.Name = "labelUser";
            this.labelUser.Size = new System.Drawing.Size(61, 13);
            this.labelUser.TabIndex = 0;
            this.labelUser.Text = "User name:";
            // 
            // labelHost
            // 
            this.labelHost.AutoSize = true;
            this.labelHost.Location = new System.Drawing.Point(12, 41);
            this.labelHost.Name = "labelHost";
            this.labelHost.Size = new System.Drawing.Size(20, 13);
            this.labelHost.TabIndex = 2;
            this.labelHost.Text = "IP:";
            // 
            // labelPort
            // 
            this.labelPort.AutoSize = true;
            this.labelPort.Location = new System.Drawing.Point(12, 67);
            this.labelPort.Name = "labelPort";
            this.labelPort.Size = new System.Drawing.Size(29, 13);
            this.labelPort.TabIndex = 4;
            this.labelPort.Text = "Port:";
            // 
            // textBoxUser
            // 
            this.textBoxUser.Location = new System.Drawing.Point(93, 12);
            this.textBoxUser.Name = "textBoxUser";
            this.textBoxUser.Size = new System.Drawing.Size(140, 20);
            this.textBoxUser.TabIndex = 1;
            this.textBoxUser.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxUser_Validating);
            // 
            // textBoxHost
            // 
            this.textBoxHost.Location = new System.Drawing.Point(93, 38);
            this.textBoxHost.Name = "textBoxHost";
            this.textBoxHost.Size = new System.Drawing.Size(140, 20);
            this.textBoxHost.TabIndex = 3;
            this.textBoxHost.Text = "127.0.0.1";
            this.textBoxHost.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxHost_Validating);
            // 
            // epUser
            // 
            this.epUser.ContainerControl = this;
            // 
            // epHost
            // 
            this.epHost.ContainerControl = this;
            // 
            // epPort
            // 
            this.epPort.ContainerControl = this;
            // 
            // buttonClient
            // 
            this.buttonClient.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonClient.DialogResult = System.Windows.Forms.DialogResult.No;
            this.buttonClient.Location = new System.Drawing.Point(148, 170);
            this.buttonClient.Name = "buttonClient";
            this.buttonClient.Size = new System.Drawing.Size(75, 23);
            this.buttonClient.TabIndex = 7;
            this.buttonClient.Text = "Client";
            this.buttonClient.UseVisualStyleBackColor = true;
            this.buttonClient.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // buttonServer
            // 
            this.buttonServer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonServer.DialogResult = System.Windows.Forms.DialogResult.Yes;
            this.buttonServer.Location = new System.Drawing.Point(67, 170);
            this.buttonServer.Name = "buttonServer";
            this.buttonServer.Size = new System.Drawing.Size(75, 23);
            this.buttonServer.TabIndex = 6;
            this.buttonServer.Text = "Server";
            this.buttonServer.UseVisualStyleBackColor = true;
            this.buttonServer.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // numericPort
            // 
            this.numericPort.Location = new System.Drawing.Point(93, 65);
            this.numericPort.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.numericPort.Name = "numericPort";
            this.numericPort.Size = new System.Drawing.Size(75, 20);
            this.numericPort.TabIndex = 5;
            this.numericPort.Value = new decimal(new int[] {
            30000,
            0,
            0,
            0});
            this.numericPort.Validating += new System.ComponentModel.CancelEventHandler(this.numericPort_Validating);
            // 
            // textMPUpdate
            // 
            this.textMPUpdate.Location = new System.Drawing.Point(93, 91);
            this.textMPUpdate.Name = "textMPUpdate";
            this.textMPUpdate.Size = new System.Drawing.Size(24, 20);
            this.textMPUpdate.TabIndex = 10;
            this.textMPUpdate.Text = "10";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 94);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(75, 13);
            this.label1.TabIndex = 9;
            this.label1.Text = "Update Every ";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(123, 94);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(49, 13);
            this.label2.TabIndex = 11;
            this.label2.Text = "Seconds";
            // 
            // showAvatar
            // 
            this.showAvatar.AutoSize = true;
            this.showAvatar.Location = new System.Drawing.Point(224, 94);
            this.showAvatar.Name = "showAvatar";
            this.showAvatar.Size = new System.Drawing.Size(87, 17);
            this.showAvatar.TabIndex = 12;
            this.showAvatar.Text = "Show Avatar";
            this.showAvatar.UseVisualStyleBackColor = true;
            // 
            // avatarURL
            // 
            this.avatarURL.Location = new System.Drawing.Point(93, 121);
            this.avatarURL.Name = "avatarURL";
            this.avatarURL.Size = new System.Drawing.Size(243, 20);
            this.avatarURL.TabIndex = 14;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 124);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(63, 13);
            this.label3.TabIndex = 13;
            this.label3.Text = "Avatar URL";
            // 
            // MultiplayerForm
            // 
            this.AcceptButton = this.buttonClient;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(348, 201);
            this.Controls.Add(this.avatarURL);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.showAvatar);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textMPUpdate);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.numericPort);
            this.Controls.Add(this.buttonServer);
            this.Controls.Add(this.buttonClient);
            this.Controls.Add(this.textBoxHost);
            this.Controls.Add(this.textBoxUser);
            this.Controls.Add(this.labelPort);
            this.Controls.Add(this.labelHost);
            this.Controls.Add(this.labelUser);
            this.Controls.Add(this.buttonCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MultiplayerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Multiplayer";
            ((System.ComponentModel.ISupportInitialize)(this.epUser)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.epHost)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.epPort)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericPort)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Label labelUser;
        private System.Windows.Forms.Label labelHost;
        private System.Windows.Forms.Label labelPort;
        private System.Windows.Forms.TextBox textBoxUser;
        private System.Windows.Forms.TextBox textBoxHost;
        private System.Windows.Forms.ErrorProvider epUser;
        private System.Windows.Forms.ErrorProvider epHost;
        private System.Windows.Forms.ErrorProvider epPort;
        private System.Windows.Forms.Button buttonServer;
        private System.Windows.Forms.Button buttonClient;
        private System.Windows.Forms.NumericUpDown numericPort;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox textMPUpdate;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.CheckBox showAvatar;
		private System.Windows.Forms.TextBox avatarURL;
		private System.Windows.Forms.Label label3;
    }
}
