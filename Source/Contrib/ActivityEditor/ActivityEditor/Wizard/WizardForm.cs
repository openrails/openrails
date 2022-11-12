//
// WizardForm.cs
//
// Copyright (C) 2002-2002 Steven M. Soloff (mailto:s_soloff@bellsouth.net)
// All rights reserved.
//

using System;
using System.Collections;
using System.Collections.Generic;

using System.IO;
using System.Drawing;
using System.Windows.Forms;
using LibAE;
using LibAE.Formats;
using Orts.Formats.OR;

namespace AEWizard
{
    /// <summary>
    /// Used to identify the various buttons that may appear within a wizard
    /// dialog.  
    /// </summary>
    [Flags]		
    public enum WizardButton
    {
        /// <summary>
        /// Identifies the <b>Back</b> button.
        /// </summary>
        Back           = 0x00000001,
        
        /// <summary>
        /// Identifies the <b>Next</b> button.
        /// </summary>
        Next           = 0x00000002,
        
        /// <summary>
        /// Identifies the <b>Finish</b> button.
        /// </summary>
        Finish         = 0x00000004,
        
        /// <summary>
        /// Identifies the disabled <b>Finish</b> button.
        /// </summary>
        DisabledFinish = 0x00000008,
    }
    
    /// <summary>
    /// Represents a wizard dialog.
    /// </summary>
    public class WizardForm : Form
	{
        ActivityInfo activityInfo;
        RouteInfo routeInfo;
        // ==================================================================
        // Public Constants
        // ==================================================================

        /// <summary>
        /// Used by a page to indicate to this wizard that the next page
        /// should be activated when either the Back or Next buttons are
        /// pressed.
        /// </summary>
        public const string NextPage = "";

        /// <summary>
        /// Used by a page to indicate to this wizard that the selected page
        /// should remain active when either the Back or Next buttons are
        /// pressed.
        /// </summary>
        public const string NoPageChange = null;
	
	
        // ==================================================================
        // Private Fields
        // ==================================================================
        
        /// <summary>
        /// Array of wizard pages.
        /// </summary>
        private ArrayList m_pages = new ArrayList();
        
        /// <summary>
        /// Index of the selected page; -1 if no page selected.
        /// </summary>
        private int m_selectedIndex = -1;


        // ==================================================================
        // Protected Fields
        // ==================================================================
        
        /// <summary>
        /// The Back button.
        /// </summary>
        protected Button m_backButton;

        /// <summary>
        /// The Next button.
        /// </summary>
        protected Button m_nextButton;

        /// <summary>
        /// The Cancel button.
        /// </summary>
        protected Button m_cancelButton;

        /// <summary>
        /// The Finish button.
        /// </summary>
        protected Button m_finishButton;

        /// <summary>
        /// The separator between the buttons and the content.
        /// </summary>
        protected GroupBox m_separator;

        // ==================================================================
        // Activity Editor data
        // ==================================================================

        ActivityDescr wiz1 = null;
        TrainInfo wiz2 = null;
        SelectRoute wiz3 = null;

        // ==================================================================
        // Public Constructors
        // ==================================================================
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SMS.Windows.Forms.WizardForm">WizardForm</see>
        /// class.
        /// </summary>
        public WizardForm(ActivityInfo activity)
		{
			// Required for Windows Form Designer support
			InitializeComponent();
            activityInfo = activity;
            wiz1 = new ActivityDescr();
            wiz1.activityInfo = activity;
            wiz1.completePage();
            wiz2 = new TrainInfo();
            wiz2.activityInfo = activity;
            wiz2.completePage();
            Controls.AddRange(new Control[] 
            {
                wiz1, wiz2
            });
            // Ensure Finish and Next buttons are positioned similarly
			m_finishButton.Location = m_nextButton.Location;
		}
        public WizardForm(RouteInfo info)
        {
            InitializeComponent();
            routeInfo = info;
            wiz3 = new SelectRoute();
            wiz3.routeInfo = info;
            wiz3.completePage();
            Controls.AddRange(new Control[]
            {
                wiz3
            });
            m_finishButton.Location = m_nextButton.Location;
        }

        // ==================================================================
        // Private Methods
        // ==================================================================
        
