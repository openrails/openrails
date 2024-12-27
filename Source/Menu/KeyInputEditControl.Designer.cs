namespace ORTS
{
    partial class KeyInputEditControl
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
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.Dock = System.Windows.Forms.DockStyle.Right;
            this.buttonOK.Location = new System.Drawing.Point(131, 0);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(23, 23);
            this.buttonOK.TabIndex = 3;
            this.buttonOK.Text = "✔";
            this.toolTip1.SetToolTip(this.buttonOK, "Accept changes");
            // 
            // buttonCancel
            // 
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Dock = System.Windows.Forms.DockStyle.Right;
            this.buttonCancel.Location = new System.Drawing.Point(154, 0);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(23, 23);
            this.buttonCancel.TabIndex = 2;
            this.buttonCancel.Text = "✘";
            this.toolTip1.SetToolTip(this.buttonCancel, "Cancel changes");
            // 
            // textBox
            // 
            this.textBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox.Location = new System.Drawing.Point(0, 0);
            this.textBox.Name = "textBox";
            this.textBox.ReadOnly = true;
            this.textBox.Size = new System.Drawing.Size(131, 20);
            this.textBox.TabIndex = 0;
            this.toolTip1.SetToolTip(this.textBox, "Press any key");
            // 
            // buttonDefault
            // 
            this.buttonDefault.BackColor = System.Drawing.SystemColors.ControlLight;
            this.buttonDefault.DialogResult = System.Windows.Forms.DialogResult.Ignore;
            this.buttonDefault.Dock = System.Windows.Forms.DockStyle.Right;
            this.buttonDefault.Location = new System.Drawing.Point(177, 0);
            this.buttonDefault.Name = "buttonDefault";
            this.buttonDefault.Size = new System.Drawing.Size(23, 23);
            this.buttonDefault.TabIndex = 1;
            this.buttonDefault.Text = "↺";
            this.toolTip1.SetToolTip(this.buttonDefault, "Reset to default");
            this.buttonDefault.UseVisualStyleBackColor = false;
            // 
            // KeyInputEditControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(200, 23);
            this.ControlBox = false;
            this.Controls.Add(this.textBox);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonDefault);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "KeyInputEditControl";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "EditKey";
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
