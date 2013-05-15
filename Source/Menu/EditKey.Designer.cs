namespace ORTS
{
    partial class EditKey
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
            if (keyboardHookId != System.IntPtr.Zero)
            {
                UnhookKeyboard();
                keyboardHookId = System.IntPtr.Zero;
            }

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
            this.components = new System.ComponentModel.Container();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.textBox = new System.Windows.Forms.TextBox();
            this.buttonDefault = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.buttonOK.BackColor = System.Drawing.Color.Chartreuse;
            this.buttonOK.Location = new System.Drawing.Point(144, 1);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(32, 21);
            this.buttonOK.TabIndex = 0;
            this.buttonOK.Text = "OK";
            this.toolTip1.SetToolTip(this.buttonOK, "Accept changes.");
            this.buttonOK.UseVisualStyleBackColor = false;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.buttonCancel.BackColor = System.Drawing.Color.Red;
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(175, 1);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(20, 21);
            this.buttonCancel.TabIndex = 0;
            this.buttonCancel.Text = "X";
            this.toolTip1.SetToolTip(this.buttonCancel, "Cancel");
            this.buttonCancel.UseVisualStyleBackColor = false;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // textBox
            // 
            this.textBox.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.textBox.BackColor = System.Drawing.Color.DarkKhaki;
            this.textBox.Location = new System.Drawing.Point(3, 2);
            this.textBox.Name = "textBox";
            this.textBox.ReadOnly = true;
            this.textBox.Size = new System.Drawing.Size(140, 20);
            this.textBox.TabIndex = 1;
            this.toolTip1.SetToolTip(this.textBox, "Press any key.");
            // 
            // buttonDefault
            // 
            this.buttonDefault.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.buttonDefault.BackColor = System.Drawing.SystemColors.ControlLight;
            this.buttonDefault.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonDefault.Location = new System.Drawing.Point(194, 1);
            this.buttonDefault.Name = "buttonDefault";
            this.buttonDefault.Size = new System.Drawing.Size(20, 21);
            this.buttonDefault.TabIndex = 0;
            this.buttonDefault.Text = "D";
            this.toolTip1.SetToolTip(this.buttonDefault, "Restore default value.");
            this.buttonDefault.UseVisualStyleBackColor = false;
            this.buttonDefault.Click += new System.EventHandler(this.buttonDefault_Click);
            // 
            // EditKey
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(220, 60);
            this.ControlBox = false;
            this.Controls.Add(this.textBox);
            this.Controls.Add(this.buttonDefault);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "EditKey";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "EditKey";
            this.Load += new System.EventHandler(this.EditKey_Load);
            this.Activated += new System.EventHandler(this.EditKey_Activated);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.EditKey_FormClosed);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.TextBox textBox;
        private System.Windows.Forms.Button buttonDefault;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}