        #region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.m_backButton = new System.Windows.Forms.Button();
            this.m_nextButton = new System.Windows.Forms.Button();
            this.m_cancelButton = new System.Windows.Forms.Button();
            this.m_finishButton = new System.Windows.Forms.Button();
            this.m_separator = new System.Windows.Forms.GroupBox();
            this.SuspendLayout();
            // 
            // m_backButton
            // 
            this.m_backButton.Location = new System.Drawing.Point(252, 327);
            this.m_backButton.Name = "m_backButton";
            this.m_backButton.Size = new System.Drawing.Size(75, 23);
            this.m_backButton.TabIndex = 8;
            this.m_backButton.Text = "< &Back";
            this.m_backButton.Click += new System.EventHandler(this.OnClickBack);
            // 
            // m_nextButton
            // 
            this.m_nextButton.Location = new System.Drawing.Point(327, 327);
            this.m_nextButton.Name = "m_nextButton";
            this.m_nextButton.Size = new System.Drawing.Size(75, 23);
            this.m_nextButton.TabIndex = 9;
            this.m_nextButton.Text = "&Next >";
            this.m_nextButton.Click += new System.EventHandler(this.OnClickNext);
            // 
            // m_cancelButton
            // 
            this.m_cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.m_cancelButton.Location = new System.Drawing.Point(412, 327);
            this.m_cancelButton.Name = "m_cancelButton";
            this.m_cancelButton.Size = new System.Drawing.Size(75, 23);
            this.m_cancelButton.TabIndex = 11;
            this.m_cancelButton.Text = "Cancel";
            this.m_cancelButton.Click += new System.EventHandler(this.OnClickCancel);
            // 
            // m_finishButton
            // 
            this.m_finishButton.Location = new System.Drawing.Point(10, 327);
            this.m_finishButton.Name = "m_finishButton";
            this.m_finishButton.Size = new System.Drawing.Size(75, 23);
            this.m_finishButton.TabIndex = 10;
            this.m_finishButton.Text = "&Finish";
            this.m_finishButton.Visible = false;
            this.m_finishButton.Click += new System.EventHandler(this.OnClickFinish);
            // 
            // m_separator
            // 
            this.m_separator.Location = new System.Drawing.Point(0, 313);
            this.m_separator.Name = "m_separator";
            this.m_separator.Size = new System.Drawing.Size(499, 2);
            this.m_separator.TabIndex = 7;
            this.m_separator.TabStop = false;
            // 
            // WizardForm
            // 
            this.AcceptButton = this.m_nextButton;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.CancelButton = this.m_cancelButton;
            this.ClientSize = new System.Drawing.Size(497, 360);
            this.Controls.Add(this.m_backButton);
            this.Controls.Add(this.m_nextButton);
            this.Controls.Add(this.m_cancelButton);
            this.Controls.Add(this.m_finishButton);
            this.Controls.Add(this.m_separator);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "WizardForm";
            this.ShowInTaskbar = false;
            this.ResumeLayout(false);

        }
		#endregion

        /// <summary>
        /// Activates the page at the specified index in the page array.
        /// </summary>
        /// <param name="newIndex">
        /// Index of new page to be selected.
        /// </param>
        private void ActivatePage( int newIndex )
        {
            // Ensure the index is valid
            if( newIndex < 0 || newIndex >= m_pages.Count )
                throw new ArgumentOutOfRangeException();

            // Deactivate the current page if applicable
            SinglePage currentPage = null;
            if( m_selectedIndex != -1 )
            {
                currentPage = (SinglePage)m_pages[m_selectedIndex];
                if( !currentPage.OnKillActive() )
                    return;
            }

            // Activate the new page
            SinglePage newPage = (SinglePage)m_pages[ newIndex ];
            if( !newPage.OnSetActive() )
                return;

            // Update state
            m_selectedIndex = newIndex;
            if( currentPage != null )
                currentPage.Visible = false;
            newPage.Visible = true;
            newPage.Focus();
        }

