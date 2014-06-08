namespace ActivityEditor.Route_Metadata
{
    partial class TrackSegmentInterface
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
            this.TrackItf_OK = new System.Windows.Forms.Button();
            this.segmentLab = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // TrackItf_OK
            // 
            this.TrackItf_OK.Location = new System.Drawing.Point(590, 20);
            this.TrackItf_OK.Name = "TrackItf_OK";
            this.TrackItf_OK.Size = new System.Drawing.Size(91, 27);
            this.TrackItf_OK.TabIndex = 0;
            this.TrackItf_OK.Text = "OK";
            this.TrackItf_OK.UseVisualStyleBackColor = true;
            this.TrackItf_OK.Click += new System.EventHandler(this.TrackItf_OK_Click);
            // 
            // segmentLab
            // 
            this.segmentLab.Location = new System.Drawing.Point(188, 24);
            this.segmentLab.Name = "segmentLab";
            this.segmentLab.Size = new System.Drawing.Size(236, 20);
            this.segmentLab.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(23, 27);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(87, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Segment Label : ";
            // 
            // TrackSegmentInterface
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(699, 262);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.segmentLab);
            this.Controls.Add(this.TrackItf_OK);
            this.Name = "TrackSegmentInterface";
            this.Text = "TrackSegmentInterface";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button TrackItf_OK;
        private System.Windows.Forms.TextBox segmentLab;
        private System.Windows.Forms.Label label1;
    }
}