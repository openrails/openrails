namespace ActivityEditor.ActionProperties
{
    partial class ControlStoppedProperties
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
            this.HornOK = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // HornOK
            // 
            this.HornOK.Location = new System.Drawing.Point(151, 82);
            this.HornOK.Name = "HornOK";
            this.HornOK.Size = new System.Drawing.Size(91, 30);
            this.HornOK.TabIndex = 2;
            this.HornOK.Text = "OK";
            this.HornOK.UseVisualStyleBackColor = true;
            this.HornOK.Click += new System.EventHandler(this.OK_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(25, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(101, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "No Parameter to set";
            // 
            // ControlStoppedProperties
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(255, 134);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.HornOK);
            this.Name = "ControlStoppedProperties";
            this.Text = "ControlStoppedProperties";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button HornOK;
        private System.Windows.Forms.Label label1;
    }
}