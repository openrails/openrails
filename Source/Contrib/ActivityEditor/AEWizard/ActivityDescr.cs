using System;
using System.Windows.Forms;
using LibAE.Formats;

namespace AEWizard
{
	/// <summary>
	/// Represents a single page within a wizard dialog.
	/// </summary>
	public class ActivityDescr : SinglePage
    {
        private Panel panel1;
        private TextBox textBox1;
        private Label label3;
        private TextBox ActivityDescrCB;
        private Label label2;
        private TextBox activityNameCB;
        private Label label1;
        private ComboBox routePathCB;

        public String RoutePath;
        public ActivityInfo activityInfo { get; set; }
       // ==================================================================
        // Public Constructors
        // ==================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="SMS.Windows.Forms.WizardPage">WizardPage</see>
        /// class.
        /// </summary>
        public ActivityDescr()
		{
            // Required for Windows Form Designer support
            InitializeComponent();
            
		}

        public void completePage()
        {
            this.routePathCB.DataSource = activityInfo.routePaths;
        }

        // ==================================================================
        // Protected Properties
        // ==================================================================
        
        
        
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ActivityDescr));
            this.panel1 = new System.Windows.Forms.Panel();
            this.label3 = new System.Windows.Forms.Label();
            this.ActivityDescrCB = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.activityNameCB = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.routePathCB = new System.Windows.Forms.ComboBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.AccessibleDescription = null;
            this.panel1.AccessibleName = null;
            resources.ApplyResources(this.panel1, "panel1");
            this.panel1.BackgroundImage = null;
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.ActivityDescrCB);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.activityNameCB);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.routePathCB);
            this.panel1.Font = null;
            this.panel1.Name = "panel1";
            // 
            // label3
            // 
            this.label3.AccessibleDescription = null;
            this.label3.AccessibleName = null;
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // ActivityDescrCB
            // 
            this.ActivityDescrCB.AccessibleDescription = null;
            this.ActivityDescrCB.AccessibleName = null;
            resources.ApplyResources(this.ActivityDescrCB, "ActivityDescrCB");
            this.ActivityDescrCB.BackgroundImage = null;
            this.ActivityDescrCB.Font = null;
            this.ActivityDescrCB.Name = "ActivityDescrCB";
            // 
            // label2
            // 
            this.label2.AccessibleDescription = null;
            this.label2.AccessibleName = null;
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // activityNameCB
            // 
            this.activityNameCB.AccessibleDescription = null;
            this.activityNameCB.AccessibleName = null;
            resources.ApplyResources(this.activityNameCB, "activityNameCB");
            this.activityNameCB.BackgroundImage = null;
            this.activityNameCB.Font = null;
            this.activityNameCB.Name = "activityNameCB";
            // 
            // label1
            // 
            this.label1.AccessibleDescription = null;
            this.label1.AccessibleName = null;
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // routePathCB
            // 
            this.routePathCB.AccessibleDescription = null;
            this.routePathCB.AccessibleName = null;
            resources.ApplyResources(this.routePathCB, "routePathCB");
            this.routePathCB.BackgroundImage = null;
            this.routePathCB.Font = null;
            this.routePathCB.FormattingEnabled = true;
            this.routePathCB.Name = "routePathCB";
            // 
            // textBox1
            // 
            this.textBox1.AccessibleDescription = null;
            this.textBox1.AccessibleName = null;
            resources.ApplyResources(this.textBox1, "textBox1");
            this.textBox1.BackgroundImage = null;
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.ShortcutsEnabled = false;
            // 
            // ActivityDescr
            // 
            this.AccessibleDescription = null;
            this.AccessibleName = null;
            resources.ApplyResources(this, "$this");
            this.BackgroundImage = null;
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.panel1);
            this.Font = null;
            this.Name = "ActivityDescr";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
		#endregion


        // ==================================================================
        // Protected Internal Methods
        // ==================================================================
        
        /// <summary>
        /// Called when the page is no longer the active page.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the page was successfully deactivated; otherwise
        /// <c>false</c>.
        /// </returns>
        /// <remarks>
        /// Override this method to perform special data validation tasks.
        /// </remarks>
        protected internal override bool OnKillActive()
        {
            // Deactivate if validation successful
            return Validate();
        }

        /// <summary>
        /// Called when the page becomes the active page.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the page was successfully set active; otherwise
        /// <c>false</c>.
        /// </returns>
        /// <remarks>
        /// Override this method to performs tasks when a page is activated.
        /// Your override of this method should call the default version
        /// before any other processing is done.
        /// </remarks>
        protected internal override bool OnSetActive()
        {
            // Activate the page
            return true;
        }
        
        /// <summary>
        /// Called when the user clicks the Back button in a wizard.
        /// </summary>
        /// <returns>
        /// <c>WizardForm.DefaultPage</c> to automatically advance to the
        /// next page; <c>WizardForm.NoPageChange</c> to prevent the page
        /// changing.  To jump to a page other than the next one, return
        /// the <c>Name</c> of the page to be displayed.
        /// </returns>
        /// <remarks>
        /// Override this method to specify some action the user must take
        /// when the Back button is pressed.
        /// </remarks>
        protected internal override string OnWizardBack()
        {
            // Move to the default previous page in the wizard
            if (routePathCB.Text.Length <= 0)
                return WizardForm.NoPageChange;

            return WizardForm.NextPage;
        }
        
        /// <summary>
        /// Called when the user clicks the Finish button in a wizard.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the wizard finishes successfully; otherwise
        /// <c>false</c>.
        /// </returns>
        /// <remarks>
        /// Override this method to specify some action the user must take
        /// when the Finish button is pressed.  Return <c>false</c> to
        /// prevent the wizard from finishing.
        /// </remarks>
        protected internal override bool OnWizardFinish()
        {
            // Finish the wizard
            if (routePathCB.Text.Length <= 0)
                return false;
            return true;
        }
        
        /// <summary>
        /// Called when the user clicks the Next button in a wizard.
        /// </summary>
        /// <returns>
        /// <c>WizardForm.DefaultPage</c> to automatically advance to the
        /// next page; <c>WizardForm.NoPageChange</c> to prevent the page
        /// changing.  To jump to a page other than the next one, return
        /// the <c>Name</c> of the page to be displayed.
        /// </returns>
        /// <remarks>
        /// Override this method to specify some action the user must take
        /// when the Next button is pressed.
        /// </remarks>
        protected internal override string OnWizardNext()
        {
            // Move to the default next page in the wizard
            if (routePathCB.Text.Length <= 0)
                return WizardForm.NoPageChange;
            activityInfo.RoutePath = routePathCB.Text;
            activityInfo.ActivityName = activityNameCB.Text;
            activityInfo.ActivityDescr = ActivityDescrCB.Text;

            return WizardForm.NextPage;
        }


        private void AEW1Button_Click(object sender, EventArgs e)
        {
            //Wizard.SetWizardButtons(WizardButton.Finish);
        }

    }
}
