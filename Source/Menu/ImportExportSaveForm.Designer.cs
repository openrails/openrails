namespace ORTS {
    partial class ImportExportSaveForm {
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
            this.bViewSavePacksFolder = new System.Windows.Forms.Button();
            this.bExport = new System.Windows.Forms.Button();
            this.bImportSave = new System.Windows.Forms.Button();
            this.ofdImportSave = new System.Windows.Forms.OpenFileDialog();
            this.cbEmptySavePacks = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxSavePacks = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // bViewSavePacksFolder
            // 
            this.bViewSavePacksFolder.Location = new System.Drawing.Point( 192, 12 );
            this.bViewSavePacksFolder.Name = "bViewSavePacksFolder";
            this.bViewSavePacksFolder.Size = new System.Drawing.Size( 153, 27 );
            this.bViewSavePacksFolder.TabIndex = 1;
            this.bViewSavePacksFolder.Text = "View SavePacks folder";
            this.bViewSavePacksFolder.UseVisualStyleBackColor = true;
            this.bViewSavePacksFolder.Click += new System.EventHandler( this.bViewSavePacksFolder_Click );
            // 
            // bExport
            // 
            this.bExport.Location = new System.Drawing.Point( 21, 45 );
            this.bExport.Name = "bExport";
            this.bExport.Size = new System.Drawing.Size( 133, 27 );
            this.bExport.TabIndex = 0;
            this.bExport.Text = "Export to SavePack";
            this.bExport.UseVisualStyleBackColor = true;
            this.bExport.Click += new System.EventHandler( this.bExport_Click );
            // 
            // bImportSave
            // 
            this.bImportSave.Location = new System.Drawing.Point( 21, 12 );
            this.bImportSave.Name = "bImportSave";
            this.bImportSave.Size = new System.Drawing.Size( 133, 27 );
            this.bImportSave.TabIndex = 0;
            this.bImportSave.Text = "Import from SavePack";
            this.bImportSave.UseVisualStyleBackColor = true;
            this.bImportSave.Click += new System.EventHandler( this.bImportSave_Click_1 );
            // 
            // ofdImportSave
            // 
            this.ofdImportSave.FileName = "openFileDialog1";
            // 
            // cbEmptySavePacks
            // 
            this.cbEmptySavePacks.AutoSize = true;
            this.cbEmptySavePacks.Checked = true;
            this.cbEmptySavePacks.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbEmptySavePacks.Location = new System.Drawing.Point( 192, 52 );
            this.cbEmptySavePacks.Name = "cbEmptySavePacks";
            this.cbEmptySavePacks.Size = new System.Drawing.Size( 15, 14 );
            this.cbEmptySavePacks.TabIndex = 2;
            this.cbEmptySavePacks.UseVisualStyleBackColor = true;
            this.cbEmptySavePacks.CheckedChanged += new System.EventHandler( this.cbEmptySavePacks_CheckedChanged );
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point( 222, 45 );
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size( 123, 26 );
            this.label1.TabIndex = 3;
            this.label1.Text = "Empty SavePacks folder\r\non closing program";
            // 
            // textBoxSavePacks
            // 
            this.textBoxSavePacks.Location = new System.Drawing.Point( 21, 93 );
            this.textBoxSavePacks.Multiline = true;
            this.textBoxSavePacks.Name = "textBoxSavePacks";
            this.textBoxSavePacks.Size = new System.Drawing.Size( 324, 97 );
            this.textBoxSavePacks.TabIndex = 4;
            // 
            // ImportExportSaveForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size( 364, 202 );
            this.Controls.Add( this.textBoxSavePacks );
            this.Controls.Add( this.label1 );
            this.Controls.Add( this.cbEmptySavePacks );
            this.Controls.Add( this.bImportSave );
            this.Controls.Add( this.bViewSavePacksFolder );
            this.Controls.Add( this.bExport );
            this.Name = "ImportExportSaveForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Import / Export SavePacks";
            this.ResumeLayout( false );
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button bViewSavePacksFolder;
        private System.Windows.Forms.Button bExport;
        private System.Windows.Forms.OpenFileDialog ofdImportSave;
        private System.Windows.Forms.Button bImportSave;
        private System.Windows.Forms.CheckBox cbEmptySavePacks;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxSavePacks;
    }
}