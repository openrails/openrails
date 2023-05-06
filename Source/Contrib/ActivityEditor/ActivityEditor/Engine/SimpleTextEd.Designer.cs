using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ActivityEditor.Engine
{
    partial class SimpleTextEd
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
            this.aEditer = new System.Windows.Forms.TextBox();
            this.fileMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.save = new System.Windows.Forms.ToolStripMenuItem();
            this.bar = new System.Windows.Forms.MenuStrip();
            this.bar.SuspendLayout();
            this.SuspendLayout();
            // 
            // aEditer
            // 
            this.aEditer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.aEditer.Location = new System.Drawing.Point(0, 24);
            this.aEditer.Multiline = true;
            this.aEditer.Name = "aEditer";
            this.aEditer.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.aEditer.Size = new System.Drawing.Size(500, 176);
            this.aEditer.TabIndex = 0;
            this.aEditer.WordWrap = false;
            // 
            // fileMenu
            // 
            this.fileMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.save});
            this.fileMenu.Name = "fileMenu";
            this.fileMenu.Size = new System.Drawing.Size(37, 20);
            this.fileMenu.Text = "File";
            // 
            // save
            // 
            this.save.Name = "save";
            this.save.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.save.Size = new System.Drawing.Size(138, 22);
            this.save.Text = "Save";
            this.save.Click += new System.EventHandler(this.Save_Click);
            // 
            // bar
            // 
            this.bar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileMenu});
            this.bar.Location = new System.Drawing.Point(0, 0);
            this.bar.Name = "bar";
            this.bar.Size = new System.Drawing.Size(500, 24);
            this.bar.TabIndex = 0;
            // 
            // SimpleTextEd
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(500, 200);
            this.Controls.Add(this.aEditer);
            this.Controls.Add(this.bar);
            this.MainMenuStrip = this.bar;
            this.Name = "SimpleTextEd";
            this.Text = "Form1";
            this.bar.ResumeLayout(false);
            this.bar.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        public TextBox aEditer;
        private ToolStripMenuItem fileMenu;
        private ToolStripMenuItem save;
        private MenuStrip bar;
    }
}
