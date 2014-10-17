namespace ActivityEditor.ActionProperties
{
    partial class ControlStartProperties
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
            this.components = new System.ComponentModel.Container();
            this.HornOK = new System.Windows.Forms.Button();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.DelayLabel = new System.Windows.Forms.Label();
            this.DurationToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.PreDelayToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            // 
            // HornOK
            // 
            this.HornOK.Location = new System.Drawing.Point(156, 123);
            this.HornOK.Name = "HornOK";
            this.HornOK.Size = new System.Drawing.Size(91, 30);
            this.HornOK.TabIndex = 1;
            this.HornOK.Text = "OK";
            this.HornOK.UseVisualStyleBackColor = true;
            this.HornOK.Click += new System.EventHandler(this.HornOK_Click);
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(160, 47);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(87, 20);
            this.textBox2.TabIndex = 5;
            this.textBox2.Validated += new System.EventHandler(this.textBox2_TextChanged);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(160, 16);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(87, 20);
            this.textBox1.TabIndex = 6;
            this.textBox1.Validated += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 50);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(47, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "ActionDuration";
            this.DurationToolTip.SetToolTip(this.label1, "ActionDuration:\r\nHow long does the action takes.  With a minimum of 10sec and a maximum" +
        " of 25sec.\r\n");
            // 
            // DelayLabel
            // 
            this.DelayLabel.AutoSize = true;
            this.DelayLabel.Location = new System.Drawing.Point(12, 19);
            this.DelayLabel.Name = "DelayLabel";
            this.DelayLabel.Size = new System.Drawing.Size(57, 13);
            this.DelayLabel.TabIndex = 4;
            this.DelayLabel.Text = "Pre-delay :";
            this.PreDelayToolTip.SetToolTip(this.DelayLabel, "Pre-Delay:\r\nDefine the advance period for this action before the end of caller.\r\n" +
        "Minimum 1sec, maximu 20sec, and not over the ActionDuration\r\n");
            // 
            // ControlStartProperties
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(281, 170);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.DelayLabel);
            this.Controls.Add(this.HornOK);
            this.Name = "ControlStartProperties";
            this.Text = "ControlStartProperties";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button HornOK;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label DelayLabel;
        private System.Windows.Forms.ToolTip DurationToolTip;
        private System.Windows.Forms.ToolTip PreDelayToolTip;
    }
}