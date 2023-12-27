

namespace Orts.Menu
{
    partial class RDButtonInputControl
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

        #region Component Designer generated code

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
            this.buttonOK.Dock = System.Windows.Forms.DockStyle.Right;
            this.buttonOK.Location = new System.Drawing.Point(131, 0);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(23, 23);
            this.buttonOK.TabIndex = 3;
            this.buttonOK.Text = "✔";
            this.toolTip1.SetToolTip(this.buttonOK, "Accept changes");
            this.buttonOK.Visible = false;
            // 
            // buttonCancel
            // 
            this.buttonCancel.Dock = System.Windows.Forms.DockStyle.Right;
            this.buttonCancel.Location = new System.Drawing.Point(154, 0);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(23, 23);
            this.buttonCancel.TabIndex = 2;
            this.buttonCancel.Text = "✘";
            this.toolTip1.SetToolTip(this.buttonCancel, "Cancel changes");
            this.buttonCancel.Visible = false;
            // 
            // textBox
            // 
            this.textBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox.Location = new System.Drawing.Point(0, 0);
            this.textBox.Name = "textBox";
            this.textBox.ReadOnly = true;
            this.textBox.Size = new System.Drawing.Size(131, 20);
            this.textBox.TabIndex = 0;
            this.toolTip1.SetToolTip(this.textBox, "Press any RailDriver button to Assign. Press Backspace to remove an assignment.");
            this.textBox.Enter += new System.EventHandler(this.TextBox_Enter);
            this.textBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TextBox_KeyPress);
            this.textBox.Leave += new System.EventHandler(this.TextBox_Leave);
            // 
            // buttonDefault
            // 
            this.buttonDefault.BackColor = System.Drawing.SystemColors.ControlLight;
            this.buttonDefault.Dock = System.Windows.Forms.DockStyle.Right;
            this.buttonDefault.Location = new System.Drawing.Point(177, 0);
            this.buttonDefault.Name = "buttonDefault";
            this.buttonDefault.Size = new System.Drawing.Size(23, 23);
            this.buttonDefault.TabIndex = 1;
            this.buttonDefault.Text = "↺";
            this.toolTip1.SetToolTip(this.buttonDefault, "Reset to default");
            this.buttonDefault.UseVisualStyleBackColor = false;
            this.buttonDefault.Visible = false;
            // 
            // RDButtonInputControl
            // 
            this.Controls.Add(this.textBox);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonDefault);
            this.Margin = new System.Windows.Forms.Padding(1);
            this.Name = "RDButtonInputControl";
            this.Size = new System.Drawing.Size(200, 23);
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
