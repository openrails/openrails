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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SimpleTextEd));
            this.aEditer = new System.Windows.Forms.TextBox();
            this.fileMenu = new System.Windows.Forms.MenuItem();
            this.save = new System.Windows.Forms.MenuItem();
            this.bar = new System.Windows.Forms.MainMenu(this.components);
            this.SuspendLayout();
            // 
            // aEditer
            // 
            this.aEditer.Location = new System.Drawing.Point(5, 5);
            this.aEditer.Multiline = true;
            this.aEditer.Name = "aEditer";
            this.aEditer.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.aEditer.Size = new System.Drawing.Size(490, 190);
            this.aEditer.TabIndex = 0;
            this.aEditer.WordWrap = false;
            // 
            // fileMenu
            // 
            this.fileMenu.Index = 0;
            this.fileMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.save});
            this.fileMenu.Text = "File";
            // 
            // save
            // 
            this.save.Index = 0;
            this.save.Shortcut = System.Windows.Forms.Shortcut.CtrlS;
            this.save.Text = "Save";
            this.save.Click += new System.EventHandler(this.Save_Click);
            // 
            // bar
            // 
            this.bar.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.fileMenu});
            // 
            // SimpleTextEd
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 162);
            this.Controls.Add(this.aEditer);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Menu = this.bar;
            this.Name = "SimpleTextEd";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        public TextBox aEditer;
        private MenuItem fileMenu;
        private MenuItem save;
        private MainMenu bar;
    }
}