//
// ExteriorWizardPage.cs
//
// Copyright (C) 2002-2002 Steven M. Soloff (mailto:s_soloff@bellsouth.net)
// All rights reserved.
//

using System;
using System.Windows.Forms;

namespace SMS.Windows.Forms
{
    /// <summary>
    /// Base class that is used to represent an exterior page (welcome or
    /// completion page) within a wizard dialog.
    /// </summary>
    public class ExteriorWizardPage : WizardPage
	{
        // ==================================================================
        // Protected Fields
        // ==================================================================

        /// <summary>
        /// The title label.
        /// </summary>
        protected Label m_titleLabel;
        
        /// <summary>
        /// The watermark graphic.
        /// </summary>
        protected PictureBox m_watermarkPicture;


        // ==================================================================
        // Public Constructors
        // ==================================================================
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SMS.Windows.Forms.ExteriorWizardPage">ExteriorWizardPage</see>
        /// class.
        /// </summary>
        public ExteriorWizardPage()
		{
			// This call is required by the Windows Form Designer
			InitializeComponent();
		}


        // ==================================================================
        // Private Methods
        // ==================================================================

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.m_titleLabel = new System.Windows.Forms.Label();
            this.m_watermarkPicture = new System.Windows.Forms.PictureBox();
            this.SuspendLayout();
            // 
            // m_titleLabel
            // 
            this.m_titleLabel.BackColor = System.Drawing.Color.White;
            this.m_titleLabel.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
            this.m_titleLabel.Location = new System.Drawing.Point(170, 13);
            this.m_titleLabel.Name = "m_titleLabel";
            this.m_titleLabel.Size = new System.Drawing.Size(292, 39);
            this.m_titleLabel.TabIndex = 0;
            this.m_titleLabel.Text = "Welcome to the Sample Setup Wizard";
            // 
            // m_watermarkPicture
            // 
            this.m_watermarkPicture.BackColor = System.Drawing.Color.White;
            this.m_watermarkPicture.Name = "m_watermarkPicture";
            this.m_watermarkPicture.Size = new System.Drawing.Size(164, 312);
            this.m_watermarkPicture.TabIndex = 1;
            this.m_watermarkPicture.TabStop = false;
            // 
            // ExteriorWizardPage
            // 
            this.BackColor = System.Drawing.Color.White;
            this.Controls.AddRange(new System.Windows.Forms.Control[] {
                                                                          this.m_watermarkPicture,
                                                                          this.m_titleLabel});
            this.Name = "ExteriorWizardPage";
            this.ResumeLayout(false);

        }
		#endregion

    }
}