        /// <summary>
        /// Handles the Click event for the Back button.
        /// </summary>
        private void OnClickBack( object sender, EventArgs e )
        {
            // Ensure a page is currently selected
            if( m_selectedIndex != -1 )
            {
                // Inform selected page that the Back button was clicked
                string pageName = ((SinglePage)m_pages[
                    m_selectedIndex ]).OnWizardBack();
                switch( pageName )
                {
                    // Do nothing
                    case NoPageChange:
                        break;
                        
                    // Activate the next appropriate page
                    case NextPage:
                        if( m_selectedIndex - 1 >= 0 )
                            ActivatePage( m_selectedIndex - 1 );
                        break;

                    // Activate the specified page if it exists
                    default:
                        foreach( SinglePage page in m_pages )
                        {
                            if( page.Name == pageName )
                                ActivatePage( m_pages.IndexOf( page ) );
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Handles the Click event for the Cancel button.
        /// </summary>
        private void OnClickCancel( object sender, EventArgs e )
        {
            // Close wizard
            DialogResult = DialogResult.Cancel;
        }

        /// <summary>
        /// Handles the Click event for the Finish button.
        /// </summary>
        private void OnClickFinish( object sender, EventArgs e )
        {
            // Ensure a page is currently selected
            if( m_selectedIndex != -1 )
            {
                // Inform selected page that the Finish button was clicked
                SinglePage page = (SinglePage)m_pages[ m_selectedIndex ];
                if( page.OnWizardFinish() )
                {
                    // Deactivate page and close wizard
                    if( page.OnKillActive() )
                        DialogResult = DialogResult.OK;
                }
            }
        }

        /// <summary>
        /// Handles the Click event for the Next button.
        /// </summary>
        private void OnClickNext( object sender, EventArgs e )
        {
            // Ensure a page is currently selected
            if( m_selectedIndex != -1 )
            {
                // Inform selected page that the Next button was clicked
                string pageName = ((SinglePage)m_pages[
                    m_selectedIndex ]).OnWizardNext();
                switch( pageName )
                {
                    // Do nothing
                    case NoPageChange:
                        break;

                    // Activate the next appropriate page
                    case NextPage:
                        if( m_selectedIndex + 1 < m_pages.Count )
                            ActivatePage( m_selectedIndex + 1 );
                        break;

                    // Activate the specified page if it exists
                    default:
                        foreach( SinglePage page in m_pages )
                        {
                            if( page.Name == pageName )
                                ActivatePage( m_pages.IndexOf( page ) );
                        }
                        break;
                }
            }
        }


        // ==================================================================
        // Protected Methods
        // ==================================================================
        
        /// <seealso cref="System.Windows.Forms.Control.OnControlAdded">
        /// System.Windows.Forms.Control.OnControlAdded
        /// </seealso>
        protected override void OnControlAdded( ControlEventArgs e )
        {
            // Invoke base class implementation
            base.OnControlAdded( e );
            
            // Set default properties for all WizardPage instances added to
            // this form
            SinglePage page = e.Control as SinglePage;
            if( page != null )
            {
                page.Visible = false;
                page.Location = new Point( 0, 0 );
                page.Size = new Size( Width, m_separator.Location.Y );
                m_pages.Add( page );
                if( m_selectedIndex == -1 )
                    m_selectedIndex = 0;
            }
        }

        /// <seealso cref="System.Windows.Forms.Form.OnLoad">
        /// System.Windows.Forms.Form.OnLoad
        /// </seealso>
        protected override void OnLoad( EventArgs e )
        {
            // Invoke base class implementation
            base.OnLoad( e );
            
            // Activate the first page in the wizard
            if( m_pages.Count > 0 )
                ActivatePage( 0 );
        }


        // ==================================================================
        // Public Methods
        // ==================================================================
        
        /// <summary>
        /// Sets the text in the Finish button.
        /// </summary>
        /// <param name="text">
        /// Text to be displayed on the Finish button.
        /// </param>
        public void SetFinishText( string text )
        {
            // Set the Finish button text
            m_finishButton.Text = text;
        }
        
        /// <summary>
        /// Enables or disables the Back, Next, or Finish buttons in the
        /// wizard.
        /// </summary>
        /// <param name="flags">
        /// A set of flags that customize the function and appearance of the
        /// wizard buttons.  This parameter can be a combination of any
        /// value in the <c>WizardButton</c> enumeration.
        /// </param>
        /// <remarks>
        /// Typically, you should call <c>SetWizardButtons</c> from
        /// <c>WizardPage.OnSetActive</c>.  You can display a Finish or a
        /// Next button at one time, but not both.
        /// </remarks>
        public void SetWizardButtons( WizardButton flags )
        {
            // Enable/disable and show/hide buttons appropriately
            m_backButton.Enabled =
                (flags & WizardButton.Back) == WizardButton.Back;
            m_nextButton.Enabled =
                (flags & WizardButton.Next) == WizardButton.Next;
            m_nextButton.Visible =
                (flags & WizardButton.Finish) == 0 &&
                (flags & WizardButton.DisabledFinish) == 0;
            m_finishButton.Enabled =
                (flags & WizardButton.DisabledFinish) == 0;
            m_finishButton.Visible =
                (flags & WizardButton.Finish) == WizardButton.Finish ||
                (flags & WizardButton.DisabledFinish) == WizardButton.DisabledFinish;
                
            // Set the AcceptButton depending on whether or not the Finish
            // button is visible or not
            AcceptButton = m_finishButton.Visible ? m_finishButton :
                m_nextButton;
        }
    }
}
