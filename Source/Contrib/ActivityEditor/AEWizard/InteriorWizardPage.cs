//
// InteriorWizardPage.cs
//
// Copyright (C) 2002-2002 Steven M. Soloff (mailto:s_soloff@bellsouth.net)
// All rights reserved.
//

using System;
using System.Windows.Forms;

namespace SMS.Windows.Forms
{
    /// <summary>
    /// Base class that is used to represent an interior page within a
    /// wizard dialog.
    /// </summary>
	public class InteriorWizardPage : WizardPage
	{
        // ==================================================================
        // Protected Fields
        // ==================================================================
	    
        /// <summary>
        /// The title label.
        /// </summary>
        protected Label m_titleLabel;
        
        /// <summary>
        /// The subtitle label.
        /// </summary>
        protected Label m_subtitleLabel;

        /// <summary>
        /// The header panel.
        /// </summary>
        protected Panel m_headerPanel;
        
        /// <summary>
        /// The header graphic.
        /// </summary>
        protected PictureBox m_headerPicture;
        
        /// <summary>
        /// The header/body separator.
        /// </summary>
        protected GroupBox m_headerSeparator;


        // ==================================================================
        // Public Constructors
        // ==================================================================
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SMS.Windows.Forms.InteriorWizardPage">InteriorWizardPage</see>
        /// class.
        /// </summary>
        public InteriorWizardPage()
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
            this.m_headerSeparator = new System.Windows.Forms.GroupBox();
            this.m_headerPanel = new System.Windows.Forms.Panel();
            this.m_titleLabel = new System.Windows.Forms.Label();
            this.m_subtitleLabel = new System.Windows.Forms.Label();
            this.m_headerPicture = new System.Windows.Forms.PictureBox();
            this.SuspendLayout();
            // 
            // m_headerSeparator
            // 
            this.m_headerSeparator.Location = new System.Drawing.Point(0, 58);
            this.m_headerSeparator.Name = "m_headerSeparator";
            this.m_headerSeparator.Size = new System.Drawing.Size(499, 2);
            this.m_headerSeparator.TabIndex = 3;
            this.m_headerSeparator.TabStop = false;
            // 
            // m_headerPanel
            // 
            this.m_headerPanel.BackColor = System.Drawing.Color.White;
            this.m_headerPanel.Name = "m_headerPanel";
            this.m_headerPanel.Size = new System.Drawing.Size(497, 58);
            this.m_headerPanel.TabIndex = 0;
            // 
            // m_titleLabel
            // 
            this.m_titleLabel.BackColor = System.Drawing.Color.White;
            this.m_titleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
            this.m_titleLabel.Location = new System.Drawing.Point(20, 10);
            this.m_titleLabel.Name = "m_titleLabel";
            this.m_titleLabel.Size = new System.Drawing.Size(410, 13);
            this.m_titleLabel.TabIndex = 1;
            this.m_titleLabel.Text = "Sample Header Title";
            // 
            // m_subtitleLabel
            // 
            this.m_subtitleLabel.BackColor = System.Drawing.Color.White;
            this.m_subtitleLabel.Location = new System.Drawing.Point(41, 25);
            this.m_subtitleLabel.Name = "m_subtitleLabel";
            this.m_subtitleLabel.Size = new System.Drawing.Size(389, 26);
            this.m_subtitleLabel.TabIndex = 2;
            this.m_subtitleLabel.Text = "Sample header subtitle will help a user complete a certain task in the Sample wiz" +
                "ard by clarifying the task in some way.";
            // 
            // m_headerPicture
            // 
            this.m_headerPicture.BackColor = System.Drawing.Color.White;
            this.m_headerPicture.Location = new System.Drawing.Point(443, 5);
            this.m_headerPicture.Name = "m_headerPicture";
            this.m_headerPicture.Size = new System.Drawing.Size(49, 49);
            this.m_headerPicture.TabIndex = 4;
            this.m_headerPicture.TabStop = false;
            // 
            // InteriorWizardPage
            // 
            this.Controls.AddRange(new System.Windows.Forms.Control[] {
                                                                          this.m_headerPicture,
                                                                          this.m_subtitleLabel,
                                                                          this.m_titleLabel,
                                                                          this.m_headerSeparator,
                                                                          this.m_headerPanel});
            this.Name = "InteriorWizardPage";
            this.ResumeLayout(false);

        }
		#endregion
    }
}
