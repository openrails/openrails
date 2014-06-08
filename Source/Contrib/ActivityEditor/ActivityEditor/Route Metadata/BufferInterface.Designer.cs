namespace ActivityEditor.Engine
{
    partial class BufferInterface
    {
        /// <summary>
        /// Variable nécessaire au concepteur.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Nettoyage des ressources utilisées.
        /// </summary>
        /// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Code généré par le Concepteur Windows Form

        /// <summary>
        /// Méthode requise pour la prise en charge du concepteur - ne modifiez pas
        /// le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
            this.TCSectionBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.bufferLab = new System.Windows.Forms.TextBox();
            this.BufferLabel = new System.Windows.Forms.Label();
            this.RemButton = new System.Windows.Forms.Button();
            this.BufDirLabel = new System.Windows.Forms.Label();
            this.BufDirCB = new System.Windows.Forms.ComboBox();
            this.OKButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // TCSectionBox
            // 
            this.TCSectionBox.Location = new System.Drawing.Point(127, 76);
            this.TCSectionBox.Name = "TCSectionBox";
            this.TCSectionBox.ReadOnly = true;
            this.TCSectionBox.Size = new System.Drawing.Size(205, 20);
            this.TCSectionBox.TabIndex = 15;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(55, 79);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(69, 13);
            this.label3.TabIndex = 14;
            this.label3.Text = "TC Section : ";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // bufferLab
            // 
            this.bufferLab.Location = new System.Drawing.Point(127, 40);
            this.bufferLab.Name = "bufferLab";
            this.bufferLab.Size = new System.Drawing.Size(205, 20);
            this.bufferLab.TabIndex = 13;
            // 
            // BufferLabel
            // 
            this.BufferLabel.AutoSize = true;
            this.BufferLabel.Location = new System.Drawing.Point(25, 43);
            this.BufferLabel.Name = "BufferLabel";
            this.BufferLabel.Size = new System.Drawing.Size(73, 13);
            this.BufferLabel.TabIndex = 12;
            this.BufferLabel.Text = "Buffer Label : ";
            this.BufferLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // RemButton
            // 
            this.RemButton.Location = new System.Drawing.Point(11, 114);
            this.RemButton.Name = "RemButton";
            this.RemButton.Size = new System.Drawing.Size(75, 23);
            this.RemButton.TabIndex = 11;
            this.RemButton.Text = "Remove";
            this.RemButton.UseVisualStyleBackColor = true;
            this.RemButton.Click += new System.EventHandler(this.RemButton_Click);
            // 
            // BufDirLabel
            // 
            this.BufDirLabel.AutoSize = true;
            this.BufDirLabel.Location = new System.Drawing.Point(32, 9);
            this.BufDirLabel.Name = "BufDirLabel";
            this.BufDirLabel.Size = new System.Drawing.Size(89, 13);
            this.BufDirLabel.TabIndex = 10;
            this.BufDirLabel.Text = "Buffer Direction : ";
            this.BufDirLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // BufDirCB
            // 
            this.BufDirCB.FormattingEnabled = true;
            this.BufDirCB.Location = new System.Drawing.Point(127, 6);
            this.BufDirCB.Name = "BufDirCB";
            this.BufDirCB.Size = new System.Drawing.Size(109, 21);
            this.BufDirCB.TabIndex = 9;
            // 
            // OKButton
            // 
            this.OKButton.Location = new System.Drawing.Point(257, 114);
            this.OKButton.Name = "OKButton";
            this.OKButton.Size = new System.Drawing.Size(75, 23);
            this.OKButton.TabIndex = 8;
            this.OKButton.Text = "OK";
            this.OKButton.UseVisualStyleBackColor = true;
            this.OKButton.Click += new System.EventHandler(this.OKButton_Click);
            // 
            // BufferInterface
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(348, 151);
            this.Controls.Add(this.TCSectionBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.bufferLab);
            this.Controls.Add(this.BufferLabel);
            this.Controls.Add(this.RemButton);
            this.Controls.Add(this.BufDirLabel);
            this.Controls.Add(this.BufDirCB);
            this.Controls.Add(this.OKButton);
            this.Name = "BufferInterface";
            this.Text = "BufferInterface";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox TCSectionBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox bufferLab;
        private System.Windows.Forms.Label BufferLabel;
        private System.Windows.Forms.Button RemButton;
        private System.Windows.Forms.Label BufDirLabel;
        private System.Windows.Forms.ComboBox BufDirCB;
        private System.Windows.Forms.Button OKButton;
    }
}