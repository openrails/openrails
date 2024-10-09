namespace ORTS.ContentManager
{
    partial class ContentManagerGUI
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ContentManagerGUI));
            this.searchBox = new CueTextBox();
            this.searchResults = new System.Windows.Forms.ListBox();
            this.treeViewContent = new System.Windows.Forms.TreeView();
            this.richTextBoxContent = new System.Windows.Forms.RichTextBox();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // searchBox
            // 
            this.searchBox.Cue = "Search for items...";
            this.searchBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.searchBox.Name = "searchBox";
            this.searchBox.ShowCueWhenFocused = true;
            this.searchBox.Size = new System.Drawing.Size(300, 30);
            this.searchBox.TabIndex = 1;
            this.searchBox.Text = "";
            this.searchBox.TextChanged += new System.EventHandler(this.searchBox_TextChanged);
            // 
            // searchResults
            // 
            this.searchResults.Dock = System.Windows.Forms.DockStyle.Top;
            this.searchResults.Name = "searchResults";
            this.searchResults.Size = new System.Drawing.Size(300, 200);
            this.searchResults.TabIndex = 2;
            this.searchResults.Visible = false;
            this.searchResults.DoubleClick += new System.EventHandler(this.searchResults_DoubleClick);
            // 
            // treeViewContent
            // 
            this.treeViewContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeViewContent.Location = new System.Drawing.Point(0, 0);
            this.treeViewContent.Name = "treeViewContent";
            this.treeViewContent.Size = new System.Drawing.Size(300, 514);
            this.treeViewContent.TabIndex = 0;
            this.treeViewContent.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.treeViewContent_BeforeExpand);
            this.treeViewContent.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeViewContent_AfterSelect);
            // 
            // richTextBoxContent
            // 
            this.richTextBoxContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBoxContent.Location = new System.Drawing.Point(0, 0);
            this.richTextBoxContent.Name = "richTextBoxContent";
            this.richTextBoxContent.ReadOnly = true;
            this.richTextBoxContent.Size = new System.Drawing.Size(516, 514);
            this.richTextBoxContent.TabIndex = 1;
            this.richTextBoxContent.Text = "";
            this.richTextBoxContent.WordWrap = false;
            this.richTextBoxContent.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.richTextBoxContent_LinkClicked);
            // 
            // splitContainer
            // 
            this.splitContainer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer.Location = new System.Drawing.Point(12, 12);
            this.splitContainer.Name = "splitContainer";
            // 
            // splitContainer.Panel1
            // 
            this.splitContainer.Panel1.Controls.Add(this.treeViewContent);
            this.splitContainer.Panel1.Controls.Add(this.searchResults);
            this.splitContainer.Panel1.Controls.Add(this.searchBox);
            this.splitContainer.Panel1MinSize = 100;
            // 
            // splitContainer.Panel2
            // 
            this.splitContainer.Panel2.Controls.Add(this.richTextBoxContent);
            this.splitContainer.Panel2MinSize = 100;
            this.splitContainer.Size = new System.Drawing.Size(820, 514);
            this.splitContainer.SplitterDistance = 300;
            this.splitContainer.TabIndex = 2;
            // 
            // ContentManagerGUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(844, 538);
            this.Controls.Add(this.splitContainer);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ContentManagerGUI";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Open Rails Content Manager";
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            this.splitContainer.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private CueTextBox searchBox;
        private System.Windows.Forms.ListBox searchResults;
        private System.Windows.Forms.TreeView treeViewContent;
        private System.Windows.Forms.RichTextBox richTextBoxContent;
        private System.Windows.Forms.SplitContainer splitContainer;
    }
}

