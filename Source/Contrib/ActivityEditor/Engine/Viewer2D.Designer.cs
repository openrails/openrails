using System;
using System.Drawing;
using System.Windows.Forms;

namespace ActivityEditor.Engine
{
    class SelectablePictureBox : PictureBox
    {
        Viewer2D parent;
        public SelectablePictureBox(Viewer2D viewer)
        {
            parent = viewer;
            this.SetStyle(ControlStyles.Selectable, true);
            this.IsInputKey(Keys.Up);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (!parent.actParent.askFocus())
                return;
            this.Focus();
            base.OnMouseDown(e);
            this.Select();
        }
        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Up || keyData == Keys.Down || keyData == Keys.Left || keyData == Keys.Right)
            {
                return true;
            }
            else
            {
                return base.IsInputKey(keyData);
            }
        }
    }
    partial class Viewer2D
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Viewer2D));
            this.routeDrawing = new SelectablePictureBox(this);
            this.PathContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.CenterCMItem1 = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.routeDrawing)).BeginInit();
            this.PathContextMenu.SuspendLayout();
            this.SuspendLayout();
            
            // 
            // routeDrawing
            // 
            this.routeDrawing.AccessibleDescription = null;
            this.routeDrawing.AccessibleName = null;
            resources.ApplyResources(this.routeDrawing, "routeDrawing");
            this.routeDrawing.BackgroundImage = null;
            this.routeDrawing.Font = null;
            this.routeDrawing.ImageLocation = null;
            this.routeDrawing.Name = "routeDrawing";
            this.routeDrawing.TabStop = false;
            this.routeDrawing.MouseMove += new System.Windows.Forms.MouseEventHandler(this.routeDrawingMouseMove);
            this.routeDrawing.Click += new System.EventHandler(this.routeDrawing_Click);
            this.routeDrawing.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.routeDrawing_MouseDoubleClick);
            this.routeDrawing.MouseDown += new System.Windows.Forms.MouseEventHandler(this.routeDrawingMouseDown);
            this.routeDrawing.MouseUp += new System.Windows.Forms.MouseEventHandler(this.routeDrawingMouseUp);
            this.routeDrawing.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Viewer2D_KeyDown);
            
            // 
            // PathContextMenu
            // 
            this.PathContextMenu.AccessibleDescription = null;
            this.PathContextMenu.AccessibleName = null;
            resources.ApplyResources(this.PathContextMenu, "PathContextMenu");
            this.PathContextMenu.BackgroundImage = null;
            this.PathContextMenu.Font = null;
            this.PathContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CenterCMItem1});
            this.PathContextMenu.Name = "PathContextMenu";
            // 
            // CenterCMItem1
            // 
            this.CenterCMItem1.AccessibleDescription = null;
            this.CenterCMItem1.AccessibleName = null;
            resources.ApplyResources(this.CenterCMItem1, "CenterCMItem1");
            this.CenterCMItem1.BackgroundImage = null;
            this.CenterCMItem1.Name = "CenterCMItem1";
            this.CenterCMItem1.ShortcutKeyDisplayString = null;
            this.CenterCMItem1.Click += new System.EventHandler(this.CenterView);
            // 
            // Viewer2D
            // 
            this.AccessibleDescription = null;
            this.AccessibleName = null;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = null;
            this.Controls.Add(this.routeDrawing);
            this.Font = null;
            this.Name = "Viewer2D";
            this.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.routeDrawingMouseWheel);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.CloseViewer);
            this.Resize += new System.EventHandler(this.Viewer2D_Resize);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Viewer2D_KeyDown);
            ((System.ComponentModel.ISupportInitialize)(this.routeDrawing)).EndInit();
            this.PathContextMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private SelectablePictureBox routeDrawing;
        private System.Windows.Forms.ContextMenuStrip PathContextMenu;
        private System.Windows.Forms.ToolStripMenuItem CenterCMItem1;
    }
}