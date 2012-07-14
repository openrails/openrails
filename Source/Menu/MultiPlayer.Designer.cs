namespace ORTS {
    partial class MultiPlayer {
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
            this.tPortNo = new System.Windows.Forms.TextBox();
            this.bClose = new System.Windows.Forms.Button();
            this.lUsername = new System.Windows.Forms.Label();
            this.lIP = new System.Windows.Forms.Label();
            this.lPort = new System.Windows.Forms.Label();
            this.rbServer = new System.Windows.Forms.RadioButton();
            this.rbClient = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tUsername = new System.Windows.Forms.TextBox();
            this.tIP = new System.Windows.Forms.TextBox();
            this.epUsername = new System.Windows.Forms.ErrorProvider( this.components );
            this.epIP = new System.Windows.Forms.ErrorProvider( this.components );
            this.epPortNo = new System.Windows.Forms.ErrorProvider( this.components );
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.epUsername)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.epIP)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.epPortNo)).BeginInit();
            this.SuspendLayout();
            // 
            // tPortNo
            // 
            this.tPortNo.Location = new System.Drawing.Point( 77, 173 );
            this.tPortNo.Name = "tPortNo";
            this.tPortNo.Size = new System.Drawing.Size( 100, 20 );
            this.tPortNo.TabIndex = 2;
            this.tPortNo.Validating += new System.ComponentModel.CancelEventHandler( this.tPortNo_Validating );
            // 
            // bClose
            // 
            this.bClose.Location = new System.Drawing.Point( 77, 216 );
            this.bClose.Name = "bClose";
            this.bClose.Size = new System.Drawing.Size( 100, 28 );
            this.bClose.TabIndex = 4;
            this.bClose.Text = "Close";
            this.bClose.UseVisualStyleBackColor = true;
            this.bClose.Click += new System.EventHandler( this.bClose_Click );
            // 
            // lUsername
            // 
            this.lUsername.AutoSize = true;
            this.lUsername.Location = new System.Drawing.Point( 13, 102 );
            this.lUsername.Name = "lUsername";
            this.lUsername.Size = new System.Drawing.Size( 55, 13 );
            this.lUsername.TabIndex = 5;
            this.lUsername.Text = "Username";
            // 
            // lIP
            // 
            this.lIP.AutoSize = true;
            this.lIP.Location = new System.Drawing.Point( 13, 138 );
            this.lIP.Name = "lIP";
            this.lIP.Size = new System.Drawing.Size( 51, 13 );
            this.lIP.TabIndex = 6;
            this.lIP.Text = "Server IP";
            // 
            // lPort
            // 
            this.lPort.AutoSize = true;
            this.lPort.Location = new System.Drawing.Point( 15, 177 );
            this.lPort.Name = "lPort";
            this.lPort.Size = new System.Drawing.Size( 46, 13 );
            this.lPort.TabIndex = 7;
            this.lPort.Text = "Port No.";
            // 
            // rbServer
            // 
            this.rbServer.AutoSize = true;
            this.rbServer.Location = new System.Drawing.Point( 15, 19 );
            this.rbServer.Name = "rbServer";
            this.rbServer.Size = new System.Drawing.Size( 56, 17 );
            this.rbServer.TabIndex = 8;
            this.rbServer.TabStop = true;
            this.rbServer.Text = "Server";
            this.rbServer.UseVisualStyleBackColor = true;
            // 
            // rbClient
            // 
            this.rbClient.AutoSize = true;
            this.rbClient.Location = new System.Drawing.Point( 15, 42 );
            this.rbClient.Name = "rbClient";
            this.rbClient.Size = new System.Drawing.Size( 51, 17 );
            this.rbClient.TabIndex = 9;
            this.rbClient.TabStop = true;
            this.rbClient.Text = "Client";
            this.rbClient.UseVisualStyleBackColor = true;
            this.rbClient.CheckedChanged += new System.EventHandler( this.rbClient_CheckedChanged );
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add( this.rbClient );
            this.groupBox1.Controls.Add( this.rbServer );
            this.groupBox1.Location = new System.Drawing.Point( 16, 12 );
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size( 161, 63 );
            this.groupBox1.TabIndex = 10;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Role";
            // 
            // tUsername
            // 
            this.tUsername.Location = new System.Drawing.Point( 77, 102 );
            this.tUsername.Name = "tUsername";
            this.tUsername.Size = new System.Drawing.Size( 100, 20 );
            this.tUsername.TabIndex = 11;
            this.tUsername.Validating += new System.ComponentModel.CancelEventHandler( this.tUsername_Validating );
            // 
            // tIP
            // 
            this.tIP.Location = new System.Drawing.Point( 77, 138 );
            this.tIP.Name = "tIP";
            this.tIP.Size = new System.Drawing.Size( 100, 20 );
            this.tIP.TabIndex = 12;
            this.tIP.Validating += new System.ComponentModel.CancelEventHandler( this.tIP_Validating );
            // 
            // epUsername
            // 
            this.epUsername.ContainerControl = this;
            // 
            // epIP
            // 
            this.epIP.ContainerControl = this;
            // 
            // epPortNo
            // 
            this.epPortNo.ContainerControl = this;
            // 
            // MultiPlayer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size( 212, 256 );
            this.Controls.Add( this.tIP );
            this.Controls.Add( this.tUsername );
            this.Controls.Add( this.groupBox1 );
            this.Controls.Add( this.lPort );
            this.Controls.Add( this.lIP );
            this.Controls.Add( this.lUsername );
            this.Controls.Add( this.bClose );
            this.Controls.Add( this.tPortNo );
            this.Name = "MultiPlayer";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "MultiPlayer";
            this.groupBox1.ResumeLayout( false );
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.epUsername)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.epIP)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.epPortNo)).EndInit();
            this.ResumeLayout( false );
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tPortNo;
        private System.Windows.Forms.Button bClose;
        private System.Windows.Forms.Label lUsername;
        private System.Windows.Forms.Label lIP;
        private System.Windows.Forms.Label lPort;
        private System.Windows.Forms.RadioButton rbServer;
        private System.Windows.Forms.RadioButton rbClient;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox tUsername;
        private System.Windows.Forms.TextBox tIP;
        private System.Windows.Forms.ErrorProvider epUsername;
        private System.Windows.Forms.ErrorProvider epIP;
        private System.Windows.Forms.ErrorProvider epPortNo;
    }
}